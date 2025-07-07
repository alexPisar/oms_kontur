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
        public override string ProcessorName => "DiadocEdoProcessor";
        public string OrgInn { get; set; }
        private bool Auth()
        {
            return _edo?.Authenticate(true, null, OrgInn) ?? false;
        }

        private void SetEdoStatus(DocEdoProcessing docEdoProcessing, int newStatus, string textMessage = null)
        {
            _abtDbContext.Entry(docEdoProcessing)?.Reload();
            docEdoProcessing.DocStatus = newStatus;
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
                                           where markedDocEdoProcessing.HonestMarkStatus == (int)HonestMark.DocEdoProcessingStatus.Sent && markedDocEdoProcessing.DocStatus == (int)Enums.DocEdoSendStatus.Signed && markedDocEdoProcessing.DocDate > dateTimeFrom
                                           join docGoods in _abtDbContext.DocGoods on markedDocEdoProcessing.IdDoc equals docGoods.IdDoc
                                           join customer in _abtDbContext.RefCustomers on docGoods.IdSeller equals customer.IdContractor
                                           where customer.Inn == company.Inn && customer.Kpp == company.Kpp
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

                    markedDocEdoProcessing.HonestMarkErrorMessage = string.Join("\n\n", errorsListStr);
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
                    SetEdoStatus(docEdoProcessing, (int)Enums.DocEdoSendStatus.Rejected, $"Документ {docEdoProcessing.IdDoc} отклонён контрагентом.");
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
                                              where comissionDocEdoProcessing.DocStatus == (int)HonestMark.DocEdoProcessingStatus.Sent && comissionDocEdoProcessing.DocDate > dateTimeFrom
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
                            ExecuteChecks(myOrg);
                        }
                    }
                }
                catch(Exception ex)
                {
                    MailReporter.Add("DiadocEdoProcessorException \r\n" + _log.GetRecursiveInnerException(ex));
                }
            }
        }
    }
}
