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
            var dateTimeFrom = DateTime.Now.AddMonths(-6);

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
