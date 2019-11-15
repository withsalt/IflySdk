using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Threading;
using System.Net.Sockets;
using IflySdk.Enum;
using IflySdk.Common;
using IflySdk.Model.Common;
using IflySdk.Model.TTS;
using System.Text.Json;

namespace IflySdk
{
    public class TTSApi
    {
        private readonly List<byte> _result = new List<byte>();

        /// <summary>
        /// 状态
        /// </summary>
        public ServiceStatus Status { get; internal set; } = ServiceStatus.Stopped;

        /// <summary>
        /// 错误
        /// </summary>
        public event EventHandler<ErrorEventArgs> OnError;

        /// <summary>
        /// 动态显示识别结果
        /// </summary>
        public event EventHandler<string> OnMessage;

        private readonly AppSettings _settings = null;
        private readonly CommonParams _common = null;
        private readonly DataParams _data = null;
        private readonly BusinessParams _business = null;

        public TTSApi(AppSettings settings, CommonParams common, DataParams data, BusinessParams business)
        {
            _settings = settings;
            _common = common;
            _data = data;
            _business = business;
        }

        public async Task<ResultModel<byte[]>> Convert(string data)
        {
            try
            {
                Status = ServiceStatus.Running;
                //BuildAuthUrl
                string host = ApiAuthorization.BuildAuthUrl(_settings);
                //Base64 convert string
                if (string.IsNullOrEmpty(data))
                {
                    throw new Exception("Convert data is null.");
                }
                string base64Text = System.Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
                if (base64Text.Length > 8000)
                {
                    throw new Exception("Convert string too long. No more than 2000 chinese characters.");
                }

                using (var ws = new ClientWebSocket())
                {
                    await ws.ConnectAsync(new Uri(host), CancellationToken.None);
                    //接收数据
                    StartReceiving(ws);

                    //Send data
                    _data.text = base64Text;
                    TTSFrameData frame = new TTSFrameData()
                    {
                        common = _common,
                        business = _business,
                        data = _data
                    };
                    //string send = JsonHelper.SerializeObject(frame);
                    await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(frame))), WebSocketMessageType.Text, true, CancellationToken.None);

                    while (Status == ServiceStatus.Running)
                    {
                        await Task.Delay(10);
                    }
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "NormalClosure", CancellationToken.None);
                }
                return new ResultModel<byte[]>()
                {
                    Code = ResultCode.Success,
                    Data = _result == null ? null : _result.ToArray(),
                };
            }
            catch (Exception ex)
            {
                //服务器主动断开连接
                if (ex.InnerException != null && ex.InnerException is SocketException && ((SocketException)ex.InnerException).SocketErrorCode == SocketError.ConnectionReset)
                {
                    return new ResultModel<byte[]>()
                    {
                        Code = ResultCode.Error,
                        Message = "服务器主动断开连接，可能是整个会话是否已经超过了60s、读取数据超时等原因引起的。",
                    };
                }
                else
                {
                    return new ResultModel<byte[]>()
                    {
                        Code = ResultCode.Error,
                        Message = ex.Message,
                    };
                }
            }
        }

        private async void StartReceiving(ClientWebSocket client)
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
                        Status = ServiceStatus.Stopped;
                        return;
                    }
                    //唔，足够大的缓冲区
                    var array = new byte[100000];
                    var receive = await client.ReceiveAsync(new ArraySegment<byte>(array), CancellationToken.None);
                    if (receive.MessageType == WebSocketMessageType.Text)
                    {
                        if (receive.Count <= 0)
                        {
                            continue;
                        }

                        string msg = Encoding.UTF8.GetString(array, 0, receive.Count);
                        TTSResult result = JsonSerializer.Deserialize<TTSResult>(msg, new JsonSerializerOptions()
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        if (result.Code != 0)
                        {
                            throw new Exception($"Result error: {result.Message}");
                        }
                        if (result.Data == null)
                        {
                            //空帧，不用管
                            continue;
                        }
                        byte[] audiaBuffer = System.Convert.FromBase64String(result.Data.Audio);
                        _result.AddRange(audiaBuffer);

                        OnMessage?.Invoke(this, result.Data.Audio);

                        if (result.Data.Status == 2)
                        {
                            Status = ServiceStatus.Stopped;
                        }
                    }
                }
                catch (WebSocketException)
                {
                    Status = ServiceStatus.Stopped;
                    return;
                }
                catch (Exception ex)
                {
                    Status = ServiceStatus.Stopped;
                    if (!ex.Message.ToLower().Contains("unable to read data from the transport connection"))
                    {
                        OnError?.Invoke(this, new ErrorEventArgs()
                        {
                            Code = ResultCode.Error,
                            Message = ex.Message,
                            Exception = ex,
                        });
                    }
                }
            }
        }
    }
}
