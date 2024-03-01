using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;

namespace WebService
{
    public class ServiceManager
    {
        private WebProxy _webProxy = null;
        private string _statusCode = null;

        public ServiceManager() { }

        public ServiceManager(string proxyAddress, string proxyUserName, string proxyPassword)
        {
            _webProxy = new WebProxy();

            _webProxy.Address = new Uri("http://" + proxyAddress);
            _webProxy.Credentials = new NetworkCredential(proxyUserName, proxyPassword);
        }

        public string GetStatusCode()
        {
            return _statusCode;
        }

        public string PostRequest(string url, string contentData, string cookie = null, string contentType = null,
            Dictionary<string, string> headers = null, Encoding encoding = null, CookieCollection cookies = null)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.Method = "POST";

            if (string.IsNullOrEmpty(contentType))
                request.ContentType = "application/x-www-form-urlencoded";
            else
                request.ContentType = contentType;

            if (_webProxy != null)
                request.Proxy = _webProxy;

            if (cookie != null)
                request.Headers.Add("Cookie", $"{cookie}");

            if(cookies != null)
            {
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(cookies);
            }

            if (headers != null)
            {
                foreach (var header in headers)
                    request.Headers.Add(header.Key, header.Value);
            }

            var requestStream = request.GetRequestStream();

            var contentDataBytes = System.Text.Encoding.UTF8.GetBytes(contentData);

            requestStream.Write(contentDataBytes, 0, contentDataBytes.Length);

            var response = request.GetResponse();

            string result;

            if (encoding == null)
            {
                using (var sr = new System.IO.StreamReader(response.GetResponseStream()))
                {
                    result = sr.ReadToEnd();
                }
            }
            else
            {
                using (var sr = new System.IO.StreamReader(response.GetResponseStream(), encoding))
                {
                    result = sr.ReadToEnd();
                }
            }

            _statusCode = ((int?)((HttpWebResponse)response)?.StatusCode)?.ToString();
            return result;
        }

        public T PostRequest<T>(string url, string contentData, string cookie = null, string contentType = null, 
            Dictionary<string, string> headers = null, Encoding encoding = null, CookieCollection cookies = null)
        {
            string resultAsJsonStr = PostRequest(url, contentData, cookie, contentType, headers, encoding, cookies);

            var result = JsonConvert.DeserializeObject<T>( resultAsJsonStr );

            return result;
        }

        public T GetRequest<T>(string url, Dictionary<string, string> headers = null)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.Method = "GET";

            if (headers != null)
            {
                foreach (var header in headers)
                    request.Headers.Add(header.Key, header.Value);
            }

            if (_webProxy != null)
                request.Proxy = _webProxy;

            var response = request.GetResponse();

            string resultAsJsonStr;

            using (var sr = new System.IO.StreamReader(response.GetResponseStream()))
            {
                resultAsJsonStr = sr.ReadToEnd();
            }

            var result = JsonConvert.DeserializeObject<T>(resultAsJsonStr);

            return result;
        }

        public byte[] DownloadDataFileFromReference(string url)
        {
            System.Net.WebClient client = new System.Net.WebClient();

            return client.DownloadData(url);
        }
    }
}
