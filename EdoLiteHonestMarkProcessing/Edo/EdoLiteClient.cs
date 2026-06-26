using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Cryptography.WinApi;
using UtilitesLibrary.ConfigSet;
using System.Security.Cryptography.X509Certificates;

namespace EdoLiteHonestMarkProcessing.Edo
{
    public class EdoLiteClient
    {
        protected static EdoLiteClient _instance;
        private string _token;
        private WebService.ServiceManager _webService;
        private string _cacheName;
        private X509Certificate2 _certificate;
        private bool _isUuidToken = false;

        private EdoLiteClient() : base()
        {
            if (Config.GetInstance().ProxyEnabled)
                _webService = new WebService.ServiceManager(Config.GetInstance().ProxyAddress,
                    Config.GetInstance().ProxyUserName,
                    Config.GetInstance().ProxyUserPassword);
            else
                _webService = new WebService.ServiceManager();
        }

        public static EdoLiteClient GetInstance()
        {
            if (_instance == null)
                _instance = new EdoLiteClient();

            return _instance;
        }

        public bool Authorization(X509Certificate2 certificate, EdiProcessingUnit.Edo.Models.Kontragent organization = null)
        {
            EdoLiteTokenCache cache;

            if (organization == null || string.IsNullOrEmpty(organization?.EmchdId))
                cache = new EdoLiteTokenCache().Load(certificate.Thumbprint);
            else
                cache = new EdoLiteTokenCache().Load($"{organization.Inn}_{certificate.Thumbprint}");

            if (cache?.Token != null && cache?.TokenExpirationDate > DateTime.Now)
            {
                _token = cache.Token;

                if(organization == null || string.IsNullOrEmpty(organization?.EmchdId))
                    _cacheName = certificate.Thumbprint;
                else
                    _cacheName = $"{organization.Inn}_{certificate.Thumbprint}";

                _certificate = certificate;
                return true;
            }

            var authData = _webService.GetRequest<Models.AuthData>($"{Properties.Settings.Default.UrlAddressHonestMark}/auth/key");

            var crypto = new WinApiCryptWrapper(certificate);

            byte[] signedData = Encoding.UTF8.GetBytes(authData.Data);
            var signature = crypto.Sign(signedData, false);

            var authRequest = new Models.AuthRequest
            {
                Uid = authData.Uid,
                Data = Convert.ToBase64String(signature),
                UnitedToken = _isUuidToken
            };

            if (organization != null && !string.IsNullOrEmpty(organization?.EmchdId))
                authRequest.Inn = organization.Inn;

            var authRequestJson = JsonConvert.SerializeObject(authRequest);

            var result = _webService.PostRequest<Models.AuthResult>($"{Properties.Settings.Default.UrlAddressHonestMark}/auth/simpleSignIn",
                authRequestJson, null, "application/json");

            if (result.ErrorMessage != null)
                throw new Exception($"Произошла ошибка с кодом {result.ErrorCode}:{result.ErrorMessage} /nОписание:{result.Description}");

            if (string.IsNullOrEmpty(result.Token) && string.IsNullOrEmpty(result.UuidToken))
                throw new Exception("Не удалось получить токен авторизации.");

            if(!string.IsNullOrEmpty(result.UuidToken))
                _token = result.UuidToken;
            else
                _token = result.Token;

            cache = new EdoLiteTokenCache()
            {
                Token = _token,
                TokenCreationDate = DateTime.Now,
                TokenExpirationDate = DateTime.Now.AddHours(10)
            };

            if (organization == null || string.IsNullOrEmpty(organization?.EmchdId))
            {
                cache.Save(cache, certificate.Thumbprint);
                _cacheName = certificate.Thumbprint;
            }
            else
            {
                cache.Save(cache, $"{organization.Inn}_{certificate.Thumbprint}");
                _cacheName = $"{organization.Inn}_{certificate.Thumbprint}";
            }

            _certificate = certificate;

            return true;
        }

        public List<Models.EdoLiteDocuments> GetAllIncomingDocuments(DateTime? dateFrom = null, DateTime? dateTo = null, string partnerInn = null, Enums.EdoLiteDocTypeEnum? docType = null)
        {
            Models.EdoLiteDocumentList edoLiteDocuments = null;
            var result = new List<Models.EdoLiteDocuments>();

            int i = 0;
            do
            {
                edoLiteDocuments = GetIncomingDocumentList(10, i * 10, dateFrom, dateTo, partnerInn, docType);

                if(edoLiteDocuments?.Items != null)
                    result.AddRange(edoLiteDocuments.Items);

                i++;
            }
            while (edoLiteDocuments != null && edoLiteDocuments.HasNextPage);

            return result;
        }

        public Models.EdoLiteDocumentList GetIncomingDocumentList(int limit = 10, int offset = 0, DateTime? dateFrom = null, DateTime? dateTo = null, string partnerInn = null, Enums.EdoLiteDocTypeEnum? docType = null)
        {
            var authData = new Dictionary<string, string>();
            authData.Add("Authorization", $"Bearer {_token}");

            string url = $"{Properties.Settings.Default.UrlAddressEdoLite}/api/v1/incoming-documents?limit={limit}&&offset={offset}";

            if (dateFrom != null)
            {
                var timestamp = new UtilitesLibrary.Service.DateTimeUtil().GetTimestampByDateTime(dateFrom.Value);
                url = $"{url}&&created_from={timestamp}";
            }

            if (dateTo != null)
            {
                var timestamp = new UtilitesLibrary.Service.DateTimeUtil().GetTimestampByDateTime(dateTo.Value);
                url = $"{url}&&created_to={timestamp}";
            }

            if (!string.IsNullOrEmpty(partnerInn))
            {
                url = $"{url}&&partner_inn={partnerInn}";
            }

            if(docType != null)
            {
                url = $"{url}&&type={(int)docType.Value}";
            }

            var documentList = _webService.GetRequest<Models.EdoLiteDocumentList>(url, authData);
            return documentList;
        }

        public Models.EdoLiteDocumentList GetOutgoingDocumentList(int limit = 10, DateTime? dateFrom = null, DateTime? dateTo = null, string partnerInn = null)
        {
            var authData = new Dictionary<string, string>();
            authData.Add("Authorization", $"Bearer {_token}");

            string url = $"{Properties.Settings.Default.UrlAddressEdoLite}/api/v1/outgoing-documents?limit={limit}";

            if (dateFrom != null)
            {
                var timestamp = new UtilitesLibrary.Service.DateTimeUtil().GetTimestampByDateTime(dateFrom.Value);
                url = $"{url}&&created_from={timestamp}";
            }

            if (dateTo != null)
            {
                var timestamp = new UtilitesLibrary.Service.DateTimeUtil().GetTimestampByDateTime(dateTo.Value);
                url = $"{url}&&created_to={timestamp}";
            }

            if (!string.IsNullOrEmpty(partnerInn))
            {
                url = $"{url}&&partner_inn={partnerInn}";
            }

            var documentList = _webService.GetRequest<Models.EdoLiteDocumentList>(url, authData);
            return documentList;
        }

        public byte[] GetIncomingDocumentContent(string documentId)
        {
            var authData = new Dictionary<string, string>();
            authData.Add("Authorization", $"Bearer {_token}");

            var docContentStr = _webService.GetRequest($"{Properties.Settings.Default.UrlAddressEdoLite}/api/v1/incoming-documents/{documentId}/content", authData, Encoding.GetEncoding(1251));

            var fileBytes = Encoding.GetEncoding(1251).GetBytes(docContentStr);
            return fileBytes;
        }

        public byte[] GetOutgoingDocumentContent(string documentId)
        {
            var authData = new Dictionary<string, string>();
            authData.Add("Authorization", $"Bearer {_token}");

            var docContentStr = _webService.GetRequest($"{Properties.Settings.Default.UrlAddressEdoLite}/api/v1/outgoing-documents/{documentId}/content", authData, Encoding.GetEncoding(1251));

            var fileBytes = Encoding.GetEncoding(1251).GetBytes(docContentStr);
            return fileBytes;
        }

        public byte[] GetIncomingZipDocument(string documentId)
        {
            var headerData = new Dictionary<string, string>();
            headerData.Add("Authorization", $"Bearer {_token}");

            var docContentStr = _webService.GetRequest($"{Properties.Settings.Default.UrlAddressEdoLite}/api/v1/incoming-documents/{documentId}", headerData, Encoding.GetEncoding(1251), "application/zip");

            var fileBytes = Encoding.GetEncoding(1251).GetBytes(docContentStr);
            return fileBytes;
        }

        public byte[] GetOutgoingZipDocument(string documentId)
        {
            var headerData = new Dictionary<string, string>();
            headerData.Add("Authorization", $"Bearer {_token}");

            var docContentStr = _webService.GetRequest($"{Properties.Settings.Default.UrlAddressEdoLite}/api/v1/outgoing-documents/{documentId}", headerData, Encoding.GetEncoding(1251), "application/zip");

            var fileBytes = Encoding.GetEncoding(1251).GetBytes(docContentStr);
            return fileBytes;
        }

        public byte[] GetIncomingDocumentPrintForm(string documentId)
        {
            var headerData = new Dictionary<string, string>();
            headerData.Add("Authorization", $"Bearer {_token}");

            var contentStr = _webService.GetRequest($"{Properties.Settings.Default.UrlAddressEdoLite}/api/v1/incoming-documents/{documentId}/print", headerData, Encoding.GetEncoding(1251));

            var contentBytes = Encoding.GetEncoding(1251).GetBytes(contentStr);
            return contentBytes;
        }

        public byte[] GetOutgoingDocumentPrintForm(string documentId)
        {
            var headerData = new Dictionary<string, string>();
            headerData.Add("Authorization", $"Bearer {_token}");

            var contentStr = _webService.GetRequest($"{Properties.Settings.Default.UrlAddressEdoLite}/api/v1/outgoing-documents/{documentId}/print", headerData, Encoding.GetEncoding(1251));

            var contentBytes = Encoding.GetEncoding(1251).GetBytes(contentStr);
            return contentBytes;
        }

        public string LoadTitleDocument(string content, string idDocument, string signature)
        {
            var authData = new Dictionary<string, string>();
            authData.Add("Authorization", $"Bearer {_token}");

            var requestData = new Dictionary<object, string>();

            requestData.Add(
                new WebService.Models.FileParameter("content")
                {
                    ContentType = "application/xml",
                    ContentDispositionType = "form-data"
                },
                content);

            requestData.Add("doc_id", idDocument);
            requestData.Add("signature", signature);

            string result = _webService.PostRequest($"{Properties.Settings.Default.UrlAddressEdoLite}/api/v1/incoming-documents/xml/upd/title/970",
                requestData, null, "multipart/form-data", authData, Encoding.GetEncoding(1251));

            var resultStatus = _webService.GetStatusCode();

            if (resultStatus != "200" && resultStatus != "201")
                throw new Exception($"Произошла ошибка с кодом {resultStatus}.\n" +
                    $"Описание: {result}");

            return result;
        }

        public string LoadOutgoingDocument(string content, string signature)
        {
            var authData = new Dictionary<string, string>();
            authData.Add("Authorization", $"Bearer {_token}");

            var requestData = new Dictionary<object, string>();

            requestData.Add(
                new WebService.Models.FileParameter("content")
                {
                    ContentType = "application/xml",
                    ContentDispositionType = "form-data"
                },
                content);

            requestData.Add("signature", signature);

            string result = _webService.PostRequest($"{Properties.Settings.Default.UrlAddressEdoLite}/api/v1/outgoing-documents",
                requestData, null, "multipart/form-data", authData, Encoding.GetEncoding(1251));

            var resultStatus = _webService.GetStatusCode();

            if (resultStatus != "200" && resultStatus != "201")
                throw new Exception($"Произошла ошибка с кодом {resultStatus}.\n" +
                    $"Описание: {result}");

            return result;
        }
    }
}
