using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace IflySdk.Common
{
    public class HttpHelper
    {
        public enum ContentType
        {
            html,
            json,
            xhtml,
            xml,
            all
        }

        private const int ConnectionLimit = 100;
        //编码
        private Encoding _encoding = Encoding.UTF8;
        //浏览器类型
        private string[] _useragents = new string[]{
            "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.90 Safari/537.36",
            "Mozilla/4.0 (compatible; MSIE 7.0; Windows NT 6.1; WOW64; Trident/7.0; SLCC2; .NET CLR 2.0.50727; .NET CLR 3.5.30729; .NET CLR 3.0.30729; Media Center PC 6.0)",
            "Mozilla/5.0 (Windows NT 6.1; rv:36.0) Gecko/2010101 Firefox/36.0",
            "Mozilla/5.0 (Windows NT 6.1; WOW64; rv:31.0) Gecko/20130401 Firefox/31.0"
        };

        private string _useragent = "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/42.0.2311.90 Safari/537.36";
        //接受类型
        private string _accept = "text/html, application/json, application/xhtml+xml, application/xml, */*";
        //超时时间
        private int _timeout = 30 * 1000;
        //类型
        private string _contenttype = "application/x-www-form-urlencoded";
        //cookies
        private string _cookies = "";
        //cookies
        private CookieCollection _cookiecollection;
        //custom heads
        private Dictionary<string, string> _headers = new Dictionary<string, string>();

        public HttpHelper()
        {
            _headers.Clear();
            //随机一个useragent
            _useragent = _useragents[new Random().Next(0, _useragents.Length)];
            //解决性能问题?
            ServicePointManager.DefaultConnectionLimit = ConnectionLimit;
        }

        #region Set
        public void InitCookie()
        {
            _cookies = "";
            _cookiecollection = null;
            _headers.Clear();
        }

        /// <summary>
        /// 设置当前编码
        /// </summary>
        /// <param name="en"></param>
        public void SetEncoding(Encoding en)
        {
            _encoding = en;
        }

        /// <summary>
        /// 设置UserAgent
        /// </summary>
        /// <param name="ua"></param>
        public void SetUserAgent(string ua)
        {
            _useragent = ua;
        }

        public void RandUserAgent()
        {
            _useragent = _useragents[new Random().Next(0, _useragents.Length)];
        }

        public void SetCookiesString(string c)
        {
            _cookies = c;
        }

        /// <summary>
        /// 设置超时时间
        /// </summary>
        /// <param name="sec"></param>
        public void SetTimeOut(int msec)
        {
            _timeout = msec;
        }

        public void SetContentType(ContentType type)
        {
            _contenttype = "application/x-www-form-urlencoded";
            switch (type)
            {
                case ContentType.all:
                    _contenttype = "*/*";
                    break;
                case ContentType.html:
                    _contenttype = "text/html";
                    break;
                case ContentType.json:
                    _contenttype = "application/json";
                    break;
                case ContentType.xhtml:
                    _contenttype = "application/xhtml+xml";
                    break;
                case ContentType.xml:
                    _contenttype = "application/xml";
                    break;
            }
        }

        public void SetAccept(ContentType type)
        {
            _accept = "text/html, application/json, application/xhtml+xml, application/xml, */*";
            switch (type)
            {
                case ContentType.all:
                    _accept = "*/*";
                    break;
                case ContentType.html:
                    _accept = "text/html";
                    break;
                case ContentType.json:
                    _accept = "application/json";
                    break;
                case ContentType.xhtml:
                    _accept = "application/xhtml+xml";
                    break;
                case ContentType.xml:
                    _accept = "application/xml";
                    break;
            }
        }

        /// <summary>
        /// 添加自定义头
        /// </summary>
        /// <param name="key"></param>
        /// <param name="ctx"></param>
        public void AddHeader(string key, string ctx)
        {
            //_headers.Add(key,ctx);
            _headers[key] = ctx;
        }

        /// <summary>
        /// 清空自定义头
        /// </summary>
        public void ClearHeader()
        {
            _headers.Clear();
        }

        #endregion

        /// <summary>
        /// 获取HTTP返回的内容
        /// </summary>
        /// <param name="response"></param>
        /// <returns></returns>
        private string GetStringFromResponse(HttpWebResponse response)
        {
            string html = "";
            try
            {
                Stream stream = response.GetResponseStream();
                StreamReader sr = new StreamReader(stream, _encoding);
                html = sr.ReadToEnd();

                sr.Close();
                stream.Close();
            }
            catch (Exception e)
            {
                Trace.WriteLine("GetStringFromResponse Error: " + e.Message);
            }

            return html;
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

        /// <summary>
        /// 发送GET请求
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public string HttpGet(string url)
        {
            return HttpGet(url, url);
        }

        /// <summary>
        /// 发送GET请求
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public T HttpGet<T>(string url) where T : class, new()
        {
            try
            {
                string result = HttpGet(url, url);
                if (string.IsNullOrEmpty(result))
                {
                    return default;
                }
                return JsonHelper.DeserializeJsonToObject<T>(result);
            }
            catch
            {
                return default;
            }
        }


        /// <summary>
        /// 发送GET请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="refer"></param>
        /// <returns></returns>
        public string HttpGet(string url, string refer)
        {
            string html;
            try
            {
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CheckCertificate);
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                request.UserAgent = _useragent;
                request.Timeout = _timeout;
                request.ContentType = _contenttype;
                request.Accept = _accept;
                request.Method = "GET";
                request.Referer = refer;
                request.KeepAlive = true;
                request.AllowAutoRedirect = true;
                request.UnsafeAuthenticatedConnectionSharing = true;
                request.CookieContainer = new CookieContainer();
                //据说能提高性能
                request.Proxy = null;
                if (_cookiecollection != null)
                {
                    foreach (Cookie c in _cookiecollection)
                    {
                        c.Domain = request.Host;
                    }

                    request.CookieContainer.Add(_cookiecollection);
                }

                foreach (KeyValuePair<String, String> hd in _headers)
                {
                    request.Headers[hd.Key] = hd.Value;
                }

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                html = GetStringFromResponse(response);
                if (request.CookieContainer != null)
                {
                    response.Cookies = request.CookieContainer.GetCookies(request.RequestUri);
                }

                if (response.Cookies != null)
                {
                    _cookiecollection = response.Cookies;
                }
                if (response.Headers["Set-Cookie"] != null)
                {
                    string tmpcookie = response.Headers["Set-Cookie"];
                    _cookiecollection.Add(ConvertCookieString(tmpcookie));
                }

                response.Close();
                return html;
            }
            catch (Exception e)
            {
                Trace.WriteLine("HttpGet Error: " + e.Message);
                return String.Empty;
            }
        }

        /// <summary>
        /// 获取MINE文件
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public Byte[] HttpGetMine(string url)
        {
            Byte[] mine = null;
            try
            {
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CheckCertificate);
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                request.UserAgent = _useragent;
                request.Timeout = _timeout;
                request.ContentType = _contenttype;
                request.Accept = _accept;
                request.Method = "GET";
                request.Referer = url;
                request.KeepAlive = true;
                request.AllowAutoRedirect = true;
                request.UnsafeAuthenticatedConnectionSharing = true;
                request.CookieContainer = new CookieContainer();
                //据说能提高性能
                request.Proxy = null;
                if (_cookiecollection != null)
                {
                    foreach (Cookie c in _cookiecollection)
                        c.Domain = request.Host;
                    request.CookieContainer.Add(_cookiecollection);
                }

                foreach (KeyValuePair<String, String> hd in _headers)
                {
                    request.Headers[hd.Key] = hd.Value;
                }

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream stream = response.GetResponseStream();
                MemoryStream ms = new MemoryStream();

                byte[] b = new byte[1024];
                while (true)
                {
                    int s = stream.Read(b, 0, b.Length);
                    ms.Write(b, 0, s);
                    if (s == 0 || s < b.Length)
                    {
                        break;
                    }
                }
                mine = ms.ToArray();
                ms.Close();

                if (request.CookieContainer != null)
                {
                    response.Cookies = request.CookieContainer.GetCookies(request.RequestUri);
                }

                if (response.Cookies != null)
                {
                    _cookiecollection = response.Cookies;
                }
                if (response.Headers["Set-Cookie"] != null)
                {
                    _cookies = response.Headers["Set-Cookie"];
                }

                stream.Close();
                stream.Dispose();
                response.Close();
                return mine;
            }
            catch (Exception e)
            {
                Trace.WriteLine("HttpGetMine Error: " + e.Message);
                return null;
            }
        }

        /// <summary>
        /// 发送POST请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public string HttpPost(string url)
        {
            return HttpPost(url, null, url);
        }

        /// <summary>
        /// 发送POST请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public string HttpPost(string url, string data)
        {
            return HttpPost(url, data, url);
        }

        /// <summary>
        /// 发送POST请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public T HttpPost<T>(string url) where T : class
        {
            try
            {
                string result = HttpPost(url, null, url);
                if (string.IsNullOrEmpty(result))
                {
                    return default;
                }
                return JsonHelper.DeserializeJsonToObject<T>(result);
            }
            catch
            {
                return default;
            }
        }

        /// <summary>
        /// 发送POST请求
        /// </summary>
        /// <param name="url"></param>
        /// <param name="data"></param>
        /// <param name="refer"></param>
        /// <returns></returns>
        public string HttpPost(string url, string data, string refer)
        {
            string html;
            try
            {
                ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(CheckCertificate);
                HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(url);
                request.UserAgent = _useragent;
                request.Timeout = _timeout;
                request.Referer = refer;
                request.ContentType = _contenttype;
                request.Accept = _accept;
                request.Method = "POST";
                request.KeepAlive = true;
                request.AllowAutoRedirect = true;
                request.CookieContainer = new CookieContainer();
                //据说能提高性能
                request.Proxy = null;

                if (_cookiecollection != null)
                {
                    foreach (Cookie c in _cookiecollection)
                    {
                        c.Domain = request.Host;
                        if (c.Domain.IndexOf(':') > 0)
                            c.Domain = c.Domain.Remove(c.Domain.IndexOf(':'));
                    }
                    request.CookieContainer.Add(_cookiecollection);
                }

                foreach (KeyValuePair<String, String> hd in _headers)
                {
                    request.Headers[hd.Key] = hd.Value;
                }
                if (!string.IsNullOrEmpty(data))
                {
                    byte[] buffer = _encoding.GetBytes(data.Trim());
                    request.ContentLength = buffer.Length;
                    request.GetRequestStream().Write(buffer, 0, buffer.Length);
                    request.GetRequestStream().Close();
                }
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                html = GetStringFromResponse(response);
                if (request.CookieContainer != null)
                {
                    response.Cookies = request.CookieContainer.GetCookies(request.RequestUri);
                }
                if (response.Cookies != null)
                {
                    _cookiecollection = response.Cookies;
                }
                if (response.Headers["Set-Cookie"] != null)
                {
                    string tmpcookie = response.Headers["Set-Cookie"];
                    _cookiecollection.Add(ConvertCookieString(tmpcookie));
                }

                response.Close();
                return html;
            }
            catch (Exception e)
            {
                Trace.WriteLine("HttpPost Error: " + e.Message);
                return String.Empty;
            }
        }


        public string UrlEncode(string str)
        {
            StringBuilder sb = new StringBuilder();
            byte[] byStr = _encoding.GetBytes(str);
            for (int i = 0; i < byStr.Length; i++)
            {
                sb.Append(@"%" + Convert.ToString(byStr[i], 16));
            }

            return (sb.ToString());
        }

        /// <summary>
        /// 转换cookie字符串到CookieCollection
        /// </summary>
        /// <param name="ck"></param>
        /// <returns></returns>
        private CookieCollection ConvertCookieString(string ck)
        {
            CookieCollection cc = new CookieCollection();
            string[] cookiesarray = ck.Split(";".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < cookiesarray.Length; i++)
            {
                string[] cookiesarray_2 = cookiesarray[i].Split(",".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                for (int j = 0; j < cookiesarray_2.Length; j++)
                {
                    string[] cookiesarray_3 = cookiesarray_2[j].Trim().Split("=".ToCharArray());
                    if (cookiesarray_3.Length == 2)
                    {
                        string cname = cookiesarray_3[0].Trim();
                        string cvalue = cookiesarray_3[1].Trim();
                        if (cname.ToLower() != "domain" && cname.ToLower() != "path" && cname.ToLower() != "expires")
                        {
                            Cookie c = new Cookie(cname, cvalue);
                            cc.Add(c);
                        }
                    }
                }
            }
            return cc;
        }
    }
}
