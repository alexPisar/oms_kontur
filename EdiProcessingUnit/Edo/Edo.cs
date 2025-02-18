using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UtilitesLibrary.Logger;
using Diadoc;
using Diadoc.Api;
using Diadoc.Api.Http;
using Diadoc.Api.Proto;
using Diadoc.Api.DataXml;
using Diadoc.Api.Proto.Events;
using Diadoc.Api.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using Diadoc.Api.DataXml.Utd820.Hyphens;
using DataContextManagementUnit.DataAccess.Contexts.Abt;
using DataContextManagementUnit.DataAccess.Contexts.Edi;

namespace EdiProcessingUnit.Edo
{
	public class Edo
	{
		private const string TokenFileName = "edocache";
		private UtilityLog _log = UtilityLog.GetInstance();
		private EdoTokenCache _cache { get; set; }
		private readonly UtilitesLibrary.ConfigSet.Config _config = UtilitesLibrary.ConfigSet.Config.GetInstance();
		private string _authToken => _cache.Token ?? "";
		private string _partyId => _cache.PartyId ?? "";
        private string _orgInn;
        private string _actualBoxId;
        private string _actualBoxIdGuid;
		private DiadocHttpApi _api;
		private X509Certificate2 _certificate;


        public Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocument GetUniversalTransferDocument(
            Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationDetails_ManualFilling_Utd970 seller,
            Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationDetails_ManualFilling_Utd970 buyer,
            string documentDate,
            string documentNumber,
            Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocumentFunction documentFunction,
            Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.TransferInfo transferInfo,
            Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTable invoiceTable,
            Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Signers signers,
            decimal currencyRate,
            string currency,
            string documentCreator)
        {
            var universalTransferDocument = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocument
            {
                Function = documentFunction,
                DocumentDate = documentDate,
                DocumentNumber = documentNumber,
                Currency = currency,
                DocumentCreator = documentCreator,
                Sellers = new[]
                {
                    new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationInfoUtd970
                    {
                        Item = seller
                    }
                },
                Buyers = new[]
                {
                    new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationInfoUtd970
                    {
                        Item = buyer
                    }
                },
                TransferInfo = transferInfo,
                Table = invoiceTable
            };

            universalTransferDocument.Signers = signers;

            return universalTransferDocument;
        }

        public GeneratedFile GenerateTitleXml(string typeNameId,
            string function,
            string version,
            int titleIndex,
            object userDocument,
            string letterId = null, string documentId = null)
        {
            byte[] userDataContract = null;

            if (userDocument as UniversalTransferDocumentWithHyphens != null)
                userDataContract = ((UniversalTransferDocumentWithHyphens)userDocument).SerializeToXml();
            else if (userDocument as Diadoc.Api.DataXml.Utd820.UniversalTransferDocument != null)
                userDataContract = ((Diadoc.Api.DataXml.Utd820.UniversalTransferDocument)userDocument).SerializeToXml();
            else if (userDocument as Diadoc.Api.DataXml.Utd820.UniversalTransferDocumentBuyerTitle != null)
                userDataContract = ((Diadoc.Api.DataXml.Utd820.UniversalTransferDocumentBuyerTitle)userDocument).SerializeToXml();
            else if (userDocument as Diadoc.Api.DataXml.Ucd736.UniversalCorrectionDocument != null)
                userDataContract = ((Diadoc.Api.DataXml.Ucd736.UniversalCorrectionDocument)userDocument).SerializeToXml();
            else if(userDocument as Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocument != null)
            {
                var signersInfo = (userDocument as Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocument)?.Signers;

                if(signersInfo != null)
                {
                    if(!string.IsNullOrEmpty(_actualBoxIdGuid))
                        signersInfo.BoxId = _actualBoxIdGuid;

                    var signerInfo = signersInfo?.Signer?.FirstOrDefault();

                    if (signerInfo != null && _certificate != null)
                        signerInfo.Certificate = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Certificate
                        {
                            CertificateThumbprint = _certificate.Thumbprint
                        };
                }

                userDataContract = ((Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocument)userDocument).SerializeToXml();
            }
            else if(userDocument as Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.UniversalTransferDocumentBuyerTitle != null)
            {
                var signersInfo = (userDocument as Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.UniversalTransferDocumentBuyerTitle)?.Signers;

                if (signersInfo != null)
                {
                    if (!string.IsNullOrEmpty(_actualBoxIdGuid))
                        signersInfo.BoxId = _actualBoxIdGuid;

                    var signerInfo = signersInfo?.Signer?.FirstOrDefault();

                    if (signerInfo != null && _certificate != null)
                        signerInfo.Certificate = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.Certificate
                        {
                            CertificateThumbprint = _certificate.Thumbprint
                        };
                }

                userDataContract = ((Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.UniversalTransferDocumentBuyerTitle)userDocument).SerializeToXml();
            }
            else throw new Exception("Неопределённый тип документа");

            return _api.GenerateTitleXml(_authToken,
                _actualBoxId,
                typeNameId,
                function,
                version,
                titleIndex,
                userDataContract, false, null, 
                letterId, documentId);
        }

        public async Task<GeneratedFile> GenerateTitleXmlAsync(string typeNameId,
            string function,
            string version,
            int titleIndex,
            object userDocument,
            string letterId = null, string documentId = null)
        {
            byte[] userDataContract = null;

            if (userDocument as UniversalTransferDocumentWithHyphens != null)
                userDataContract = ((UniversalTransferDocumentWithHyphens)userDocument).SerializeToXml();
            else if (userDocument as Diadoc.Api.DataXml.Utd820.UniversalTransferDocument != null)
                userDataContract = ((Diadoc.Api.DataXml.Utd820.UniversalTransferDocument)userDocument).SerializeToXml();
            else if (userDocument as Diadoc.Api.DataXml.Utd820.UniversalTransferDocumentBuyerTitle != null)
                userDataContract = ((Diadoc.Api.DataXml.Utd820.UniversalTransferDocumentBuyerTitle)userDocument).SerializeToXml();
            else if (userDocument as Diadoc.Api.DataXml.Ucd736.UniversalCorrectionDocument != null)
                userDataContract = ((Diadoc.Api.DataXml.Ucd736.UniversalCorrectionDocument)userDocument).SerializeToXml();
            else if (userDocument as Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocument != null)
            {
                var signersInfo = (userDocument as Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocument)?.Signers;

                if (signersInfo != null)
                {
                    if (!string.IsNullOrEmpty(_actualBoxIdGuid))
                        signersInfo.BoxId = _actualBoxIdGuid;

                    var signerInfo = signersInfo?.Signer?.FirstOrDefault();

                    if (signerInfo != null && _certificate != null)
                        signerInfo.Certificate = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Certificate
                        {
                            CertificateThumbprint = _certificate.Thumbprint
                        };
                }
                userDataContract = ((Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocument)userDocument).SerializeToXml();
            }
            else if (userDocument as Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.UniversalTransferDocumentBuyerTitle != null)
            {
                var signersInfo = (userDocument as Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.UniversalTransferDocumentBuyerTitle)?.Signers;

                if (signersInfo != null)
                {
                    if (!string.IsNullOrEmpty(_actualBoxIdGuid))
                        signersInfo.BoxId = _actualBoxIdGuid;

                    var signerInfo = signersInfo?.Signer?.FirstOrDefault();

                    if (signerInfo != null && _certificate != null)
                        signerInfo.Certificate = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.Certificate
                        {
                            CertificateThumbprint = _certificate.Thumbprint
                        };
                }

                userDataContract = ((Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.UniversalTransferDocumentBuyerTitle)userDocument).SerializeToXml();
            }
            else throw new Exception("Неопределённый тип документа");

            return await _api.GenerateTitleXmlAsync(_authToken,
                _actualBoxId,
                typeNameId,
                function,
                version,
                titleIndex,
                userDataContract, false, null,
                letterId, documentId);
        }

        public Message SendXmlDocument(string senderOrgId, 
            string recipientOrgId,
            bool isOurRecipient,
            List<SignedContent> contents,
            string function,
            PowerOfAttorneyToPost powerOfAttorneyToPost = null,
            string comment=null, 
            string customDocumentId = null)
        {
            OrganizationList myOrganizations = CallApiSafe(new Func<OrganizationList>(() => _api.GetMyOrganizations(_authToken, false)));

            if ((myOrganizations?.Organizations?.Count ?? 0) == 0)
                throw new Exception("Не найдены свои организации по токену.");

            var senderOrganization = myOrganizations.Organizations.FirstOrDefault(o => o.OrgId == senderOrgId);

            if(senderOrganization == null)
                throw new Exception($"Не найдена организация с ID отправителя {senderOrgId}");

            Organization recipientOrganization;

            if (isOurRecipient)
            {
                recipientOrganization = myOrganizations.Organizations.FirstOrDefault(o => o.OrgId == recipientOrgId);

                if (recipientOrganization == null)
                    throw new Exception($"Не найдена своя организация-отправитель с ID {recipientOrgId}");
            }
            else
            {
                var counteragents = GetKontragents(senderOrgId);

                counteragents = counteragents.Where(c => c.Organization?.OrgId == recipientOrgId)?
                    .ToList() ?? new List<Counteragent>();

                if (counteragents.Count == 0)
                    throw new Exception("Не найдены контрагенты");

                var counteragent = counteragents.First();

                if (counteragent?.Organization == null)
                    throw new Exception("Не найдена организация");

                recipientOrganization = counteragent.Organization;
            }

            return SendDocumentAttachment(senderOrganization.Boxes.First().BoxId, recipientOrganization.Boxes.First().BoxId, "UniversalTransferDocument", function, "utd970_05_03_01",
                contents, comment, customDocumentId, powerOfAttorneyToPost);
        }

        public Message SendDocumentAttachment(string ourBoxId, string counteragentBoxId, string typeNameId, string function, string version,
            List<SignedContent> contents, string comment = null, string customDocumentId = null, PowerOfAttorneyToPost powerOfAttorneyToPost = null,
            DocumentId initialDocumentId = null)
        {
            var messageToPost = new MessageToPost
            {
                FromBoxId = ourBoxId ?? _actualBoxId,
                ToBoxId = counteragentBoxId
            };

            Diadoc.Api.Proto.PowersOfAttorney.PowerOfAttorneyPrevalidateResult powerOfAttorneyStatus = null;
            if (powerOfAttorneyToPost != null)
            {
                powerOfAttorneyStatus = CallApiSafe(new Func<Diadoc.Api.Proto.PowersOfAttorney.PowerOfAttorneyPrevalidateResult>(() =>
                {
                    return _api.PrevalidatePowerOfAttorney(_authToken, ourBoxId ?? _actualBoxId,
                        powerOfAttorneyToPost.FullId.RegistrationNumber, powerOfAttorneyToPost.FullId.IssuerInn,
                        new Diadoc.Api.Proto.PowersOfAttorney.PowerOfAttorneyPrevalidateRequest
                        {
                            ConfidantCertificate = new Diadoc.Api.Proto.PowersOfAttorney.ConfidantCertificateToPrevalidate
                            {
                                Content = new Content_v3
                                {
                                    Content = _certificate.RawData
                                }
                            }
                        });
                }));

                if (powerOfAttorneyStatus.PrevalidateStatus.StatusNamedId != Diadoc.Api.Proto.PowersOfAttorney.PowerOfAttorneyValidationStatusNamedId.IsValid)
                {
                    if (powerOfAttorneyStatus.PrevalidateStatus.StatusNamedId == Diadoc.Api.Proto.PowersOfAttorney.PowerOfAttorneyValidationStatusNamedId.IsNotValid ||
                        powerOfAttorneyStatus.PrevalidateStatus.StatusNamedId == Diadoc.Api.Proto.PowersOfAttorney.PowerOfAttorneyValidationStatusNamedId.ValidationError)
                    {
                        var errorText = $"Статус доверенности некорректный: {powerOfAttorneyStatus.PrevalidateStatus.StatusText} \r\n";

                        if ((powerOfAttorneyStatus.PrevalidateStatus.Errors?.Count ?? 0) > 0)
                        {
                            errorText = errorText + "Список ошибок:\r\n";

                            foreach (var error in powerOfAttorneyStatus.PrevalidateStatus.Errors)
                            {
                                errorText = errorText + $"Описание:{error.Text}, код:{error.Code}\r\n";
                            }
                        }

                        throw new Exception(errorText);
                    }
                    else
                    {
                        if ((powerOfAttorneyStatus.PrevalidateStatus.Errors?.Count ?? 0) > 0)
                        {
                            var errorText = "Список ошибок:\r\n";

                            foreach (var error in powerOfAttorneyStatus.PrevalidateStatus.Errors)
                            {
                                errorText = errorText + $"Описание:{error.Text}, код:{error.Code}\r\n";
                            }
                            throw new Exception(errorText);
                        }
                    }
                }
            }

            foreach (var content in contents)
            {
                var documentAttachment = new DocumentAttachment
                {
                    TypeNamedId = typeNameId,
                    Function = function,
                    Version = version,
                    SignedContent = content
                };

                if (powerOfAttorneyStatus != null && powerOfAttorneyToPost != null)
                    if (powerOfAttorneyStatus.PrevalidateStatus.StatusNamedId == Diadoc.Api.Proto.PowersOfAttorney.PowerOfAttorneyValidationStatusNamedId.IsValid)
                        documentAttachment.SignedContent.PowerOfAttorney = powerOfAttorneyToPost;

                documentAttachment.Comment = comment;
                documentAttachment.CustomDocumentId = customDocumentId;

                if (initialDocumentId != null)
                    documentAttachment.InitialDocumentIds.Add(initialDocumentId);

                messageToPost.DocumentAttachments.Add(documentAttachment);
            }
            return CallApiSafe(new Func<Message>(()=> { return _api.PostMessage(_authToken, messageToPost); }));
        }

        public async Task<Message> SendXmlDocumentAsync(string senderOrgId,
            string recipientOrgId,
            bool isOurRecipient,
            SignedContent content,
            string function,
            PowerOfAttorneyToPost powerOfAttorneyToPost = null)
        {
            //MessageToPost postMessage = null;

            //postMessage = await Task.Run(async () =>
            //{
                OrganizationList myOrganizations = await CallApiSafeAsync(new Func<Task<OrganizationList>>(async () => await _api.GetMyOrganizationsAsync(_authToken, false)));

                if ((myOrganizations?.Organizations?.Count ?? 0) == 0)
                    throw new Exception("Не найдены свои организации по токену.");

                var senderOrganization = myOrganizations.Organizations.FirstOrDefault(o => o.OrgId == senderOrgId);

                if (senderOrganization == null)
                    throw new Exception($"Не найдена организация с ID отправителя {senderOrgId}");

                var senderBox = senderOrganization?.Boxes?.First();
                string senderBoxId = senderBox?.BoxId ?? _actualBoxId;
                string senderBoxIdGuid = senderBox?.BoxIdGuid ?? _actualBoxId.Substring(0, _actualBoxId.IndexOf('@'));

                Organization recipientOrganization;

                if (isOurRecipient)
                {
                    recipientOrganization = myOrganizations.Organizations.FirstOrDefault(o => o.OrgId == recipientOrgId);

                    if (recipientOrganization == null)
                        throw new Exception($"Не найдена своя организация-отправитель с ID {recipientOrgId}");
                }
                else
                {
                    var counteragents = await GetKontragentsAsync(senderBoxIdGuid, myOrganizations);

                    counteragents = counteragents.Where(c => c.Organization?.OrgId == recipientOrgId)?
                        .ToList() ?? new List<Counteragent>();

                    if (counteragents.Count == 0)
                        throw new Exception("Не найдены контрагенты");

                    var counteragent = counteragents.First();

                    if (counteragent?.Organization == null)
                        throw new Exception("Не найдена организация");

                    recipientOrganization = counteragent.Organization;
                }

                var messageToPost = new MessageToPost
                {
                    FromBoxId = senderBoxId,
                    ToBoxId = recipientOrganization?.Boxes?.First().BoxId
                };

                string typeNameId = "UniversalTransferDocument";
                string version = "utd970_05_03_01";

                Diadoc.Api.Proto.PowersOfAttorney.PowerOfAttorneyPrevalidateResult powerOfAttorneyStatus = null;
                if (powerOfAttorneyToPost != null)
                {
                    powerOfAttorneyStatus = await CallApiSafeAsync(new Func<Task<Diadoc.Api.Proto.PowersOfAttorney.PowerOfAttorneyPrevalidateResult>>(async () =>
                    {
                        return await _api.PrevalidatePowerOfAttorneyAsync(_authToken, messageToPost.FromBoxId,
                            powerOfAttorneyToPost.FullId.RegistrationNumber, powerOfAttorneyToPost.FullId.IssuerInn,
                            new Diadoc.Api.Proto.PowersOfAttorney.PowerOfAttorneyPrevalidateRequest
                            {
                                ConfidantCertificate = new Diadoc.Api.Proto.PowersOfAttorney.ConfidantCertificateToPrevalidate
                                {
                                    Content = new Content_v3
                                    {
                                        Content = _certificate.RawData
                                    }
                                }
                            });
                    }));

                    if (powerOfAttorneyStatus.PrevalidateStatus.StatusNamedId != Diadoc.Api.Proto.PowersOfAttorney.PowerOfAttorneyValidationStatusNamedId.IsValid)
                    {
                        if (powerOfAttorneyStatus.PrevalidateStatus.StatusNamedId == Diadoc.Api.Proto.PowersOfAttorney.PowerOfAttorneyValidationStatusNamedId.IsNotValid ||
                            powerOfAttorneyStatus.PrevalidateStatus.StatusNamedId == Diadoc.Api.Proto.PowersOfAttorney.PowerOfAttorneyValidationStatusNamedId.ValidationError)
                        {
                            var errorText = $"Статус доверенности некорректный: {powerOfAttorneyStatus.PrevalidateStatus.StatusText} \r\n";

                            if ((powerOfAttorneyStatus.PrevalidateStatus.Errors?.Count ?? 0) > 0)
                            {
                                errorText = errorText + "Список ошибок:\r\n";

                                foreach (var error in powerOfAttorneyStatus.PrevalidateStatus.Errors)
                                {
                                    errorText = errorText + $"Описание:{error.Text}, код:{error.Code}\r\n";
                                }
                            }

                            throw new Exception(errorText);
                        }
                        else
                        {
                            if ((powerOfAttorneyStatus.PrevalidateStatus.Errors?.Count ?? 0) > 0)
                            {
                                var errorText = "Список ошибок:\r\n";

                                foreach (var error in powerOfAttorneyStatus.PrevalidateStatus.Errors)
                                {
                                    errorText = errorText + $"Описание:{error.Text}, код:{error.Code}\r\n";
                                }
                                throw new Exception(errorText);
                            }
                        }
                    }
                }

                var documentAttachment = new DocumentAttachment
                {
                    TypeNamedId = typeNameId,
                    Function = function,
                    Version = version,
                    SignedContent = content
                };

                if (powerOfAttorneyStatus != null && powerOfAttorneyToPost != null)
                    if (powerOfAttorneyStatus.PrevalidateStatus.StatusNamedId == Diadoc.Api.Proto.PowersOfAttorney.PowerOfAttorneyValidationStatusNamedId.IsValid)
                        documentAttachment.SignedContent.PowerOfAttorney = powerOfAttorneyToPost;

                messageToPost.DocumentAttachments.Add(documentAttachment);
            //    return messageToPost;
            //});
            return await CallApiSafeAsync(new Func<Task<Message>>(async () => { return await _api.PostMessageAsync(_authToken, messageToPost); }));
        }

        public List<Counteragent> GetKontragents(string orgId = null)
        {
            CounteragentList list;
            List<Counteragent> result = new List<Counteragent>();

            string currentIndexKey = null;

            do
            {
                if (string.IsNullOrEmpty(orgId))
                {
                    OrganizationList MyOrganizations = CallApiSafe(new Func<OrganizationList>(() => _api.GetMyOrganizations(_authToken, false)));
                    Organization myOrganization = MyOrganizations.Organizations.First();

                    list = CallApiSafe(new Func<CounteragentList>(() => { return _api.GetCounteragents(_authToken, myOrganization.OrgId, "IsMyCounteragent", currentIndexKey); }));
                }
                else
                {
                    list = CallApiSafe(new Func<CounteragentList>(() => { return _api.GetCounteragents(_authToken, orgId, "IsMyCounteragent", currentIndexKey); }));
                }

                currentIndexKey = null;

                if (list?.Counteragents != null)
                {
                    result.AddRange(list.Counteragents.Where(l => l.Organization?.Inn != null && !result
                    .Exists(r => r.Organization.Inn == l.Organization.Inn && r.Organization.Kpp == l.Organization.Kpp)));

                    if(list.Counteragents.Count >= 100)
                        currentIndexKey = list.Counteragents.Last().IndexKey;
                }
            }
            while (!string.IsNullOrEmpty(currentIndexKey));

            return result;
        }

        public async Task<List<Counteragent>> GetKontragentsAsync(string boxId = null, OrganizationList MyOrganizations = null)
        {
            List<Counteragent> result = new List<Counteragent>();

            string currentIndexKey = null;

            if (string.IsNullOrEmpty(boxId))
            {
                if (MyOrganizations == null)
                    MyOrganizations = await CallApiSafeAsync(new Func<Task<OrganizationList>>(async () => await _api.GetMyOrganizationsAsync(_authToken, false)));

                Organization myOrganization = MyOrganizations.Organizations.First();
                boxId = myOrganization?.Boxes?.First().BoxIdGuid ?? _actualBoxId.Substring(0, _actualBoxId.IndexOf('@'));
            }

            do
            {
                var list = await CallApiSafeAsync(new Func<Task<CounteragentList>>(async () => { return await _api.GetCounteragentsV3Async(_authToken, boxId, "IsMyCounteragent", currentIndexKey); }));

                currentIndexKey = null;

                if (list?.Counteragents != null)
                {
                    result.AddRange(list.Counteragents.Where(l => l.Organization?.Inn != null && !result
                    .Exists(r => r.Organization.Inn == l.Organization.Inn && r.Organization.Kpp == l.Organization.Kpp)));

                    if (list.Counteragents.Count >= 100)
                        currentIndexKey = list.Counteragents.Last().IndexKey;
                }
            }
            while (!string.IsNullOrEmpty(currentIndexKey));

            return result;
        }

        public List<Models.Kontragent> GetOrganizations(string orgId = null)
        {
            var counterAgents = new List<Counteragent>();

            if (string.IsNullOrEmpty(orgId))
                counterAgents = GetKontragents();
            else
                counterAgents = GetKontragents(orgId);

            List<Models.Kontragent> organizations = new List<Models.Kontragent>();

            foreach (var counteragent in counterAgents)
            {
                Models.Kontragent organization = new Models.Kontragent(counteragent.Organization?.FullName ?? "", 
                    counteragent?.Organization?.Inn ?? "", 
                    counteragent.Organization.Kpp);

                organization.OrgId = counteragent?.Organization?.OrgId ?? "";
                organization.Address = counteragent?.Organization?.Address;
                organization.FnsParticipantId = counteragent?.Organization?.FnsParticipantId;
                organizations.Add(organization);
            }

            return organizations;
        }

        public Models.Kontragent GetKontragentByInnKpp(string inn, string kpp = null, bool isNotRoaming = true)
        {
            var organizations = CallApiSafe(new Func<OrganizationList>(() => _api.GetOrganizationsByInnKpp(inn, kpp)));

            Organization organization = null;

            if(isNotRoaming)
                organization = organizations?.Organizations?.FirstOrDefault(o => !o.IsRoaming);
            else
                organization = organizations?.Organizations?.FirstOrDefault();

            return new Models.Kontragent(organization?.FullName, organization?.Inn, organization?.Kpp)
            {
                OrgId = organization?.OrgId,
                Address = organization?.Address
            };
        }

        public Diadoc.Api.Proto.Invoicing.Signers.ExtendedSignerDetails GetExtendedSignerDetails(Diadoc.Api.Proto.Invoicing.Signers.DocumentTitleType documentTitleType)
        {
            try
            {
                return _api.GetExtendedSignerDetails(_authToken, _actualBoxId, _certificate.RawData, documentTitleType);
            }
            catch
            {
                return null;
            }
        }

        public Diadoc.Api.Proto.Documents.Document GetDocument(string messageId, string entityId)
        {
            var document = CallApiSafe(new Func<Diadoc.Api.Proto.Documents.Document>(() => _api.GetDocument(_authToken, _actualBoxId, messageId, entityId)));
            return document;
        }

        public Diadoc.Api.Proto.Documents.Types.GetDocumentTypesResponseV2 GetDocumentTypes()
        {
            var docTypes = CallApiSafe(new Func<Diadoc.Api.Proto.Documents.Types.GetDocumentTypesResponseV2>(() => _api.GetDocumentTypesV2(_authToken, _actualBoxId)));
            return docTypes;
        }

        public Message GetMessage(string messageId, string entityId, bool includedContent = false)
        {
            var message = CallApiSafe(new Func<Message>(() => _api.GetMessage(_authToken, _actualBoxId, messageId, entityId, false, includedContent)));
            return message;
        }

        public MessagePatch SendPatchRecipientXmlDocument(string messageId, int docType, IEnumerable<RecipientTitleAttachment> attachments, PowerOfAttorneyToPost powerOfAttorneyToPost = null)
        {
            var messageToPost = new MessagePatchToPost
            {
                BoxId = _actualBoxId,
                MessageId = messageId
            };

            foreach (var recipientAttachment in attachments)
            {
                if (powerOfAttorneyToPost != null)
                    recipientAttachment.SignedContent.PowerOfAttorney = powerOfAttorneyToPost;

                if (docType == (int)DocumentType.UniversalTransferDocument)
                    messageToPost.AddUniversalTransferDocumentBuyerTitle(recipientAttachment);
                else if (docType == (int)DocumentType.XmlTorg12)
                    messageToPost.AddXmlTorg12BuyerTitle(recipientAttachment);
                else if (docType == (int)DocumentType.XmlAcceptanceCertificate)
                    messageToPost.AddXmlAcceptanceCertificateBuyerTitle(recipientAttachment);
            }

            return CallApiSafe(new Func<MessagePatch>(() => { return _api.PostMessagePatch(_authToken, messageToPost); }));
        }

        public async Task<MessagePatch> SendPatchRecipientXmlDocumentAsync(string messageId, int docType, IEnumerable<RecipientTitleAttachment> attachments, PowerOfAttorneyToPost powerOfAttorneyToPost = null)
        {
            var messageToPost = new MessagePatchToPost
            {
                BoxId = _actualBoxId,
                MessageId = messageId
            };

            foreach (var recipientAttachment in attachments)
            {
                if (powerOfAttorneyToPost != null)
                    recipientAttachment.SignedContent.PowerOfAttorney = powerOfAttorneyToPost;

                if (docType == (int)DocumentType.UniversalTransferDocument)
                    messageToPost.AddUniversalTransferDocumentBuyerTitle(recipientAttachment);
                else if (docType == (int)DocumentType.XmlTorg12)
                    messageToPost.AddXmlTorg12BuyerTitle(recipientAttachment);
                else if (docType == (int)DocumentType.XmlAcceptanceCertificate)
                    messageToPost.AddXmlAcceptanceCertificateBuyerTitle(recipientAttachment);
            }

            return await CallApiSafeAsync(new Func<Task<MessagePatch>>(async () => { return await _api.PostMessagePatchAsync(_authToken, messageToPost); }));
        }

        public MessagePatch SendPatchRecipientXmlDocument(string messageId, int docType, string entityId, byte[] content, byte[] signature = null, PowerOfAttorneyToPost powerOfAttorneyToPost = null)
        {
            var recipientAttachment = new RecipientTitleAttachment
            {
                ParentEntityId = entityId,
                SignedContent = new SignedContent
                {
                    Content = content
                }
            };

            if (signature != null)
                recipientAttachment.SignedContent.Signature = signature;

            if (powerOfAttorneyToPost != null)
                recipientAttachment.SignedContent.PowerOfAttorney = powerOfAttorneyToPost;

            var messageToPost = new MessagePatchToPost
            {
                BoxId = _actualBoxId,
                MessageId = messageId
            };

            if (docType == (int)DocumentType.UniversalTransferDocument)
                messageToPost.AddUniversalTransferDocumentBuyerTitle(recipientAttachment);
            else if (docType == (int)DocumentType.XmlTorg12)
                messageToPost.AddXmlTorg12BuyerTitle(recipientAttachment);
            else if (docType == (int)DocumentType.XmlAcceptanceCertificate)
                messageToPost.AddXmlAcceptanceCertificateBuyerTitle(recipientAttachment);

            return CallApiSafe(new Func<MessagePatch>(() => { return _api.PostMessagePatch(_authToken, messageToPost); }));
        }

        public GeneratedFile GenerateRevocationRequestXml(string messageId, string entityId, string text, Diadoc.Api.Proto.Invoicing.Signer signer)
        {
            var revocationRequestInfo = new Diadoc.Api.Proto.Invoicing.RevocationRequestInfo
            {
                Comment = text,
                Signer = signer
            };

            return _api.GenerateRevocationRequestXml(_authToken, _actualBoxId, messageId, entityId, revocationRequestInfo, "revocation_request_02");
        }

        public void SendRevocationDocument(string messageId, string entityId, byte[] fileBytes, byte[] signature, PowerOfAttorneyToPost powerOfAttorneyToPost = null)
        {
            var signatureAttachment = new RevocationRequestAttachment()
            {
                ParentEntityId = entityId,
                SignedContent = new SignedContent
                {
                    Content = fileBytes,
                    Signature = signature
                }
            };

            if (powerOfAttorneyToPost != null)
                signatureAttachment.SignedContent.PowerOfAttorney = powerOfAttorneyToPost;

            var postMessage = new MessagePatchToPost
            {
                BoxId = _actualBoxId,
                MessageId = messageId
            };

            postMessage.AddRevocationRequestAttachment(signatureAttachment);
            var messagePatch = CallApiSafe(new Func<MessagePatch>(() => _api.PostMessagePatch(_authToken, postMessage)));
        }

        public MessagePatch SendPatchSignedDocument(string messageId, string parentEntityId, byte[] signature, PowerOfAttorneyToPost powerOfAttorneyToPost = null)
        {
            var messageToPost = new MessagePatchToPost
            {
                BoxId = _actualBoxId,
                MessageId = messageId
            };

            var documentSignature = new DocumentSignature
            {
                ParentEntityId = parentEntityId,
                Signature = signature
            };

            if (powerOfAttorneyToPost != null)
                documentSignature.PowerOfAttorney = powerOfAttorneyToPost;

            messageToPost.AddSignature(documentSignature);

            return CallApiSafe(new Func<MessagePatch>(() => { return _api.PostMessagePatch(_authToken, messageToPost); }));
        }

        public GeneratedFile GenerateSignatureRejectionXml(string messageId, string entityId, string text, Diadoc.Api.Proto.Invoicing.Signer signer)
        {
            var xmlRejectionInfo = new Diadoc.Api.Proto.Invoicing.SignatureRejectionInfo
            {
                ErrorMessage = text,
                Signer = signer
            };

            return _api.GenerateSignatureRejectionXml(_authToken, _actualBoxId, messageId, entityId, xmlRejectionInfo);
        }

        public void SendRejectionDocument(string messageId, string entityId, byte[] fileBytes, byte[] signature, PowerOfAttorneyToPost powerOfAttorneyToPost = null)
        {
            var signatureRejectionAttachment = new XmlSignatureRejectionAttachment()
            {
                ParentEntityId = entityId,
                SignedContent = new SignedContent
                {
                    Content = fileBytes,
                    Signature = signature
                }
            };

            if (powerOfAttorneyToPost != null)
                signatureRejectionAttachment.SignedContent.PowerOfAttorney = powerOfAttorneyToPost;

            var postMessage = new MessagePatchToPost
            {
                BoxId = _actualBoxId,
                MessageId = messageId
            };

            postMessage.AddXmlSignatureRejectionAttachment(signatureRejectionAttachment);

            var messagePatch = CallApiSafe(new Func<MessagePatch>(() => _api.PostMessagePatch(_authToken, postMessage)));
        }

        /// <summary>
        /// Отправка запроса на регистрацию машиночитаемой доверенности (МЧД)
        /// </summary>
        public string RegisterPowerOfAttorneyByRegNumber(string registrationNumber, string issuerInn)
        {
            var powerOfAttorneyToRegister = new Diadoc.Api.Proto.PowersOfAttorney.PowerOfAttorneyToRegister
            {
                FullId = new Diadoc.Api.Proto.PowersOfAttorney.PowerOfAttorneyFullId
                {
                    RegistrationNumber = registrationNumber,
                    IssuerInn = issuerInn
                }
            };

            var result = CallApiSafe(new Func<AsyncMethodResult>(() => _api.RegisterPowerOfAttorney(_authToken, _actualBoxId, powerOfAttorneyToRegister)));
            return result?.TaskId;
        }

        /// <summary>
        /// Отправка запроса на регистрацию машиночитаемой доверенности (МЧД)
        /// </summary>
        public string RegisterPowerOfAttorneyByFiles(byte[] xmlFileBytes, byte[] signFileBytes)
        {
            var powerOfAttorneyToRegister = new Diadoc.Api.Proto.PowersOfAttorney.PowerOfAttorneyToRegister
            {
                Content = new Diadoc.Api.Proto.PowersOfAttorney.PowerOfAttorneySignedContent
                {
                    Content = new Content_v3
                    {
                        Content = xmlFileBytes
                    },
                    Signature = new Content_v3
                    {
                        Content = signFileBytes
                    }
                }
            };

            var result = CallApiSafe(new Func<AsyncMethodResult>(() => _api.RegisterPowerOfAttorney(_authToken, _actualBoxId, powerOfAttorneyToRegister)));
            return result?.TaskId;
        }

        public Diadoc.Api.Proto.PowersOfAttorney.PowerOfAttorneyRegisterResult RegisterPowerOfAttorneyResult(string taskId)
        {
            var result = CallApiSafe(new Func<Diadoc.Api.Proto.PowersOfAttorney.PowerOfAttorneyRegisterResult>(() => _api.RegisterPowerOfAttorneyResult(_authToken, _actualBoxId, taskId)));
            return result;
        }

        public void SetOrganizationParameters(Models.Kontragent kAgent)
        {
            OrganizationList myOrganizations = CallApiSafe(new Func<OrganizationList>(() => _api.GetMyOrganizations(_authToken, false)));

            Organization organization;

            if (!string.IsNullOrEmpty(kAgent.Kpp))
            {
                organization = myOrganizations.Organizations.FirstOrDefault(o => o.Inn == kAgent.Inn && o.Kpp == kAgent.Kpp);

                if(organization == null)
                    organization = myOrganizations.Organizations.FirstOrDefault(o => o.Inn == kAgent.Inn);
            }
            else
                organization = myOrganizations.Organizations.FirstOrDefault(o => o.Inn == kAgent.Inn);

            kAgent.OrgId = organization.OrgId;
            kAgent.Address = organization.Address;
        }

        private void SetBoxId()
		{
            if(!string.IsNullOrEmpty(_orgInn))
            {
                OrganizationList myOrganizations = CallApiSafe(new Func<OrganizationList>(() => _api.GetMyOrganizations(_authToken, false)));

                var organization = myOrganizations?.Organizations?
                    .FirstOrDefault(k => k.Inn == _orgInn && k.IsRoaming == false);

                _actualBoxId = organization?
                    .Boxes?
                    .FirstOrDefault()?
                    .BoxId;

                _actualBoxIdGuid = organization?
                    .Boxes?
                    .FirstOrDefault()?
                    .BoxIdGuid;
            }
		}

		/// <summary>
		/// Получить токен аутентификации
		/// </summary>
		public bool Authenticate(bool byLogin = false, X509Certificate2 cert = null, string orgInn = null)
		{
            _orgInn = orgInn;

            if (!byLogin)
            {
                if (cert != null)
                    _certificate = cert;
                else
                    _certificate = new X509Certificate2(File.ReadAllBytes(_config.CertFullPath));

                _cache = new EdoTokenCache().Load(_certificate.Thumbprint);
            }
            else
                _cache = new EdoTokenCache().Load( TokenFileName );

			if (_cache != null && !IsTokenExpired)
			{
                SetBoxId();

				return true;
			}
			if (_cache == null || IsTokenExpired)
			{
				string authToken;

                if(byLogin)
                    authToken = (string)CallApiSafe(new Func<object>(() => _api.Authenticate(_config.EdoUserName, _config.EdoUserPassword)));
                else
                    authToken = (string)CallApiSafe(new Func<object>(() => _api.Authenticate(_certificate.RawData)));

                if (string.IsNullOrEmpty( authToken ))
					return false;

                if (byLogin)
                {
                    _cache = new EdoTokenCache(authToken, $"User {_config.EdoUserName}", _cache?.PartyId ?? "");
                    _cache.Save(_cache, TokenFileName);
                }
                else
                {
                    _cache = new EdoTokenCache(authToken, $"Certificate, Serial Number {_certificate.SerialNumber}", _cache?.PartyId ?? "");
                    _cache.Save(_cache, _certificate.Thumbprint);
                }

                SetBoxId();

				return true;
			}
			return false;			
		}

		/// <summary>
		/// Истёк ли токен аутентификации
		/// </summary>
		public bool IsTokenExpired => _cache?.TokenExpirationDate < DateTime.Now;
		private static volatile Edo _instance;
		private static readonly object syncRoot = new object();

		private Edo()
		{
			var hClient = new HttpClient( _config.EdoApiUrl );
			// если edo хочет ходить через прокси - пусть будет так
			if (_config.ProxyEnabled)
			{
				hClient.SetProxyUri( "http://"+_config.ProxyAddress );
				hClient.UseSystemProxy = false;
				hClient.SetProxyCredentials( new NetworkCredential(
					_config.ProxyUserName,
					_config.ProxyUserPassword
				) );
			}
			_api = new DiadocHttpApi( _config.EdoApiClientId, hClient, new WinApiCrypt() );	
		}

		public static Edo GetInstance()
		{
			if (_instance == null)
			{
				lock (syncRoot)
				{
					if (_instance == null)
					{
						_instance = new Edo();
					}
				}
			}
			return _instance;
		}

		private TOut CallApiSafe<TOut>(Func<TOut> CallingDelegate) where TOut : new()
		{
			_log.Log( "SafeCall: " + CallingDelegate.Method.Name );

			TOut ret;
			int tries = 15;
			Exception ex = null;

			while (tries-- >= 0)
			{
				try
				{
					ret = CallingDelegate.Invoke();
					return ret;
				}
                catch (WebException webEx)
                {
                    _log.Error(webEx);
                    ex = webEx;
                }
				catch (Exception e)
				{
					_log.Error( e );
					ex = e;
				}
			}

			if (ex != null)
				throw ex;

			throw new Exception( "метод не получилось вызвать более 15 раз" );
		}

        private async Task<TOut> CallApiSafeAsync<TOut>(Func<Task<TOut>> CallingDelegate) where TOut : new()
        {
            _log.Log("SafeCall: " + CallingDelegate.Method.Name);

            TOut ret;
            int tries = 15;
            Exception ex = null;

            while (tries-- >= 0)
            {
                try
                {
                    ret = await CallingDelegate.Invoke();
                    return ret;
                }
                catch (WebException webEx)
                {
                    _log.Error(webEx);
                    ex = webEx;
                }
                catch (Exception e)
                {
                    _log.Error(e);
                    ex = e;
                }
            }

            if (ex != null)
                throw ex;

            throw new Exception("метод не получилось вызвать более 15 раз");
        }

    }
}
