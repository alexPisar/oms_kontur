using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Cryptography.WinApi;
using System.Security.Cryptography.X509Certificates;
using UtilitesLibrary.Logger;
using EdiProcessingUnit.Edo.Models;
using EdiProcessingUnit.Infrastructure;
using DataContextManagementUnit.DataAccess.Contexts.Abt;

namespace SendEdoDocumentsProcessingUnit.Processors
{
    public class ClientEdoProcessor : EdiProcessingUnit.Infrastructure.EdoProcessor
    {
        private AbtDbContext _abt;
        private Utils.XmlCertificateUtil _utils;
        private List<X509Certificate2> _personalCertificates = null;
        private Dictionary<decimal, Kontragent> _consignors;

        private readonly object locker = new object();

        public ClientEdoProcessor():base()
        {
            var client = new System.Net.WebClient();

            if (_conf.ProxyEnabled)
            {
                var webProxy = new System.Net.WebProxy();

                webProxy.Address = new Uri("http://" + _conf.ProxyAddress);
                webProxy.Credentials = new System.Net.NetworkCredential(_conf.ProxyUserName, _conf.ProxyUserPassword);

                client.Proxy = webProxy;
            }
            _utils = new Utils.XmlCertificateUtil(client);
            _consignors = new Dictionary<decimal, Kontragent>();
        }

        private List<X509Certificate2> GetPersonalCertificates()
        {
            _log.Log("GetPersonalCertificates: загрузка сертификатов из хранилища Личные.");
            var crypto = new WinApiCryptWrapper();

            if (_personalCertificates == null)
                _personalCertificates = crypto.GetAllGostPersonalCertificates()?.Where(c => c.NotAfter > DateTime.Now)?.ToList();

            _log.Log("GetPersonalCertificates: выполнено.");
            return _personalCertificates;
        }

        private int FilterByStatuses(string status)
        {
            switch (status)
            {
                case "0":
                    return 0;
                case "5":
                    return 5;
                case "6":
                    return 6;
                default:
                    return 5;
            };
        }

        public async Task<bool> RunAsync()
        {
            List<string> connectionStringList = new EdiProcessingUnit.UsersConfig().GetAllConnectionStrings();
            var updDocType = (int)EdiProcessingUnit.Enums.DocEdoType.Upd;
            List<X509Certificate2> personalCertificates = GetPersonalCertificates();
            var dataBaseUser = UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser;

            var fileController = new WebService.Controllers.FileController();
            var dateTimeLastPeriod = fileController.GetApplicationConfigParameter<DateTime>("KonturEdo", "DocsDateTime");

            foreach (var connStr in connectionStringList)
            {
                using (_abt = new AbtDbContext(connStr, true))
                {
                    try
                    {
                        _abt.Database.CommandTimeout = 500;
                        var docJournals = _abt.Set<DocJournal>().Include("DocMaster").Include("DocGoodsI").Include("DocGoodsDetailsIs");

                        var orgs = (from a in _abt.RefAuthoritySignDocuments
                                    where a.EmchdEndDate != null && a.IsMainDefault
                                    join c in _abt.RefCustomers
                                    on a.IdCustomer equals (c.Id)
                                    join refUser in _abt.RefUsersByOrgEdo
                                    on c.Id equals refUser.IdCustomer
                                    where refUser.UserName == dataBaseUser
                                    select new Kontragent
                                    {
                                        Name = c.Name,
                                        Inn = c.Inn,
                                        Kpp = c.Kpp,
                                        EmchdId = a.EmchdId,
                                        EmchdBeginDate = a.EmchdBeginDate,
                                        EmchdEndDate = a.EmchdEndDate,
                                        EmchdPersonInn = a.Inn,
                                        EmchdPersonSurname = a.Surname,
                                        EmchdPersonName = a.Name,
                                        EmchdPersonPatronymicSurname = a.PatronymicSurname,
                                        EmchdPersonPosition = a.Position
                                    }).ToList();

                        var orgsInnKpp = orgs.Select(o => new KeyValuePair<string, string>(o.Inn, o.Kpp)).Distinct().ToList();
                        orgsInnKpp.Add(new KeyValuePair<string, string>("2539108495", "253901001"));
                        orgsInnKpp.Add(new KeyValuePair<string, string>("2536090987", "253901001"));

                        foreach (var myOrganization in orgs)
                        {
                            try
                            {
                                var certs = personalCertificates.Where(c => myOrganization.EmchdPersonInn == _utils.ParseCertAttribute(c.Subject, "ИНН").TrimStart('0') && _utils.IsCertificateValid(c)).OrderByDescending(c => c.NotBefore);
                                myOrganization.Certificate = certs.FirstOrDefault(c => string.IsNullOrEmpty(_utils.GetOrgInnFromCertificate(c)));

                                if (!_edo.Authenticate(false, myOrganization.Certificate, myOrganization.Inn))
                                    throw new Exception("Не удалось авторизоваться в системе по сертификату.");

                                var myOrganizationBoxIdGuid = _edo.ActualBoxIdGuid;
                                _edo.SetOrganizationParameters(myOrganization);

                                var signerDetails = _edo.GetExtendedSignerDetails(Diadoc.Api.Proto.Invoicing.Signers.DocumentTitleType.UtdSeller);
                                var counteragents = _edo.GetOrganizations(myOrganizationBoxIdGuid);

                                await SendUniversalTransferDocuments(myOrganization, counteragents, signerDetails, orgsInnKpp);
                                await SendUniversalCorrectionDocuments(myOrganization, counteragents);
                            }
                            catch (Exception ex)
                            {
                                _log.Log(ex);
                                MailReporter.Add(ex, $"Произошла ошибка перед отправкой документов организации {myOrganization.Name}:\r\n");
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        _log.Log(ex);
                        MailReporter.Add(ex);
                    }
                }
            }
            return true;
        }

        private async Task SendUniversalTransferDocuments(Kontragent myOrganization, List<Kontragent> counteragents, Diadoc.Api.Proto.Invoicing.Signers.ExtendedSignerDetails signerDetails, List<KeyValuePair<string, string>> orgsInnKpp)
        {
            var totalDocProcessings = new List<Utils.AsyncOperationEntity<DocEdoProcessing>>();
            IEnumerable<UniversalTransferDocumentV2> docs = GetDocumentsForEdoAutomaticSend(myOrganization);

            int position = 0, block = 10, count = docs.Count();
            docs = docs ?? new List<UniversalTransferDocumentV2>();
            var errors = new List<Utils.AsyncOperationEntity<DocEdoProcessing>>();

            while (count > position)
            {
                var length = count - position > block ? block : count - position;
                var docsFromBlock = docs.Skip(position).Take(length).ToList();

                foreach (var docFromBlock in docsFromBlock.Where(doc => doc.IsMarked))
                {
                    var sendDocComissionEdoProcessingResult = await GetDocComissionEdoProcessingAfterSending(myOrganization, docFromBlock, orgsInnKpp);

                    if(sendDocComissionEdoProcessingResult.Entity != null)
                        docFromBlock.DocComissionEdoProcessings = sendDocComissionEdoProcessingResult.Entity;
                    else
                        docFromBlock.Error = new KeyValuePair<string, Exception>(sendDocComissionEdoProcessingResult.Description, sendDocComissionEdoProcessingResult.Exception);
                }

                var tasks = docsFromBlock.Select((doc) =>
                {
                    var receiver = counteragents.FirstOrDefault(r => r.Inn == doc.BuyerInn);
                    var task = GetDocEdoProcessingAfterSending(myOrganization, receiver, doc, signerDetails);
                    return task;
                });

                var result = await Task.WhenAll(tasks);
                var docProcessings = result.Where(r => r.Entity != null);

                totalDocProcessings.AddRange(docProcessings);
                errors.AddRange(result.Where(r => r.Entity == null));

                position += block;
            }

            foreach (var docProcessingResult in totalDocProcessings)
            {
                var docProcessing = docProcessingResult.Entity;
                if (docProcessing.ComissionDocument != null && !_abt.DocEdoProcessings.Any(d => d.Id == docProcessing.Id))
                    _abt.DocEdoProcessings.Add(docProcessing);
                else if (docProcessing.ComissionDocument == null)
                    _abt.DocEdoProcessings.Add(docProcessing);

                MailReporter.Add(docProcessingResult.Description);
            }

            if (totalDocProcessings.Count > 0)
                _abt.SaveChanges();

            foreach (var error in errors)
            {
                _log.Log(error.Exception);
                MailReporter.Add(error.Exception, $"{error.Description}:\r\n");
            }
        }

        private async Task SendUniversalCorrectionDocuments(Kontragent myOrganization, List<Kontragent> counteragents)
        {
            var totalDocProcessings = new List<Utils.AsyncOperationEntity<DocEdoProcessing>>();
            List<UniversalCorrectionDocumentV2> corrDocs = new List<UniversalCorrectionDocumentV2>();

            var fileController = new WebService.Controllers.FileController();
            var dateTimeLastPeriod = fileController.GetApplicationConfigParameter<DateTime>("KonturEdo", "DocsDateTime");

            var returnCorrDocs = GetCorrectionDocumentsForEdoAutomaticSend(myOrganization, "VIEW_RETURNS_EDO_AUTOMATIC", dateTimeLastPeriod) ?? new List<UniversalCorrectionDocumentV2>();
            corrDocs.AddRange(returnCorrDocs);

            var corrInvoices = GetCorrectionDocumentsForEdoAutomaticSend(myOrganization, "VIEW_CORRECTIONS_EDO_AUTOMATIC", dateTimeLastPeriod) ?? new List<UniversalCorrectionDocumentV2>();
            corrDocs.AddRange(corrInvoices);

            int position = 0, block = 10, count = corrDocs.Count();
            var errors = new List<Utils.AsyncOperationEntity<DocEdoProcessing>>();

            while(count > position)
            {
                var length = count - position > block ? block : count - position;
                var corrDocsFromBlock = corrDocs.Skip(position).Take(length);

                var tasks = corrDocsFromBlock.Select(doc => 
                {
                    var receiver = counteragents.FirstOrDefault(c => c.Inn == doc.BuyerInn);
                    var task = GetDocEdoProcessingAfterSending(myOrganization, receiver, doc);
                    return task;
                });

                var result = await Task.WhenAll(tasks);
                var docProcessings = result.Where(r => r.Entity != null);

                totalDocProcessings.AddRange(docProcessings);
                errors.AddRange(result.Where(r => r.Entity == null));

                position += block;
            }

            foreach (var docProcessingResult in totalDocProcessings)
                MailReporter.Add(docProcessingResult.Description);

            if (totalDocProcessings.Count > 0)
                _abt.SaveChanges();

            foreach(var error in errors)
            {
                _log.Log(error.Exception);
                MailReporter.Add(error.Exception, $"{error.Description}:\r\n");
            }
        }

        private async Task<Utils.AsyncOperationEntity<List<DocComissionEdoProcessing>>> GetDocComissionEdoProcessingAfterSending(Kontragent myOrganization, UniversalTransferDocumentV2 doc, List<KeyValuePair<string, string>> orgsInnKpp = null)
        {
            var operationEntityResult = new Utils.AsyncOperationEntity<List<DocComissionEdoProcessing>>();
            operationEntityResult.Entity = null;
            var docComissionProcessings = new List<DocComissionEdoProcessing>();

            try
            {
                if (!EdiProcessingUnit.HonestMark.HonestMarkClient.GetInstance().Authorization(myOrganization.Certificate, myOrganization))
                    throw new Exception("Не удалось авторизоваться в системе ЧЗ по сертификату.");

                DocComissionEdoProcessing docComissionEdoProcessing = null;
                var labels = (from label in _abt.DocGoodsDetailsLabels where label.IdDocSale == doc.IdDocMaster select label).ToList();
                var docJournal = _abt.DocJournals.FirstOrDefault(j => j.Id == doc.IdDoc);

                if (doc?.Details?
                    .Exists(g => _abt.RefItems.Any(r => r.IdName == 30071 && r.IdGood == g.IdGood && r.Quantity == 1) &&
                    (labels?.Where(l => l.IdGood == g.IdGood)?.Count() ?? 0) < g.Quantity) ?? false)
                {
                    throw new Exception("Для некоторых товаров отсутствует маркировка.");
                }
                else if (doc?.Details?
                    .Exists(g => _abt.RefItems.Any(r => r.IdName == 30071 && r.IdGood == g.IdGood && r.Quantity == 1) &&
                    (labels?.Where(l => l.IdGood == g.IdGood)?.Count() ?? 0) > g.Quantity) ?? false)
                {
                    throw new Exception("В данном документе есть избыток кодов маркировки.");
                }

                if (orgsInnKpp != null)
                {
                    var markedCodesInfo = EdiProcessingUnit.HonestMark.HonestMarkClient.GetInstance()
                        .GetMarkCodesInfo(EdiProcessingUnit.HonestMark.ProductGroupsEnum.None, labels.Select(l => l.DmLabel)?.ToArray())
                        .Where(m => m.CisInfo.OwnerInn != doc.SellerInn);

                    if (markedCodesInfo.Any())
                    {
                        if (markedCodesInfo.Any(m => !orgsInnKpp.Exists(r => r.Key == m.CisInfo.OwnerInn)))
                            throw new Exception("Среди кодов есть не принадлежащие нашей организации.");

                        var markedCodesInfoByOrganizations = markedCodesInfo.GroupBy(m => m.CisInfo.OwnerInn);

                        foreach (var markedCodesInfoByOrganization in markedCodesInfoByOrganizations)
                        {
                            var orgInnKpp = orgsInnKpp.First(o => o.Key == markedCodesInfoByOrganization.Key);

                            var org = GetMyKontragent(orgInnKpp.Key, orgInnKpp.Value);

                            if (org == null)
                                throw new Exception($"Не найдена наша организация с ИНН {orgInnKpp.Key}.");

                            var labelsByDoc = labels.Where(l => markedCodesInfoByOrganization.Any(m => m.CisInfo.Cis == l.DmLabel))?.ToList();

                            docComissionEdoProcessing = await SendComissionDocumentForHonestMark(myOrganization, doc, docJournal, org, labelsByDoc, docComissionProcessings.Count + 1);

                            lock (locker)
                            {
                                _abt.DocComissionEdoProcessings.Add(docComissionEdoProcessing);
                                _abt.SaveChanges();
                            }

                            docComissionProcessings.Add(docComissionEdoProcessing);
                        }
                    }
                }

                operationEntityResult.Entity = docComissionProcessings;
            }
            catch (Exception ex)
            {
                operationEntityResult.SetException(ex, $"Произошла ошибка при отправке комиссионного документа {doc.InvoiceNumber}");
            }

            return operationEntityResult;
        }

        private async Task<Utils.AsyncOperationEntity<DocEdoProcessing>> GetDocEdoProcessingAfterSending(Kontragent myOrganization, Kontragent receiver, UniversalTransferDocumentV2 doc, 
            Diadoc.Api.Proto.Invoicing.Signers.ExtendedSignerDetails signerDetails)
        {
            var operationEntityResult = new Utils.AsyncOperationEntity<DocEdoProcessing>();
            var docComissionProcessings = doc?.DocComissionEdoProcessings ?? new List<DocComissionEdoProcessing>();

            if (doc.IsMarked && doc.Error != null)
            {
                var error = doc.Error.Value;
                operationEntityResult.SetException(error.Value, error.Key);
                return operationEntityResult;
            }

            try
            {
                var universalDocument = await GetUniversalDocumentAsync(doc, myOrganization, signerDetails);

                if (universalDocument == null)
                    throw new Exception("Не удалось сформировать документ.");

                var message = await new Utils.XmlCertificateUtil().SignAndSendAsync(myOrganization.Certificate, myOrganization, receiver, universalDocument);
                DocEdoProcessing docProcessing = null;

                if (message != null)
                {
                    _log.Log($"Сохранение в базе данных документа, Id сообщения {message.MessageId}");

                    var documentNumber = universalDocument.DocumentNumber;
                    var entity = message.Entities.FirstOrDefault(t => t.AttachmentType == Diadoc.Api.Proto.Events.AttachmentType.UniversalTransferDocument &&
                    t?.DocumentInfo?.DocumentNumber == documentNumber);

                    var fileNameLength = entity.FileName.LastIndexOf('.');

                    if (fileNameLength < 0)
                        fileNameLength = entity.FileName.Length;

                    var fileName = entity.FileName.Substring(0, fileNameLength);

                    docProcessing = new DocEdoProcessing
                    {
                        Id = Guid.NewGuid().ToString(),
                        MessageId = message.MessageId,
                        EntityId = entity.EntityId,
                        FileName = fileName,
                        IsReprocessingStatus = 0,
                        IdDoc = doc.IdDocMaster,
                        DocDate = DateTime.Now,
                        UserName = UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser,
                        ReceiverName = receiver.Name,
                        ReceiverInn = receiver.Inn,
                        DocType = (int)EdiProcessingUnit.Enums.DocEdoType.Upd,
                        HonestMarkStatus = doc.IsMarked ? (int)EdiProcessingUnit.HonestMark.DocEdoProcessingStatus.Sent : (int)EdiProcessingUnit.HonestMark.DocEdoProcessingStatus.None
                    };

                    if (docComissionProcessings.Count == 1)
                    {
                        var docComissionEdoProcessing = docComissionProcessings.First();
                        docProcessing.IdComissionDocument = docComissionEdoProcessing.Id;
                        docProcessing.ComissionDocument = docComissionEdoProcessing;
                        docComissionEdoProcessing.MainDocuments.Add(docProcessing);
                    }
                    
                    operationEntityResult.Entity = docProcessing;
                    operationEntityResult.Description = $"Документ {doc?.InvoiceNumber} успешно отправлен и сохранён.";
                }
            }
            catch(Exception ex)
            {
                operationEntityResult.SetException(ex, $"Произошла ошибка при отправке документа {doc.InvoiceNumber}");
            }

            return operationEntityResult;
        }

        private async Task<Utils.AsyncOperationEntity<DocEdoProcessing>> GetDocEdoProcessingAfterSending(Kontragent myOrganization, Kontragent receiver, UniversalCorrectionDocumentV2 doc)
        {
            var operationEntityResult = new Utils.AsyncOperationEntity<DocEdoProcessing>();
            var typeNamedId = "UniversalCorrectionDocument";
            var function = "КСЧФДИС";
            var version = "ucd736_05_01_02";

            try
            {
                if (doc.BaseProcessing == null)
                    throw new Exception($"Не найден основной документ для корректировки {doc.CorrectionNumber}");

                var baseDocument = await EdiProcessingUnit.Edo.Edo.GetInstance().GetDocumentAsync(doc.BaseProcessing.MessageId, doc.BaseProcessing.EntityId);

                if (baseDocument == null)
                    throw new Exception("Не найден базовый документ.");

                var universalCorrectionDocument = await GetUniversalCorrectionDocumentAsync(doc, myOrganization, receiver, baseDocument);

                if (universalCorrectionDocument == null)
                    throw new Exception("Не удалось сформировать корректировочный документ.");

                var generatedFile = await EdiProcessingUnit.Edo.Edo.GetInstance().GenerateTitleXmlAsync(typeNamedId,
                                function, version, 0, universalCorrectionDocument);

                var crypt = new WinApiCryptWrapper(myOrganization.Certificate);
                var signature = crypt.Sign(generatedFile.Content, true);

                Diadoc.Api.Proto.Events.Message message;

                var signedContent = new Diadoc.Api.Proto.Events.SignedContent
                {
                    Content = generatedFile.Content,
                    Signature = signature
                };

                if (!string.IsNullOrEmpty(myOrganization.EmchdId))
                {
                    var powerOfAttorneyToPost = new Diadoc.Api.Proto.Events.PowerOfAttorneyToPost
                    {
                        UseDefault = false,
                        FullId = new Diadoc.Api.Proto.PowersOfAttorney.PowerOfAttorneyFullId
                        {
                            RegistrationNumber = myOrganization.EmchdId,
                            IssuerInn = myOrganization.Inn,
                            RepresentativeInn = myOrganization.EmchdPersonInn
                        }
                    };

                    message = await EdiProcessingUnit.Edo.Edo.GetInstance().SendDocumentAttachmentAsync(null, baseDocument.CounteragentBoxId, typeNamedId, function, version,
                        new List<Diadoc.Api.Proto.Events.SignedContent>(new[] { signedContent }),
                        null, null, powerOfAttorneyToPost,
                        new Diadoc.Api.Proto.DocumentId
                        {
                            MessageId = doc.BaseProcessing.MessageId,
                            EntityId = doc.BaseProcessing.EntityId
                        });
                }
                else
                {
                    message = await EdiProcessingUnit.Edo.Edo.GetInstance().SendDocumentAttachmentAsync(null, baseDocument.CounteragentBoxId, typeNamedId, function, version,
                        new List<Diadoc.Api.Proto.Events.SignedContent>(new[] { signedContent }),
                            null, null, null,
                            new Diadoc.Api.Proto.DocumentId
                            {
                                MessageId = doc.BaseProcessing.MessageId,
                                EntityId = doc.BaseProcessing.EntityId
                            });
                }

                if (message != null)
                {
                    var entity = message.Entities.FirstOrDefault(t => t.AttachmentType == Diadoc.Api.Proto.Events.AttachmentType.UniversalCorrectionDocument);

                    var fileNameLength = entity.FileName.LastIndexOf('.');

                    if (fileNameLength < 0)
                        fileNameLength = entity.FileName.Length;

                    var fileName = entity.FileName.Substring(0, fileNameLength);

                    var newDocEdoProcessing = new DocEdoProcessing
                    {
                        Id = Guid.NewGuid().ToString(),
                        MessageId = message.MessageId,
                        EntityId = entity.EntityId,
                        FileName = fileName,
                        IsReprocessingStatus = 0,
                        IdDoc = doc.IdDoc,
                        DocDate = DateTime.Now,
                        UserName = UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser,
                        ReceiverName = doc.BaseProcessing.ReceiverName,
                        ReceiverInn = doc.BaseProcessing.ReceiverInn,
                        DocType = (int)EdiProcessingUnit.Enums.DocEdoType.Ucd,
                        IdParent = doc.BaseProcessing.Id,
                        Parent = doc.BaseProcessing,
                        HonestMarkStatus = doc.IsMarked ? (int)EdiProcessingUnit.HonestMark.DocEdoProcessingStatus.Sent : (int)EdiProcessingUnit.HonestMark.DocEdoProcessingStatus.None
                    };
                    doc.BaseProcessing.Children.Add(newDocEdoProcessing);

                    operationEntityResult.Entity = newDocEdoProcessing;
                    operationEntityResult.Description = $"Корректировка {doc?.CorrectionNumber} успешно отправлена и сохранена.";
                }
            }
            catch (Exception ex)
            {
                operationEntityResult.SetException(ex, $"Произошла ошибка при отправке корректировки {doc.CorrectionNumber}");
            }

            return operationEntityResult;
        }

        public override void Run()
        {
            List<string> connectionStringList = new EdiProcessingUnit.UsersConfig().GetAllConnectionStrings();
            List<X509Certificate2> personalCertificates = GetPersonalCertificates();

            foreach (var connStr in connectionStringList)
            {
                using (_abt = new AbtDbContext(connStr, true))
                {
                    var fileController = new WebService.Controllers.FileController();
                    var dateTimeFrom = fileController.GetApplicationConfigParameter<DateTime>("KonturEdo", "DocsDateTime");
                    var updDocType = (int)EdiProcessingUnit.Enums.DocEdoType.Upd;

                    var dataBaseUser = UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser;

                    try
                    {
                        var docJournals = _abt.Set<DocJournal>().Include("DocGoodsDetailsIs");

                        var orgs = (from a in _abt.RefAuthoritySignDocuments
                                    where a.EmchdEndDate != null && a.IsMainDefault
                                    join c in _abt.RefCustomers
                                    on a.IdCustomer equals (c.Id)
                                    join refUser in _abt.RefUsersByOrgEdo
                                    on c.Id equals refUser.IdCustomer
                                    where refUser.UserName == dataBaseUser
                                    select new Kontragent
                                    {
                                        Name = c.Name,
                                        Inn = c.Inn,
                                        Kpp = c.Kpp,
                                        EmchdId = a.EmchdId,
                                        EmchdBeginDate = a.EmchdBeginDate,
                                        EmchdEndDate = a.EmchdEndDate,
                                        EmchdPersonInn = a.Inn,
                                        EmchdPersonSurname = a.Surname,
                                        EmchdPersonName = a.Name,
                                        EmchdPersonPatronymicSurname = a.PatronymicSurname,
                                        EmchdPersonPosition = a.Position
                                    }).ToList();

                        foreach (var myOrganization in orgs)
                        {
                            try
                            {
                                if (!string.IsNullOrEmpty(myOrganization.EmchdPersonInn))
                                {
                                    var certs = personalCertificates.Where(c => myOrganization.EmchdPersonInn == _utils.ParseCertAttribute(c.Subject, "ИНН").TrimStart('0') && _utils.IsCertificateValid(c)).OrderByDescending(c => c.NotBefore);
                                    myOrganization.Certificate = certs.FirstOrDefault(c => string.IsNullOrEmpty(_utils.GetOrgInnFromCertificate(c)));
                                }

                                if (myOrganization.Certificate == null || string.IsNullOrEmpty(myOrganization.EmchdPersonInn))
                                {
                                    if (myOrganization.Certificate == null && !string.IsNullOrEmpty(myOrganization.EmchdPersonInn))
                                        myOrganization.SetNullEmchdValues();

                                    var certs = personalCertificates.Where(c => myOrganization.Inn == _utils.GetOrgInnFromCertificate(c) && _utils.IsCertificateValid(c)).OrderByDescending(c => c.NotBefore);
                                    myOrganization.Certificate = certs.FirstOrDefault();
                                }

                                if (myOrganization.EmchdEndDate != null && myOrganization.EmchdEndDate.Value < DateTime.Now)
                                    throw new Exception($"Срок доверенности истёк для физ лица с ИНН {myOrganization.EmchdPersonInn}");

                                if (myOrganization.Certificate == null)
                                    throw new Exception($"Не найден сертификат организации с ИНН {myOrganization.Inn}");

                                if (!_edo.Authenticate(false, myOrganization.Certificate, myOrganization.Inn))
                                    throw new Exception("Не удалось авторизоваться в системе по сертификату.");

                                var myOrganizationBoxIdGuid = _edo.ActualBoxIdGuid;
                                _edo.SetOrganizationParameters(myOrganization);

                                var addressSender = myOrganization?.Address?.RussianAddress?.ZipCode +
                                    (string.IsNullOrEmpty(myOrganization?.Address?.RussianAddress?.City) ? "" : $", {myOrganization.Address.RussianAddress.City}") +
                                    (string.IsNullOrEmpty(myOrganization?.Address?.RussianAddress?.Street) ? "" : $", {myOrganization.Address.RussianAddress.Street}") +
                                    (string.IsNullOrEmpty(myOrganization?.Address?.RussianAddress?.Building) ? "" : $", {myOrganization.Address.RussianAddress.Building}");

                                var signerDetails = _edo.GetExtendedSignerDetails(Diadoc.Api.Proto.Invoicing.Signers.DocumentTitleType.UtdSeller);
                                var counteragents = _edo.GetOrganizations(myOrganizationBoxIdGuid);

                                IEnumerable<UniversalTransferDocumentV2> docs = GetDocumentsForEdoAutomaticSend(myOrganization);

                                foreach (var doc in docs ?? new List<UniversalTransferDocumentV2>())
                                {
                                    try
                                    {
                                        var receiver = counteragents.FirstOrDefault(r => r.Inn == doc.BuyerInn);

                                        if (receiver == null)
                                            continue;

                                        DocComissionEdoProcessing docComissionEdoProcessing = null;
                                        if (doc.IsMarked)
                                        {
                                            var docJournal = _abt.DocJournals.FirstOrDefault(j => j.Id == doc.IdDoc);
                                            docComissionEdoProcessing = SendComissionDocumentForHonestMark(myOrganization, doc, docJournal).Result;
                                            _abt.DocComissionEdoProcessings.Add(docComissionEdoProcessing);
                                            _abt.SaveChanges();
                                        }

                                        var universalDocument = GetUniversalDocument(doc, myOrganization, doc.Details.ToList(), doc.RefEdoGoodChannel as RefEdoGoodChannel);

                                        if (universalDocument == null)
                                            throw new Exception("Не удалось сформировать документ.");

                                        var message = 
                                            new Utils.XmlCertificateUtil().SignAndSend(myOrganization.Certificate, myOrganization, receiver, new List<object>(new object[]{ universalDocument }));

                                        DocEdoProcessing docProcessing = null;
                                        if (message != null)
                                        {
                                            _log.Log($"Сохранение в базе данных документа, Id сообщения {message.MessageId}");

                                            var documentNumber = universalDocument.DocumentNumber;
                                            var entity = message.Entities.FirstOrDefault(t => t.AttachmentType == Diadoc.Api.Proto.Events.AttachmentType.UniversalTransferDocument &&
                                            t?.DocumentInfo?.DocumentNumber == documentNumber);

                                            var fileNameLength = entity.FileName.LastIndexOf('.');

                                            if (fileNameLength < 0)
                                                fileNameLength = entity.FileName.Length;

                                            var fileName = entity.FileName.Substring(0, fileNameLength);

                                            docProcessing = new DocEdoProcessing
                                            {
                                                Id = Guid.NewGuid().ToString(),
                                                MessageId = message.MessageId,
                                                EntityId = entity.EntityId,
                                                FileName = fileName,
                                                IsReprocessingStatus = 0,
                                                IdDoc = doc.IdDocMaster,
                                                DocDate = DateTime.Now,
                                                UserName = UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser,
                                                ReceiverName = receiver.Name,
                                                ReceiverInn = receiver.Inn,
                                                DocType = (int)EdiProcessingUnit.Enums.DocEdoType.Upd,
                                                HonestMarkStatus = doc.IsMarked ? (int)EdiProcessingUnit.HonestMark.DocEdoProcessingStatus.Sent : (int)EdiProcessingUnit.HonestMark.DocEdoProcessingStatus.None
                                            };

                                            if (docComissionEdoProcessing != null)
                                            {
                                                docProcessing.IdComissionDocument = docComissionEdoProcessing.Id;
                                                docProcessing.ComissionDocument = docComissionEdoProcessing;
                                                docComissionEdoProcessing.MainDocuments.Add(docProcessing);

                                                if (!_abt.DocEdoProcessings.Any(d => d.Id == docProcessing.Id))
                                                    _abt.DocEdoProcessings.Add(docProcessing);
                                            }
                                            else
                                            {
                                                _abt.DocEdoProcessings.Add(docProcessing);
                                            }
                                            MailReporter.Add($"Документ {doc.InvoiceNumber} успешно отправлен и сохранён.");
                                            _abt.SaveChanges();
                                        }
                                        else
                                        {
                                            _log.Log($"Не удалось идентифицировать отправленное сообщение.");
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _log.Log(ex);
                                        MailReporter.Add(ex, $"Произошла ошибка при отправке документа {doc.InvoiceNumber}:\r\n");
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _log.Log(ex);
                                MailReporter.Add(ex, $"Произошла ошибка перед отправкой документов организации {myOrganization.Name}:\r\n");
                            }
                        }
                    }
                    catch(Exception ex)
                    {
                        _log.Log(ex);
                        MailReporter.Add(ex);
                    }
                }
            }
        }

        private List<UniversalTransferDocumentV2> GetDocumentsForEdoAutomaticSend(Kontragent organization)
        {
            var fileController = new WebService.Controllers.FileController();
            var dateTimeLastPeriod = fileController.GetApplicationConfigParameter<DateTime>("KonturEdo", "DocsDateTime");

            var fromDateParam = new Oracle.ManagedDataAccess.Client.OracleParameter(@"FromDate", dateTimeLastPeriod);
            fromDateParam.OracleDbType = Oracle.ManagedDataAccess.Client.OracleDbType.Date;

            string sqlString = string.Empty;
            var properties = typeof(UniversalTransferDocumentV2).GetProperties();

            foreach (var property in properties)
            {
                var colAttribute = property?.GetCustomAttributes(false)?
                    .FirstOrDefault(c => c as System.ComponentModel.DataAnnotations.Schema.ColumnAttribute != null) as System.ComponentModel.DataAnnotations.Schema.ColumnAttribute;

                if (colAttribute != null)
                    sqlString += sqlString == string.Empty ? $"{colAttribute.Name} as {property.Name}" : $", {colAttribute.Name} as {property.Name}";
            }

            sqlString = $"select {sqlString} from VIEW_INVOICES_EDO_AUTOMATIC_1 D where D.DOC_DATE >= :FromDate and D.ORDER_DATE >= :FromDate" +
                $" and SELLER_INN = '{organization.Inn}' and SELLER_KPP = '{organization.Kpp}'" +
                " and ((D.is_marked = 1 and D.act_status >= 5) or (D.act_status >= D.PERMISSION_STATUS and " +
                "exists(select * from log_actions where id_object = D.ID_DOC_MASTER and id_action = D.PERMISSION_STATUS and action_datetime > sysdate - 14)))";
            var docs = _abt.Database.SqlQuery<UniversalTransferDocumentV2>(sqlString, fromDateParam).ToList();

            docs = docs.Select(u =>
            {
                try
                {
                    return u.Init(_abt);
                }
                catch (Exception ex)
                {
                    _log.Log(ex);
                    MailReporter.Add(ex, $"Ошибка в документе {u.InvoiceNumber}: ");
                    return null;
                }
            }).Where(u => u != null).ToList();
            return docs;
        }

        private List<UniversalCorrectionDocumentV2> GetCorrectionDocumentsForEdoAutomaticSend(Kontragent organization, string viewName, DateTime? dateTimeLastPeriod = null)
        {
            if (dateTimeLastPeriod == null)
            {
                var fileController = new WebService.Controllers.FileController();
                dateTimeLastPeriod = fileController.GetApplicationConfigParameter<DateTime>("KonturEdo", "DocsDateTime");
            }

            var fromDateParam = new Oracle.ManagedDataAccess.Client.OracleParameter(@"FromDate", dateTimeLastPeriod.Value);
            fromDateParam.OracleDbType = Oracle.ManagedDataAccess.Client.OracleDbType.Date;

            string sqlString = string.Empty;
            var properties = typeof(UniversalCorrectionDocumentV2).GetProperties();

            foreach (var property in properties)
            {
                var colAttribute = property?.GetCustomAttributes(false)?
                    .FirstOrDefault(c => c as System.ComponentModel.DataAnnotations.Schema.ColumnAttribute != null) as System.ComponentModel.DataAnnotations.Schema.ColumnAttribute;

                if (colAttribute != null)
                    sqlString += sqlString == string.Empty ? $"{colAttribute.Name} as {property.Name}" : $", {colAttribute.Name} as {property.Name}";
            }

            sqlString = $"select {sqlString} from {viewName} C where " +
                $"DOC_DATE >= :FromDate and " +
                $"SELLER_INN = '{organization.Inn}' and SELLER_KPP = '{organization.Kpp}' and " +
                $"exists(select * from log_actions where id_object = C.ID_DOC and id_action = C.ACT_STATUS and action_datetime > sysdate - 14)";

            var docs = _abt.Database.SqlQuery<UniversalCorrectionDocumentV2>(sqlString, fromDateParam).ToList();

            docs = docs.Select(u =>
            {
                try
                {
                    return u.Init(_abt);
                }
                catch (Exception ex)
                {
                    _log.Log(ex);
                    MailReporter.Add(ex, $"Ошибка в корректировке {u.DocNumber}: ");
                    return null;
                }
            }).Where(u => u != null).ToList();
            return docs;
        }

        private async Task<Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocument> GetUniversalDocumentAsync(
            UniversalTransferDocumentV2 doc, Kontragent myOrganization, Diadoc.Api.Proto.Invoicing.Signers.ExtendedSignerDetails signerDetails = null)
        {
            var result = await Task.Run(() =>
            {
                var universalDocument = GetUniversalDocument(doc, myOrganization, doc.Details.ToList(), doc.RefEdoGoodChannel as RefEdoGoodChannel);
                return universalDocument;
            });

            return result;
        }

        public Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocument GetUniversalDocument(
            UniversalTransferDocumentV2 d, Kontragent organization, List<UniversalTransferDocumentDetail> docDetails, RefEdoGoodChannel edoGoodChannel = null)
        {
            var document = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocument()
            {
                Function = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocumentFunction.СЧФДОП,
                DocumentName = "Универсальный передаточный документ",
                DocumentNumber = d.InvoiceNumber,
                DocumentDate = d.InvoiceDate?.Date.ToString("dd.MM.yyyy") ?? DateTime.Now.ToString("dd.MM.yyyy"),
                Currency = d.Currency,
                Table = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTable
                {
                    TotalSpecified = true,
                    TotalWithVatExcludedSpecified = true
                },
                TransferInfo = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.TransferInfo
                {
                    OperationInfo = "Товары переданы",
                    TransferDate = d.InvoiceDate?.Date.ToString("dd.MM.yyyy") ?? DateTime.Now.ToString("dd.MM.yyyy")
                }
            };

            if (!string.IsNullOrEmpty(organization.EmchdId))
                document.DocumentCreator = $"{organization.EmchdPersonSurname} {organization.EmchdPersonName} {organization.EmchdPersonPatronymicSurname}";
            else
                document.DocumentCreator = _utils.ParseCertAttribute(organization.Certificate.Subject, "SN") + " " + _utils.ParseCertAttribute(organization.Certificate.Subject, "G");

            document.Table.VatSpecified = true;
            document.Table.Total = d.InvoiceTotalSumm;
            document.Table.Vat = d.InvoiceTaxSumm;
            document.Table.TotalWithVatExcluded = d.InvoiceTotalSumm - d.InvoiceTaxSumm;

            if (!string.IsNullOrEmpty(d.Employee))
            {
                document.TransferInfo.Employee = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.EmployeeUtd970
                {
                    Position = Properties.Settings.Default.DefaultEmployePosition,
                    EmployeeInfo = d.Employee,
                    LastName = d.Employee.Substring(0, d.Employee.IndexOf(' ')),
                    FirstName = d.Employee.Substring(d.Employee.IndexOf(' ') + 1)
                };
            }

            if (!(string.IsNullOrEmpty(d.ContractNumber) || string.IsNullOrEmpty(d.ContractDate)))
            {
                document.TransferInfo.TransferBases = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.DocumentRequisitesType[]
                {
                    new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.DocumentRequisitesType
                    {
                        DocumentName = "Договор поставки",
                        DocumentNumber = d.ContractNumber,
                        DocumentDate = d.ContractDate
                    }
                };
            }

            Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.RussianAddressUtd970 senderAddress = organization?.Address?.RussianAddress != null ?
                            new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.RussianAddressUtd970
                            {
                                ZipCode = organization.Address.RussianAddress.ZipCode,
                                Region = organization.Address.RussianAddress.Region,
                                Street = string.IsNullOrEmpty(organization?.Address?.RussianAddress?.Street) ? null : organization.Address.RussianAddress.Street,
                                City = string.IsNullOrEmpty(organization?.Address?.RussianAddress?.City) ? null : organization.Address.RussianAddress.City,
                                Locality = string.IsNullOrEmpty(organization?.Address?.RussianAddress?.Locality) ? null : organization.Address.RussianAddress.Locality,
                                Territory = string.IsNullOrEmpty(organization?.Address?.RussianAddress?.Territory) ? null : organization.Address.RussianAddress.Territory,
                                Building = string.IsNullOrEmpty(organization?.Address?.RussianAddress?.Building) ? null : organization.Address.RussianAddress.Building
                            } : null;

            document.Sellers = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationInfoUtd970[]
            {
                        new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationInfoUtd970
                        {
                            Item = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationDetailsUtd970
                            {
                                Inn = organization.Inn,
                                Kpp = organization.Kpp,
                                OrgType = organization.Inn.Length == 12 ? Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.OrganizationType_DatabaseOrder.Item1 : Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.OrganizationType_DatabaseOrder.Item2,
                                OrgName = organization.Name,
                                Address = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AddressUtd970
                                {
                                    Item = senderAddress
                                }
                            }
                        }
            };

            document.Shippers = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocumentShipper[]
            {
                new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocumentShipper
                {
                    Item = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationDetailsUtd970
                    {
                        Inn = organization.Inn,
                        Kpp = organization.Kpp,
                        OrgType = organization.Inn.Length == 12 ? Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.OrganizationType_DatabaseOrder.Item1 : Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.OrganizationType_DatabaseOrder.Item2,
                        Address = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AddressUtd970
                        {
                            Item = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ForeignAddressUtd970
                            {
                                Country = d.DefaultCountryCode,
                                Address = d.ShipperAddress
                            }
                        },
                        OrgName = d.ShipperName
                    }
                }
            };

            document.Consignees = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationInfoUtd970[]
            {
                new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationInfoUtd970
                {
                    Item = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationDetailsUtd970
                    {
                        Inn = d?.BuyerInn,
                        Kpp = d?.BuyerKpp,
                        OrgType = d?.BuyerInn?.Length == 12 ? Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.OrganizationType_DatabaseOrder.Item1 : Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.OrganizationType_DatabaseOrder.Item2,
                        Address = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AddressUtd970
                        {
                            Item = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ForeignAddressUtd970
                            {
                                Country = d.DefaultCountryCode,
                                Address = d?.ConsigneeAddress
                            }
                        },
                        OrgName = d?.ConsigneeName
                    }
                }
            };

            document.Buyers = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationInfoUtd970[]
            {
                new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationInfoUtd970
                {
                    Item = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationDetailsUtd970
                    {
                        Inn = d?.BuyerInn,
                        Kpp = d?.BuyerKpp,
                        OrgType = d?.BuyerInn?.Length == 12 ? Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.OrganizationType_DatabaseOrder.Item1 : Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.OrganizationType_DatabaseOrder.Item2,
                        OrgName = d?.BuyerName,
                        Address = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AddressUtd970
                        {
                            Item = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ForeignAddressUtd970
                            {
                                Country = d.DefaultCountryCode,
                                Address = d?.BuyerAddress
                            }
                        }
                    }
                }
            };

            Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Signer signer;
            if (string.IsNullOrEmpty(organization.EmchdId))
            {
                var firstMiddleName = _utils.ParseCertAttribute(organization.Certificate.Subject, "G");
                string signerFirstName = firstMiddleName.IndexOf(" ") > 0 ? firstMiddleName.Substring(0, firstMiddleName.IndexOf(" ")) : string.Empty;
                string signerMiddleName = firstMiddleName.IndexOf(" ") >= 0 && firstMiddleName.Length > firstMiddleName.IndexOf(" ") + 1 ? firstMiddleName.Substring(firstMiddleName.IndexOf(" ") + 1) : string.Empty;

                signer = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Signer
                {
                    Fio = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Fio
                    {
                        FirstName = signerFirstName,
                        MiddleName = signerMiddleName,
                        LastName = _utils.ParseCertAttribute(organization.Certificate.Subject, "SN")
                    },
                    Position = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerPosition
                    {
                        PositionSource = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerPositionPositionSource.Manual,
                        Value = _utils.ParseCertAttribute(organization.Certificate.Subject, "T")
                    },
                    SignatureTypeSpecified = true,
                    SignatureType = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerSignatureType.Item1,
                    SigningDate = DateTime.Now.ToString("dd.MM.yyyy"),
                    SignerPowersConfirmationMethodSpecified = true,
                    SignerPowersConfirmationMethod = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerSignerPowersConfirmationMethod.Item1
                };
            }
            else
            {
                signer = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Signer
                {
                    Fio = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Fio
                    {
                        FirstName = organization.EmchdPersonName,
                        MiddleName = organization.EmchdPersonPatronymicSurname,
                        LastName = organization.EmchdPersonSurname
                    },
                    Position = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerPosition
                    {
                        PositionSource = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerPositionPositionSource.Manual,
                        Value = organization.EmchdPersonPosition
                    },
                    SignatureTypeSpecified = true,
                    SignatureType = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerSignatureType.Item1,
                    SigningDate = DateTime.Now.ToString("dd.MM.yyyy"),
                    SignerPowersConfirmationMethodSpecified = true,
                    SignerPowersConfirmationMethod = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerSignerPowersConfirmationMethod.Item3,
                    PowerOfAttorney = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.PowerOfAttorney
                    {
                        Electronic = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Electronic
                        {
                            Item = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Storage
                            {
                                UseDefault = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.StorageUseDefault.@false,
                                FullId = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.StorageFullId
                                {
                                    RegistrationNumber = organization.EmchdId,
                                    IssuerInn = organization.Inn,
                                    RepresentativeInn = organization.EmchdPersonInn
                                }
                            }
                        }
                    }
                };
            }

            document.Signers = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Signers
            {
                Signer = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Signer[]
                {
                    signer
                }
            };

            int docLineCount = d.Details.Count;
            document.DocumentShipments = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.DocumentRequisitesType[]
            {
                                new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.DocumentRequisitesType
                                {
                                    DocumentName = "Универсальный передаточный документ",
                                    DocumentNumber = d.InvoiceNumber,
                                    DocumentDate = d.InvoiceDate?.Date.ToString("dd.MM.yyyy") ?? DateTime.Now.ToString("dd.MM.yyyy")
                                }
            };

            var details = new List<Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItem>();

            var additionalInfoList = new List<Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AdditionalInfo>();

            if (edoGoodChannel != null)
            {
                if (!string.IsNullOrEmpty(edoGoodChannel.NumberUpdId))
                    additionalInfoList.Add(new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AdditionalInfo { Id = edoGoodChannel.NumberUpdId, Value = d.InvoiceNumber });

                if (!string.IsNullOrEmpty(edoGoodChannel.OrderNumberUpdId))
                {
                    if (string.IsNullOrEmpty(d.OrderNumber))
                        throw new Exception("Отсутствует номер заказа покупателя.");

                    additionalInfoList.Add(new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AdditionalInfo { Id = edoGoodChannel.OrderNumberUpdId, Value = d.OrderNumber });
                }

                if (!string.IsNullOrEmpty(edoGoodChannel.OrderDateUpdId))
                    additionalInfoList.Add(new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AdditionalInfo { Id = edoGoodChannel.OrderDateUpdId, Value = d.OrderDate.ToString("dd.MM.yyyy") });

                if (!string.IsNullOrEmpty(edoGoodChannel.GlnShipToUpdId))
                {
                    if (string.IsNullOrEmpty(d.GlnShipTo))
                        throw new Exception("Не указан GLN грузополучателя.");

                    additionalInfoList.Add(new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AdditionalInfo { Id = edoGoodChannel.GlnShipToUpdId, Value = d.GlnShipTo });
                }

                foreach (var keyValuePair in edoGoodChannel.EdoValuesPairs)
                    additionalInfoList.Add(new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AdditionalInfo { Id = keyValuePair.Key, Value = keyValuePair.Value });
            }

            if (additionalInfoList.Count > 0)
                document.AdditionalInfoId = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AdditionalInfoId { AdditionalInfo = additionalInfoList.ToArray() };

            int number = 1;
            foreach(var docDetail in docDetails)
            {
                var docJournalDetail = docDetail.DocDetailI;
                var refGood = docDetail.Good;

                if (refGood == null)
                    continue;

                var barCode = docDetail.ItemVendorCode;

                string countryCode = docDetail.CountryCode;

                if (countryCode.Length == 1)
                    countryCode = "00" + countryCode;
                else if (countryCode.Length == 2)
                    countryCode = "0" + countryCode;

                var subtotal = Math.Round(docJournalDetail.Quantity * ((decimal)docJournalDetail.Price - (decimal)docJournalDetail.DiscountSumm), 2);
                var vat = (decimal)Math.Round(subtotal * docJournalDetail.TaxRate / (docJournalDetail.TaxRate + 100), 2, MidpointRounding.AwayFromZero);

                decimal price = 0;

                if (docJournalDetail.Quantity > 0)
                    price = (decimal)Math.Round((subtotal - vat) / docJournalDetail.Quantity, 2, MidpointRounding.AwayFromZero);
                else
                    price = (decimal)Math.Round(docJournalDetail.Price - docJournalDetail.DiscountSumm - docJournalDetail.TaxSumm, 2);

                var detail = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItem
                {
                    Product = refGood.Name,
                    Unit = Properties.Settings.Default.DefaultUnit,
                    Quantity = docJournalDetail.Quantity,
                    QuantitySpecified = true,
                    VatSpecified = true,
                    Vat = vat,
                    PriceSpecified = true,
                    Price = price,
                    SubtotalSpecified = true,
                    Subtotal = subtotal,
                    SubtotalWithVatExcludedSpecified = true,
                    SubtotalWithVatExcluded = subtotal - vat,
                    ItemVendorCode = barCode
                };

                if (!string.IsNullOrEmpty(refGood.CustomsNo))
                {
                    detail.CustomsDeclarations = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemCustomsDeclaration[]
                    {
                            new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemCustomsDeclaration
                            {
                                Country = countryCode,
                                DeclarationNumber = refGood.CustomsNo
                            }
                    };
                }

                switch (docJournalDetail.TaxRate)
                {
                    case 0:
                        detail.TaxRate = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.TaxRateWithTwentyTwoPercent.ZeroPercent;
                        break;
                    case 10:
                        detail.TaxRate = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.TaxRateWithTwentyTwoPercent.TenPercent;
                        break;
                    //case 18:
                    //    detail.TaxRate = Diadoc.Api.DataXml.Utd820.Hyphens.TaxRateUcd736AndUtd820.Item18;
                    //    break;
                    case 20:
                        detail.TaxRate = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.TaxRateWithTwentyTwoPercent.TwentyPercent;
                        break;
                    default:
                        detail.TaxRate = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.TaxRateWithTwentyTwoPercent.NoVat;
                        break;
                }

                var docGoodDetailLabels = docDetail.Labels;

                if (docGoodDetailLabels.Count > 0)
                {
                    detail.ItemMark = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemItemMark.Item4;
                    detail.ItemIdentificationNumbers = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemItemIdentificationNumber[1];
                    detail.ItemIdentificationNumbers[0] = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemItemIdentificationNumber
                    {
                        ItemsElementName = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ItemsChoiceType[docGoodDetailLabels.Count],
                        Items = new string[docGoodDetailLabels.Count]
                    };

                    int j = 0;
                    foreach (var doc in docGoodDetailLabels)
                    {
                        detail.ItemIdentificationNumbers[0].ItemsElementName[j] = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ItemsChoiceType.Unit;
                        detail.ItemIdentificationNumbers[0].Items[j] = doc;
                        j++;
                    }
                }

                var detailAdditionalInfos = new List<Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AdditionalInfo>();
                if (edoGoodChannel != null)
                {
                    var idChannel = edoGoodChannel.IdChannel;
                    if (!string.IsNullOrEmpty(edoGoodChannel.DetailBuyerCodeUpdId))
                    {
                        if (!string.IsNullOrEmpty(docDetail?.BuyerCode))
                            detailAdditionalInfos.Add(new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AdditionalInfo { Id = edoGoodChannel.DetailBuyerCodeUpdId, Value = docDetail?.BuyerCode });
                        else
                            throw new Exception("Не для всех товаров заданы коды покупателя.");
                    }

                    if (!string.IsNullOrEmpty(edoGoodChannel.DetailBarCodeUpdId))
                        detailAdditionalInfos.Add(new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AdditionalInfo { Id = edoGoodChannel.DetailBarCodeUpdId, Value = barCode });

                    if (!string.IsNullOrEmpty(edoGoodChannel.DetailPositionUpdId))
                        detailAdditionalInfos.Add(new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AdditionalInfo { Id = edoGoodChannel.DetailPositionUpdId, Value = number.ToString() });
                }

                if (detailAdditionalInfos.Count > 0)
                    detail.AdditionalInfos = detailAdditionalInfos.ToArray();

                details.Add(detail);
                number++;
            }
            document.Table.Total = details.Sum(i => i.Subtotal);
            document.Table.Vat = details.Sum(i => i.Vat);
            document.Table.TotalWithVatExcluded = document.Table.Total - document.Table.Vat;

            document.Table.Item = details.ToArray();

            return document;
        }

        public async Task<Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.UniversalCorrectionDocument> GetUniversalCorrectionDocumentAsync(UniversalCorrectionDocumentV2 d, Kontragent organization, Kontragent receiver, Diadoc.Api.Proto.Documents.Document baseDocument)
        {
            var detailsList = d.Details.ToList();

            var result = await Task.Run(() =>
            {
                var universalCorrectionDocument = GetUniversalCorrectionDocument(d, organization, receiver, baseDocument.CounteragentBoxId, detailsList, d.RefEdoGoodChannel);
                return universalCorrectionDocument;
            });

            return result;
        }

        public Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.UniversalCorrectionDocument GetUniversalCorrectionDocument(UniversalCorrectionDocumentV2 d, Kontragent organization, Kontragent receiver, string counteragentBoxId, List<UniversalCorrectionDocumentDetail> docDetails, RefEdoGoodChannel edoGoodChannel = null)
        {
            if (d?.BaseProcessing == null)
                throw new Exception($"Не найден основной документ для корректировки {d?.CorrectionNumber}");

            var correctionDocument = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.UniversalCorrectionDocument
            {
                Currency = Properties.Settings.Default.DefaultCurrency,
                Function = Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.UniversalCorrectionDocumentFunction.КСЧФДИС,
                DocumentNumber = d.CorrectionNumber,
                DocumentDate = d.DocDate.Date.ToString("dd.MM.yyyy") ?? DateTime.Now.ToString("dd.MM.yyyy")
            };

            correctionDocument.Seller = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ExtendedOrganizationInfo_ForeignAddress1000();

            object sellerAddress;

            if (organization?.Address?.RussianAddress != null)
            {
                sellerAddress = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.RussianAddress
                {
                    ZipCode = organization.Address.RussianAddress.ZipCode,
                    Region = organization.Address.RussianAddress.Region,
                    Street = string.IsNullOrEmpty(organization?.Address?.RussianAddress?.Street) ? null : organization.Address.RussianAddress.Street,
                    City = string.IsNullOrEmpty(organization?.Address?.RussianAddress?.City) ? null : organization.Address.RussianAddress.City,
                    Locality = string.IsNullOrEmpty(organization?.Address?.RussianAddress?.Locality) ? null : organization.Address.RussianAddress.Locality,
                    Territory = string.IsNullOrEmpty(organization?.Address?.RussianAddress?.Territory) ? null : organization.Address.RussianAddress.Territory,
                    Building = string.IsNullOrEmpty(organization?.Address?.RussianAddress?.Building) ? null : organization.Address.RussianAddress.Building
                };
            }
            else
            {
                sellerAddress = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ForeignAddress1000
                {
                    Country = Properties.Settings.Default.DefaultOrgCountryCode,
                    Address = d.SellerAddress
                };
            }

            (correctionDocument.Seller as Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ExtendedOrganizationInfo_ForeignAddress1000).Item =
                new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ExtendedOrganizationDetails_ForeignAddress1000
                {
                    Inn = d.SellerInn,
                    Kpp = d.SellerKpp,
                    OrgName = d.SellerOrgName,
                    OrgType = d.SellerInn.Length == 12 ? Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.OrganizationType.Item2 : Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.OrganizationType.Item1,
                    Address = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.Address_ForeignAddress1000
                    {
                        Item = sellerAddress
                    }
                };
            

            var orgInn = organization.Inn;

            if (!string.IsNullOrEmpty(d?.BuyerInn))
            {
                correctionDocument.Buyer = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ExtendedOrganizationInfo_ForeignAddress1000
                {
                    Item = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ExtendedOrganizationDetails_ForeignAddress1000
                    {
                        OrgName = d.BuyerOrgName,
                        OrgType = d.BuyerInn.Length == 12 ? Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.OrganizationType.Item2 : Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.OrganizationType.Item1,
                        Inn = d.BuyerInn,
                        Kpp = d.BuyerKpp,
                        Address = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.Address_ForeignAddress1000
                        {
                            Item = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ForeignAddress1000
                            {
                                Country = Properties.Settings.Default.DefaultOrgCountryCode,
                                Address = d.BuyerAddress
                            }
                        }
                    }
                };

                if (!string.IsNullOrEmpty(receiver?.FnsParticipantId))
                    (correctionDocument.Buyer.Item as Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ExtendedOrganizationDetails_ForeignAddress1000).FnsParticipantId = receiver.FnsParticipantId;

                var contractNumber = d?.ContractNumber;
                var contractDate = d?.ContractDate;

                if (!(string.IsNullOrEmpty(contractNumber) || string.IsNullOrEmpty(contractDate)))
                {
                    correctionDocument.EventContent = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.EventContent()
                    {
                        OperationContent = "Изменение стоимости товаров и услуг",
                        TransferDocDetails = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.DocType[]
                        {
                                    new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.DocType
                                    {
                                        BaseDocumentName = "УПД",
                                        BaseDocumentNumber = d.InvoiceNumber,
                                        BaseDocumentDate = d.InvoiceDeliveryDate?.Date.ToString("dd.MM.yyyy")
                                    }
                        },
                        CorrectionBase = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.DocType[]
                        {
                                    new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.DocType
                                    {
                                        BaseDocumentName = "Договор поставки",
                                        BaseDocumentNumber = contractNumber,
                                        BaseDocumentDate = contractDate
                                    }
                        }
                    };
                }
            }
            else
                correctionDocument.Buyer = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ExtendedOrganizationInfo_ForeignAddress1000
                {
                    Item = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ExtendedOrganizationReference
                    {
                        BoxId = counteragentBoxId,
                        ShortOrgName = d.BaseProcessing.ReceiverName,
                        OrgType = d.BaseProcessing.ReceiverInn.Length == 12 ? Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.OrganizationType.Item2 : Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.OrganizationType.Item1
                    }
                };

            Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ExtendedSignerDetails_CorrectionSellerTitle[] signer;
            if (string.IsNullOrEmpty(organization.EmchdId))
            {
                var firstMiddleName = _utils.ParseCertAttribute(organization.Certificate.Subject, "G");
                string signerFirstName = firstMiddleName.IndexOf(" ") > 0 ? firstMiddleName.Substring(0, firstMiddleName.IndexOf(" ")) : string.Empty;
                string signerMiddleName = firstMiddleName.IndexOf(" ") >= 0 && firstMiddleName.Length > firstMiddleName.IndexOf(" ") + 1 ? firstMiddleName.Substring(firstMiddleName.IndexOf(" ") + 1) : string.Empty;
                string signerLastName = _utils.ParseCertAttribute(organization.Certificate.Subject, "SN");

                signer = new[]
                {
                    new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ExtendedSignerDetails_CorrectionSellerTitle
                    {
                        SignerType = Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ExtendedSignerDetailsBaseSignerType.Item1,
                        FirstName = signerFirstName,
                        MiddleName = signerMiddleName,
                        LastName = signerLastName,
                        SignerOrganizationName = _utils.ParseCertAttribute(organization.Certificate.Subject, "CN"),
                        Inn = orgInn,
                        Position = _utils.ParseCertAttribute(organization.Certificate.Subject, "T")
                    }
                };
            }
            else
            {
                signer = new[]
                {
                    new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ExtendedSignerDetails_CorrectionSellerTitle
                    {
                        SignerType = Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ExtendedSignerDetailsBaseSignerType.Item1,
                        FirstName = organization.EmchdPersonName,
                        MiddleName = organization.EmchdPersonPatronymicSurname,
                        LastName = organization.EmchdPersonSurname,
                        SignerOrganizationName = organization.Name,
                        Inn = orgInn,
                        Position = organization.EmchdPersonPosition
                    }
                };
            }

            correctionDocument.Signers = signer;

            correctionDocument.DocumentCreator = $"{signer.First().LastName} {signer.First().FirstName} {signer.First().MiddleName}";

            var idChannel = d?.IdChannel;
            var itemDetails = new List<Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ExtendedInvoiceCorrectionItem>();

            if (d?.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.ReturnFromBuyer)
            {
                foreach (var docDetail in docDetails)
                {
                    if (docDetail?.BaseDetail == null)
                        throw new Exception($"Не найден товар в исходном документе {d.InvoiceNumber}, ID товара {docDetail.IdGood}");

                    if (docDetail?.DocDetail == null)
                        throw new Exception($"Не найден товар в корректировочном документе {d.CorrectionNumber}, ID товара {docDetail.IdGood}");

                    var additionalInfos = new List<Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.AdditionalInfo100>();

                    int baseIndex = docDetail.BaseIndex;
                    var barCode = docDetail.ItemVendorCode;
                    var baseDetail = docDetail.BaseDetail;
                    var detail = docDetail.DocDetail;

                    string countryCode = docDetail.CountryCode;

                    if (countryCode.Length == 1)
                        countryCode = "00" + countryCode;
                    else if (countryCode.Length == 2)
                        countryCode = "0" + countryCode;

                    var oldSubtotal = Math.Round(baseDetail.Quantity * ((decimal)baseDetail.Price - (decimal)baseDetail.DiscountSumm), 2);
                    var oldVat = (decimal)Math.Round(oldSubtotal * baseDetail.TaxRate / (baseDetail.TaxRate + 100), 2);
                    var oldPrice = (decimal)Math.Round((oldSubtotal - oldVat) / baseDetail.Quantity, 2, MidpointRounding.AwayFromZero);

                    var newSubtotal = Math.Round(detail.Quantity * ((decimal)detail.Price - (decimal)detail.DiscountSumm), 2);
                    var newVat = (decimal)Math.Round(newSubtotal * baseDetail.TaxRate / (baseDetail.TaxRate + 100), 2);
                    var newPrice = (decimal)Math.Round((newSubtotal - newVat) / detail.Quantity, 2, MidpointRounding.AwayFromZero);

                    var item = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ExtendedInvoiceCorrectionItem
                    {
                        Product = detail.Good.Name,
                        ItemVendorCode = barCode,
                        OriginalNumber = baseIndex.ToString(),
                        Unit = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ExtendedInvoiceCorrectionItemUnit
                        {
                            OriginalValue = Properties.Settings.Default.DefaultUnit,
                            CorrectedValue = Properties.Settings.Default.DefaultUnit
                        },
                        UnitName = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ExtendedInvoiceCorrectionItemUnitName
                        {
                            OriginalValue = "шт.",
                            CorrectedValue = "шт."
                        },
                        Quantity = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ExtendedInvoiceCorrectionItemQuantity
                        {
                            OriginalValue = baseDetail.Quantity,
                            OriginalValueSpecified = true,
                            CorrectedValue = baseDetail.Quantity - detail.Quantity,
                            CorrectedValueSpecified = true
                        },
                        TaxRate = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ExtendedInvoiceCorrectionItemTaxRate
                        {
                            OriginalValue = Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.TaxRateUcd736AndUtd820.Item20,
                            CorrectedValue = Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.TaxRateUcd736AndUtd820.Item20
                        },
                        Price = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ExtendedInvoiceCorrectionItemPrice
                        {
                            OriginalValue = oldPrice,
                            OriginalValueSpecified = true,
                            CorrectedValue = newPrice,
                            CorrectedValueSpecified = true
                        },
                        Vat = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ExtendedInvoiceCorrectionItemVat
                        {
                            OriginalValue = oldVat,
                            OriginalValueSpecified = true,
                            CorrectedValue = oldVat - newVat,
                            CorrectedValueSpecified = true,
                            ItemElementName = Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ItemChoiceType2.AmountsDec,
                            Item = newVat
                        },
                        Subtotal = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ExtendedInvoiceCorrectionItemSubtotal
                        {
                            OriginalValue = oldSubtotal,
                            OriginalValueSpecified = true,
                            CorrectedValue = oldSubtotal - newSubtotal,
                            CorrectedValueSpecified = true,
                            ItemElementName = Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ItemChoiceType3.AmountsDec,
                            Item = newSubtotal
                        },
                        SubtotalWithVatExcluded = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ExtendedInvoiceCorrectionItemSubtotalWithVatExcluded
                        {
                            OriginalValue = oldSubtotal - oldVat,
                            OriginalValueSpecified = true,
                            CorrectedValue = oldSubtotal - oldVat - newSubtotal + newVat,
                            CorrectedValueSpecified = true,
                            Item = newSubtotal - newVat,
                            ItemElementName = Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ItemChoiceType1.AmountsDec
                        }
                    };

                    if (!string.IsNullOrEmpty(countryCode))
                    {
                        additionalInfos.Add(new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.AdditionalInfo100 { Id = "цифровой код страны происхождения", Value = countryCode });
                        additionalInfos.Add(new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.AdditionalInfo100 { Id = "краткое наименование страны происхождения", Value = docDetail.CountryName });

                        if (!string.IsNullOrEmpty(docDetail?.CustomsNo))
                            additionalInfos.Add(new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.AdditionalInfo100 { Id = "регистрационный номер декларации на товары", Value = docDetail.CustomsNo });
                    }

                    if (edoGoodChannel != null)
                    {
                        if (!string.IsNullOrEmpty(edoGoodChannel.DetailBuyerCodeUpdId))
                        {
                            if (!string.IsNullOrEmpty(docDetail?.BuyerCode))
                            {
                                additionalInfos.Add(new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.AdditionalInfo100 { Id = edoGoodChannel.DetailBuyerCodeUpdId, Value = docDetail.BuyerCode });
                            }
                            else
                                throw new Exception("Не для всех товаров заданы коды покупателя.");
                        }

                        if (!string.IsNullOrEmpty(edoGoodChannel.DetailBarCodeUpdId))
                            additionalInfos.Add(new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.AdditionalInfo100 { Id = edoGoodChannel.DetailBarCodeUpdId, Value = barCode });

                        if (!string.IsNullOrEmpty(edoGoodChannel.DetailPositionUpdId))
                            additionalInfos.Add(new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.AdditionalInfo100 { Id = edoGoodChannel.DetailPositionUpdId, Value = item.OriginalNumber });
                    }

                    item.AdditionalInfos = additionalInfos.ToArray();

                    if (d.IsMarked)
                    {
                        var originalMarkedCodes = docDetail.OriginalMarkedCodes;
                        var correctedMarkedCodes = docDetail.CorrectedMarkedCodes;

                        if (originalMarkedCodes.Count > 0)
                        {
                            item.OriginalItemIdentificationNumbers = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ItemIdentificationNumbersItemIdentificationNumber[1];
                            item.OriginalItemIdentificationNumbers[0] = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ItemIdentificationNumbersItemIdentificationNumber
                            {
                                ItemsElementName = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ItemsChoiceType[originalMarkedCodes.Count],
                                Items = new string[originalMarkedCodes.Count]
                            };

                            int j = 0;
                            foreach (var markedCode in originalMarkedCodes)
                            {
                                item.OriginalItemIdentificationNumbers[0].ItemsElementName[j] = Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ItemsChoiceType.Unit;
                                item.OriginalItemIdentificationNumbers[0].Items[j] = markedCode;
                                j++;
                            }
                        }

                        if (correctedMarkedCodes.Count > 0)
                        {
                            if (correctedMarkedCodes.Count != (int)item.Quantity.CorrectedValue)
                                throw new Exception($"Количество кодов маркировки не совпадает с количеством товара в документе. ID товара {detail.IdGood}");

                            item.CorrectedItemIdentificationNumbers = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ItemIdentificationNumbersItemIdentificationNumber[1];
                            item.CorrectedItemIdentificationNumbers[0] = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ItemIdentificationNumbersItemIdentificationNumber
                            {
                                ItemsElementName = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ItemsChoiceType[correctedMarkedCodes.Count],
                                Items = new string[correctedMarkedCodes.Count]
                            };

                            int j = 0;
                            foreach (var markedCode in correctedMarkedCodes)
                            {
                                item.CorrectedItemIdentificationNumbers[0].ItemsElementName[j] = Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ItemsChoiceType.Unit;
                                item.CorrectedItemIdentificationNumbers[0].Items[j] = markedCode;
                                j++;
                            }
                        }
                        else
                        {
                            if (item.Quantity.CorrectedValue > 0)
                                if (docDetail.HonestMarkGood)
                                    throw new Exception($"Товар из ЧЗ {detail.IdGood} забыли пропикать!!");
                        }
                    }
                    else
                    {
                        if (item.Quantity.CorrectedValue > 0)
                            if (docDetail.HonestMarkGood)
                                throw new Exception($"Товар из ЧЗ {detail.IdGood} забыли пропикать!!");
                    }

                    itemDetails.Add(item);
                }

                correctionDocument.Table = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.InvoiceCorrectionTable
                {
                    TotalsDec = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.InvoiceTotalsDiff736
                    {
                        Total = itemDetails.Sum(det => det.Subtotal.OriginalValue) - itemDetails.Sum(det => det.Subtotal.CorrectedValue),
                        TotalSpecified = true,
                        Vat = itemDetails.Sum(det => det.Vat.OriginalValue) - itemDetails.Sum(det => det.Vat.CorrectedValue),
                        VatSpecified = true,
                        TotalWithVatExcluded = itemDetails.Sum(det => det.SubtotalWithVatExcluded.OriginalValue) - itemDetails.Sum(det => det.SubtotalWithVatExcluded.CorrectedValue)
                    },

                    TotalsInc = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.InvoiceTotalsDiff736
                    {
                        Total = 0,
                        TotalSpecified = true,
                        Vat = 0,
                        VatSpecified = true,
                        TotalWithVatExcluded = 0
                    },
                    Items = itemDetails.ToArray()
                };
            }
            else if (d?.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Correction)
            {
                foreach(var docDetail in docDetails)
                {
                    if (docDetail?.BaseDetail == null)
                        throw new Exception($"Не найден товар в исходном документе {d.InvoiceNumber}, ID товара {docDetail.IdGood}");

                    if (docDetail?.DocDetailsI == null)
                        throw new Exception($"Не найден товар в корректировочном документе {d.CorrectionNumber}, ID товара {docDetail.IdGood}");

                    var additionalInfos = new List<Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.AdditionalInfo100>();

                    var baseDetail = docDetail.BaseDetail;
                    int baseIndex = docDetail.BaseIndex;
                    var barCode = docDetail.ItemVendorCode;
                    var detail = docDetail.DocDetailsI;

                    string countryCode = docDetail.CountryCode;

                    if (countryCode.Length == 1)
                        countryCode = "00" + countryCode;
                    else if (countryCode.Length == 2)
                        countryCode = "0" + countryCode;

                    var oldSubtotal = Math.Round(baseDetail.Quantity * ((decimal)baseDetail.Price - (decimal)baseDetail.DiscountSumm), 2);
                    var oldVat = (decimal)Math.Round(oldSubtotal * baseDetail.TaxRate / (baseDetail.TaxRate + 100), 2);
                    var oldPrice = (decimal)Math.Round((oldSubtotal - oldVat) / baseDetail.Quantity, 2, MidpointRounding.AwayFromZero);

                    var newSubtotal = Math.Round(detail.Quantity * ((decimal)detail.Price - (decimal)detail.DiscountSumm), 2);
                    var newVat = (decimal)Math.Round(newSubtotal * baseDetail.TaxRate / (baseDetail.TaxRate + 100), 2);
                    var newPrice = (decimal)Math.Round((newSubtotal - newVat) / detail.Quantity, 2, MidpointRounding.AwayFromZero);

                    var item = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ExtendedInvoiceCorrectionItem
                    {
                        Product = detail.Good.Name,
                        ItemVendorCode = barCode,
                        OriginalNumber = baseIndex.ToString(),
                        Unit = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ExtendedInvoiceCorrectionItemUnit
                        {
                            OriginalValue = Properties.Settings.Default.DefaultUnit,
                            CorrectedValue = Properties.Settings.Default.DefaultUnit
                        },
                        UnitName = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ExtendedInvoiceCorrectionItemUnitName
                        {
                            OriginalValue = "шт.",
                            CorrectedValue = "шт."
                        },
                        Quantity = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ExtendedInvoiceCorrectionItemQuantity
                        {
                            OriginalValue = baseDetail.Quantity,
                            OriginalValueSpecified = true,
                            CorrectedValue = detail.Quantity,
                            CorrectedValueSpecified = true
                        },
                        TaxRate = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ExtendedInvoiceCorrectionItemTaxRate
                        {
                            OriginalValue = Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.TaxRateUcd736AndUtd820.Item20,
                            CorrectedValue = Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.TaxRateUcd736AndUtd820.Item20
                        },
                        Price = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ExtendedInvoiceCorrectionItemPrice
                        {
                            OriginalValue = oldPrice,
                            OriginalValueSpecified = true,
                            CorrectedValue = newPrice,
                            CorrectedValueSpecified = true
                        },
                        Vat = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ExtendedInvoiceCorrectionItemVat
                        {
                            OriginalValue = oldVat,
                            OriginalValueSpecified = true,
                            CorrectedValue = newVat,
                            CorrectedValueSpecified = true,
                            ItemElementName = oldVat > newVat ? Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ItemChoiceType2.AmountsDec : Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ItemChoiceType2.AmountsInc,
                            Item = Math.Abs(newVat - oldVat)
                        },
                        Subtotal = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ExtendedInvoiceCorrectionItemSubtotal
                        {
                            OriginalValue = oldSubtotal,
                            OriginalValueSpecified = true,
                            CorrectedValue = newSubtotal,
                            CorrectedValueSpecified = true,
                            ItemElementName = oldSubtotal > newSubtotal ? Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ItemChoiceType3.AmountsDec : Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ItemChoiceType3.AmountsInc,
                            Item = Math.Abs(newSubtotal - oldSubtotal)
                        },
                        SubtotalWithVatExcluded = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ExtendedInvoiceCorrectionItemSubtotalWithVatExcluded
                        {
                            OriginalValue = oldSubtotal - oldVat,
                            OriginalValueSpecified = true,
                            CorrectedValue = newSubtotal - newVat,
                            CorrectedValueSpecified = true,
                            Item = Math.Abs(oldSubtotal - oldVat - newSubtotal + newVat),
                            ItemElementName = oldSubtotal - oldVat - newSubtotal + newVat > 0 ? Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ItemChoiceType1.AmountsDec : Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ItemChoiceType1.AmountsInc
                        }
                    };

                    if (oldVat == newVat)
                        throw new Exception($"Сумма НДС до и после изменения не должны совпадать. ID товара {detail.IdGood}");

                    if (!string.IsNullOrEmpty(countryCode))
                    {
                        additionalInfos.Add(new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.AdditionalInfo100 { Id = "цифровой код страны происхождения", Value = countryCode });
                        additionalInfos.Add(new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.AdditionalInfo100 { Id = "краткое наименование страны происхождения", Value = docDetail.CountryName });

                        if (!string.IsNullOrEmpty(docDetail?.CustomsNo))
                            additionalInfos.Add(new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.AdditionalInfo100 { Id = "регистрационный номер декларации на товары", Value = docDetail.CustomsNo });
                    }

                    if (edoGoodChannel != null)
                    {
                        if (!string.IsNullOrEmpty(edoGoodChannel.DetailBuyerCodeUpdId))
                        {
                            if (!string.IsNullOrEmpty(docDetail?.BuyerCode))
                            {
                                additionalInfos.Add(new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.AdditionalInfo100 { Id = edoGoodChannel.DetailBuyerCodeUpdId, Value = docDetail.BuyerCode });
                            }
                            else
                                throw new Exception("Не для всех товаров заданы коды покупателя.");
                        }

                        if (!string.IsNullOrEmpty(edoGoodChannel.DetailBarCodeUpdId))
                            additionalInfos.Add(new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.AdditionalInfo100 { Id = edoGoodChannel.DetailBarCodeUpdId, Value = barCode });

                        if (!string.IsNullOrEmpty(edoGoodChannel.DetailPositionUpdId))
                            additionalInfos.Add(new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.AdditionalInfo100 { Id = edoGoodChannel.DetailPositionUpdId, Value = item.OriginalNumber });
                    }

                    item.AdditionalInfos = additionalInfos.ToArray();

                    if (d.IsMarked)
                    {
                        var originalMarkedCodes = docDetail.OriginalMarkedCodes;
                        var correctedMarkedCodes = docDetail.CorrectedMarkedCodes;

                        if (originalMarkedCodes.Count > 0)
                        {
                            item.OriginalItemIdentificationNumbers = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ItemIdentificationNumbersItemIdentificationNumber[1];
                            item.OriginalItemIdentificationNumbers[0] = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ItemIdentificationNumbersItemIdentificationNumber
                            {
                                ItemsElementName = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ItemsChoiceType[originalMarkedCodes.Count],
                                Items = new string[originalMarkedCodes.Count]
                            };

                            int j = 0;
                            foreach (var markedCode in originalMarkedCodes)
                            {
                                item.OriginalItemIdentificationNumbers[0].ItemsElementName[j] = Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ItemsChoiceType.Unit;
                                item.OriginalItemIdentificationNumbers[0].Items[j] = markedCode;
                                j++;
                            }
                        }

                        if (correctedMarkedCodes.Count > 0)
                        {
                            if (correctedMarkedCodes.Count != (int)item.Quantity.CorrectedValue)
                                throw new Exception($"Количество кодов маркировки не совпадает с количеством товара в документе. ID товара {detail.IdGood}");

                            item.CorrectedItemIdentificationNumbers = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ItemIdentificationNumbersItemIdentificationNumber[1];
                            item.CorrectedItemIdentificationNumbers[0] = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ItemIdentificationNumbersItemIdentificationNumber
                            {
                                ItemsElementName = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ItemsChoiceType[correctedMarkedCodes.Count],
                                Items = new string[correctedMarkedCodes.Count]
                            };

                            int j = 0;
                            foreach (var markedCode in correctedMarkedCodes)
                            {
                                item.CorrectedItemIdentificationNumbers[0].ItemsElementName[j] = Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ItemsChoiceType.Unit;
                                item.CorrectedItemIdentificationNumbers[0].Items[j] = markedCode;
                                j++;
                            }
                        }
                    }

                    itemDetails.Add(item);
                }

                correctionDocument.Table = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.InvoiceCorrectionTable
                {
                    TotalsDec = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.InvoiceTotalsDiff736
                    {
                        Total = itemDetails.Where(i => i.Subtotal.ItemElementName == Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ItemChoiceType3.AmountsDec).Sum(det => det.Subtotal.Item),
                        TotalSpecified = true,
                        Vat = itemDetails.Where(i => i.Vat.ItemElementName == Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ItemChoiceType2.AmountsDec).Sum(det => det.Vat.Item),
                        VatSpecified = true,
                        TotalWithVatExcluded = itemDetails.Where(i => i.SubtotalWithVatExcluded.ItemElementName == Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ItemChoiceType1.AmountsDec).Sum(det => det.SubtotalWithVatExcluded.Item)
                    },

                    TotalsInc = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.InvoiceTotalsDiff736
                    {
                        Total = itemDetails.Where(i => i.Subtotal.ItemElementName == Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ItemChoiceType3.AmountsInc).Sum(det => det.Subtotal.Item),
                        TotalSpecified = true,
                        Vat = itemDetails.Where(i => i.Vat.ItemElementName == Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ItemChoiceType2.AmountsInc).Sum(det => det.Vat.Item),
                        VatSpecified = true,
                        TotalWithVatExcluded = itemDetails.Where(i => i.SubtotalWithVatExcluded.ItemElementName == Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.ItemChoiceType1.AmountsInc).Sum(det => det.SubtotalWithVatExcluded.Item)
                    },
                    Items = itemDetails.ToArray()
                };
            }

            correctionDocument.Invoices = new[]
                    {
                        new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.InvoiceForCorrectionInfo
                        {
                            Date = d.InvoiceDeliveryDate?.Date.ToString("dd.MM.yyyy"),
                            Number = d.InvoiceNumber
                        }
                    };

            var additionalInfoList = new List<Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.AdditionalInfo>();

            if(edoGoodChannel != null)
            {
                if (!string.IsNullOrEmpty(edoGoodChannel.NumberUpdId))
                    additionalInfoList.Add(new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.AdditionalInfo { Id = edoGoodChannel.NumberUpdId, Value = d.InvoiceNumber });

                if (!string.IsNullOrEmpty(edoGoodChannel.OrderNumberUpdId))
                {
                    if (string.IsNullOrEmpty(d.OrderNumber))
                        throw new Exception("Отсутствует номер заказа покупателя.");

                    additionalInfoList.Add(new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.AdditionalInfo { Id = edoGoodChannel.OrderNumberUpdId, Value = d.OrderNumber });
                }

                if (!string.IsNullOrEmpty(edoGoodChannel.OrderDateUpdId))
                    additionalInfoList.Add(new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.AdditionalInfo { Id = edoGoodChannel.OrderDateUpdId, Value = d.OrderDate.ToString("dd.MM.yyyy") });

                if (!string.IsNullOrEmpty(edoGoodChannel.GlnShipToUpdId))
                {
                    if (string.IsNullOrEmpty(d.GlnShipTo))
                        throw new Exception("Не указан GLN грузополучателя.");

                    additionalInfoList.Add(new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.AdditionalInfo { Id = edoGoodChannel.GlnShipToUpdId, Value = d.GlnShipTo });
                }

                foreach (var keyValuePair in edoGoodChannel.EdoUcdValuesPairs.Where(
                    u => (d?.IdDocType != null && u.IdDocType == d.IdDocType) || u.IdDocType == 0))
                    additionalInfoList.Add(new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.AdditionalInfo { Id = keyValuePair.Key, Value = keyValuePair.Value });

                if (!string.IsNullOrEmpty(edoGoodChannel.DocReturnNumberUcdId))
                {
                    if (!string.IsNullOrEmpty(d.DocReturnNumber))
                        additionalInfoList.Add(new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.AdditionalInfo { Id = edoGoodChannel.DocReturnNumberUcdId, Value = d.DocReturnNumber });
                }

                if (!string.IsNullOrEmpty(edoGoodChannel.DocReturnDateUcdId))
                {
                    if (!string.IsNullOrEmpty(d.DocReturnDate))
                        additionalInfoList.Add(new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.AdditionalInfo { Id = edoGoodChannel.DocReturnDateUcdId, Value = d.DocReturnDate });
                }
            }

            if (additionalInfoList.Count > 0)
                correctionDocument.AdditionalInfoId = new Diadoc.Api.DataXml.ON_NKORSCHFDOPPR_UserContract_1_996_03_05_01_03.AdditionalInfoId
                {
                    AdditionalInfo = additionalInfoList.ToArray()
                };

            return correctionDocument;
        }

        private Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.UniversalTransferDocumentBuyerTitle CreateBuyerShipmentDocument(Kontragent receiverOrganization)
        {
            Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.Signer signer;
            Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.EmployeeUtd970 employee;

            if (string.IsNullOrEmpty(receiverOrganization.EmchdId))
            {
                var firstMiddleName = _utils.ParseCertAttribute(receiverOrganization.Certificate.Subject, "G");
                string signerFirstName = firstMiddleName.IndexOf(" ") > 0 ? firstMiddleName.Substring(0, firstMiddleName.IndexOf(" ")) : string.Empty;
                string signerMiddleName = firstMiddleName.IndexOf(" ") >= 0 && firstMiddleName.Length > firstMiddleName.IndexOf(" ") + 1 ? firstMiddleName.Substring(firstMiddleName.IndexOf(" ") + 1) : string.Empty;

                signer = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.Signer
                {
                    Fio = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.Fio
                    {
                        FirstName = signerFirstName,
                        MiddleName = signerMiddleName,
                        LastName = _utils.ParseCertAttribute(receiverOrganization.Certificate.Subject, "SN")
                    },
                    Position = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.SignerPosition
                    {
                        PositionSource = Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.SignerPositionPositionSource.Manual,
                        Value = _utils.ParseCertAttribute(receiverOrganization.Certificate.Subject, "T")
                    },
                    SignatureTypeSpecified = true,
                    SignatureType = Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.SignerSignatureType.Item1,
                    SigningDate = DateTime.Now.ToString("dd.MM.yyyy"),
                    SignerPowersConfirmationMethodSpecified = true,
                    SignerPowersConfirmationMethod = Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.SignerSignerPowersConfirmationMethod.Item1
                };

                employee = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.EmployeeUtd970
                {
                    Position = signer.Position.Value,
                    LastName = signer.Fio.LastName,
                    MiddleName = signer.Fio.MiddleName,
                    FirstName = signer.Fio.FirstName
                };
            }
            else
            {
                signer = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.Signer
                {
                    Fio = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.Fio
                    {
                        FirstName = receiverOrganization.EmchdPersonName,
                        MiddleName = receiverOrganization.EmchdPersonPatronymicSurname,
                        LastName = receiverOrganization.EmchdPersonSurname
                    },
                    Position = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.SignerPosition
                    {
                        PositionSource = Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.SignerPositionPositionSource.Manual,
                        Value = receiverOrganization.EmchdPersonPosition
                    },
                    SignatureTypeSpecified = true,
                    SignatureType = Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.SignerSignatureType.Item1,
                    SigningDate = DateTime.Now.ToString("dd.MM.yyyy"),
                    SignerPowersConfirmationMethodSpecified = true,
                    SignerPowersConfirmationMethod = Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.SignerSignerPowersConfirmationMethod.Item3,
                    PowerOfAttorney = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.PowerOfAttorney
                    {
                        Electronic = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.Electronic
                        {
                            Item = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.Storage
                            {
                                UseDefault = Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.StorageUseDefault.@false,
                                FullId = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.StorageFullId
                                {
                                    RegistrationNumber = receiverOrganization.EmchdId,
                                    IssuerInn = receiverOrganization.Inn
                                }
                            }
                        }
                    }
                };
                employee = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.EmployeeUtd970
                {
                    Position = receiverOrganization.EmchdPersonPosition,
                    FirstName = receiverOrganization.EmchdPersonName,
                    MiddleName = receiverOrganization.EmchdPersonPatronymicSurname,
                    LastName = receiverOrganization.EmchdPersonSurname
                };
            }

            var document = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.UniversalTransferDocumentBuyerTitle()
            {
                Signers = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.Signers
                {
                    Signer = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.Signer[] { signer }
                },
                OperationContent = "Товары (работы, услуги, права) приняты без расхождений (претензий)",
                ContentOperCode = new Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.UniversalTransferDocumentBuyerTitleContentOperCode
                {
                    TotalCode = Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.UniversalTransferDocumentBuyerTitleContentOperCodeTotalCode.Item1
                },
                Employee = employee,
                AcceptanceDate = DateTime.Now.ToString("dd.MM.yyyy")
            };

            if (!string.IsNullOrEmpty(receiverOrganization.EmchdId))
                document.DocumentCreator = $"{receiverOrganization.EmchdPersonSurname} {receiverOrganization.EmchdPersonName} {receiverOrganization.EmchdPersonPatronymicSurname}";
            else
                document.DocumentCreator = _utils.ParseCertAttribute(receiverOrganization.Certificate.Subject, "SN") + " " + _utils.ParseCertAttribute(receiverOrganization.Certificate.Subject, "G");

            return document;
        }

        private async Task<Diadoc.Api.DataXml.ON_NSCHFDOPPOK_UserContract_970_05_02_01.UniversalTransferDocumentBuyerTitle> CreateBuyerShipmentDocumentAsync(Kontragent receiverOrganization)
        {
            var result = await Task.Run(() =>
            {
                var document = CreateBuyerShipmentDocument(receiverOrganization);
                return document;
            });
            return result;
        }

        private async Task<Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocument> CreateShipmentDocumentAsync(
            DocJournal d, Kontragent senderOrganization, Kontragent receiverOrganization, List<UniversalTransferDocumentDetail> docDetails, List<DocGoodsDetailsLabels> detailsLabels, string documentNumber, string employee = null, bool considerOnlyLabeledGoods = false)
        {
            if (d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice && d.DocMaster == null)
                return null;

            if (d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice && d.DocGoodsI == null)
                return null;

            if (d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Translocation && d.DocGoods == null)
                return null;

            var result = await Task.Run(() =>
            {
                var document = CreateShipmentDocument(d, senderOrganization, receiverOrganization, docDetails, detailsLabels, documentNumber, employee, considerOnlyLabeledGoods);
                return document;
            });
            return result;
        }

        private Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocument CreateShipmentDocument(
            DocJournal d, Kontragent senderOrganization, Kontragent receiverOrganization, List<UniversalTransferDocumentDetail> docDetails, List<DocGoodsDetailsLabels> detailsLabels, string documentNumber, string employee = null, bool considerOnlyLabeledGoods = false)
        {
            if (d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice && d.DocMaster == null)
                return null;

            if (d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice && d.DocGoodsI == null)
                return null;

            if (d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Translocation && d.DocGoods == null)
                return null;

            var document = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocument()
            {
                Function = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocumentFunction.ДОП,
                DocumentNumber = documentNumber,
                DocumentDate = d.DeliveryDate?.Date.ToString("dd.MM.yyyy") ?? DateTime.Now.ToString("dd.MM.yyyy"),
                Currency = Properties.Settings.Default.DefaultCurrency,
                Table = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTable
                {
                    TotalSpecified = true,
                    TotalWithVatExcludedSpecified = true
                },
                TransferInfo = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.TransferInfo
                {
                    OperationInfo = "Товары переданы",
                    TransferDate = d.DeliveryDate?.Date.ToString("dd.MM.yyyy") ?? DateTime.Now.ToString("dd.MM.yyyy")
                }
            };

            if (!string.IsNullOrEmpty(senderOrganization.EmchdId))
                document.DocumentCreator = $"{senderOrganization.EmchdPersonSurname} {senderOrganization.EmchdPersonName} {senderOrganization.EmchdPersonPatronymicSurname}";
            else
                document.DocumentCreator = _utils.ParseCertAttribute(senderOrganization.Certificate.Subject, "SN") + " " + _utils.ParseCertAttribute(senderOrganization.Certificate.Subject, "G");

            if (!string.IsNullOrEmpty(employee))
            {
                document.TransferInfo.Employee = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.EmployeeUtd970
                {
                    Position = Properties.Settings.Default.DefaultEmployePosition,
                    EmployeeInfo = employee,
                    LastName = employee.Substring(0, employee.IndexOf(' ')),
                    FirstName = employee.Substring(employee.IndexOf(' ') + 1)
                };
            }


            Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.RussianAddressUtd970 senderAddress = senderOrganization?.Address?.RussianAddress != null ?
                            new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.RussianAddressUtd970
                            {
                                ZipCode = senderOrganization.Address.RussianAddress.ZipCode,
                                Region = senderOrganization.Address.RussianAddress.Region,
                                Street = string.IsNullOrEmpty(senderOrganization?.Address?.RussianAddress?.Street) ? null : senderOrganization.Address.RussianAddress.Street,
                                City = string.IsNullOrEmpty(senderOrganization?.Address?.RussianAddress?.City) ? null : senderOrganization.Address.RussianAddress.City,
                                Locality = string.IsNullOrEmpty(senderOrganization?.Address?.RussianAddress?.Locality) ? null : senderOrganization.Address.RussianAddress.Locality,
                                Territory = string.IsNullOrEmpty(senderOrganization?.Address?.RussianAddress?.Territory) ? null : senderOrganization.Address.RussianAddress.Territory,
                                Building = string.IsNullOrEmpty(senderOrganization?.Address?.RussianAddress?.Building) ? null : senderOrganization.Address.RussianAddress.Building
                            } : null;

            document.Sellers = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationInfoUtd970[]
            {
                new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationInfoUtd970
                {
                    Item = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationDetailsUtd970
                    {
                        Inn = senderOrganization.Inn,
                        Kpp = senderOrganization.Kpp,
                        OrgType = senderOrganization.Inn.Length == 12 ? Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.OrganizationType_DatabaseOrder.Item1 : Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.OrganizationType_DatabaseOrder.Item2,
                        OrgName = senderOrganization.Name,
                        Address = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AddressUtd970
                        {
                            Item = senderAddress
                        }
                    }
                }
            };

            Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.RussianAddressUtd970 receiverAddress = receiverOrganization?.Address?.RussianAddress != null ?
                new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.RussianAddressUtd970
                {
                    ZipCode = receiverOrganization.Address.RussianAddress.ZipCode,
                    Region = receiverOrganization.Address.RussianAddress.Region,
                    Street = string.IsNullOrEmpty(receiverOrganization?.Address?.RussianAddress?.Street) ? null : receiverOrganization.Address.RussianAddress.Street,
                    City = string.IsNullOrEmpty(receiverOrganization?.Address?.RussianAddress?.City) ? null : receiverOrganization.Address.RussianAddress.City,
                    Locality = string.IsNullOrEmpty(receiverOrganization?.Address?.RussianAddress?.Locality) ? null : receiverOrganization.Address.RussianAddress.Locality,
                    Territory = string.IsNullOrEmpty(receiverOrganization?.Address?.RussianAddress?.Territory) ? null : receiverOrganization.Address.RussianAddress.Territory,
                    Building = string.IsNullOrEmpty(receiverOrganization?.Address?.RussianAddress?.Building) ? null : receiverOrganization.Address.RussianAddress.Building
                } : null;

            document.Buyers = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationInfoUtd970[]
            {
                new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationInfoUtd970
                {
                    Item = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationDetailsUtd970
                    {
                        Inn = receiverOrganization.Inn,
                        Kpp = receiverOrganization.Kpp,
                        OrgType = receiverOrganization.Inn.Length == 12 ? Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.OrganizationType_DatabaseOrder.Item1 : Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.OrganizationType_DatabaseOrder.Item2,
                        OrgName = receiverOrganization.Name,
                        Address = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AddressUtd970
                        {
                            Item = receiverAddress
                        }
                    }
                }
            };

            Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Signer signer;
            if (string.IsNullOrEmpty(senderOrganization.EmchdId))
            {
                var firstMiddleName = _utils.ParseCertAttribute(senderOrganization.Certificate.Subject, "G");
                string signerFirstName = firstMiddleName.IndexOf(" ") > 0 ? firstMiddleName.Substring(0, firstMiddleName.IndexOf(" ")) : string.Empty;
                string signerMiddleName = firstMiddleName.IndexOf(" ") >= 0 && firstMiddleName.Length > firstMiddleName.IndexOf(" ") + 1 ? firstMiddleName.Substring(firstMiddleName.IndexOf(" ") + 1) : string.Empty;

                signer = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Signer
                {
                    Fio = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Fio
                    {
                        FirstName = signerFirstName,
                        MiddleName = signerMiddleName,
                        LastName = _utils.ParseCertAttribute(senderOrganization.Certificate.Subject, "SN")
                    },
                    Position = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerPosition
                    {
                        PositionSource = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerPositionPositionSource.Manual,
                        Value = _utils.ParseCertAttribute(senderOrganization.Certificate.Subject, "T")
                    },
                    SignatureTypeSpecified = true,
                    SignatureType = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerSignatureType.Item1,
                    SigningDate = DateTime.Now.ToString("dd.MM.yyyy"),
                    SignerPowersConfirmationMethodSpecified = true,
                    SignerPowersConfirmationMethod = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerSignerPowersConfirmationMethod.Item1
                };
            }
            else
            {
                signer = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Signer
                {
                    Fio = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Fio
                    {
                        FirstName = senderOrganization.EmchdPersonName,
                        MiddleName = senderOrganization.EmchdPersonPatronymicSurname,
                        LastName = senderOrganization.EmchdPersonSurname
                    },
                    Position = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerPosition
                    {
                        PositionSource = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerPositionPositionSource.Manual,
                        Value = senderOrganization.EmchdPersonPosition
                    },
                    SignatureTypeSpecified = true,
                    SignatureType = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerSignatureType.Item1,
                    SigningDate = DateTime.Now.ToString("dd.MM.yyyy"),
                    SignerPowersConfirmationMethodSpecified = true,
                    SignerPowersConfirmationMethod = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.SignerSignerPowersConfirmationMethod.Item3,
                    PowerOfAttorney = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.PowerOfAttorney
                    {
                        Electronic = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Electronic
                        {
                            Item = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Storage
                            {
                                UseDefault = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.StorageUseDefault.@false,
                                FullId = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.StorageFullId
                                {
                                    RegistrationNumber = senderOrganization.EmchdId,
                                    IssuerInn = senderOrganization.Inn,
                                    RepresentativeInn = senderOrganization.EmchdPersonInn
                                }
                            }
                        }
                    }
                };
            }

            document.Signers = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Signers
            {
                Signer = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.Signer[] { signer }
            };

            int docLineCount = d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice ? d.DocGoodsDetailsIs.Count : d.Details.Count;
            document.DocumentShipments = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.DocumentRequisitesType[]
            {
                                new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.DocumentRequisitesType
                                {
                                    DocumentName = "Документ об отгрузке товаров (выполнении работ), передаче имущественных прав (документ об оказании услуг)",
                                    DocumentNumber = documentNumber,
                                    DocumentDate = d.DeliveryDate?.Date.ToString("dd.MM.yyyy") ?? DateTime.Now.ToString("dd.MM.yyyy")
                                }
            };

            var details = new List<Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItem>();

            if (d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice)
            {
                foreach (var docDetail in docDetails)
                {
                    var docJournalDetail = docDetail.DocDetailI;
                    var refGood = docDetail.Good;

                    if (refGood == null)
                        continue;

                    var docGoodDetailLabels = detailsLabels.Where(l => l.IdGood == docJournalDetail.IdGood).ToList();

                    if (considerOnlyLabeledGoods && docGoodDetailLabels.Count == 0)
                        continue;

                    var barCode = docDetail.ItemVendorCode;

                    string countryCode = docDetail.CountryCode;

                    int quantity;

                    if (considerOnlyLabeledGoods)
                        quantity = docGoodDetailLabels.Count;
                    else
                        quantity = docJournalDetail.Quantity;

                    var vat = (decimal)Math.Round(docJournalDetail.TaxSumm * quantity, 2);
                    var subtotal = Math.Round(quantity * ((decimal)docJournalDetail.Price - (decimal)docJournalDetail.DiscountSumm), 2);

                    var detail = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItem
                    {
                        Product = refGood.Name,
                        Unit = Properties.Settings.Default.DefaultUnit,
                        Quantity = quantity,
                        QuantitySpecified = true,
                        VatSpecified = true,
                        Vat = vat,
                        PriceSpecified = true,
                        Price = (decimal)Math.Round(docJournalDetail.Price - docJournalDetail.DiscountSumm - docJournalDetail.TaxSumm, 2),
                        SubtotalSpecified = true,
                        Subtotal = subtotal,
                        SubtotalWithVatExcludedSpecified = true,
                        SubtotalWithVatExcluded = subtotal - vat,
                        ItemVendorCode = barCode
                    };

                    if (!string.IsNullOrEmpty(refGood.CustomsNo))
                    {
                        detail.CustomsDeclarations = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemCustomsDeclaration[]
                        {
                            new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemCustomsDeclaration
                            {
                                Country = countryCode,
                                DeclarationNumber = refGood.CustomsNo
                            }
                        };
                    }

                    switch (docJournalDetail.TaxRate)
                    {
                        case 0:
                            detail.TaxRate = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.TaxRateWithTwentyTwoPercent.ZeroPercent;
                            break;
                        case 10:
                            detail.TaxRate = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.TaxRateWithTwentyTwoPercent.TenPercent;
                            break;
                        //case 18:
                        //    detail.TaxRate = Diadoc.Api.DataXml.Utd820.Hyphens.TaxRateUcd736AndUtd820.Item18;
                        //    break;
                        case 20:
                            detail.TaxRate = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.TaxRateWithTwentyTwoPercent.TwentyPercent;
                            break;
                        default:
                            detail.TaxRate = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.TaxRateWithTwentyTwoPercent.NoVat;
                            break;
                    }

                    decimal idGood = docJournalDetail.IdGood;

                    if (docGoodDetailLabels.Count > 0)
                    {
                        detail.ItemMark = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemItemMark.Item4;
                        detail.ItemIdentificationNumbers = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemItemIdentificationNumber[1];
                        detail.ItemIdentificationNumbers[0] = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemItemIdentificationNumber
                        {
                            ItemsElementName = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ItemsChoiceType[docGoodDetailLabels.Count],
                            Items = new string[docGoodDetailLabels.Count]
                        };

                        int j = 0;
                        foreach (var doc in docGoodDetailLabels)
                        {
                            detail.ItemIdentificationNumbers[0].ItemsElementName[j] = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ItemsChoiceType.Unit;
                            detail.ItemIdentificationNumbers[0].Items[j] = doc.DmLabel;
                            j++;
                        }
                    }

                    details.Add(detail);
                }
            }
            else
            {
                foreach (var docJournalDetail in d.Details)
                {
                    var refGood = _abt.RefGoods?
                    .FirstOrDefault(r => r.Id == docJournalDetail.IdGood);

                    if (refGood == null)
                        continue;

                    var docGoodDetailLabels = detailsLabels.Where(l => l.IdGood == docJournalDetail.IdGood).ToList();

                    if (considerOnlyLabeledGoods && docGoodDetailLabels.Count == 0)
                        continue;

                    var barCode = _abt.RefBarCodes?
                        .FirstOrDefault(b => b.IdGood == docJournalDetail.IdGood && b.IsPrimary == false)?
                        .BarCode;

                    string countryCode = _abt.SelectSingleValue("select NUM_CODE from REF_COUNTRIES where id in" +
                        $"(select ID_COUNTRY from REF_GOODS where ID = {refGood.Id})");

                    int quantity;

                    if (considerOnlyLabeledGoods)
                        quantity = docGoodDetailLabels.Count;
                    else
                        quantity = docJournalDetail.Quantity;

                    var subtotal = Math.Round(quantity * ((decimal)docJournalDetail.Price - (decimal)docJournalDetail.DiscountSumm), 2);
                    var detail = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItem
                    {
                        Product = refGood.Name,
                        Unit = Properties.Settings.Default.DefaultUnit,
                        Quantity = docJournalDetail.Quantity,
                        QuantitySpecified = true,
                        PriceSpecified = true,
                        Price = (decimal)Math.Round(docJournalDetail.Price - docJournalDetail.DiscountSumm, 2),
                        SubtotalSpecified = true,
                        Subtotal = subtotal,
                        SubtotalWithVatExcludedSpecified = true,
                        WithoutVat = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemWithoutVat.@true,
                        SubtotalWithVatExcluded = subtotal,
                        ItemVendorCode = barCode
                    };

                    if (!string.IsNullOrEmpty(refGood.CustomsNo))
                    {
                        detail.CustomsDeclarations = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemCustomsDeclaration[]
                        {
                            new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemCustomsDeclaration
                            {
                                Country = countryCode,
                                DeclarationNumber = refGood.CustomsNo
                            }
                        };
                    }

                    if (docGoodDetailLabels.Count > 0)
                    {
                        detail.ItemMark = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemItemMark.Item4;
                        detail.ItemIdentificationNumbers = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemItemIdentificationNumber[1];
                        detail.ItemIdentificationNumbers[0] = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItemItemIdentificationNumber
                        {
                            ItemsElementName = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ItemsChoiceType[docGoodDetailLabels.Count],
                            Items = new string[docGoodDetailLabels.Count]
                        };

                        int j = 0;
                        foreach (var doc in docGoodDetailLabels)
                        {
                            detail.ItemIdentificationNumbers[0].ItemsElementName[j] = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ItemsChoiceType.Unit;
                            detail.ItemIdentificationNumbers[0].Items[j] = doc.DmLabel;
                            j++;
                        }
                    }

                    details.Add(detail);
                }
            }

            document.Table.Item = details.ToArray();

            if (d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice)
                document.Table.VatSpecified = true;
            else
                document.Table.WithoutVat = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableWithoutVat.@true;

            if (considerOnlyLabeledGoods)
            {
                document.Table.Total = details.Select(i => i.Subtotal).Sum();
                document.Table.TotalWithVatExcluded = details.Select(i => i.SubtotalWithVatExcluded).Sum();

                if (d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice)
                    document.Table.Vat = details.Select(i => i.Vat).Sum();
            }
            else
            {
                if (d.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice)
                {
                    document.Table.Total = (decimal)d.DocGoodsI.TotalSumm;
                    document.Table.Vat = (decimal)d.DocGoodsI.TaxSumm;
                    document.Table.TotalWithVatExcluded = (decimal)(d.DocGoodsI.TotalSumm - d.DocGoodsI.TaxSumm);
                }
                else
                {
                    document.Table.Total = (decimal)(d?.DocGoods?.TotalSumm ?? 0);
                    document.Table.TotalWithVatExcluded = (decimal)(d?.DocGoods?.TotalSumm ?? 0);
                }
            }

            return document;
        }

        public Kontragent GetMyKontragent(string inn, string kpp = null)
        {
            var kontragent = _edo.GetKontragentByInnKpp(inn, kpp);

            List<X509Certificate2> personalCertificates = GetPersonalCertificates();

            var authoritySignDocuments = (from cust in _abt.RefCustomers
                                          where cust.Inn == inn && (cust.Kpp == kpp || kpp == null)
                                          join a in _abt.RefAuthoritySignDocuments
                                          on cust.Id equals a.IdCustomer
                                          where a.IsMainDefault
                                          select a)?.FirstOrDefault();

            if (authoritySignDocuments != null && !string.IsNullOrEmpty(authoritySignDocuments?.EmchdId))
            {
                kontragent.Certificate = personalCertificates
                    .Where(c => authoritySignDocuments.Inn == _utils.ParseCertAttribute(c.Subject, "ИНН").TrimStart('0') && _utils.IsCertificateValid(c))
                    .OrderByDescending(c => c.NotBefore).FirstOrDefault(c => string.IsNullOrEmpty(_utils.GetOrgInnFromCertificate(c)));
                kontragent.EmchdId = authoritySignDocuments.EmchdId;
                kontragent.EmchdBeginDate = authoritySignDocuments.EmchdBeginDate;
                kontragent.EmchdEndDate = authoritySignDocuments.EmchdEndDate;
                kontragent.EmchdPersonInn = authoritySignDocuments.Inn;
                kontragent.EmchdPersonSurname = authoritySignDocuments.Surname;
                kontragent.EmchdPersonName = authoritySignDocuments.Name;
                kontragent.EmchdPersonPatronymicSurname = authoritySignDocuments.PatronymicSurname;
                kontragent.EmchdPersonPosition = authoritySignDocuments.Position;
            }

            if (kontragent.Certificate == null || string.IsNullOrEmpty(authoritySignDocuments?.EmchdId))
            {
                kontragent.Certificate = personalCertificates
                    .Where(c => inn == _utils.GetOrgInnFromCertificate(c) && _utils.IsCertificateValid(c))
                    .OrderByDescending(c => c.NotBefore).FirstOrDefault();

                kontragent.SetNullEmchdValues();
            }

            if (kontragent.Certificate == null)
                throw new Exception($"Не найден сертификат организации с ИНН {kontragent.Inn}.");

            return kontragent;
        }

        public Kontragent GetConsignorFromDocument(UniversalTransferDocumentV2 document, decimal? idSubdivision = null)
        {
            if (idSubdivision == null)
                idSubdivision = document?.IdSubdivision;

            if (idSubdivision == null)
                throw new Exception("Не найдена организация");

            Kontragent kontragent = null;

            if (_consignors.Any(c => c.Key == idSubdivision.Value))
            {
                kontragent = _consignors.First(c => c.Key == idSubdivision.Value).Value;
            }
            else
            {
                RefCustomer consignor = (from s in _abt.RefSubdivisions
                                         where s.Id == idSubdivision join c in _abt.RefContractors on s.OldId equals c.Id
                                         where c.DefaultCustomer != null join r in _abt.RefCustomers on c.DefaultCustomer equals r.Id
                                         select r)?.FirstOrDefault();

                if (consignor == null)
                    throw new Exception("Не найден комитент");

                kontragent = GetMyKontragent(consignor.Inn, consignor.Kpp);

                if (kontragent == null)
                    throw new Exception("Не найден контрагент");

                _consignors.Add(idSubdivision.Value, kontragent);
            }

            if (kontragent == null)
                throw new Exception("Не найден контрагент");

            return kontragent;
        }

        private async Task<DocComissionEdoProcessing> SendComissionDocumentForHonestMark(Kontragent myOrganization, UniversalTransferDocumentV2 document, DocJournal docJournal, Kontragent consignor = null, IEnumerable<DocGoodsDetailsLabels> labels = null, int? numberOfDocument = null)
        {
            if (myOrganization == null)
                throw new Exception("Не задана организация.");

            if (myOrganization.Certificate == null)
                throw new Exception("Не задан сертификат организации.");

            if (document == null)
                throw new Exception("Не задан документ.");

            if (labels == null)
            {
                labels = from label in _abt.DocGoodsDetailsLabels where label.IdDocSale == document.IdDocMaster select label;

                if (document?.Details?
                    .Exists(g => _abt.RefItems.Any(r => r.IdName == 30071 && r.IdGood == g.IdGood && r.Quantity == 1) &&
                    (labels?.Where(l => l.IdGood == g.IdGood)?.Count() ?? 0) < g.Quantity) ?? false)
                {
                    throw new Exception("Для некоторых товаров отсутствует маркировка.");
                }
                else if (document?.Details?
                    .Exists(g => _abt.RefItems.Any(r => r.IdName == 30071 && r.IdGood == g.IdGood && r.Quantity == 1) &&
                    (labels?.Where(l => l.IdGood == g.IdGood)?.Count() ?? 0) > g.Quantity) ?? false)
                {
                    throw new Exception("В данном документе есть избыток кодов маркировки.");
                }
            }

            if (labels.Count() == 0)
                return null;

            if (document.IdSubdivision == null)
                throw new Exception("Не задана организация-комитент");

            if (consignor == null)
                consignor = GetConsignorFromDocument(null, document.IdSubdivision);

            if (consignor == null)
                throw new Exception("Не найден комитент.");

            var crypt = new WinApiCryptWrapper(consignor.Certificate);

            Diadoc.Api.Proto.Events.Message message;
            string documentNumber;
            try
            {
                _edo.Authenticate(false, consignor.Certificate, consignor.Inn);

                if(numberOfDocument != null)
                    documentNumber = numberOfDocument.Value > 9 ? $"{document.InvoiceNumber}-{numberOfDocument.Value}" : $"{document.InvoiceNumber}-0{numberOfDocument.Value}";
                else
                    documentNumber = document.InvoiceNumber;

                var comissionDocument = await CreateShipmentDocumentAsync(docJournal, consignor, myOrganization, document.Details.ToList(), labels.ToList(), documentNumber, document.Employee, true);
                var generatedFile = await _edo.GenerateTitleXmlAsync("UniversalTransferDocument", "ДОП", "utd970_05_03_01", 0, comissionDocument);
                byte[] signature = crypt.Sign(generatedFile.Content, true);

                var signedContent = new Diadoc.Api.Proto.Events.SignedContent
                {
                    Content = generatedFile.Content
                };

                signedContent.Signature = signature ?? throw new Exception("Не удалось вычислить подпись.");

                Diadoc.Api.Proto.Events.PowerOfAttorneyToPost consignorPowerOfAttorneyToPost = null;

                if (!string.IsNullOrEmpty(consignor.EmchdId))
                    consignorPowerOfAttorneyToPost = new Diadoc.Api.Proto.Events.PowerOfAttorneyToPost
                    {
                        UseDefault = false,
                        FullId = new Diadoc.Api.Proto.PowersOfAttorney.PowerOfAttorneyFullId
                        {
                            RegistrationNumber = consignor.EmchdId,
                            IssuerInn = consignor.Inn,
                            RepresentativeInn = consignor.EmchdPersonInn
                        }
                    };

                message = await _edo.SendXmlDocumentAsync(consignor.OrgId, myOrganization.OrgId, false, signedContent, "ДОП", consignorPowerOfAttorneyToPost);
            }
            finally
            {
                _edo.Authenticate(false, myOrganization.Certificate, myOrganization.Inn);
            }

            if (message == null)
                throw new Exception("Не задано сообщение.");

            var buyerDocument = await CreateBuyerShipmentDocumentAsync(myOrganization);
            crypt.InitializeCertificate(myOrganization.Certificate);

            var attachments = message.Entities.Where(t => t.AttachmentType == Diadoc.Api.Proto.Events.AttachmentType.UniversalTransferDocument).Select(ent =>
            {
                var generatedBuyerFile = _edo.GenerateTitleXml("UniversalTransferDocument", "ДОП", "utd970_05_03_01", 1, buyerDocument, message.MessageId, ent.EntityId);
                return new Diadoc.Api.Proto.Events.RecipientTitleAttachment
                {
                    ParentEntityId = ent.EntityId,
                    SignedContent = new Diadoc.Api.Proto.Events.SignedContent
                    {
                        Content = generatedBuyerFile.Content,
                        Signature = crypt.Sign(generatedBuyerFile.Content, true)
                    }
                };
            });

            Diadoc.Api.Proto.Events.PowerOfAttorneyToPost myOrganizationPowerOfAttorneyToPost = null;

            if (!string.IsNullOrEmpty(myOrganization.EmchdId))
                myOrganizationPowerOfAttorneyToPost = new Diadoc.Api.Proto.Events.PowerOfAttorneyToPost
                {
                    UseDefault = false,
                    FullId = new Diadoc.Api.Proto.PowersOfAttorney.PowerOfAttorneyFullId
                    {
                        RegistrationNumber = myOrganization.EmchdId,
                        IssuerInn = myOrganization.Inn,
                        RepresentativeInn = myOrganization.EmchdPersonInn
                    }
                };

            await _edo.SendPatchRecipientXmlDocumentAsync(message.MessageId, (int)Diadoc.Api.Proto.DocumentType.UniversalTransferDocument,
                attachments, myOrganizationPowerOfAttorneyToPost);

            var entity = message.Entities.FirstOrDefault(t => t.AttachmentType == Diadoc.Api.Proto.Events.AttachmentType.UniversalTransferDocument &&
                           t?.DocumentInfo?.DocumentNumber == documentNumber);

            var fileNameLength = entity?.DocumentInfo?.FileName?.LastIndexOf('.') ?? 0;

            if (fileNameLength < 0)
                fileNameLength = entity.DocumentInfo.FileName.Length;

            var docComissionProcessing = new DocComissionEdoProcessing
            {
                Id = Guid.NewGuid().ToString(),
                MessageId = message.MessageId,
                EntityId = entity?.EntityId,
                IdDoc = document.IdDocMaster,
                SenderInn = consignor.Inn,
                ReceiverInn = myOrganization.Inn,
                DocStatus = 1,
                UserName = UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser,
                FileName = entity?.DocumentInfo?.FileName?.Substring(0, fileNameLength),
                DocDate = DateTime.Now,
                DeliveryDate = document?.InvoiceDate
            };

            if (!document.IsMarked)
                docComissionProcessing.DocStatus = 2;

            return docComissionProcessing;
        }
    }
}
