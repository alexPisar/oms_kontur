using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using Newtonsoft.Json;

namespace UpdaterConturEdi
{
    public class FileWebService
    {
        //размер файла который считается большим
        private const long BigFileSize = 20000000;

        private string _url;
        private HttpClient _client;

        public FileWebService(string url)
        {
            _url = url;
            _client = new HttpClient();
        }

        public string[] GetDirectories(string relativePath, string appVersion)
        {
            string contentData = $"list=directories&appName=KonturEdi&path={relativePath}";

            return PostRequest<string[]>("/request.php", contentData, $"version={appVersion}");
        }

        public string[] GetFilesListByPath(string relativePath, string appVersion)
        {
            string contentData = $"list=files&appName=KonturEdi&path={relativePath}";

            return PostRequest<string[]>("/request.php", contentData, $"version={appVersion}" );
        }

        public byte[] GetFileDataByPath(string relativeFilePath, string appVersion)
        {
            WebClient client = new WebClient();

            byte[] resultBytes = client.DownloadData(_url + $"/KonturEdi/{appVersion}/" + relativeFilePath);

            return resultBytes;
        }

        public UpdateInfo GetAppUpdateInfo()
        {
            var updateInfo = GetRequest<UpdateInfo>("/request.php?application=KonturEdi");

            return updateInfo;
        }

        private T PostRequest<T>(string route, string contentData, string cookie = null)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create( _url + route );

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            if (cookie != null)
                request.Headers.Add( "Cookie", $"{cookie}");

            var requestStream = request.GetRequestStream();

            var contentDataBytes = System.Text.Encoding.UTF8.GetBytes( contentData );

            requestStream.Write( contentDataBytes, 0, contentDataBytes.Length );

            var response = request.GetResponse();

            string resultAsJsonStr;

            using (var sr = new System.IO.StreamReader( response.GetResponseStream() ))
            {
                resultAsJsonStr = sr.ReadToEnd();
            }

            var result = JsonConvert.DeserializeObject<T>( resultAsJsonStr );

            return result;
        }

        private T GetRequest<T>(string route)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create( _url + route );

            request.Method = "GET";

            var response = request.GetResponse();

            string resultAsJsonStr;

            using (var sr = new System.IO.StreamReader( response.GetResponseStream() ))
            {
                resultAsJsonStr = sr.ReadToEnd();
            }

            var result = JsonConvert.DeserializeObject<T>( resultAsJsonStr );

            return result;
        }
    }
}
