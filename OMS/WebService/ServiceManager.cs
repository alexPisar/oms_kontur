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

        public async Task<T> PostRequestAsync<T>(string url, object contentData, CookieCollection cookies = null, string contentType = null,
            Dictionary<string, string> headers = null, Encoding encoding = null, string accept = null) where T : class, new()
        {
            if (string.IsNullOrEmpty(contentType))
                contentType = "application/x-www-form-urlencoded";

            HttpClient client;

            if (_webProxy != null)
            {
                var handler = new HttpClientHandler
                {
                    Proxy = _webProxy,
                    UseProxy = true
                };

                if (cookies != null)
                {
                    handler.UseCookies = true;
                    handler.CookieContainer = new CookieContainer();
                    handler.CookieContainer.Add(cookies);
                }

                client = new HttpClient(handler);
            }
            else if (cookies != null)
            {
                var handler = new HttpClientHandler
                {
                    UseCookies = true,
                    CookieContainer = new CookieContainer()
                };

                handler.CookieContainer.Add(cookies);
                client = new HttpClient(handler);
            }
            else
                client = new HttpClient();

            if (accept != null)
                client.DefaultRequestHeaders.Add("Accept", accept);

            if (headers != null)
            {
                foreach (var header in headers)
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            var content = new StringContent(contentData as string, encoding, contentType);
            var response = await client.PostAsync(url, content);

            string resultAsJson;
            bool isSuccessStatusCode = response.IsSuccessStatusCode;
            var statusCode = response.StatusCode;

            try
            {
                resultAsJson = await response.Content.ReadAsStringAsync();
            }
            catch
            {
                resultAsJson = string.Empty;
            }

            if (!isSuccessStatusCode)
                throw new Exception($"Произошла ошибка со статусом {statusCode}\n{resultAsJson}");

            var result = JsonConvert.DeserializeObject<T>(resultAsJson);

            return result;
        }

        public string PostRequest(string url, object contentData, string cookie = null, string contentType = null,
            Dictionary<string, string> headers = null, Encoding encoding = null, CookieCollection cookies = null)
        {
            if (contentData as Dictionary<object, string> != null)
            {
                var dictionaryContentData = contentData as Dictionary<object, string>;

                HttpContent requestContent = null;
                var fileStreamList = new List<System.IO.FileStream>();

                try
                {
                    if (contentType == "multipart/form-data")
                    {
                        requestContent = new System.Net.Http.MultipartFormDataContent();
                        foreach (var parameter in dictionaryContentData)
                        {
                            if (parameter.Key?.GetType() == typeof(Models.FileParameter))
                            {
                                var fileParameter = parameter.Key as Models.FileParameter;

                                System.Net.Http.HttpContent fileContent;
                                var stream = new System.IO.FileStream(parameter.Value, System.IO.FileMode.Open, System.IO.FileAccess.Read);

                                fileStreamList.Add(stream);
                                fileContent = new System.Net.Http.StreamContent(stream);

                                string fileName = fileParameter?.FileName;
                                if (string.IsNullOrEmpty(fileName))
                                {
                                    var fileInfo = new System.IO.FileInfo(parameter.Value);
                                    fileName = fileInfo.Name;

                                    var fileNameLength = fileName.LastIndexOf('.');

                                    if (fileNameLength < 0)
                                        fileNameLength = fileName.Length;

                                    fileName = fileName.Substring(0, fileNameLength);
                                }

                                fileContent.Headers.ContentDisposition =
                                    new System.Net.Http.Headers.ContentDispositionHeaderValue(fileParameter.ContentDispositionType)
                                    {
                                        Name = fileParameter.Name,
                                        FileName = fileName
                                    };

                                fileContent.Headers.ContentType =
                                    System.Net.Http.Headers.MediaTypeHeaderValue.Parse(fileParameter.ContentType);

                                ((System.Net.Http.MultipartFormDataContent)requestContent).Add(fileContent, fileParameter.Name, fileName);

                            }
                            else if (parameter.Key?.GetType() == typeof(string))
                            {
                                var stringContent = new System.Net.Http.StringContent(parameter.Value);
                                ((System.Net.Http.MultipartFormDataContent)requestContent).Add(stringContent, (string)parameter.Key);
                            }
                        }
                    }
                    else if (contentType == "application/x-www-form-urlencoded")
                    {
                        var parametersList = new Dictionary<string, string>();

                        foreach(var parameter in dictionaryContentData)
                        {
                            if (parameter.Key?.GetType() == typeof(Models.FileParameter))
                            {
                                var fileParameter = parameter.Key as Models.FileParameter;

                                string fileName = string.IsNullOrEmpty(fileParameter.FileName)
                                    ? parameter.Value : fileParameter.FileName;

                                parametersList.Add(fileParameter.Name, fileName);
                            }
                            else if (parameter.Key?.GetType() == typeof(string))
                            {
                                parametersList.Add(parameter.Key as string, parameter.Value);
                            }
                        }

                        requestContent = new FormUrlEncodedContent(parametersList);
                    }

                    if (requestContent == null)
                        return null;

                    System.Net.Http.HttpClient client;
                    if (_webProxy != null)
                    {
                        System.Net.Http.HttpClientHandler httpClientHandler = new System.Net.Http.HttpClientHandler()
                        {
                            Proxy = _webProxy,
                            PreAuthenticate = true,
                            UseDefaultCredentials = false,
                        };

                        client = new System.Net.Http.HttpClient(httpClientHandler);
                    }
                    else
                        client = new System.Net.Http.HttpClient();

                    if (headers != null)
                        foreach (var header in headers)
                            client.DefaultRequestHeaders.Add(header.Key, header.Value);

                    var res = client.PostAsync(url, requestContent).Result;
                    _statusCode = ((int)res.StatusCode).ToString();
                    return res.Content.ReadAsStringAsync().Result;
                }
                finally
                {
                    foreach (var fileStream in fileStreamList)
                        fileStream.Close();
                }
            }
            else if (!string.IsNullOrEmpty(contentData as string))
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

                var contentDataBytes = System.Text.Encoding.UTF8.GetBytes(contentData as string);

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
            else
                return null;
        }

        public T PostRequest<T>(string url, string contentData, string cookie = null, string contentType = null, 
            Dictionary<string, string> headers = null, Encoding encoding = null, CookieCollection cookies = null)
        {
            string resultAsJsonStr = PostRequest(url, contentData, cookie, contentType, headers, encoding, cookies);

            var result = JsonConvert.DeserializeObject<T>( resultAsJsonStr );

            return result;
        }

        public string GetRequest(string url, Dictionary<string, string> headers = null, Encoding encoding = null, string accept = null)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            request.Method = "GET";

            if (accept != null)
                request.Accept = accept;

            if (headers != null)
            {
                foreach (var header in headers)
                    request.Headers.Add(header.Key, header.Value);
            }

            if (_webProxy != null)
                request.Proxy = _webProxy;

            var response = request.GetResponse();

            string result;

            if (encoding != null)
            {
                using (var sr = new System.IO.StreamReader(response.GetResponseStream(), encoding))
                {
                    result = sr.ReadToEnd();
                }
            }
            else
            {
                using (var sr = new System.IO.StreamReader(response.GetResponseStream()))
                {
                    result = sr.ReadToEnd();
                }
            }

            return result;
        }

        public T GetRequest<T>(string url, Dictionary<string, string> headers = null)
        {
            string resultAsJsonStr = GetRequest(url, headers);

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
