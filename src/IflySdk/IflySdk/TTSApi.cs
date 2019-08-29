using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using IflySdk.Common;
using IflySdk.Enum;
using IflySdk.Interface;
using IflySdk.Model.Common;
using IflySdk.Model.TTS;

namespace IflySdk
{
    public class TTSApi : IApi
    {
        private readonly AppSettings _settings = null;
        private readonly DataParams _data = null;

        /// <summary>
        /// 错误
        /// </summary>
        public event EventHandler<Model.Common.ErrorEventArgs> OnError;

        public TTSApi(AppSettings appSetting, DataParams data)
        {
            _settings = appSetting;
            _data = data;
        }

        public async Task<ResultModel<MemoryStream>> Convert(string input)
        {
            try
            {
                if (string.IsNullOrEmpty(input))
                {
                    throw new Exception("Input string is null.");
                }
                MemoryStream stream = await MainMethod(input);
                if (stream == null)
                {
                    return new ResultModel<MemoryStream>()
                    {
                        Code = ResultCode.Error,
                        Message = "Convert failed. Result stream is null."
                    };
                }
                else
                {
                    return new ResultModel<MemoryStream>()
                    {
                        Code = ResultCode.Success,
                        Message = "success",
                        Data = stream
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultModel<MemoryStream>()
                {
                    Code = ResultCode.Error,
                    Message = ex.Message
                };
            }
        }


        public async Task<ResultModel<string>> ConvertAndSave(string input)
        {
            try
            {
                if (string.IsNullOrEmpty(input))
                {
                    throw new Exception("Input string is null.");
                }
                MemoryStream stream = await MainMethod(input);
                if (stream == null)
                {
                    return new ResultModel<string>()
                    {
                        Code = ResultCode.Error,
                        Message = "Convert failed. Result stream is null."
                    };
                }
                File.WriteAllBytes(_data.save_path, await StreamToByte(stream));
                string path;
                if (_data.save_path.Split('\\').Length > 1 || _data.save_path.Split('/').Length > 1)
                {
                    path = _data.save_path;
                }
                else
                {
                    path = Environment.CurrentDirectory + "\\" + _data.save_path;

                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        path.Replace('\\', '/');
                    }
                    else
                    {
                        path.Replace('/', '\\');
                    }
                }
                if (File.Exists(path))
                {
                    return new ResultModel<string>()
                    {
                        Code = ResultCode.Success,
                        Message = "success",
                        Data = path,
                    };
                }
                else
                {
                    return new ResultModel<string>()
                    {
                        Code = ResultCode.Error,
                        Message = "Save data to file failed."
                    };
                }
            }
            catch (Exception ex)
            {
                return new ResultModel<string>()
                {
                    Code = ResultCode.Error,
                    Message = ex.Message
                };
            }
        }

        #region private
        private string Md5(string s)
        {
            using (MD5 md5 = new MD5CryptoServiceProvider())
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(s);
                bytes = md5.ComputeHash(bytes);
                md5.Clear();
                string ret = "";
                for (int i = 0; i < bytes.Length; i++)
                {
                    ret += System.Convert.ToString(bytes[i], 16).PadLeft(2, '0');
                }
                return ret.PadLeft(32, '0');
            }
        }

        private async Task<byte[]> StreamToByte(MemoryStream memoryStream)
        {
            byte[] buffer = new byte[memoryStream.Length];
            memoryStream.Seek(0, SeekOrigin.Begin);
            int count = await memoryStream.ReadAsync(buffer, 0, buffer.Length);
            return buffer;
        }

        /// <summary>
        /// 检测证书
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="errors"></param>
        /// <returns></returns>
        private bool CheckCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true;
        }

        private async Task<MemoryStream> MainMethod(string input)
        {
            MemoryStream outstream = null;
            try
            {
                // 对要合成语音的文字先用utf-8然后进行URL加密
                byte[] textData = Encoding.UTF8.GetBytes(input);
                input = HttpUtility.UrlEncode(textData);

                string param = JsonHelper.SerializeObject(_data);
                // 获取十位的时间戳
                TimeSpan ts = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, 0);
                string curTime = System.Convert.ToInt64(ts.TotalSeconds).ToString();
                // 对参数先utf-8然后用base64编码
                byte[] paramData = Encoding.UTF8.GetBytes(param);
                string paraBase64 = System.Convert.ToBase64String(paramData);
                // 形成签名
                string checkSum = Md5(_settings.ApiKey + curTime + paraBase64);

                // 组装http请求头
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckCertificate);
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(_settings.TTSUrl);
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.AllowAutoRedirect = true;
                request.Proxy = null;
                request.Headers.Add("X-Param", paraBase64);
                request.Headers.Add("X-CurTime", curTime);
                request.Headers.Add("X-Appid", _settings.AppID);
                request.Headers.Add("X-CheckSum", checkSum);

                //.Net Core必须添加，不然会报异常
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                Stream requestStream = await request.GetRequestStreamAsync();
                StreamWriter streamWriter = new StreamWriter(requestStream, Encoding.GetEncoding("gb2312"));
                streamWriter.Write(string.Format("text={0}", input));
                streamWriter.Close();

                string htmlStr = string.Empty;
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                using (Stream responseStream = response.GetResponseStream())
                {
                    using (StreamReader reader = new StreamReader(responseStream, Encoding.GetEncoding("UTF-8")))
                    {
                        string header_type = response.Headers["Content-Type"];
                        if (header_type.ToLower() == "audio/mpeg")
                        {
                            Stream st = response.GetResponseStream();
                            outstream = new MemoryStream();
                            const int bufferLen = 4096;
                            byte[] buffer = new byte[bufferLen];
                            int count = 0;
                            while ((count = st.Read(buffer, 0, bufferLen)) > 0)
                            {
                                outstream.Write(buffer, 0, count);
                            }
                        }
                        else if (header_type.ToLower() == "text/plain")
                        {
                            htmlStr = reader.ReadToEnd();
                            TTSResult result = JsonHelper.DeserializeJsonToObject<TTSResult>(htmlStr);
                            throw new Exception($"{result.Code}|{result.Sid}|{result.Desc}");
                        }
                        else
                        {
                            htmlStr = reader.ReadToEnd();
                            throw new Exception(htmlStr);
                        }
                    }
                    responseStream.Close();
                }
                return outstream;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion
    }
}
