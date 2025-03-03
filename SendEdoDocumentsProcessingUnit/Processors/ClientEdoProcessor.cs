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
                        string employee = _abt.SelectSingleValue("select const_value from ref_const where id = 1200");
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

                        foreach (var myOrganization in orgs)
                        {
                            var totalDocProcessings = new List<Utils.AsyncOperationEntity<DocEdoProcessing>>();
                            try
                            {
                                var certs = personalCertificates.Where(c => myOrganization.EmchdPersonInn == _utils.ParseCertAttribute(c.Subject, "ИНН").TrimStart('0') && _utils.IsCertificateValid(c)).OrderByDescending(c => c.NotBefore);
                                myOrganization.Certificate = certs.FirstOrDefault(c => string.IsNullOrEmpty(_utils.GetOrgInnFromCertificate(c)));

                                if (!_edo.Authenticate(false, myOrganization.Certificate, myOrganization.Inn))
                                    throw new Exception("Не удалось авторизоваться в системе по сертификату.");

                                _edo.SetOrganizationParameters(myOrganization);

                                var addressSender = myOrganization?.Address?.RussianAddress?.ZipCode +
                                                    (string.IsNullOrEmpty(myOrganization?.Address?.RussianAddress?.City) ? "" : $", {myOrganization.Address.RussianAddress.City}") +
                                                    (string.IsNullOrEmpty(myOrganization?.Address?.RussianAddress?.Street) ? "" : $", {myOrganization.Address.RussianAddress.Street}") +
                                                    (string.IsNullOrEmpty(myOrganization?.Address?.RussianAddress?.Building) ? "" : $", {myOrganization.Address.RussianAddress.Building}");

                                var signerDetails = _edo.GetExtendedSignerDetails(Diadoc.Api.Proto.Invoicing.Signers.DocumentTitleType.UtdSeller);
                                var counteragents = _edo.GetOrganizations(myOrganization.OrgId);

                                UniversalTransferDocument.DbContext = _abt;
                                IEnumerable<UniversalTransferDocument> docs = (from doc in docJournals
                                                                               where doc.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice && doc.DocDatetime >= dateTimeLastPeriod
                                                                               join docMaster in _abt.DocJournals on doc.IdDocMaster equals docMaster.Id
                                                                               where docMaster.DocDatetime >= dateTimeLastPeriod
                                                                               join docGoods in _abt.DocGoods on docMaster.Id equals docGoods.IdDoc
                                                                               join customer in _abt.RefCustomers on docGoods.IdSeller equals customer.IdContractor
                                                                               where customer.Inn == myOrganization.Inn && customer.Kpp == myOrganization.Kpp
                                                                               let isMarked = (from label in _abt.DocGoodsDetailsLabels where label.IdDocSale == docMaster.Id select label).Count() > 0
                                                                               let honestMarkStatus = (from docComissionEdoProcessing in _abt.DocComissionEdoProcessings
                                                                                                       where isMarked && docComissionEdoProcessing.IdDoc == docMaster.Id
                                                                                                       orderby docComissionEdoProcessing.DocDate descending
                                                                                                       select docComissionEdoProcessing)
                                                                               let docEdoProcessing = (from docEdo in _abt.DocEdoProcessings
                                                                                                       where docEdo.IdDoc == docMaster.Id && docEdo.DocType == updDocType
                                                                                                       orderby docEdo.DocDate descending
                                                                                                       select docEdo)
                                                                               where docEdoProcessing.Count() == 0
                                                                               join buyerContractor in _abt.RefContractors
                                                                               on docGoods.IdCustomer equals buyerContractor.Id
                                                                               join buyerCustomer in _abt.RefCustomers
                                                                               on buyerContractor.DefaultCustomer equals buyerCustomer.Id
                                                                               join refRefTag in _abt.RefRefTags
                                                                               on buyerCustomer.Id equals refRefTag.IdObject
                                                                               where refRefTag.IdTag == 242
                                                                               let refRefTagValue = refRefTag.TagValue
                                                                               select new UniversalTransferDocument
                                                                               {
                                                                                   Total = doc.DocGoodsI.TotalSumm,
                                                                                   Vat = doc.DocGoodsI.TaxSumm,
                                                                                   TotalWithVatExcluded = (doc.DocGoodsI.TotalSumm - doc.DocGoodsI.TaxSumm),
                                                                                   DocJournal = doc,
                                                                                   DocJournalNumber = docMaster.Code,
                                                                                   DocumentNumber = doc.Code,
                                                                                   ActStatus = docMaster.ActStatus,
                                                                                   OrgId = myOrganization.OrgId,
                                                                                   SenderName = myOrganization.Name,
                                                                                   SenderInnKpp = myOrganization.Inn + "/" + myOrganization.Kpp,
                                                                                   SenderAddress = addressSender,
                                                                                   BuyerCustomer = buyerCustomer,
                                                                                   SellerContractor = docMaster.DocGoods.Seller,
                                                                                   BuyerContractor = buyerContractor,
                                                                                   ProcessingStatus = honestMarkStatus,
                                                                                   IsMarked = isMarked,
                                                                                   ActStatusForSendFromTraderStr = refRefTagValue
                                                                               })?.ToList();

                                docs = docs?/*.AsParallel()?*/.Where(u => 
                                {
                                    var permissionStatus = FilterByStatuses(u.ActStatusForSendFromTraderStr);
                                    return permissionStatus > 0 && u.ActStatus >= permissionStatus && _abt.Database.SqlQuery<int>(
                                        $"select count(*) from log_actions where id_object = {u.DocJournal.IdDocMaster} and id_action = {permissionStatus} and action_datetime > sysdate - 14")
                                        .First() > 0;
                                })?.Select(u =>
                                {
                                    try
                                    {
                                        return u.Init(_abt);
                                    }
                                    catch(Exception ex)
                                    {
                                        _log.Log(ex);
                                        MailReporter.Add(ex, $"Ошибка в документе {u.DocJournal.Code}: ");
                                        return null;
                                    }
                                }
                                )?.Where(u => u != null)?.ToList();

                                int position = 0, block = 10, count = docs.Count();
                                docs = docs ?? new List<UniversalTransferDocument>();
                                var errors = new List<Utils.AsyncOperationEntity<DocEdoProcessing>>();

                                while (count > position)
                                {
                                    var length = count - position > block ? block : count - position;
                                    var docsFromBlock = docs.Skip(position).Take(length);

                                    var tasks = docsFromBlock.Select((doc) =>
                                    {
                                        var receiver = counteragents.FirstOrDefault(r => r.Inn == doc.BuyerInn);
                                        var task = GetDocEdoProcessingAfterSending(myOrganization, receiver, doc, signerDetails, employee);
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
                                    else
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

        private async Task<Utils.AsyncOperationEntity<DocEdoProcessing>> GetDocEdoProcessingAfterSending(Kontragent myOrganization, Kontragent receiver, UniversalTransferDocument doc, 
            Diadoc.Api.Proto.Invoicing.Signers.ExtendedSignerDetails signerDetails, string employee)
        {
            var operationEntityResult = new Utils.AsyncOperationEntity<DocEdoProcessing>();

            try
            {
                if (doc.IsMarked)
                    if ((doc.ProcessingStatus as DocComissionEdoProcessing)?.DocStatus != 2 && (doc.ProcessingStatus as DocComissionEdoProcessing)?.DocStatus != 1)
                    {
                        DocComissionEdoProcessing docComissionEdoProcessing = null;
                        var labels = (from label in _abt.DocGoodsDetailsLabels where label.IdDocSale == doc.CurrentDocJournalId select label).ToList();

                        if ((doc.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice &&
                        (doc?.DocJournal?.DocGoodsDetailsIs?.Exists(g => _abt.RefItems.Any(r => r.IdName == 30071 && r.IdGood == g.IdGood && r.Quantity == 1) &&
                        (labels?.Where(l => l.IdGood == g.IdGood)?.Count() ?? 0) < g.Quantity) ?? false)) ||
                        (doc.DocJournal.IdDocType != (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice &&
                        (doc?.DocJournal?.Details?.Exists(g => _abt.RefItems.Any(r => r.IdName == 30071 && r.IdGood == g.IdGood && r.Quantity == 1) &&
                        (labels?.Where(l => l.IdGood == g.IdGood)?.Count() ?? 0) < g.Quantity) ?? false)))
                        {
                            throw new Exception("Для некоторых товаров отсутствует маркировка.");
                        }
                        else if ((doc.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice &&
                            (doc?.DocJournal?.DocGoodsDetailsIs?.Exists(g => _abt.RefItems.Any(r => r.IdName == 30071 && r.IdGood == g.IdGood && r.Quantity == 1) &&
                            (labels?.Where(l => l.IdGood == g.IdGood)?.Count() ?? 0) > g.Quantity) ?? false)) ||
                            (doc.DocJournal.IdDocType != (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice &&
                            (doc?.DocJournal?.Details?.Exists(g => _abt.RefItems.Any(r => r.IdName == 30071 && r.IdGood == g.IdGood && r.Quantity == 1) &&
                            (labels?.Where(l => l.IdGood == g.IdGood)?.Count() ?? 0) > g.Quantity) ?? false)))
                        {
                            throw new Exception("В данном документе есть избыток кодов маркировки.");
                        }

                        var consignor = GetConsignorFromDocument(null, doc.IdSubdivision);

                        if (consignor == null)
                            throw new Exception("Не найден комитент.");

                        docComissionEdoProcessing = await SendComissionDocumentForHonestMark(myOrganization, doc, employee, consignor, labels);

                        lock (locker)
                        {
                            _abt.DocComissionEdoProcessings.Add(docComissionEdoProcessing);
                            _abt.SaveChanges();
                        }

                        doc.ProcessingStatus = docComissionEdoProcessing;
                    }

                var universalDocument = await GetUniversalDocumentAsync(doc, myOrganization, employee, signerDetails);

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
                        IdDoc = doc.CurrentDocJournalId,
                        DocDate = DateTime.Now,
                        UserName = UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser,
                        ReceiverName = receiver.Name,
                        ReceiverInn = receiver.Inn,
                        DocType = (int)EdiProcessingUnit.Enums.DocEdoType.Upd
                    };

                    doc.EdoProcessing = docProcessing;

                    if (doc.ProcessingStatus as DocComissionEdoProcessing != null)
                    {
                        var docComissionEdoProcessing = doc.ProcessingStatus as DocComissionEdoProcessing;
                        docProcessing.IdComissionDocument = docComissionEdoProcessing.Id;
                        docProcessing.ComissionDocument = docComissionEdoProcessing;
                        docComissionEdoProcessing.MainDocuments.Add(docProcessing);
                    }
                    
                    operationEntityResult.Entity = docProcessing;
                    operationEntityResult.Description = $"Документ {doc?.DocJournal?.Code} успешно отправлен и сохранён.";
                }
            }
            catch(Exception ex)
            {
                operationEntityResult.SetException(ex, $"Произошла ошибка при отправке документа {doc.DocJournal.Code}");
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
                        string employee = _abt.SelectSingleValue("select const_value from ref_const where id = 1200");

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

                                _edo.SetOrganizationParameters(myOrganization);

                                var addressSender = myOrganization?.Address?.RussianAddress?.ZipCode +
                                    (string.IsNullOrEmpty(myOrganization?.Address?.RussianAddress?.City) ? "" : $", {myOrganization.Address.RussianAddress.City}") +
                                    (string.IsNullOrEmpty(myOrganization?.Address?.RussianAddress?.Street) ? "" : $", {myOrganization.Address.RussianAddress.Street}") +
                                    (string.IsNullOrEmpty(myOrganization?.Address?.RussianAddress?.Building) ? "" : $", {myOrganization.Address.RussianAddress.Building}");

                                var signerDetails = _edo.GetExtendedSignerDetails(Diadoc.Api.Proto.Invoicing.Signers.DocumentTitleType.UtdSeller);
                                var counteragents = _edo.GetOrganizations(myOrganization.OrgId);

                                IEnumerable<UniversalTransferDocument> docs = (from doc in docJournals
                                            where doc.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice && doc.DocDatetime >= dateTimeFrom
                                            join docMaster in _abt.DocJournals on doc.IdDocMaster equals docMaster.Id
                                            where docMaster.DocDatetime >= dateTimeFrom
                                            join docGoods in _abt.DocGoods on docMaster.Id equals docGoods.IdDoc
                                            join customer in _abt.RefCustomers on docGoods.IdSeller equals customer.IdContractor
                                            where customer.Inn == myOrganization.Inn && customer.Kpp == myOrganization.Kpp
                                            let isMarked = (from label in _abt.DocGoodsDetailsLabels where label.IdDocSale == docMaster.Id select label).Count() > 0
                                            let honestMarkStatus = (from docComissionEdoProcessing in _abt.DocComissionEdoProcessings
                                                                    where isMarked && docComissionEdoProcessing.IdDoc == docMaster.Id
                                                                    orderby docComissionEdoProcessing.DocDate descending
                                                                    select docComissionEdoProcessing)
                                            let docEdoProcessing = (from docEdo in _abt.DocEdoProcessings
                                                                    where docEdo.IdDoc == docMaster.Id && docEdo.DocType == updDocType
                                                                    orderby docEdo.DocDate descending
                                                                    select docEdo)
                                            where docEdoProcessing.Count() == 0
                                            join buyerContractor in _abt.RefContractors
                                            on docGoods.IdCustomer equals buyerContractor.Id
                                            join buyerCustomer in _abt.RefCustomers
                                            on buyerContractor.DefaultCustomer equals buyerCustomer.Id
                                            join refRefTag in _abt.RefRefTags
                                            on buyerCustomer.Id equals refRefTag.IdObject
                                            where refRefTag.IdTag == 242
                                            let refRefTagValue = refRefTag.TagValue
                                            select new UniversalTransferDocument
                                            {
                                                Total = doc.DocGoodsI.TotalSumm,
                                                Vat = doc.DocGoodsI.TaxSumm,
                                                TotalWithVatExcluded = (doc.DocGoodsI.TotalSumm - doc.DocGoodsI.TaxSumm),
                                                DocJournal = doc,
                                                DocJournalNumber = docMaster.Code,
                                                DocumentNumber = doc.Code,
                                                ActStatus = docMaster.ActStatus,
                                                OrgId = myOrganization.OrgId,
                                                SenderName = myOrganization.Name,
                                                SenderInnKpp = myOrganization.Inn + "/" + myOrganization.Kpp,
                                                SenderAddress = addressSender,
                                                BuyerCustomer = buyerCustomer,
                                                SellerContractor = docMaster.DocGoods.Seller,
                                                BuyerContractor = buyerContractor,
                                                ProcessingStatus = honestMarkStatus,
                                                IsMarked = isMarked,
                                                ActStatusForSendFromTraderStr = refRefTagValue
                                            })?.ToList();

                                docs = docs?.AsParallel()?.Where(u =>
                                {
                                    var permissionStatus = FilterByStatuses(u.ActStatusForSendFromTraderStr);
                                    return permissionStatus > 0 && u.ActStatus >= permissionStatus && _abt.Database.SqlQuery<int>(
                                        $"select count(*) from log_actions where id_object = {u.DocJournal.IdDocMaster} and id_action = {permissionStatus} and action_datetime > sysdate - 14")
                                        .First() > 0;
                                })?.Select(u => u.Init(_abt))?.ToList();

                                foreach (var doc in docs ?? new List<UniversalTransferDocument>())
                                {
                                    try
                                    {
                                        var receiver = counteragents.FirstOrDefault(r => r.Inn == doc.BuyerInn);

                                        if (receiver == null)
                                            continue;

                                        DocComissionEdoProcessing docComissionEdoProcessing = null;
                                        if (doc.IsMarked)
                                            if ((doc.ProcessingStatus as DocComissionEdoProcessing)?.DocStatus != 2 && (doc.ProcessingStatus as DocComissionEdoProcessing)?.DocStatus != 1)
                                            {
                                                docComissionEdoProcessing = SendComissionDocumentForHonestMark(myOrganization, doc, employee).Result;
                                                _abt.DocComissionEdoProcessings.Add(docComissionEdoProcessing);
                                                _abt.SaveChanges();
                                            }

                                        var universalDocument = GetUniversalDocument(doc, myOrganization, doc.Details.ToList(), employee, signerDetails, doc.RefEdoGoodChannel as RefEdoGoodChannel);

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
                                                IdDoc = doc.CurrentDocJournalId,
                                                DocDate = DateTime.Now,
                                                UserName = UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser,
                                                ReceiverName = receiver.Name,
                                                ReceiverInn = receiver.Inn,
                                                DocType = (int)EdiProcessingUnit.Enums.DocEdoType.Upd
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
                                            MailReporter.Add($"Документ {doc.DocJournal.Code} успешно отправлен и сохранён.");
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
                                        MailReporter.Add(ex, $"Произошла ошибка при отправке документа {doc.DocJournal.Code}:\r\n");
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

        private async Task<Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocument> GetUniversalDocumentAsync(
            UniversalTransferDocument doc, Kontragent myOrganization, string employee = null, Diadoc.Api.Proto.Invoicing.Signers.ExtendedSignerDetails signerDetails = null)
        {
            if (doc.DocJournal == null)
                return null;

            if (doc.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice && doc.DocJournal.DocMaster == null)
                return null;

            if (doc.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice && doc.DocJournal.DocGoodsI == null)
                return null;

            if (doc.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Translocation && doc.DocJournal.DocGoods == null)
                return null;

            var result = await Task.Run(() =>
            {
                var universalDocument = GetUniversalDocument(doc, myOrganization, doc.Details.ToList(), employee, signerDetails, doc.RefEdoGoodChannel as RefEdoGoodChannel);
                return universalDocument;
            });

            return result;
        }

        private Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocument GetUniversalDocument(
            UniversalTransferDocument d, Kontragent organization, List<UniversalTransferDocumentDetail> docDetails, string employee = null, Diadoc.Api.Proto.Invoicing.Signers.ExtendedSignerDetails signerDetails = null, RefEdoGoodChannel edoGoodChannel = null)
        {
            if (d.DocJournal == null)
                return null;

            if (d.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice && d.DocJournal.DocMaster == null)
                return null;

            if (d.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice && d.DocJournal.DocGoodsI == null)
                return null;

            if (d.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Translocation && d.DocJournal.DocGoods == null)
                return null;

            var document = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocument()
            {
                Function = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocumentFunction.СЧФДОП,
                DocumentNumber = d.DocJournal.Code,
                DocumentDate = d.DocJournal.DeliveryDate?.Date.ToString("dd.MM.yyyy") ?? DateTime.Now.ToString("dd.MM.yyyy"),
                Currency = Properties.Settings.Default.DefaultCurrency,
                Table = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTable
                {
                    TotalSpecified = true,
                    TotalWithVatExcludedSpecified = true
                },
                TransferInfo = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.TransferInfo
                {
                    OperationInfo = "Товары переданы",
                    TransferDate = d.DocJournal.DeliveryDate?.Date.ToString("dd.MM.yyyy") ?? DateTime.Now.ToString("dd.MM.yyyy")
                }
            };

            if (!string.IsNullOrEmpty(organization.EmchdId))
                document.DocumentCreator = $"{organization.EmchdPersonSurname} {organization.EmchdPersonName} {organization.EmchdPersonPatronymicSurname}";
            else
                document.DocumentCreator = _utils.ParseCertAttribute(organization.Certificate.Subject, "SN") + " " + _utils.ParseCertAttribute(organization.Certificate.Subject, "G");

            if (d.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice)
            {
                document.Table.VatSpecified = true;
                document.Table.Total = (decimal)d.DocJournal.DocGoodsI.TotalSumm;
                document.Table.Vat = (decimal)d.DocJournal.DocGoodsI.TaxSumm;
                document.Table.TotalWithVatExcluded = (decimal)(d.DocJournal.DocGoodsI.TotalSumm - d.DocJournal.DocGoodsI.TaxSumm);
            }
            else
            {
                document.Table.Total = (decimal)(d?.DocJournal?.DocGoods?.TotalSumm ?? 0);
                document.Table.TotalWithVatExcluded = (decimal)(d?.DocJournal?.DocGoods?.TotalSumm ?? 0);
                document.Table.WithoutVat = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableWithoutVat.@true;
            }

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

            if (d.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice)
            {
                    document.Shippers = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocumentShipper[]
                    {
                            new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.UniversalTransferDocumentShipper
                            {
                                Item = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationDetailsUtd970
                                {
                                    Inn = organization.Inn,
                                    OrgType = organization.Inn.Length == 12 ? Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.OrganizationType_DatabaseOrder.Item1 : Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.OrganizationType_DatabaseOrder.Item2,
                                    Address = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AddressUtd970
                                    {
                                        Item = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ForeignAddressUtd970
                                        {
                                            Country = Properties.Settings.Default.DefaultOrgCountryCode,
                                            Address = d.ShipperAddress
                                        }
                                    },
                                    OrgName = d.ShipperName
                                }
                            }
                    };
            }

                        if (d.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice)
                        {
                            document.Consignees = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationInfoUtd970[]
                        {
                                        new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationInfoUtd970
                                        {
                                            Item = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ExtendedOrganizationDetailsUtd970
                                            {
                                                Inn = d?.BuyerInn,
                                                OrgType = d?.BuyerInn?.Length == 12 ? Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.OrganizationType_DatabaseOrder.Item1 : Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.OrganizationType_DatabaseOrder.Item2,
                                                Address = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AddressUtd970
                                                {
                                                    Item = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.ForeignAddressUtd970
                                                    {
                                                        Country = Properties.Settings.Default.DefaultOrgCountryCode,
                                                        Address = d?.ConsigneeAddress
                                                    }
                                                },
                                                OrgName = d?.ConsigneeName
                                            }
                                        }
                        };
                        }

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
                                                    Country = Properties.Settings.Default.DefaultOrgCountryCode,
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
                                    IssuerInn = organization.Inn
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

            int docLineCount = d.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice ? d.DocJournal.DocGoodsDetailsIs.Count : d.DocJournal.Details.Count;
            document.DocumentShipments = new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.DocumentRequisitesType[]
            {
                                new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.DocumentRequisitesType
                                {
                                    DocumentName = "Счет-фактура и документ об отгрузке товаров (выполнении работ), передаче имущественных прав (документ об оказании услуг)",
                                    DocumentNumber = d.DocJournal.Code,
                                    DocumentDate = d.DocJournal?.DeliveryDate?.Date.ToString("dd.MM.yyyy") ?? DateTime.Now.ToString("dd.MM.yyyy")
                                }
            };

            var details = new List<Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.InvoiceTableItem>();

            if (d.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice)
            {
                var additionalInfoList = new List<Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AdditionalInfo>();

                if (edoGoodChannel != null)
                {
                    if (!string.IsNullOrEmpty(edoGoodChannel.NumberUpdId))
                        additionalInfoList.Add(new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AdditionalInfo { Id = edoGoodChannel.NumberUpdId, Value = d.DocJournal.Code });

                    if (!string.IsNullOrEmpty(edoGoodChannel.OrderNumberUpdId))
                    {
                        if (string.IsNullOrEmpty(d.OrderNumber))
                            throw new Exception("Отсутствует номер заказа покупателя.");

                        additionalInfoList.Add(new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AdditionalInfo { Id = edoGoodChannel.OrderNumberUpdId, Value = d.OrderNumber });
                    }

                    if (!string.IsNullOrEmpty(edoGoodChannel.OrderDateUpdId))
                        additionalInfoList.Add(new Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.AdditionalInfo { Id = edoGoodChannel.OrderDateUpdId, Value = d.DocJournal.DocMaster.DocDatetime.ToString("dd.MM.yyyy") });

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
                foreach (var docDetail in docDetails)
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
                            detail.TaxRate = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.TaxRateUtd970.ZeroPercent;
                            break;
                        case 10:
                            detail.TaxRate = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.TaxRateUtd970.TenPercent;
                            break;
                        //case 18:
                        //    detail.TaxRate = Diadoc.Api.DataXml.Utd820.Hyphens.TaxRateUcd736AndUtd820.Item18;
                        //    break;
                        case 20:
                            detail.TaxRate = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.TaxRateUtd970.TwentyPercent;
                            break;
                        default:
                            detail.TaxRate = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.TaxRateUtd970.NoVat;
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
            }
            else
            {
                foreach (var docJournalDetail in d.DocJournal.Details)
                {
                    var refGood = _abt.RefGoods?
                    .FirstOrDefault(r => r.Id == docJournalDetail.IdGood);

                    if (refGood == null)
                        continue;

                    var barCode = _abt.RefBarCodes?
                        .FirstOrDefault(b => b.IdGood == docJournalDetail.IdGood && b.IsPrimary == false)?
                        .BarCode;

                    string countryCode = _abt.SelectSingleValue("select NUM_CODE from REF_COUNTRIES where id in" +
                        $"(select ID_COUNTRY from REF_GOODS where ID = {refGood.Id})");

                    if (countryCode.Length == 1)
                        countryCode = "00" + countryCode;
                    else if (countryCode.Length == 2)
                        countryCode = "0" + countryCode;

                    var subtotal = Math.Round(docJournalDetail.Quantity * ((decimal)docJournalDetail.Price - (decimal)docJournalDetail.DiscountSumm), 2);
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

                    decimal idGood = docJournalDetail.IdGood, idDoc = (decimal)d.DocJournal.Id;

                    var docGoodDetailLabels = _abt?.Database?
                    .SqlQuery<string>($"select DM_LABEL from doc_goods_details_labels where id_doc_sale = {idDoc} and id_good = {idGood}")?
                    .ToList() ?? new List<string>();

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

                    details.Add(detail);
                }
            }
            document.Table.Item = details.ToArray();

            return document;
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
                                    IssuerInn = senderOrganization.Inn
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
                            detail.TaxRate = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.TaxRateUtd970.ZeroPercent;
                            break;
                        case 10:
                            detail.TaxRate = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.TaxRateUtd970.TenPercent;
                            break;
                        //case 18:
                        //    detail.TaxRate = Diadoc.Api.DataXml.Utd820.Hyphens.TaxRateUcd736AndUtd820.Item18;
                        //    break;
                        case 20:
                            detail.TaxRate = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.TaxRateUtd970.TwentyPercent;
                            break;
                        default:
                            detail.TaxRate = Diadoc.Api.DataXml.ON_NSCHFDOPPR_UserContract_970_05_03_01.TaxRateUtd970.NoVat;
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

        public Kontragent GetConsignorFromDocument(UniversalTransferDocument document, decimal? idSubdivision = null)
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

        private async Task<DocComissionEdoProcessing> SendComissionDocumentForHonestMark(Kontragent myOrganization, UniversalTransferDocument document, string employee, Kontragent consignor = null, IEnumerable<DocGoodsDetailsLabels> labels = null)
        {
            if (myOrganization == null)
                throw new Exception("Не задана организация.");

            if (myOrganization.Certificate == null)
                throw new Exception("Не задан сертификат организации.");

            if (document == null)
                throw new Exception("Не задан документ.");

            var idDocType = document.DocJournal.IdDocType;

            if (labels == null)
            {
                labels = from label in _abt.DocGoodsDetailsLabels where label.IdDocSale == document.CurrentDocJournalId select label;

                if ((document.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice &&
                (document?.DocJournal?.DocGoodsDetailsIs?.Exists(g => _abt.RefItems.Any(r => r.IdName == 30071 && r.IdGood == g.IdGood && r.Quantity == 1) &&
                (labels?.Where(l => l.IdGood == g.IdGood)?.Count() ?? 0) < g.Quantity) ?? false)) ||
                (document.DocJournal.IdDocType != (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice &&
                (document?.DocJournal?.Details?.Exists(g => _abt.RefItems.Any(r => r.IdName == 30071 && r.IdGood == g.IdGood && r.Quantity == 1) &&
                (labels?.Where(l => l.IdGood == g.IdGood)?.Count() ?? 0) < g.Quantity) ?? false)))
                {
                    throw new Exception("Для некоторых товаров отсутствует маркировка.");
                }
                else if ((document.DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice &&
                    (document?.DocJournal?.DocGoodsDetailsIs?.Exists(g => _abt.RefItems.Any(r => r.IdName == 30071 && r.IdGood == g.IdGood && r.Quantity == 1) &&
                    (labels?.Where(l => l.IdGood == g.IdGood)?.Count() ?? 0) > g.Quantity) ?? false)) ||
                    (document.DocJournal.IdDocType != (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice &&
                    (document?.DocJournal?.Details?.Exists(g => _abt.RefItems.Any(r => r.IdName == 30071 && r.IdGood == g.IdGood && r.Quantity == 1) &&
                    (labels?.Where(l => l.IdGood == g.IdGood)?.Count() ?? 0) > g.Quantity) ?? false)))
                {
                    throw new Exception("В данном документе есть избыток кодов маркировки.");
                }
            }

            if (labels.Count() == 0)
                return null;

            if (idDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice)
            {
                if (document.IdSubdivision == null)
                    throw new Exception("Не задана организация-комитент");

                if (consignor == null)
                    consignor = GetConsignorFromDocument(null, document.IdSubdivision);

                if (consignor == null)
                    throw new Exception("Не найден комитент.");

                var crypt = new WinApiCryptWrapper(consignor.Certificate);
                _edo.Authenticate(false, consignor.Certificate, consignor.Inn);

                string documentNumber = document.DocJournal?.Code;
                var comissionDocument = await CreateShipmentDocumentAsync(document.DocJournal, consignor, myOrganization, document.Details.ToList(), labels.ToList(), documentNumber, employee, true);
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
                            IssuerInn = consignor.Inn
                        }
                    };

                var message = await _edo.SendXmlDocumentAsync(consignor.OrgId, myOrganization.OrgId, false, signedContent, "ДОП", consignorPowerOfAttorneyToPost);

                _edo.Authenticate(false, myOrganization.Certificate, myOrganization.Inn);
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
                            IssuerInn = myOrganization.Inn
                        }
                    };

                await _edo.SendPatchRecipientXmlDocumentAsync(message.MessageId, (int)Diadoc.Api.Proto.DocumentType.UniversalTransferDocument,
                    attachments, myOrganizationPowerOfAttorneyToPost);

                var entity = message.Entities.FirstOrDefault(t => t.AttachmentType == Diadoc.Api.Proto.Events.AttachmentType.UniversalTransferDocument &&
                               t?.DocumentInfo?.DocumentNumber == document.DocJournal.Code);

                var fileNameLength = entity.DocumentInfo.FileName.LastIndexOf('.');

                if (fileNameLength < 0)
                    fileNameLength = entity.DocumentInfo.FileName.Length;

                var docComissionProcessing = new DocComissionEdoProcessing
                {
                    Id = Guid.NewGuid().ToString(),
                    MessageId = message.MessageId,
                    EntityId = entity.EntityId,
                    IdDoc = document.CurrentDocJournalId,
                    SenderInn = consignor.Inn,
                    ReceiverInn = myOrganization.Inn,
                    DocStatus = 1,
                    UserName = UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser,
                    FileName = entity.DocumentInfo.FileName.Substring(0, fileNameLength),
                    DocDate = DateTime.Now,
                    DeliveryDate = document?.DocJournal?.DeliveryDate
                };

                if (!document.IsMarked)
                    docComissionProcessing.DocStatus = 2;

                document.ProcessingStatus = docComissionProcessing;
                return docComissionProcessing;
            }
            else
            {
                return null;
            }
        }
    }
}
