﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cryptography.WinApi;
using EdiProcessingUnit.Edo.Models;
using System.Security.Cryptography.X509Certificates;

namespace SendEdoDocumentsProcessingUnit.Utils
{
    public class XmlCertificateUtil
    {
        private UtilitesLibrary.Logger.UtilityLog _log = UtilitesLibrary.Logger.UtilityLog.GetInstance();
        private System.Net.WebClient _client = null;
        private List<KeyValuePair<string, Org.BouncyCastle.X509.X509Crl>> _listOfRevoke = new List<KeyValuePair<string, Org.BouncyCastle.X509.X509Crl>>();

        public XmlCertificateUtil()
        {

        }

        public XmlCertificateUtil(System.Net.WebClient client)
        {
            _client = client;
        }

        public Diadoc.Api.Proto.Events.Message SignAndSend(X509Certificate2 signerCertificate,
            Kontragent sender, Kontragent receiver,
            List<object> documents)
        {
            _log.Log("SignAndSend: отправка с подписанием.");
            if (documents == null || documents.Count == 0)
                throw new Exception("Не указаны отправляемые документы.");

            if (receiver == null)
                throw new Exception("Не выбран контрагент.");

            Diadoc.Api.Proto.Events.PowerOfAttorneyToPost powerOfAttorneyToPost = null;

            if (!string.IsNullOrEmpty(sender.EmchdId))
                powerOfAttorneyToPost = new Diadoc.Api.Proto.Events.PowerOfAttorneyToPost
                {
                    UseDefault = false,
                    FullId = new Diadoc.Api.Proto.PowersOfAttorney.PowerOfAttorneyFullId
                    {
                        RegistrationNumber = sender.EmchdId,
                        IssuerInn = sender.Inn
                    }
                };

            var crypt = new WinApiCryptWrapper(signerCertificate);
            Diadoc.Api.Proto.Events.Message message = null;
            var contents = new List<Diadoc.Api.Proto.Events.SignedContent>();

            foreach (var document in documents)
            {
                if (!string.IsNullOrEmpty(receiver.FnsParticipantId))
                {
                    if (document as Diadoc.Api.DataXml.Utd820.Hyphens.UniversalTransferDocumentWithHyphens != null)
                    {
                        var orgData = (document as Diadoc.Api.DataXml.Utd820.Hyphens.UniversalTransferDocumentWithHyphens).Buyers?.FirstOrDefault()?.Item as Diadoc.Api.DataXml.Utd820.Hyphens.ExtendedOrganizationDetails;

                        if (orgData != null)
                            orgData.FnsParticipantId = receiver.FnsParticipantId;
                    }
                    else if (document as Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_02_01.UniversalTransferDocument != null)
                    {
                        var orgData = (document as Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_02_01.UniversalTransferDocument).Buyers?.FirstOrDefault()?.Item as Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_02_01.ExtendedOrganizationDetailsUtd970;

                        if (orgData != null)
                            orgData.FnsParticipantId = receiver.FnsParticipantId;
                    }
                }

                var generatedFile = GetGeneratedFile(document);

                var content = new Diadoc.Api.Proto.Events.SignedContent
                {
                    Content = generatedFile.Content
                };

                byte[] signature = crypt.Sign(generatedFile.Content, true);
                content.Signature = signature;

                contents.Add(content);
            }

            message = EdiProcessingUnit.Edo.Edo.GetInstance().SendXmlDocument(sender.OrgId,
                receiver.OrgId, false,
                contents, "СЧФДОП", powerOfAttorneyToPost);


            return message;
        }

        public async Task<Diadoc.Api.Proto.Events.Message> SignAndSendAsync(X509Certificate2 signerCertificate,
            Kontragent sender, Kontragent receiver,
            object document)
        {
            _log.Log("SignAndSend: отправка с подписанием.");
            if (document == null)
                throw new Exception("Не указан отправляемый документ.");

            if (receiver == null)
                throw new Exception("Не выбран контрагент.");

            Diadoc.Api.Proto.Events.PowerOfAttorneyToPost powerOfAttorneyToPost = null;

            if (!string.IsNullOrEmpty(sender.EmchdId))
                powerOfAttorneyToPost = new Diadoc.Api.Proto.Events.PowerOfAttorneyToPost
                {
                    UseDefault = false,
                    FullId = new Diadoc.Api.Proto.PowersOfAttorney.PowerOfAttorneyFullId
                    {
                        RegistrationNumber = sender.EmchdId,
                        IssuerInn = sender.Inn
                    }
                };

            var crypt = new WinApiCryptWrapper(signerCertificate);
            Diadoc.Api.Proto.Events.Message message = null;

            if (!string.IsNullOrEmpty(receiver.FnsParticipantId))
            {
                if (document as Diadoc.Api.DataXml.Utd820.Hyphens.UniversalTransferDocumentWithHyphens != null)
                {
                    var orgData = (document as Diadoc.Api.DataXml.Utd820.Hyphens.UniversalTransferDocumentWithHyphens).Buyers?.FirstOrDefault()?.Item as Diadoc.Api.DataXml.Utd820.Hyphens.ExtendedOrganizationDetails;

                    if (orgData != null)
                        orgData.FnsParticipantId = receiver.FnsParticipantId;
                }
                else if (document as Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_02_01.UniversalTransferDocument != null)
                {
                    var orgData = (document as Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_02_01.UniversalTransferDocument).Buyers?.FirstOrDefault()?.Item as Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_02_01.ExtendedOrganizationDetailsUtd970;

                    if (orgData != null)
                        orgData.FnsParticipantId = receiver.FnsParticipantId;
                }
            }

            var generatedFile = await GetGeneratedFileAsync(document);

            var content = new Diadoc.Api.Proto.Events.SignedContent
            {
                Content = generatedFile.Content
            };

            byte[] signature = crypt.Sign(generatedFile.Content, true);
            content.Signature = signature;

            message = await EdiProcessingUnit.Edo.Edo.GetInstance().SendXmlDocumentAsync(sender.OrgId,
                receiver.OrgId, false,
                content, "СЧФДОП", powerOfAttorneyToPost);


            return message;
        }

        public async Task<Diadoc.Api.Proto.Events.GeneratedFile> GetGeneratedFileAsync(object document)
        {
            string version = null;

            if (document as Diadoc.Api.DataXml.Utd820.Hyphens.UniversalTransferDocumentWithHyphens != null)
                version = "utd820_05_01_01_hyphen";
            else if (document as Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_02_01.UniversalTransferDocument != null)
                version = "utd970_05_02_01";

            return await EdiProcessingUnit.Edo.Edo.GetInstance().GenerateTitleXmlAsync("UniversalTransferDocument",
                    "СЧФДОП", version, 0, document);
        }

        public Diadoc.Api.Proto.Events.GeneratedFile GetGeneratedFile(object document)
        {
            string version = null;

            if(document as Diadoc.Api.DataXml.Utd820.Hyphens.UniversalTransferDocumentWithHyphens != null)
                version = "utd820_05_01_01_hyphen";
            else if (document as Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_02_01.UniversalTransferDocument != null)
                version = "utd970_05_02_01";

            return EdiProcessingUnit.Edo.Edo.GetInstance().GenerateTitleXml("UniversalTransferDocument",
                    "СЧФДОП", version, 0, document);
        }

        public string ParseCertAttribute(string certData, string attributeName)
        {
            string result = String.Empty;
            try
            {
                if (certData == null || certData == "") return result;

                attributeName = attributeName + "=";

                if (!certData.Contains(attributeName)) return result;

                int start = certData.IndexOf(attributeName);

                if (start > 0 && !certData.Substring(0, start).EndsWith(" "))
                {
                    attributeName = " " + attributeName;

                    if (!certData.Contains(attributeName)) return result;
                }

                start = certData.IndexOf(attributeName) + attributeName.Length;

                int length = certData.IndexOf('=', start) == -1 ? certData.Length - start : certData.IndexOf(", ", start) - start;

                if (length == 0) return result;
                if (length > 0)
                {
                    result = certData.Substring(start, length);

                }
                else
                {
                    result = certData.Substring(start);
                }
                return result;

            }
            catch (Exception)
            {
                return result;
            }
        }

        public string GetOrgInnFromCertificate(X509Certificate2 certificate)
        {
            var inn = ParseCertAttribute(certificate.Subject, "ИНН").TrimStart('0');

            if (string.IsNullOrEmpty(inn) || inn.Length == 12)
            {
                var crypt = new WinApiCryptWrapper(certificate);
                inn = crypt.GetValueBySubjectOid("1.2.643.100.4");
            }

            return inn;
        }

        /// <summary>
        /// Проверка сертификата на валидность, и что он не содержится в списках отзыва
        /// </summary>
        /// <param name="certificate"></param>
        /// <returns></returns>
        public bool IsCertificateValid(X509Certificate2 certificate)
        {
            _log.Log($"IsCertificateValid : проверка на валидность сертификата с серийным номером {certificate.SerialNumber}");
            var cert = Org.BouncyCastle.Security.DotNetUtilities.FromX509Certificate(certificate);

            if (!cert.IsValidNow)
                return false;

            if (_client == null)
                return true;

            _log.Log($"Проверка наличия сертификата в списках отзыва");
            var crypto = new WinApiCryptWrapper(certificate);

            var references = crypto.GetCrlReferences();
            bool isCertRevoked = false;

            foreach (var reference in references)
            {
                if (string.IsNullOrEmpty(reference))
                    continue;

                if (isCertRevoked)
                    continue;

                Org.BouncyCastle.X509.X509Crl crl = null;

                try
                {
                    if (_listOfRevoke.Exists(l => l.Key == reference))
                    {
                        crl = _listOfRevoke.First(l => l.Key == reference).Value;
                        isCertRevoked = isCertRevoked || crl.IsRevoked(cert);
                    }
                    else
                    {
                        var bytes = _client.DownloadData(reference);
                        isCertRevoked = isCertRevoked || crypto.IsCertRevoked(bytes, out crl);

                        if (crl != null)
                            _listOfRevoke.Add(new KeyValuePair<string, Org.BouncyCastle.X509.X509Crl>(reference, crl));
                    }
                }
                catch
                {
                    continue;
                }
            }

            _log.Log($"Результат проверки на наличие в списках отзыва - {isCertRevoked.ToString()}");
            return !isCertRevoked;
        }
    }
}
