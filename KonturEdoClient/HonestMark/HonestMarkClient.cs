using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json;
using Cryptography.WinApi;
using WebService;

namespace KonturEdoClient.HonestMark
{
    public class HonestMarkClient
    {
        private static HonestMarkClient _instance;

        private ServiceManager _webService;
        private X509Certificate2 _certificate;
        private string _token;

        private ServiceManager _webManager;

        private HonestMarkClient()
        {
            if (UtilitesLibrary.ConfigSet.Config.GetInstance().ProxyEnabled)
                _webService = new ServiceManager(UtilitesLibrary.ConfigSet.Config.GetInstance().ProxyAddress,
                    UtilitesLibrary.ConfigSet.Config.GetInstance().ProxyUserName,
                    UtilitesLibrary.ConfigSet.Config.GetInstance().ProxyUserPassword);
            else
                _webService = new ServiceManager();
        }

        public static HonestMarkClient GetInstance()
        {
            if (_instance == null)
                _instance = new HonestMarkClient();

            return _instance;
        }

        public bool Authorization(X509Certificate2 certificate)
        {
            var cache = new HonestMarkTokenCache().Load(certificate.Thumbprint);

            if (cache?.Token != null && cache?.TokenExpirationDate > DateTime.Now)
            {
                _token = cache.Token;
                _certificate = certificate;
                return true;
            }

            var authData = _webService.GetRequest<Models.AuthRequest>($"{Properties.Settings.Default.UrlAddressHonestMark}/auth/key");

            var crypto = new WinApiCryptWrapper(certificate);

            byte[] signedData = Encoding.UTF8.GetBytes(authData.Data);
            var signature = crypto.Sign(signedData, false);

            var authRequest = new Models.AuthRequest
            {
                Uid = authData.Uid,
                Data = Convert.ToBase64String(signature)
            };

            var authRequestJson = JsonConvert.SerializeObject(authRequest);

            var result = _webService.PostRequest<Models.AuthResult>($"{Properties.Settings.Default.UrlAddressHonestMark}/auth/simpleSignIn",
                authRequestJson, null, "application/json");

            if (result.ErrorMessage != null)
                throw new Exception($"Произошла ошибка с кодом {result.ErrorCode}:{result.ErrorMessage} /nОписание:{result.Description}");

            if (string.IsNullOrEmpty(result.Token))
                throw new Exception("Не удалось получить токен авторизации.");

            _token = result.Token;

            cache = new HonestMarkTokenCache()
            {
                Token = result.Token,
                TokenCreationDate = DateTime.Now,
                TokenExpirationDate = DateTime.Now.AddHours(10)
            };
            cache.Save(cache, certificate.Thumbprint);
            _certificate = certificate;

            return true;
        }

        public string CreateDocument(ProductGroupsEnum productGroup, DocumentFormatsEnum documentFormat, string codeDocType, IDocument documentData)
        {
            if (string.IsNullOrEmpty(_token) || _certificate == null)
                throw new Exception("Ошибка авторизации. Не определён токен либо сертификат пользователя.");

            var enumUtil = new UtilitesLibrary.Service.EnumUtil();
            var crypto = new WinApiCryptWrapper(_certificate);

            var productGroupStr = enumUtil.GetEnumMemberAttrValue(productGroup);
            var documentFormatStr = enumUtil.GetEnumMemberAttrValue(documentFormat);

            var documentAsJson = JsonConvert.SerializeObject(documentData);
            var documentDataBytes = Encoding.UTF8.GetBytes(documentAsJson);
            var signature = crypto.Sign(documentDataBytes, true);

            var documentCreateRequestData = new Models.DocumentCreateRequest()
            {
                DocumentFormat = documentFormatStr,
                ProductDocument = Convert.ToBase64String(documentDataBytes),
                Signature = Convert.ToBase64String(signature),
                Type = codeDocType
            };
            var documentCreateRequestAsJson = JsonConvert.SerializeObject(documentCreateRequestData);

            var authData = new Dictionary<string, string>();
            authData.Add("Authorization", $"Bearer {_token}");

            var url = $"{Properties.Settings.Default.UrlAddressHonestMark}/lk/documents/create?pg={productGroupStr}";
            var result = _webService.PostRequest(url, documentCreateRequestAsJson,
                null, "application/json", authData);

            var resultStatus = _webService.GetStatusCode();

            if(resultStatus != "200" && resultStatus != "201")
            {
                var error = JsonConvert.DeserializeObject<Models.DocumentCreateResponse>(result);

                throw new Exception($"Произошла ошибка с кодом {resultStatus}:\n" +
                    $"Описание ошибки: {error?.ErrorMessage ?? ""}");
            }

            return result;
        }

        public Models.ReprocessDocumentResponse ReprocessDocument(string docId)
        {
            var reprocessDocumentRequest = new { documentId = docId };
            var reprocessDocumentRequestAsJson = JsonConvert.SerializeObject(reprocessDocumentRequest);

            var authData = new Dictionary<string, string>();
            authData.Add("Authorization", $"Bearer {_token}");

            var url = $"{Properties.Settings.Default.UrlAddressHonestMark}/document/reprocess";
            var result = _webService.PostRequest<Models.ReprocessDocumentResponse>(url, reprocessDocumentRequestAsJson, null, "application/json", authData);

            var resultStatus = _webService.GetStatusCode();

            if (resultStatus != "200" && resultStatus != "201")
            {
                throw new Exception($"Произошла ошибка с кодом {resultStatus}:\n" +
                    $"Описание ошибки: {result?.ErrorDescription ?? ""}");
            }

            return result;
        }

        public Models.MarkCodeInfo[] GetMarkCodesInfo(ProductGroupsEnum productGroup, string[] markCodes)
        {
            if (string.IsNullOrEmpty(_token) || _certificate == null)
                throw new Exception("Ошибка авторизации. Не определён токен либо сертификат пользователя.");

            string productGroupStr;

            if (productGroup == ProductGroupsEnum.None)
                productGroupStr = string.Empty;
            else
            {
                var enumUtil = new UtilitesLibrary.Service.EnumUtil();
                productGroupStr = $"?pg={enumUtil.GetEnumMemberAttrValue(productGroup)}";
            }

            var authData = new Dictionary<string, string>();
            authData.Add("Authorization", $"Bearer {_token}");

            var markCodesAsJson = JsonConvert.SerializeObject(markCodes);

            var markCodesInfos = _webService.PostRequest<Models.MarkCodeInfo[]>($"{Properties.Settings.Default.UrlAddressHonestMark}/cises/info{productGroupStr}",
                markCodesAsJson, null, "application/json", authData);

            return markCodesInfos;
        }

        public Models.DocumentInfo GetDocumentInfo(ProductGroupsEnum productGroup, string docId)
        {
            var enumUtil = new UtilitesLibrary.Service.EnumUtil();
            var productGroupStr = enumUtil.GetEnumMemberAttrValue(productGroup);

            var authData = new Dictionary<string, string>();
            authData.Add("Authorization", $"Bearer {_token}");

            var docInfo = _webService.GetRequest<Models.DocumentInfo>($"{Properties.Settings.Default.UrlAddressHonestMark}/doc/{docId}/info?pg={productGroupStr}", authData);

            return docInfo;
        }

        public Models.DocumentEdoProcessResultInfo GetEdoDocumentProcessInfo(string docId)
        {
            var authData = new Dictionary<string, string>();
            authData.Add("Authorization", $"Bearer {_token}");

            var docInfo = _webService.GetRequest<Models.DocumentEdoProcessResultInfo>($"{Properties.Settings.Default.UrlAddressHonestMark}/documents/edo/tpr/ud?fileId={docId}", authData);

            return docInfo;
        }

        public bool IsOrgRegistered(string inn)
        {
            var authData = new Dictionary<string, string>();
            authData.Add("Authorization", $"Bearer {_token}");

            var orgRegisteredInfo = _webService.GetRequest<Models.OrgRegistrationResponse[]>($"{Properties.Settings.Default.UrlAddressHonestMark}/participants?inns={inn}", authData);
            return orgRegisteredInfo.FirstOrDefault()?.IsRegistered ?? false;
        }
    }
}
