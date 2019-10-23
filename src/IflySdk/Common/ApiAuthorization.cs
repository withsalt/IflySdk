using System;
using System.Security.Cryptography;
using System.Text;
using System.Web;

using IflySdk.Common.Utils;
using IflySdk.Model.Common;

namespace IflySdk.Common
{
    public class ApiAuthorization
    {
        /// <summary>
        /// 加密
        /// </summary>
        /// <param name="apiSecretIsKey"></param>
        /// <param name="buider"></param>
        /// <returns></returns>
        public static string HMACSha256(string apiSecretIsKey, string buider)
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
        public static string BuildAuthUrl(AppSettings _settings)
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
            urlBuilder.Append(Base64.Base64Encode(authorization));
            urlBuilder.Append("&");
            urlBuilder.Append("date=");
            urlBuilder.Append(HttpUtility.UrlEncode(date).Replace("+", "%20"));  //默认会将空格编码为+号
            urlBuilder.Append("&");
            urlBuilder.Append("host=");
            urlBuilder.Append(uri.Host);
            return urlBuilder.ToString();
        }
    }
}
