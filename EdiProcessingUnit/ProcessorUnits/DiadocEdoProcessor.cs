using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EdiProcessingUnit.Infrastructure;
using DataContextManagementUnit.DataAccess.Contexts.Abt;

namespace EdiProcessingUnit.ProcessorUnits
{
    public class DiadocEdoProcessor : EdoProcessor
    {
        internal AbtDbContext _abtDbContext;
        private const string providerName = "DIADOC";
        public override string ProcessorName => "DiadocEdoProcessor";
        public string OrgInn { get; set; }
        private bool Auth()
        {
            return _edo?.Authenticate(true, null, OrgInn) ?? false;
        }

        private void SetEdoStatus(DocEdoProcessing docEdoProcessing, int newStatus, string textMessage = null, string rejectionReason = null)
        {
            _abtDbContext.Entry(docEdoProcessing)?.Reload();
            docEdoProcessing.DocStatus = newStatus;
            docEdoProcessing.RejectionReason = rejectionReason;
            _abtDbContext?.SaveChanges();

            if (!string.IsNullOrEmpty(textMessage))
                MailReporter.Add(textMessage);
        }

        private void ExecuteChecks(RefCustomer company)
        {
            var updDocType = (int)Enums.DocEdoType.Upd;
            var ucdDocType = (int)Enums.DocEdoType.Ucd;
            var dateTimeFrom = DateTime.Now.AddMonths(-6);

            var markedDocEdoProcessings = (from markedDocEdoProcessing in _abtDbContext.DocEdoProcessings
                                           where markedDocEdoProcessing.HonestMarkStatus == (int)HonestMark.DocEdoProcessingStatus.Sent && 
                                           (markedDocEdoProcessing.DocStatus == (int)Enums.DocEdoSendStatus.Signed || markedDocEdoProcessing.DocStatus == (int)Enums.DocEdoSendStatus.PartialSigned) && 
                                           markedDocEdoProcessing.DocDate > dateTimeFrom
                                           join docJournal in _abtDbContext.DocJournals on markedDocEdoProcessing.IdDoc equals docJournal.Id
                                           join docGoods in _abtDbContext.DocGoods on markedDocEdoProcessing.IdDoc equals docGoods.IdDoc
                                           let sellers = (from cust in _abtDbContext.RefCustomers
                                                          where cust.IdContractor == docGoods.IdSeller && cust.Inn == company.Inn && cust.Kpp == company.Kpp
                                                          select cust)
                                           let customers = (from cust in _abtDbContext.RefCustomers
                                                            where docJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.ReturnFromBuyer && 
                                                            cust.IdContractor == docGoods.IdCustomer && cust.Inn == company.Inn && cust.Kpp == company.Kpp
                                                            select cust)
                                           where docJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.ReturnFromBuyer && customers.Count() > 0 || 
                                           docJournal.IdDocType != (decimal)DataContextManagementUnit.DataAccess.DocJournalType.ReturnFromBuyer && sellers.Count() > 0
                                           select markedDocEdoProcessing);

            foreach (var markedDocEdoProcessing in markedDocEdoProcessings)
            {
                var doc = _edo.GetDocument(markedDocEdoProcessing.MessageId, markedDocEdoProcessing.EntityId);

                if (doc == null)
                    throw new Exception($"Не удалось найти маркированный документ в Диадоке. ID {markedDocEdoProcessing.Id}");

                var lastDocFlow = doc.LastOuterDocflows?.FirstOrDefault(l => l?.OuterDocflow?.DocflowNamedId == "TtGis" && l.OuterDocflow?.Status?.Type != null);
                Diadoc.Api.Proto.OuterDocflows.OuterStatusType? statusDocFlow = lastDocFlow?.OuterDocflow?.Status?.Type;

                if (statusDocFlow == Diadoc.Api.Proto.OuterDocflows.OuterStatusType.Success)
                {
                    markedDocEdoProcessing.HonestMarkStatus = (int)HonestMark.DocEdoProcessingStatus.Processed;

                    if(markedDocEdoProcessing.DocType == (int)Enums.DocEdoType.Ucd && !string.IsNullOrEmpty(markedDocEdoProcessing.IdParent))
                    {
                        var parent = markedDocEdoProcessing.Parent;

                        if (parent == null)
                            parent = _abtDbContext.DocEdoProcessings.FirstOrDefault(p => p.Id == markedDocEdoProcessing.IdParent);
                        else
                            _abtDbContext.Entry(parent)?.Reload();

                        if (parent.HonestMarkStatus == (int)HonestMark.DocEdoProcessingStatus.Processed)
                        {
                            IEnumerable<DocGoodsDetailsLabels> returnedLabels = (from label in _abtDbContext.DocGoodsDetailsLabels
                                                                                 where label.IdDocReturn == markedDocEdoProcessing.IdDoc && label.IdDocSale == parent.IdDoc
                                                                                 select label);

                            if (returnedLabels == null)
                                returnedLabels = new List<DocGoodsDetailsLabels>();

                            foreach (var label in returnedLabels)
                            {
                                label.IdDocSale = null;
                                label.SaleDmLabel = null;
                                label.SaleDateTime = null;
                            }
                        }
                    }

                    _abtDbContext?.SaveChanges();
                    MailReporter.Add($"Маркированный документ {markedDocEdoProcessing.IdDoc} успешно обработан.");
                }
                else if (statusDocFlow == Diadoc.Api.Proto.OuterDocflows.OuterStatusType.Error)
                {
                    markedDocEdoProcessing.HonestMarkStatus = (int)HonestMark.DocEdoProcessingStatus.ProcessingError;

                    var errors = lastDocFlow.OuterDocflow.Status?.Details ?? new List<Diadoc.Api.Proto.OuterDocflows.StatusDetail>();

                    var errorsListStr = new List<string>();
                    foreach (var error in errors)
                        errorsListStr.Add($"Произошла ошибка с кодом:{error.Code} \nОписание:{error.Text}\n");

                    var honestMarkErrorMessage = string.Join("\n\n", errorsListStr);

                    if (honestMarkErrorMessage.Length > 500)
                        honestMarkErrorMessage = honestMarkErrorMessage.Substring(0, 500);

                    markedDocEdoProcessing.HonestMarkErrorMessage = honestMarkErrorMessage;
                    _abtDbContext?.SaveChanges();
                    MailReporter.Add($"Маркированный документ {markedDocEdoProcessing.IdDoc} обработан с ошибками.");
                }
            }

            var sentStatusDocEdoProcessings = (from docEdoProcessing in _abtDbContext.DocEdoProcessings
            where docEdoProcessing.DocType == updDocType && docEdoProcessing.DocStatus == (int)Enums.DocEdoSendStatus.Sent && docEdoProcessing.AnnulmentStatus <= 0 && docEdoProcessing.DocDate > dateTimeFrom
            join docGoods in _abtDbContext.DocGoods on docEdoProcessing.IdDoc equals docGoods.IdDoc
            join customer in _abtDbContext.RefCustomers on docGoods.IdSeller equals customer.IdContractor
            where customer.Inn == company.Inn && customer.Kpp == company.Kpp
            select docEdoProcessing).ToList();

            foreach (var docEdoProcessing in sentStatusDocEdoProcessings)
            {
                var doc = _edo.GetDocument(docEdoProcessing.MessageId, docEdoProcessing.EntityId);

                if (doc.RecipientResponseStatus == Diadoc.Api.Proto.Documents.RecipientResponseStatus.WithRecipientSignature)
                {
                    SetEdoStatus(docEdoProcessing, (int)Enums.DocEdoSendStatus.Signed, $"Документ {docEdoProcessing.IdDoc} подписан контрагентом.");
                }
                else if (doc.RecipientResponseStatus == Diadoc.Api.Proto.Documents.RecipientResponseStatus.RecipientSignatureRequestRejected)
                {
                    string rejectionReason = null;
                    var docflow = _edo.GetDocFlow(docEdoProcessing.MessageId, docEdoProcessing.EntityId, true);
                    var signatureRejection = docflow?.Docflow?.RecipientResponse?.Rejection;

                    if(signatureRejection != null)
                        rejectionReason = signatureRejection.PlainText;

                    if (!string.IsNullOrEmpty(rejectionReason))
                        if (rejectionReason.Length > 2000)
                            rejectionReason = rejectionReason.Substring(0, 2000);

                    SetEdoStatus(docEdoProcessing, (int)Enums.DocEdoSendStatus.Rejected, $"Документ {docEdoProcessing.IdDoc} отклонён контрагентом.", rejectionReason);
                }
                else if (doc.RecipientResponseStatus == Diadoc.Api.Proto.Documents.RecipientResponseStatus.WithRecipientPartiallySignature)
                {
                    SetEdoStatus(docEdoProcessing, (int)Enums.DocEdoSendStatus.PartialSigned, $"Документ {docEdoProcessing.IdDoc} подписан с расхождениями.");
                }
            }

            var correctionDocEdoProcessings = (from correctionDocEdoProcessing in _abtDbContext.DocEdoProcessings
                                               where correctionDocEdoProcessing.DocType == ucdDocType && correctionDocEdoProcessing.DocStatus == (int)Enums.DocEdoSendStatus.Sent && correctionDocEdoProcessing.DocDate > dateTimeFrom
                                               join corDocJournal in _abtDbContext.DocJournals on correctionDocEdoProcessing.IdDoc equals corDocJournal.Id
                                               where (corDocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.ReturnFromBuyer || corDocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Correction)
                                               let docGoodsSeller = (from docGoods in _abtDbContext.DocGoods where corDocJournal.IdDocMaster == docGoods.IdDoc select docGoods.IdSeller)
                                               let docGoodsISeller = (from docGoodsI in _abtDbContext.DocGoodsIs where corDocJournal.IdDocMaster == docGoodsI.IdDoc select docGoodsI.IdSeller)
                                               let customers = (from customer in _abtDbContext.RefCustomers where customer.Inn == company.Inn && customer.Kpp == company.Kpp select customer)
                                               where customers.Any(c => docGoodsSeller.Any(d => d == c.IdContractor) || docGoodsISeller.Any(d => d == c.Id))
                                               select correctionDocEdoProcessing);


            foreach (var docProcessing in correctionDocEdoProcessings)
            {
                var correctionDocument = _edo.GetDocument(docProcessing.MessageId, docProcessing.EntityId);

                if (correctionDocument.RecipientResponseStatus == Diadoc.Api.Proto.Documents.RecipientResponseStatus.WithRecipientSignature)
                {
                    SetEdoStatus(docProcessing, (int)Enums.DocEdoSendStatus.Signed, $"Корректировка {docProcessing.IdDoc} подписана контрагентом.");
                }
                else if (correctionDocument.RecipientResponseStatus == Diadoc.Api.Proto.Documents.RecipientResponseStatus.RecipientSignatureRequestRejected)
                {
                    SetEdoStatus(docProcessing, (int)Enums.DocEdoSendStatus.Rejected, $"Корректировка {docProcessing.IdDoc} отклонена контрагентом.");
                }
                else if (correctionDocument.RecipientResponseStatus == Diadoc.Api.Proto.Documents.RecipientResponseStatus.WithRecipientPartiallySignature)
                {
                    SetEdoStatus(docProcessing, (int)Enums.DocEdoSendStatus.PartialSigned, $"Корректировка {docProcessing.IdDoc} подписана с расхождениями.");
                }
            }

            var comissionDocEdoProcessings = (from comissionDocEdoProcessing in _abtDbContext.DocComissionEdoProcessings
                                              where comissionDocEdoProcessing.EntityId != null && comissionDocEdoProcessing.DocStatus == (int)HonestMark.DocEdoProcessingStatus.Sent && comissionDocEdoProcessing.DocDate > dateTimeFrom
                                              join docGoods in _abtDbContext.DocGoods on comissionDocEdoProcessing.IdDoc equals docGoods.IdDoc
                                              join customer in _abtDbContext.RefCustomers on docGoods.IdSeller equals customer.IdContractor
                                              where customer.Inn == company.Inn && customer.Kpp == company.Kpp
                                              select comissionDocEdoProcessing);

            foreach (var comissionDocEdoProcessing in comissionDocEdoProcessings)
            {
                var doc = _edo.GetDocument(comissionDocEdoProcessing.MessageId, comissionDocEdoProcessing.EntityId);

                if (doc == null)
                    throw new Exception($"Не удалось найти комиссионный документ в Диадоке. ID {comissionDocEdoProcessing.Id}");

                var lastDocFlow = doc.LastOuterDocflows?.FirstOrDefault(l => l?.OuterDocflow?.DocflowNamedId == "TtGis" && l.OuterDocflow?.Status?.Type != null);
                Diadoc.Api.Proto.OuterDocflows.OuterStatusType? statusDocFlow = lastDocFlow?.OuterDocflow?.Status?.Type;

                if (statusDocFlow == Diadoc.Api.Proto.OuterDocflows.OuterStatusType.Success)
                    comissionDocEdoProcessing.DocStatus = (int)HonestMark.DocEdoProcessingStatus.Processed;
                else if (statusDocFlow == Diadoc.Api.Proto.OuterDocflows.OuterStatusType.Error)
                {
                    comissionDocEdoProcessing.DocStatus = (int)HonestMark.DocEdoProcessingStatus.ProcessingError;

                    var errors = lastDocFlow.OuterDocflow.Status?.Details ?? new List<Diadoc.Api.Proto.OuterDocflows.StatusDetail>();

                    var errorsListStr = new List<string>();
                    foreach (var error in errors)
                        errorsListStr.Add($"Произошла ошибка с кодом:{error.Code} \nОписание:{error.Text}\n");

                    comissionDocEdoProcessing.ErrorMessage = string.Join("\n\n", errorsListStr);
                }
            }

            var docsForAnnulmentProcessing = from docForAnnulmentProcessing in _abtDbContext.DocEdoProcessings
                                             where docForAnnulmentProcessing.AnnulmentStatus == (int)HonestMark.AnnulmentDocumentStatus.Requested && docForAnnulmentProcessing.DocDate > dateTimeFrom
                                             join docGoods in _abtDbContext.DocGoods on docForAnnulmentProcessing.IdDoc equals docGoods.IdDoc
                                             join customer in _abtDbContext.RefCustomers on docGoods.IdSeller equals customer.IdContractor
                                             where customer.Inn == company.Inn && customer.Kpp == company.Kpp
                                             select docForAnnulmentProcessing;

            if ((docsForAnnulmentProcessing?.Count() ?? 0) != 0)
            {
                foreach (var docProcessing in docsForAnnulmentProcessing)
                {
                    var doc = _edo.GetDocument(docProcessing.MessageId, docProcessing.EntityId);

                    if (doc.RevocationStatus == Diadoc.Api.Proto.Documents.RevocationStatus.RevocationAccepted)
                    {
                        if (doc.RecipientResponseStatus == Diadoc.Api.Proto.Documents.RecipientResponseStatus.WithRecipientSignature
                        && docProcessing.HonestMarkStatus == (int)HonestMark.DocEdoProcessingStatus.Processed)
                            docProcessing.AnnulmentStatus = (int)HonestMark.AnnulmentDocumentStatus.RevokedWaitProcessing;
                        else
                            docProcessing.AnnulmentStatus = (int)HonestMark.AnnulmentDocumentStatus.Revoked;

                        MailReporter.Add($"Документ {docProcessing.IdDoc} успешно аннулирован.");
                    }
                    else if (doc.RevocationStatus == Diadoc.Api.Proto.Documents.RevocationStatus.RevocationRejected)
                    {
                        docProcessing.AnnulmentStatus = (int)HonestMark.AnnulmentDocumentStatus.Rejected;
                        MailReporter.Add($"Аннулирование документа {docProcessing.IdDoc} было отклонено.");
                    }
                }
                _abtDbContext.SaveChanges();
            }
        }

        private void ChecksCounteragents(RefCustomer company)
        {
            var dateTimeFrom = DateTime.Now.AddMonths(-6);

            var refEdoCounteragents = from a in _abtDbContext.RefEdoCounteragents
                                      where a.IsConnected == 0 && a.InsertDatetime > dateTimeFrom
                                      join r in _abtDbContext.RefCustomers
                                      on a.IdCustomerSeller equals r.Id
                                      where r.Inn == company.Inn && r.Kpp == company.Kpp
                                      select a;

            if((refEdoCounteragents?.Count() ?? 0) != 0)
            {
                foreach(var refEdoCounteragent in refEdoCounteragents)
                {
                    var counteragents = _edo.GetKontragents(_edo.ActualBoxIdGuid);

                    if (counteragents == null || counteragents.Count == 0 || string.IsNullOrEmpty(refEdoCounteragent?.IdFnsBuyer))
                        continue;

                    var counteragent = counteragents.Where(c => c?.Organization?.FnsParticipantId?.ToUpper() == refEdoCounteragent.IdFnsBuyer.ToUpper())?.FirstOrDefault();

                    if(counteragent != null && counteragent?.CurrentStatus == Diadoc.Api.Proto.CounteragentStatus.IsMyCounteragent)
                    {
                        _abtDbContext.Entry(refEdoCounteragent)?.Reload();
                        refEdoCounteragent.ConnectStatus = (int)Diadoc.Api.Proto.CounteragentStatus.IsMyCounteragent;
                        refEdoCounteragent.IsConnected = 1;
                        _abtDbContext.SaveChanges();
                    }
                }
            }
        }

        private void AddCounteragentsToTrader(RefCustomer company)
        {
            var dateTimeFrom = DateTime.Now.AddMonths(-6);

            var refEdoCounteragents = from a in _abtDbContext.RefEdoCounteragents
                                      where a.IsConnected == 1 && a.IsDefault == 1 && a.InsertDatetime > dateTimeFrom
                                      && !_abtDbContext.RefRefTags.Any(r => r.IdTag == 223 && r.IdObject == a.IdCustomerBuyer && r.TagValue == a.IdFnsBuyer)
                                      join r in _abtDbContext.RefCustomers
                                      on a.IdCustomerSeller equals r.Id
                                      where r.Inn == company.Inn && r.Kpp == company.Kpp
                                      select a;

            if ((refEdoCounteragents?.Count() ?? 0) != 0)
            {
                foreach (var refEdoCounteragent in refEdoCounteragents)
                {
                    var refRefTag = _abtDbContext.RefRefTags.FirstOrDefault(r => r.IdTag == 223 && r.IdObject == refEdoCounteragent.IdCustomerBuyer);

                    if(refRefTag != null)
                    {
                        refRefTag.TagValue = refEdoCounteragent.IdFnsBuyer;
                    }
                    else
                    {
                        _abtDbContext.RefRefTags.Add(new RefRefTag
                        {
                            IdObject = refEdoCounteragent.IdCustomerBuyer,
                            IdTag = 223,
                            TagValue = refEdoCounteragent.IdFnsBuyer
                        });
                    }

                    _abtDbContext.SaveChanges();
                }
            }

            var refEdoCounteragentConsignees = from a in _abtDbContext.RefEdoCounteragentConsignees
                                               where a.InsertDatetime > dateTimeFrom &&
                                               !_abtDbContext.RefRefTags.Any(r => r.IdTag == 224 && r.IdObject == a.IdContractorConsignee && r.TagValue == a.IdFnsBuyer)
                                               join r in _abtDbContext.RefCustomers on a.IdCustomerSeller equals r.Id
                                               where r.Inn == company.Inn && r.Kpp == company.Kpp
                                               let edoCounteragents = (from refEdo in _abtDbContext.RefEdoCounteragents
                                                                       where refEdo.IdCustomerSeller == a.IdCustomerSeller
                                                                       && refEdo.IdCustomerBuyer == a.IdCustomerBuyer
                                                                       && refEdo.IdFnsBuyer == a.IdFnsBuyer && refEdo.IsConnected == 1
                                                                       select refEdo)
                                               where edoCounteragents.Count() > 0
                                               select a;

            if((refEdoCounteragentConsignees?.Count() ?? 0) != 0)
            {
                foreach (var refEdoCounteragentConsignee in refEdoCounteragentConsignees)
                {
                    var refRefTag = _abtDbContext.RefRefTags.FirstOrDefault(r => r.IdTag == 224 && r.IdObject == refEdoCounteragentConsignee.IdContractorConsignee);

                    if(refRefTag != null)
                    {
                        refRefTag.TagValue = refEdoCounteragentConsignee.IdFnsBuyer;
                    }
                    else
                    {
                        _abtDbContext.RefRefTags.Add(new RefRefTag
                        {
                            IdObject = refEdoCounteragentConsignee.IdContractorConsignee,
                            IdTag = 224,
                            TagValue = refEdoCounteragentConsignee.IdFnsBuyer
                        });
                    }

                    _abtDbContext.SaveChanges();
                }
            }
        }

        public override void Run()
        {
            List<string> connectionStringList = new UsersConfig().GetAllConnectionStrings();

            foreach(var connectionString in connectionStringList)
            {
                try
                {
                    using (_abtDbContext = new AbtDbContext(connectionString, true))
                    {
                        var myOrgs = (from r in _abtDbContext.RefUsersByOrgEdo where r.UserName == _conf.DataBaseUser
                                      join c in _abtDbContext.RefCustomers on r.IdCustomer equals c.Id select c).ToList();

                        foreach (var myOrg in myOrgs)
                        {
                            OrgInn = myOrg.Inn;
                            Auth();

                            try
                            {
                                ExecuteChecks(myOrg);
                            }
                            catch(Exception exception)
                            {
                                MailReporter.Add($"DiadocEdoProcessorException \r\nProcessing exception for organization with Inn = {myOrg.Inn}\r\n" + _log.GetRecursiveInnerException(exception));
                            }

                            ChecksCounteragents(myOrg);
                            AddCounteragentsToTrader(myOrg);
                        }

                        ReceiveDocumentsForConsignors();
                        ExecuteCheckReceiveDocuments();
                    }
                }
                catch(Exception ex)
                {
                    MailReporter.Add("DiadocEdoProcessorException \r\n" + _log.GetRecursiveInnerException(ex));
                }
            }
        }

        public void ReceiveDocumentsForConsignors()
        {
            try
            {
                var dataBaseUser = UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser;
                var idCustomersConsignors = (from c in _abtDbContext.RefUserByEdoConsignors
                                             where c.UserName == dataBaseUser
                                             join r in _abtDbContext.RefRefTags on c.IdCustomerConsignor equals r.IdObject
                                             where r.IdTag == 215 && r.TagValue == "1"
                                             select c.IdCustomerConsignor)?.Distinct()?.ToList() ?? new List<decimal>();

                foreach(var idCustomerConsignor in idCustomersConsignors)
                {
                    var customerConsignor = _abtDbContext.RefCustomers.FirstOrDefault(r => r.Id == idCustomerConsignor);

                    if (customerConsignor == null)
                        continue;

                    OrgInn = customerConsignor.Inn;
                    if (!Auth())
                        continue;

                    var organization = _edo.GetMyOrganizationByInnKpp(customerConsignor.Inn);

                    var documents = _edo.GetDocuments("Any.Inbound") ?? new List<Diadoc.Api.Proto.Documents.Document>();

                    if(documents.Count > 0)
                    {
                        foreach(var document in documents)
                        {
                            if (_abtDbContext.DocEdoPurchasings.Any(d => d.IdDocEdo == document.MessageId))
                                continue;

                            Reporter.IReport report = null;
                            var reporterDll = new Reporter.ReporterDll();
                            List<Reporter.Entities.Product> products = null;

                            if (document.Version == "utd970_05_03_01")
                            {
                                var doc = _edo.GetDocument(document.MessageId, document.EntityId);
                                var docContentBytes = doc.Content.Data;
                                report = reporterDll.ParseDocument<Reporter.Reports.UniversalTransferSellerDocumentUtd970>(docContentBytes);
                                products = (report as Reporter.Reports.UniversalTransferSellerDocumentUtd970)?.Products;
                            }

                            if (report != null)
                                AddDocEdoPurchasingToDataBase(report, customerConsignor, document, organization?.FnsParticipantId);
                        }
                    }
                }
            }
            catch (System.Net.WebException webEx)
            {
                MailReporter.Add("DiadocEdoProcessorException.ReceiveDocumentsForConsignors: Произошла ошибка на удалённом сервере\r\n" + _log.GetRecursiveInnerException(webEx));
            }
            catch (Exception ex)
            {
                MailReporter.Add("DiadocEdoProcessorException.ReceiveDocumentsForConsignors: Произошла ошибка \r\n" + _log.GetRecursiveInnerException(ex));
            }
        }

        public void ExecuteCheckReceiveDocuments()
        {
            try
            {
                var dateFrom = DateTime.Now.AddMonths(-6);
                var dateTo = DateTime.Now.AddDays(1);

                var docs = _abtDbContext.DocEdoPurchasings.Where(d => d.CreateDate > dateFrom && d.CreateDate <= dateTo &&
                d.ReceiverInn == OrgInn);

                var errorsList = new List<string>();
                var processingDocuments = docs.Where(d => d.DocStatus == (int)HonestMark.DocEdoProcessingStatus.Sent).ToList() ?? new List<DocEdoPurchasing>();

                foreach (var processingDocument in processingDocuments)
                {
                    try
                    {
                        var doc = _edo?.GetDocumentsByMessageId(processingDocument.IdDocEdo)?
                            .FirstOrDefault(d => d.Type == Diadoc.Api.Com.DocumentType.UniversalTransferDocument && d.DocumentNumber == processingDocument.Name);

                        if (doc == null)
                            throw new Exception($"Не удалось найти маркированный документ в Диадоке. ID {processingDocument.IdDocEdo}");

                        var lastDocFlow = doc.LastOuterDocflows?.FirstOrDefault(l => l?.OuterDocflow?.DocflowNamedId == "TtGis" && l.OuterDocflow?.Status?.Type != null);
                        Diadoc.Api.Proto.OuterDocflows.OuterStatusType? statusDocFlow = lastDocFlow?.OuterDocflow?.Status?.Type;

                        if (statusDocFlow == Diadoc.Api.Proto.OuterDocflows.OuterStatusType.Success)
                        {
                            processingDocument.DocStatus = (int)HonestMark.DocEdoProcessingStatus.Processed;

                            if (processingDocument.IdDocJournal != null && processingDocument.IdDocJournal != 0)
                            {
                                var docJournal = _abtDbContext.DocJournals.First(d => d.Id == processingDocument.IdDocJournal);

                                if (docJournal.IdDocType == (int)DataContextManagementUnit.DataAccess.DocJournalType.Receipt)
                                    _abtDbContext.Database.ExecuteSqlCommand($"UPDATE doc_goods_details_labels SET LABEL_STATUS = 2, POST_DATETIME = sysdate where id_doc = {processingDocument.IdDocJournal}");
                            }

                            _abtDbContext?.SaveChanges();
                            MailReporter.Add($"Принятый маркированный документ {processingDocument.Name} для организации {OrgInn} успешно обработан.");
                        }
                        else if(statusDocFlow == Diadoc.Api.Proto.OuterDocflows.OuterStatusType.Error)
                        {
                            processingDocument.DocStatus = (int)HonestMark.DocEdoProcessingStatus.ProcessingError;

                            var errors = lastDocFlow.OuterDocflow.Status?.Details ?? new List<Diadoc.Api.Proto.OuterDocflows.StatusDetail>();

                            var errorsListStr = new List<string>();
                            foreach (var error in errors)
                                errorsListStr.Add($"Произошла ошибка с кодом:{error.Code} \nОписание:{error.Text}\n");

                            var honestMarkErrorMessage = string.Join("\n\n", errorsListStr);

                            if (honestMarkErrorMessage.Length > 500)
                                honestMarkErrorMessage = honestMarkErrorMessage.Substring(0, 500);

                            processingDocument.ErrorMessage = honestMarkErrorMessage;

                            _abtDbContext?.SaveChanges();
                            MailReporter.Add($"Маркированный документ {processingDocument.Name} для организации {OrgInn} обработан с ошибками.");
                        }

                    }
                    catch (System.Net.WebException webEx)
                    {
                        MailReporter.Add("DiadocEdoProcessorException.ExecuteCheckReceiveDocuments: Произошла ошибка на удалённом сервере\r\n" + _log.GetRecursiveInnerException(webEx));
                    }
                    catch (Exception ex)
                    {
                        MailReporter.Add("DiadocEdoProcessorException.ExecuteCheckReceiveDocuments: Произошла ошибка\r\n" + _log.GetRecursiveInnerException(ex));
                    }
                }

                if (processingDocuments.Exists(p => p.DocStatus != (int)HonestMark.DocEdoProcessingStatus.Sent))
                    _abtDbContext?.SaveChanges();



                var newDocuments = docs.Where(d => d.DocStatus == (int)HonestMark.DocEdoProcessingStatus.None || d.DocStatus == (int)HonestMark.AnnulmentDocEdoPurchasingStatus.RevokeRequested).ToList() ?? new List<DocEdoPurchasing>();


                foreach (var newDocument in newDocuments)
                {
                    try
                    {
                        var document = _edo.GetDocument(newDocument.IdDocEdo, newDocument.ParentEntityId);

                        if (document.RevocationStatus == Diadoc.Api.Proto.Documents.RevocationStatus.RequestsMyRevocation)
                            newDocument.DocStatus = (int)HonestMark.AnnulmentDocEdoPurchasingStatus.RevokeRequired;
                        else if (document.RevocationStatus == Diadoc.Api.Proto.Documents.RevocationStatus.RevocationAccepted)
                            newDocument.DocStatus = (int)HonestMark.AnnulmentDocEdoPurchasingStatus.Revoked;
                        else if (document.RevocationStatus == Diadoc.Api.Proto.Documents.RevocationStatus.RevocationRejected)
                            newDocument.DocStatus = (int)HonestMark.AnnulmentDocEdoPurchasingStatus.RejectRevoke;

                        _abtDbContext?.SaveChanges();
                    }
                    catch (System.Net.WebException webEx)
                    {
                        MailReporter.Add("DiadocEdoProcessorException.ExecuteCheckReceiveDocuments: Произошла ошибка на удалённом сервере\r\n" + _log.GetRecursiveInnerException(webEx));
                    }
                    catch (Exception ex)
                    {
                        MailReporter.Add("DiadocEdoProcessorException.ExecuteCheckReceiveDocuments: Произошла ошибка\r\n" + _log.GetRecursiveInnerException(ex));
                    }
                }
            }
            catch(Exception ex)
            {
                MailReporter.Add("DiadocEdoProcessorException.ExecuteCheckReceiveDocuments: Произошла ошибка\r\n" + _log.GetRecursiveInnerException(ex));
            }
        }

        public object AddDocEdoPurchasingToDataBase(Reporter.IReport report, RefCustomer myOrganization, Diadoc.Api.Proto.Documents.Document document, string orgEdoId)
        {
            string orgInn = OrgInn, orgKpp = myOrganization.Kpp, orgName = myOrganization.Name;
            var dataBaseUser = UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser;

            if (document.DocumentType != Diadoc.Api.Proto.DocumentType.XmlAcceptanceCertificate && document.DocumentType != Diadoc.Api.Proto.DocumentType.Invoice &&
                document.DocumentType != Diadoc.Api.Proto.DocumentType.XmlTorg12 && document.DocumentType != Diadoc.Api.Proto.DocumentType.UniversalTransferDocument &&
                document.DocumentType != Diadoc.Api.Proto.DocumentType.UniversalTransferDocumentRevision)
                return null;

            string total, vat;

            if (document.DocumentType == Diadoc.Api.Proto.DocumentType.XmlAcceptanceCertificate)
            {
                total = document?.XmlAcceptanceCertificateMetadata?.Total;
                vat = document?.XmlAcceptanceCertificateMetadata?.Vat;
            }
            else if (document.DocumentType == Diadoc.Api.Proto.DocumentType.Invoice)
            {
                total = document?.InvoiceMetadata?.Total;
                vat = document?.InvoiceMetadata?.Vat;
            }
            else if (document.DocumentType == Diadoc.Api.Proto.DocumentType.XmlTorg12)
            {
                total = document?.XmlTorg12Metadata?.Total;
                vat = document?.XmlTorg12Metadata.Vat;
            }
            else if (document.DocumentType == Diadoc.Api.Proto.DocumentType.UniversalTransferDocument)
            {
                total = document?.UniversalTransferDocumentMetadata?.Total;
                vat = document?.UniversalTransferDocumentMetadata?.Vat;
            }
            else if (document.DocumentType == Diadoc.Api.Proto.DocumentType.UniversalTransferDocumentRevision)
            {
                total = document?.UniversalTransferDocumentRevisionMetadata?.Total;
                vat = document?.UniversalTransferDocumentRevisionMetadata?.Vat;
            }
            else
            {
                total = null;
                vat = null;
            }

            DocEdoPurchasing newDoc = null;
            if (document.Version == "utd970_05_03_01")
            {
                newDoc = new DocEdoPurchasing
                {
                    IdDocEdo = document.MessageId,
                    EdoProviderName = providerName,
                    Name = document.DocumentNumber,
                    IdDocType = (int)document.DocumentType,
                    CounteragentEdoBoxId = document.CounteragentBoxId,
                    ParentEntityId = document.EntityId,
                    ReceiveDate = document?.DeliveryTimestamp,
                    CreateDate = document?.CreationTimestamp,
                    TotalPrice = total,
                    TotalVatAmount = vat,
                    SenderEdoId = (report as Reporter.Reports.UniversalTransferSellerDocumentUtd970).SenderEdoId,
                    ReceiverEdoId = (report as Reporter.Reports.UniversalTransferSellerDocumentUtd970).ReceiverEdoId,
                    SenderEdoOrgId = (report as Reporter.Reports.UniversalTransferSellerDocumentUtd970).EdoId,
                    FileName = (report as Reporter.Reports.UniversalTransferSellerDocumentUtd970).FileName,
                    UserName = dataBaseUser,
                    DocVersionFormat = "utd970_05_03_01"
                };

                var counteragents = Edo.Edo.GetInstance().GetKontragents(Edo.Edo.GetInstance().ActualBoxIdGuid);

                var organization = counteragents?.FirstOrDefault(c => c?.Organization?.Boxes?.Any(b => b.BoxId == document.CounteragentBoxId) ?? false)?.Organization;

                if (organization == null)
                    throw new Exception($"Организация не найдена для документа {document.Title}.");

                var counteragentName = organization?.FullName;
                var counteragentInn = organization?.Inn;
                var counteragentKpp = organization?.Kpp;
                var counteragentEdoId = organization?.FnsParticipantId;

                newDoc.SenderInn = counteragentInn;
                newDoc.SenderName = counteragentName;
                newDoc.SenderKpp = counteragentKpp;
                newDoc.SenderEdoId = counteragentEdoId;
                newDoc.ReceiverInn = orgInn;
                newDoc.ReceiverKpp = orgKpp;
                newDoc.ReceiverName = orgName;
                newDoc.ReceiverEdoId = orgEdoId;
                

                if (document?.DocflowStatus?.PrimaryStatus?.Severity == "Success")
                    newDoc.DocStatus = (int)HonestMark.DocEdoProcessingStatus.Processed;
                else if (document?.DocflowStatus?.PrimaryStatus?.Severity == "Error")
                {
                    newDoc.DocStatus = (int)HonestMark.DocEdoProcessingStatus.ProcessingError;
                    newDoc.ErrorMessage = document?.DocflowStatus?.PrimaryStatus?.StatusText;
                }
                else
                    newDoc.DocStatus = (int)HonestMark.DocEdoProcessingStatus.None;

                if (document.DocumentType == Diadoc.Api.Proto.DocumentType.UniversalTransferDocumentRevision)
                {
                    newDoc.Name = $"Исправление {(report as Reporter.Reports.UniversalTransferSellerDocumentUtd970).DocNumber} № {newDoc.Name}";

                    var parent = _abtDbContext.DocEdoPurchasings.FirstOrDefault(d => d.EdoProviderName == providerName &&
                    d.Name == (report as Reporter.Reports.UniversalTransferSellerDocumentUtd970).DocNumber
                    && d.SenderInn == newDoc.SenderInn && d.ReceiverInn == newDoc.ReceiverInn);

                    if (parent != null)
                    {
                        newDoc.Parent = parent;
                        newDoc.ParentIdDocEdo = parent.IdDocEdo;
                        parent.Children.Add(newDoc);
                    }
                }

                var docProcessing = (from d in _abtDbContext.DocEdoProcessings
                                     where d.MessageId == newDoc.IdDocEdo && d.EntityId == newDoc.ParentEntityId
                                     select d)?.FirstOrDefault();

                if (docProcessing != null)
                    newDoc.IdDocJournal = docProcessing.IdDoc;

                foreach (var product in (report as Reporter.Reports.UniversalTransferSellerDocumentUtd970).Products)
                {
                    var newDetail = new DocEdoPurchasingDetail
                    {
                        BarCode = product.BarCode,
                        Quantity = product.Quantity,
                        Description = product.Description,
                        Price = product.Price,
                        Subtotal = product.Subtotal,
                        TaxAmount = product.TaxAmount,
                        IdDocEdoPurchasing = newDoc.IdDocEdo,
                        EdoDocument = newDoc,
                        DetailNumber = product.Number,
                        Gtin = product.Gtin
                    };

                    if (!string.IsNullOrEmpty(product.QuantityMark))
                        newDetail.QuantityMark = Convert.ToDecimal(product.QuantityMark);

                    var refGoods = _abtDbContext.RefBarCodes?
                                .Where(b => b.BarCode == newDetail.BarCode && b.IsPrimary == 0)?
                                .Select(b => b.IdGood)?.Distinct()?.ToList() ?? new List<decimal?>();

                    if (refGoods.Count == 1)
                        newDetail.IdGood = refGoods.First();
                    else if (newDoc.Parent != null)
                    {
                        var curDetail = newDoc.Parent.Details.FirstOrDefault(d => d.BarCode == newDetail.BarCode);

                        if (curDetail != null)
                            newDetail.IdGood = curDetail.IdGood;
                    }

                    if (newDetail.IdGood == null && !string.IsNullOrEmpty(newDetail.Gtin))
                    {
                        newDetail.IdGood = _abtDbContext.RefBarCodes?
                            .Where(b => b.BarCode == newDetail.Gtin && b.IsPrimary == 10 && b.IdGood != null)?
                            .Select(b => b.IdGood)?.FirstOrDefault();

                        if (newDetail.IdGood == null)
                        {
                            var barCodeByGtin = newDetail.Gtin.TrimStart('0');

                            newDetail.IdGood = _abtDbContext.RefBarCodes?
                            .Where(b => b.BarCode == barCodeByGtin && b.IsPrimary == 0 && b.IdGood != null)?
                            .Select(b => b.IdGood)?.FirstOrDefault();
                        }
                    }

                    newDoc.Details.Add(newDetail);
                }
            }

            if (newDoc != null)
            {
                _abtDbContext.DocEdoPurchasings.Add(newDoc);
                _abtDbContext.SaveChanges();
                MailReporter.Add($"Входящий документ {newDoc.Name} для организации {orgInn} успешно добавлен.");
            }

            return newDoc;
        }
    }
}
