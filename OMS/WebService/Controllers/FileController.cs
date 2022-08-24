using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtilitesLibrary.ConfigSet;

namespace WebService.Controllers
{
    public class FileController : IController
    {
        //размер файла который считается большим
        private const long BigFileSize = 20000000;

        ServiceManager _service;

        public override string Reference => Config.GetInstance().UpdaterFilesLoadReference;

        public FileController():base()
        {
            _service = new ServiceManager();
        }

        public string[] GetFilesListFromPath(string appName, string version, string relativePath = "")
        {
            string contentData = $"list=files&appName={appName}&path={relativePath}";

            return _service.PostRequest<string[]>( Reference + "/request.php", contentData, $"version={version}" );
        }

        public string[] GetDirectoriesFromPath(string appName, string version, string relativePath = "")
        {
            string contentData = $"list=directories&appName={appName}&path={relativePath}";

            return _service.PostRequest<string[]>( Reference + "/request.php", contentData, $"version={version}" );
        }

        public byte[] GetFileDataByPath(string appName, string version, string relativeFilePath = "")
        {
            System.Net.WebClient client = new System.Net.WebClient();

            byte[] resultBytes = _service.DownloadDataFileFromReference(Reference + $"/{appName}/{version}/" + relativeFilePath);

            return resultBytes;
        }

        public string GetVersion(string appName)
        {
            var obj = _service.GetRequest<Newtonsoft.Json.Linq.JObject>(Reference + $"/request.php?application={appName}");
            return (string)obj["Version"];
        }

        public T GetUpdateParameters<T>(string appName)
        {
            return _service.GetRequest<T>(Reference + $"/request.php?application={appName}");
        }
    }
}
