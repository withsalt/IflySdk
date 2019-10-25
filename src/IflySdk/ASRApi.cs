using System;
using System.Text;
using System.Threading.Tasks;
using IflySdk.Common;
using IflySdk.Enum;
using IflySdk.Interface;
using IflySdk.Model.Common;
using IflySdk.Model.IAT;
using System.Net.WebSockets;
using System.Threading;
using IflySdk.Common.Utils;
using System.Collections.Generic;
using IflySdk.Model.IAT.ResultNode;
using System.Linq;
using System.Diagnostics;
using System.Net.Sockets;

namespace IflySdk
{
    public class ASRApi : IApi
    {
        private const int _frameSize = 1280;
        private const int _intervel = 20;
        private FrameState _status = FrameState.First;
        private string _host;
        private ClientWebSocket _ws;
        private readonly RestBuffer _rest = new RestBuffer(_frameSize);
        private readonly Queue<CacheBuffer> _cache = new Queue<CacheBuffer>();
        private readonly List<ResultWPGSInfo> _result = new List<ResultWPGSInfo>();
        private readonly static object _cacheLocker = new object();
        private Task _receiveTask = null;

        /// <summary>
        /// 错误
        /// </summary>
        public event EventHandler<Model.Common.ErrorEventArgs> OnError;

        /// <summary>
        /// 动态显示识别结果
        /// </summary>
        public event EventHandler<string> OnMessage;

        /// <summary>
        /// 状态
        /// </summary>
        public ServiceStatus Status { get; internal set; } = ServiceStatus.Stopped;

        private readonly AppSettings _settings = null;
        private readonly CommonParams _common = null;
        private readonly DataParams _data = null;
        private readonly BusinessParams _business = null;

        public ASRApi(AppSettings settings, CommonParams common, DataParams data, BusinessParams business)
        {
            _settings = settings;
            _common = common;
            _data = data;
            _business = business;
        }

        /// <summary>
        /// 语音转写一个完整的音频文件
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<ResultModel<string>> ConvertAudio(byte[] data)
        {
            try
            {
                using (_ws = new ClientWebSocket())
                {
                    Status =  ServiceStatus.Running;
                    _host = ApiAuthorization.BuildAuthUrl(_settings);
                    await _ws.ConnectAsync(new Uri(_host), CancellationToken.None);
                    _receiveTask = StartReceiving(_ws);
                    //开始发送数据
                    for (int i = 0; i < data.Length; i += _frameSize)
                    {
                        byte[] buffer = SubArray(data, i, _frameSize);
                        if (buffer == null || data.Length - i < _frameSize)
                        {
                            _status = FrameState.Last;  //文件读完
                        }
                        switch (_status)
                        {
                            case FrameState.First:
                                FirstFrameData firstFrame = new FirstFrameData
                                {
                                    common = _common,
                                    business = _business,
                                    data = _data
                                };
                                firstFrame.data.status = FrameState.First;
                                firstFrame.data.audio = System.Convert.ToBase64String(buffer);
                                await _ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonHelper.SerializeObject(firstFrame)))
                                    , WebSocketMessageType.Text
                                    , true
                                    , CancellationToken.None);
                                _status = FrameState.Continue;
                                break;
                            case FrameState.Continue:  //中间帧
                                ContinueFrameData continueFrame = new ContinueFrameData
                                {
                                    data = _data
                                };
                                continueFrame.data.status = FrameState.Continue;
                                continueFrame.data.audio = System.Convert.ToBase64String(buffer);
                                await _ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonHelper.SerializeObject(continueFrame)))
                                    , WebSocketMessageType.Text
                                    , true
                                    , CancellationToken.None);
                                break;
                            case FrameState.Last:    // 最后一帧音频
                                LastFrameData lastFrame = new LastFrameData
                                {
                                    data = _data
                                };
                                lastFrame.data.status = FrameState.Last;
                                lastFrame.data.audio = System.Convert.ToBase64String(buffer);
                                await _ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonHelper.SerializeObject(lastFrame)))
                                    , WebSocketMessageType.Text
                                    , true
                                    , CancellationToken.None);
                                break;
                        }
                        await Task.Delay(_intervel);
                    }

                    while (_receiveTask.Status != TaskStatus.RanToCompletion)
                    {
                        await Task.Delay(10);
                    }
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "NormalClosure", CancellationToken.None);
                }

                StringBuilder result = new StringBuilder();
                foreach (var item in _result)
                {
                    result.Append(item.data);
                }
                ResetState();
                return new ResultModel<string>()
                {
                    Code = ResultCode.Success,
                    Data = result.ToString(),
                };
            }
            catch (Exception ex)
            {
                return new ResultModel<string>()
                {
                    Code = ResultCode.Error,
                    Message = ex.Message,
                };
            }
        }

        /// <summary>
        /// 分片转写语音
        /// </summary>
        /// <param name="data"></param>
        public void Convert(byte[] data, bool isEnd = false)
        {
            if (Status == ServiceStatus.Stopped)
            {
                Status = ServiceStatus.Running;
                Task.Run(() => StartConvert());
            }
            if(Status == ServiceStatus.Running)
            {
                lock (_cacheLocker)
                {
                    if (isEnd)
                    {
                        Status = ServiceStatus.Stopping;
                    }
                    _cache.Enqueue(new CacheBuffer()
                    {
                        Data = data,
                        IsEnd = isEnd
                    });
                }
            }
        }

        /// <summary>
        /// 退出
        /// </summary>
        /// <returns></returns>
        public bool Stop()
        {
            try
            {
                if (Status != ServiceStatus.Stopped)
                {
                    Convert(null, true);
                    while (Status != ServiceStatus.Stopped)
                    {
                        Thread.Sleep(10);
                    }
                }
                return true;
            }
            catch(Exception ex)
            {
                OnError?.Invoke(this, new ErrorEventArgs()
                {
                    Code = ResultCode.Warning,
                    Message = ex.Message,
                    Exception = ex,
                });
                return false;
            }
        }

        #region private

        /// <summary>
        /// 分片转写
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private async void StartConvert()
        {
            try
            {
                Stopwatch stopwatch = new Stopwatch();
                bool isStart = false;
                long lastDataTimespan = 0;
                int connectOutTime = 60;
                int sendDataOutTime = 8;

                while (Status != ServiceStatus.Stopped)
                {
                    CacheBuffer data = null;

                    lock (_cacheLocker)
                    {
                        if (_cache.Count > 0)
                        {
                            data = _cache.Dequeue();
                            lastDataTimespan = stopwatch.ElapsedMilliseconds;
                        }
                    }

                    if (data == null)
                    {
                        if ((stopwatch.ElapsedMilliseconds / 1000 > connectOutTime) || ((stopwatch.ElapsedMilliseconds - lastDataTimespan) / 1000 > sendDataOutTime))
                        {
                            data = new CacheBuffer(new byte[1] { 0 }, true);
                        }
                        else
                        {
                            continue;
                        }
                    }

                    if (!isStart)
                    {
                        stopwatch.Start();
                        isStart = true;
                    }

                    ResultModel<string> fragmentResult;
                    if (data.IsEnd || (stopwatch.ElapsedMilliseconds / 1000 > connectOutTime) || ((stopwatch.ElapsedMilliseconds - lastDataTimespan) / 1000 > sendDataOutTime))
                    {
                        data.IsEnd = true;
                        if (data.Data == null)
                            data.Data = new byte[1] { 0 };
                        fragmentResult = await DoFragmentAsr(data.Data, FrameState.Last);
                    }
                    else
                    {
                        if (data.Data == null)
                            continue;
                        fragmentResult = await DoFragmentAsr(data.Data);
                    }
                    if (fragmentResult.Code == ResultCode.Disconnect)
                    {
                        OnError?.Invoke(this, new ErrorEventArgs()
                        {
                            Code = ResultCode.Warning,
                            Message = fragmentResult.Message,
                            Exception = new Exception(fragmentResult.Message),
                        });
                        break;
                    }
                    else if (fragmentResult.Code != ResultCode.Success)
                    {
                        string message = $"Recognition data failed. {fragmentResult.Message}";

                        OnError?.Invoke(this, new ErrorEventArgs()
                        {
                            Code = ResultCode.Warning,
                            Message = message,
                            Exception = new Exception(message),
                        });
                    }
                    if (data.IsEnd)
                    {
                        break;
                    }
                }
                stopwatch.Stop();
            }
            catch (Exception ex)
            {
                OnError?.Invoke(this, new ErrorEventArgs()
                {
                    Code = ResultCode.Warning,
                    Message = ex.Message,
                    Exception = new Exception(ex.Message),
                });
            }
            finally
            {
                ResetState();
            }
        }

        private async Task<ResultModel<string>> DoFragmentAsr(byte[] data, FrameState state = FrameState.First)
        {
            try
            {
                if (_ws == null && _status == FrameState.First)
                {
                    _ws = new ClientWebSocket();
                    _status = FrameState.First;
                    _host = ApiAuthorization.BuildAuthUrl(_settings);

                    await _ws.ConnectAsync(new Uri(_host), CancellationToken.None);
                    if (_ws.State != WebSocketState.Open)
                    {
                        throw new Exception("Connect to xfyun api server failed.");
                    }
                    _receiveTask = StartReceiving(_ws);
                }

                //开始发送数据
                for (int i = 0; i < data.Length; i += _frameSize)
                {
                    byte[] buffer = null;
                    if (_rest.Length == 0)  //没有上次分片的数据
                    {
                        if (data.Length - i < _frameSize)  //最后一帧不满一个完整的识别帧，那么加入缓存，下个分片的时候继续使用
                        {
                            if (state != FrameState.Last)
                            {
                                int length = data.Length - i;
                                Array.Copy(data, i, _rest.Cache, 0, length);
                                _rest.Length = length;
                            }
                            else
                            {
                                buffer = SubArray(data, i, _frameSize);
                                _status = FrameState.Last;
                            }
                        }
                        else
                        {
                            buffer = SubArray(data, i, _frameSize);
                            if (state == FrameState.Last && data.Length - i == _frameSize)
                            {
                                _status = FrameState.Last;
                                if (buffer == null)
                                {
                                    buffer = new byte[1] { 0 };
                                }
                            }
                        }
                    }
                    else  //有上次分片的数据
                    {
                        if (data.Length + _rest.Length <= _frameSize)
                        {
                            buffer = new byte[_rest.Length + data.Length];
                            Array.Copy(_rest.Cache, 0, buffer, 0, _rest.Length);
                            //最后分片加上缓存不满一个帧大小的情况
                            Array.Copy(data, i, buffer, _rest.Length, data.Length);
                            _status = FrameState.Last;
                            i = data.Length - _frameSize;
                        }
                        else
                        {
                            buffer = new byte[_frameSize];
                            Array.Copy(_rest.Cache, 0, buffer, 0, _rest.Length);
                            Array.Copy(data, i, buffer, _rest.Length, _frameSize - _rest.Length);
                            i -= _rest.Length;
                        }
                        _rest.Clear();  //清空
                    }

                    if (_rest.Length != 0)
                    {
                        break;
                    }

                    switch (_status)
                    {
                        case FrameState.First:
                            FirstFrameData firstFrame = new FirstFrameData
                            {
                                common = _common,
                                business = _business,
                                data = _data
                            };
                            firstFrame.data.status = FrameState.First;
                            firstFrame.data.audio = System.Convert.ToBase64String(buffer);
                            await _ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonHelper.SerializeObject(firstFrame)))
                                , WebSocketMessageType.Text
                                , true
                                , CancellationToken.None);
                            _status = FrameState.Continue;
                            break;
                        case FrameState.Continue:  //中间帧
                            ContinueFrameData continueFrame = new ContinueFrameData
                            {
                                data = _data
                            };
                            continueFrame.data.status = FrameState.Continue;
                            continueFrame.data.audio = System.Convert.ToBase64String(buffer);
                            await _ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonHelper.SerializeObject(continueFrame)))
                                , WebSocketMessageType.Text
                                , true
                                , CancellationToken.None);
                            break;
                        case FrameState.Last:    // 最后一帧音频
                            LastFrameData lastFrame = new LastFrameData
                            {
                                data = _data
                            };
                            lastFrame.data.status = FrameState.Last;
                            lastFrame.data.audio = System.Convert.ToBase64String(buffer);
                            await _ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonHelper.SerializeObject(lastFrame)))
                                , WebSocketMessageType.Text
                                , true
                                , CancellationToken.None);
                            break;
                    }
                    await Task.Delay(_intervel);
                }

                if (state == FrameState.Last)
                {
                    while (_receiveTask.Status != TaskStatus.RanToCompletion)
                    {
                        await Task.Delay(10);
                    }
                    await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "NormalClosure", CancellationToken.None);
                }
                return new ResultModel<string>()
                {
                    Code = ResultCode.Success,
                    Data = null,
                };
            }
            catch (Exception ex)
            {
                //服务器主动断开连接
                if (ex.InnerException != null && ex.InnerException is SocketException && ((SocketException)ex.InnerException).SocketErrorCode == SocketError.ConnectionReset)
                {
                    try
                    {
                        await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "NormalClosure", CancellationToken.None);
                    }
                    catch { }
                    while (_receiveTask.Status != TaskStatus.RanToCompletion)
                    {
                        await Task.Delay(10);
                    }
                    return new ResultModel<string>()
                    {
                        Code = ResultCode.Disconnect,
                        Message = "服务器主动断开连接，可能是整个会话是否已经超过了60s、读取数据超时、静默检测超时等原因引起的。",
                    };
                }
                else
                {
                    return new ResultModel<string>()
                    {
                        Code = ResultCode.Error,
                        Message = ex.Message,
                    };
                }
            }
        }

        private async Task StartReceiving(ClientWebSocket client)
        {
            if (_result != null)
            {
                _result.Clear();
            }
            while (true)
            {
                try
                {
                    if (client.CloseStatus == WebSocketCloseStatus.EndpointUnavailable ||
                        client.CloseStatus == WebSocketCloseStatus.InternalServerError ||
                        client.CloseStatus == WebSocketCloseStatus.EndpointUnavailable)
                    {
                        return;
                    }

                    var array = new byte[4096];
                    var receive = await client.ReceiveAsync(new ArraySegment<byte>(array), CancellationToken.None);
                    if (receive.MessageType == WebSocketMessageType.Text)
                    {
                        if (receive.Count <= 0)
                        {
                            continue;
                        }

                        string msg = Encoding.UTF8.GetString(array, 0, receive.Count);
                        ASRResult result = JsonHelper.DeserializeJsonToObject<ASRResult>(msg);
                        if (result.Code != 0)
                        {
                            throw new Exception($"Result error({result.Code}): {result.Message}");
                        }
                        if (result.Data == null
                            || result.Data.result == null
                            || result.Data.result.ws == null)
                        {
                            return;
                        }
                        //分析数据
                        StringBuilder itemStringBuilder = new StringBuilder();
                        foreach (var item in result.Data.result.ws)
                        {
                            foreach (var child in item.cw)
                            {
                                if (string.IsNullOrEmpty(child.w))
                                {
                                    continue;
                                }
                                itemStringBuilder.Append(child.w);
                            }
                        }
                        if (result.Data.result.pgs == "apd")
                        {
                            _result.Add(new ResultWPGSInfo()
                            {
                                sn = result.Data.result.sn,
                                data = itemStringBuilder.ToString()
                            });
                        }
                        else if (result.Data.result.pgs == "rpl")
                        {
                            if (result.Data.result.rg == null || result.Data.result.rg.Count != 2)
                            {
                                continue;
                            }
                            int first = result.Data.result.rg[0];
                            int end = result.Data.result.rg[1];
                            try
                            {
                                ResultWPGSInfo item = _result.Where(p => p.sn >= first && p.sn <= end).SingleOrDefault();
                                if (item == null)
                                {
                                    continue;
                                }
                                else
                                {
                                    item.sn = result.Data.result.sn;
                                    item.data = itemStringBuilder.ToString();
                                }
                            }
                            catch
                            {
                                continue;
                            }
                        }

                        StringBuilder totalStringBuilder = new StringBuilder();
                        foreach (var item in _result)
                        {
                            totalStringBuilder.Append(item.data);
                        }

                        OnMessage?.Invoke(this, totalStringBuilder.ToString());
                        //最后一帧，结束
                        if (result.Data.status == 2)
                        {
                            return;
                        }
                    }
                }
                catch (WebSocketException)
                {
                    return;
                }
                catch (Exception ex)
                {
                    //服务器主动断开连接
                    if (ex.InnerException != null && ex.InnerException is SocketException && ((SocketException)ex.InnerException).SocketErrorCode == SocketError.ConnectionReset)
                    {
                        return;
                    }
                    OnError?.Invoke(this, new ErrorEventArgs()
                    {
                        Code = ResultCode.Error,
                        Message = ex.Message,
                        Exception = ex,
                    });
                    return;
                }
            }
        }

        private void ResetState()
        {
            _status = FrameState.First;
            _host = null;
            _ws = null;
            _receiveTask = null;
            _rest.Clear();
            _result.Clear();
            Status = ServiceStatus.Stopped;

            lock (_cacheLocker)
            {
                _cache.Clear();
            }
        }

        /// <summary>
        /// 从此实例检索子数组
        /// </summary>
        /// <param name="source">要检索的数组</param>
        /// <param name="startIndex">起始索引号</param>
        /// <param name="length">检索最大长度</param>
        /// <returns>与此实例中在 startIndex 处开头、长度为 length 的子数组等效的一个数组</returns>
        private byte[] SubArray(byte[] source, int startIndex, int length)
        {
            if (startIndex < 0 || startIndex > source.Length || length < 0)
            {
                return null;
            }
            byte[] Destination;
            if (startIndex + length <= source.Length)
            {
                Destination = new byte[length];
                Array.Copy(source, startIndex, Destination, 0, length);
            }
            else
            {
                Destination = new byte[(source.Length - startIndex)];
                Array.Copy(source, startIndex, Destination, 0, source.Length - startIndex);
            }
            return Destination;
        }

        #endregion
    }
}
