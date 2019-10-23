using IflySdk.Common;
using IflySdk.Model.Common;
using IflySdk.Model.TTS;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Threading;
using IflySdk.Enum;
using IflySdk.Common.Utils;

namespace IflySdk
{
    public class TTSApi
    {
        private bool _isEnd = false;

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

        public TTSApi(AppSettings settings, CommonParams common, DataParams data, BusinessParams business)
        {
            _settings = settings;
            _common = common;
            _data = data;
            _business = business;
        }

        public async Task<ResultModel<string>> Convert(string data)
        {
            try
            {
                //BuildAuthUrl
                string host = ApiAuthorization.BuildAuthUrl(_settings);
                //Base64 convert string
                if (string.IsNullOrEmpty(data))
                {
                    throw new Exception("Convert data is null.");
                }
                string base64Text = Base64.Base64Encode(data);
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
                    await ws.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(JsonHelper.SerializeObject(frame))), WebSocketMessageType.Text, true, CancellationToken.None);

                    while (!_isEnd)
                    {
                        await Task.Delay(10);
                    }
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "NormalClosure", CancellationToken.None);
                }
                return new ResultModel<string>()
                {
                    Code = ResultCode.Success,
                    //Data = _resultStringBuilder == null ? "" : _resultStringBuilder.ToString(),
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

        private async void StartReceiving(ClientWebSocket client)
        {
            while (true)
            {
                try
                {
                    if (client.CloseStatus == WebSocketCloseStatus.EndpointUnavailable ||
                        client.CloseStatus == WebSocketCloseStatus.InternalServerError ||
                        client.CloseStatus == WebSocketCloseStatus.EndpointUnavailable)
                    {
                        _isEnd = true;
                        return;
                    }

                    var array = new byte[409600];
                    var receive = await client.ReceiveAsync(new ArraySegment<byte>(array), CancellationToken.None);
                    if (receive.MessageType == WebSocketMessageType.Text)
                    {
                        if (receive.Count <= 0)
                        {
                            continue;
                        }

                        string msg = Encoding.UTF8.GetString(array, 0, receive.Count);
                        Console.WriteLine(msg.Length);
                        TTSResult result = JsonHelper.DeserializeJsonToObject<TTSResult>(msg);
                        if (result.Code != 0)
                        {
                            throw new Exception($"Result error: {result.Message}");
                        }
                        if (result.Data == null)
                        {
                            //空帧，不用管
                            continue;
                        }
                        OnMessage?.Invoke(this, result.Data.Audio);

                        if(result.Data.Status == 2)
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
    }
}
