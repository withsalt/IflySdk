using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using WebSocketSharp;
using IflySdk.Common;
using IflySdk.Enum;
using IflySdk.Interface;
using IflySdk.Model.Common;
using IflySdk.Model.IAT;

namespace IflySdk
{
    public class ASRApi : IApi
    {
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
                int frameSize = 122 * 8; //每一帧音频的大小,建议每 40ms 发送 122B
                int intervel = 10;
                FrameState status = FrameState.First;
                byte[] buffer;

                string host = BuildAuthUrl();

                using (var ws = new WebSocketClient(host))
                {
                    ws.OnMessage += OnMessageMethod;
                    ws.OnOpen += OnOpenMethod;
                    ws.OnClose += OnCloseMethod;
                    ws.OnError += OnErrorMethod;
                    ws.Connect();

                    for (int i = 0; i < data.Length; i += frameSize)
                    {
                        buffer = SubArray(data, i, frameSize);
                        if (buffer == null)
                        {
                            status = FrameState.Last;  //文件读完
                        }
                        switch (status)
                        {
                            case FrameState.First:
                                FirstFrameData firstFrame = new FirstFrameData();
                                firstFrame.common = _common;
                                firstFrame.business = _business;
                                firstFrame.data = _data;
                                firstFrame.data.status = FrameState.First;
                                firstFrame.data.audio = System.Convert.ToBase64String(buffer);
                                if (ws.IsAlive)
                                {
                                    ws.Send(Encoding.UTF8.GetBytes(JsonHelper.SerializeObject(firstFrame)));
                                    status = FrameState.Continue;
                                }
                                else
                                {
                                    throw new Exception($"Disconnect the connection to the server during identification. Frame state:{status}");
                                }
                                break;
                            case FrameState.Continue:  //中间帧
                                ContinueFrameData continueFrame = new ContinueFrameData();
                                continueFrame.data = _data;
                                continueFrame.data.status = FrameState.Continue;
                                continueFrame.data.audio = System.Convert.ToBase64String(buffer);
                                if (ws.IsAlive)
                                {
                                    ws.Send(Encoding.UTF8.GetBytes(JsonHelper.SerializeObject(continueFrame)));
                                }
                                else
                                {
                                    throw new Exception($"Disconnect the connection to the server during identification. Frame state:{status}");
                                }
                                break;
                            case FrameState.Last:    // 最后一帧音频
                                LastFrameData lastFrame = new LastFrameData();
                                lastFrame.data = _data;
                                lastFrame.data.audio = System.Convert.ToBase64String(buffer);
                                if (ws.IsAlive)
                                {
                                    ws.Send(Encoding.UTF8.GetBytes(JsonHelper.SerializeObject(lastFrame)));
                                }
                                else
                                {
                                    throw new Exception($"Disconnect the connection to the server during identification. Frame state:{status}");
                                }
                                break;
                        }
                        await Task.Delay(intervel); //模拟音频采样延时
                    }
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

        #region Action
        private void OnErrorMethod(object sender, WebSocketSharp.ErrorEventArgs e)
        {
            OnError?.Invoke(this, new Model.Common.ErrorEventArgs()
            {
                Code = ResultCode.Error,
                Message = e.Message,
                Exception = e.Exception
            });
        }

        private void OnCloseMethod(object sender, CloseEventArgs e)
        {

        }

        private void OnOpenMethod(object sender, EventArgs e)
        {

        }

        private void OnMessageMethod(object sender, MessageEventArgs e)
        {
            if (string.IsNullOrEmpty(e.Data))
            {
                return;
            }
            try
            {
                if (_resultStringBuilder != null)
                {
                    _resultStringBuilder.Clear();
                }
                IATResult result = JsonHelper.DeserializeJsonToObject<IATResult>(e.Data);
                if (result.code != 0)
                {
                    throw new Exception($"Result error: {result.message}");
                }
                if (result.data == null
                    || result.data.result == null
                    || result.data.result.ws == null)
                {
                    return;
                }
                foreach (var item in result.data.result.ws)
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
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
        #endregion

        #region private


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

        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="apiSecretIsKey"></param>
        /// <param name="buider"></param>
        /// <returns></returns>
        private string HMACSha256(string apiSecretIsKey, string buider)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(apiSecretIsKey);

            using (HMACSHA256 hMACSHA256 = new HMACSHA256(bytes))
            {
                byte[] date = Encoding.UTF8.GetBytes(buider);
                date = hMACSHA256.ComputeHash(date);
                hMACSHA256.Clear();
                return System.Convert.ToBase64String(date);
            }
        }

        //生成URL
        private string BuildAuthUrl()
        {
            string date = DateTime.UtcNow.ToString("r");
            Uri uri = new Uri(_settings.ASRUrl);
            //build signature string
            string signatureOrigin = $"host: {uri.Host}\ndate: {date}\nGET {uri.LocalPath} HTTP/1.1";
            string signature = HMACSha256(_settings.ApiSecret, signatureOrigin);
            string authorization = $"api_key=\"{_settings.ApiKey}\", algorithm=\"hmac-sha256\", headers=\"host date request-line\", signature=\"{signature}\"";
            //Build url
            StringBuilder urlBuilder = new StringBuilder();
            urlBuilder.Append(_settings.ASRUrl);
            urlBuilder.Append("?");
            urlBuilder.Append("authorization=");
            urlBuilder.Append(System.Convert.ToBase64String(Encoding.UTF8.GetBytes(authorization)));
            urlBuilder.Append("&");
            urlBuilder.Append("date=");
            urlBuilder.Append(HttpUtility.UrlEncode(date).Replace("+", "%20"));  //默认会将空格编码为+号
            urlBuilder.Append("&");
            urlBuilder.Append("host=");
            urlBuilder.Append(uri.Host);
            return urlBuilder.ToString();
        }

        #endregion
    }
}
