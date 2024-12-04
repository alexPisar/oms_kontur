using System;
using System.Net;
using Newtonsoft.Json;

namespace WebService.Controllers
{
    public class FinDbController : IController
    {
        private static FinDbController _instance;
        private ServiceManager _serviceManager;
        private WebConfig.FinDbConfig _finDbConfig;
        private bool _loadedConfig = false;

        public override string Reference => $"{UtilitesLibrary.ConfigSet.Config.GetInstance().UpdaterFilesLoadReference}/get";
        public bool LoadedConfig => _loadedConfig;

        private FinDbController()
        {
            _serviceManager = new ServiceManager();
        }

        private static readonly object _syncRoot = new object();
        public static FinDbController GetInstance()
        {
            if (_instance == null)
            {
                lock (_syncRoot)
                {
                    if (_instance == null)
                    {
                        _instance = new FinDbController();
                    }
                }
            }

            return _instance;
        }

        public string GetCipherContentForConnect(string appName)
        {
            var serviceManager = new ServiceManager();
            var contentBytes = serviceManager.DownloadDataFileFromReference($"{Reference}/auth/{appName}/FindbConnection");
            var content = System.Text.Encoding.UTF8.GetString(contentBytes);
            return content;
        }

        public string GetConfigFileName()
        {
            return "connect_findb_password";
        }

        public void InitConfig(string content)
        {
            _finDbConfig = JsonConvert.DeserializeObject<WebConfig.FinDbConfig>(content);
            _loadedConfig = true;
        }

        private string FinDbWebRequest(string urlPath, string getContent = null, CookieCollection cookies = null, string contentData = null)
        {
            if (cookies == null)
                cookies = new CookieCollection();

            var referenceUri = new Uri(Reference);
            cookies.Add(new Cookie("position", _finDbConfig.Position.ToString()) { Domain = referenceUri.Host });
            cookies.Add(new Cookie("shift", _finDbConfig.Shift.ToString()) { Domain = referenceUri.Host });

            if (string.IsNullOrEmpty(contentData))
                contentData = $"authorization={_finDbConfig.EdiCipherPassword}";
            else
                contentData = $"authorization={_finDbConfig.EdiCipherPassword}&{contentData}";

            if (string.IsNullOrEmpty(getContent))
                getContent = "user=edi";
            else
                getContent = $"{getContent}&user=edi";

            return _serviceManager.PostRequest($"{Reference}{urlPath}?{getContent}", contentData,
                null, null, null, System.Text.Encoding.GetEncoding(1251), cookies);
        }

        private T FinDbWebRequest<T>(string urlPath, string getContent = null, CookieCollection cookies = null, string contentData = null)
        {
            string resultAsJsonStr = FinDbWebRequest(urlPath, getContent, cookies, contentData);

            var result = JsonConvert.DeserializeObject<T>(resultAsJsonStr);

            return result;
        }

        public Models.DocOrderInfo GetDocOrderInfoByIdDocAndOrderStatus(decimal idDoc, int? orderStatus = null)
        {
            if (orderStatus == null)
                orderStatus = 1;

            var result = FinDbWebRequest<Models.DocOrderInfo>("/document/edi/index.php", $"IdDoc={idDoc}&OrderStatus={orderStatus}");

            if (string.IsNullOrEmpty(result?.Id))
                throw new Exception("Не удалось найти заказ EDI в базе");

            return result;
        }

        public Models.RefEdiChannel[] GetEdiChannels()
        {
            var result = FinDbWebRequest<Models.RefEdiChannel[]>("/channels/edi/index.php");
            return result;
        }
    }
}
