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

namespace IflySdk
{
    public class ASRApi : IApi
    {
        private bool _isEnd = false;

        readonly StringBuilder _resultStringBuilder = new StringBuilder();

        /// <summary>
        /// 错误
        /// </summary>
        public event EventHandler<Model.Common.ErrorEventArgs> OnError;

        /// <summary>
        /// 动态显示识别结果
        /// </summary>
        public event EventHandler<string> OnMessage;

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

        public async Task<ResultModel<string>> Convert(byte[] data)
        {
            try
            {
                int frameSize = 1280, intervel = 10;
                FrameState status = FrameState.First;
                string host = ApiAuthorization.BuildAuthUrl(_settings);

                using (var ws = new ClientWebSocket())
                {
                    await ws.ConnectAsync(new Uri(host), CancellationToken.None);
                    StartReceiving(ws);
                    //开始发送数据
                    for (int i = 0; i < data.Length; i += frameSize)
                    {
                        byte[] buffer = SubArray(data, i, frameSize);
                        if (buffer == null || data.Length - i < frameSize)
                        {
                            status = FrameState.Last;  //文件读完
                        }
                        switch (status)
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
                                await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonHelper.SerializeObject(firstFrame)))
                                    , WebSocketMessageType.Text
                                    , true
                                    , CancellationToken.None);
                                status = FrameState.Continue;
                                break;
                            case FrameState.Continue:  //中间帧
                                ContinueFrameData continueFrame = new ContinueFrameData
                                {
                                    data = _data
                                };
                                continueFrame.data.status = FrameState.Continue;
                                continueFrame.data.audio = System.Convert.ToBase64String(buffer);
                                await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonHelper.SerializeObject(continueFrame)))
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
                                await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonHelper.SerializeObject(lastFrame)))
                                    , WebSocketMessageType.Text
                                    , true
                                    , CancellationToken.None);
                                break;
                        }
                        await Task.Delay(intervel);
                    }

                    while (!_isEnd)
                    {
                        await Task.Delay(10);
                    }
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "NormalClosure", CancellationToken.None);
                }
                return new ResultModel<string>()
                {
                    Code = ResultCode.Success,
                    Data = _resultStringBuilder == null ? "" : _resultStringBuilder.ToString(),
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

        #region private

        private async void StartReceiving(ClientWebSocket client)
        {
            while (true)
            {
                try
                {
                    if (_resultStringBuilder != null)
                    {
                        _resultStringBuilder.Clear();
                    }

                    if (client.CloseStatus == WebSocketCloseStatus.EndpointUnavailable ||
                        client.CloseStatus == WebSocketCloseStatus.InternalServerError ||
                        client.CloseStatus == WebSocketCloseStatus.EndpointUnavailable)
                    {
                        _isEnd = true;
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
                        IATResult result = JsonHelper.DeserializeJsonToObject<IATResult>(msg);
                        if (result.Code != 0)
                        {
                            throw new Exception($"Result error: {result.Message}");
                        }
                        if (result.Data == null
                            || result.Data.result == null
                            || result.Data.result.ws == null)
                        {
                            _isEnd = true;
                            return;
                        }
                        foreach (var item in result.Data.result.ws)
                        {
                            foreach (var child in item.cw)
                            {
                                if (string.IsNullOrEmpty(child.w))
                                {
                                    continue;
                                }
                                _resultStringBuilder.Append(child.w);
                            }
                        }
                        OnMessage?.Invoke(this, _resultStringBuilder.ToString());
                        //最后一帧，结束
                        if (result.Data.status == 2)
                        {
                            _isEnd = true;
                        }
                    }
                }
                catch (WebSocketException)
                {
                    _isEnd = true;
                    return;
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(this, new ErrorEventArgs()
                    {
                        Code = ResultCode.Error,
                        Message = ex.Message,
                        Exception = ex,
                    });
                    _isEnd = true;
                }
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
