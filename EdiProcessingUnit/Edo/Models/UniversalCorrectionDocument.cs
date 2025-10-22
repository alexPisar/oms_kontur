using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataContextManagementUnit.DataAccess.Contexts.Abt;

namespace EdiProcessingUnit.Edo.Models
{
    public class UniversalCorrectionDocument
    {
        private DocEdoProcessing _edoProcessing;
        private string _docNumber;
        private string _actStatusStr;

        public DocJournal CorrectionDocJournal { get; set; }
        public DocJournal InvoiceDocJournal { get; set; }

        public bool IsMarked { get; set; }

        public object EdoProcessing
        {
            set { _edoProcessing = value as DocEdoProcessing; }
        }
        public object DocJournalTag { get; set; }
        public string DocumentNumber
        {
            get {
                if(DocJournalTag as IEnumerable<DocJournalTag> != null)
                    DocJournalTag = (DocJournalTag as IEnumerable<DocJournalTag>).FirstOrDefault();

                if (DocJournalTag as DocJournalTag != null)
                    return (DocJournalTag as DocJournalTag).TagValue;

                return _docNumber;
            }
            set {
                _docNumber = value;
            }
        }

        public string DocType
        {
            get {

                switch (CorrectionDocJournal?.IdDocType)
                {
                    case (decimal)DataContextManagementUnit.DataAccess.DocJournalType.ReturnFromBuyer:
                        return "Возврат от покупателя";
                    case (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Correction:
                        return "Корректировочная с/ф";
                    default:
                        return "";
                }
            }
        }

        public string BuyerName
        {
            get {
                switch (CorrectionDocJournal?.IdDocType)
                {
                    case (decimal)DataContextManagementUnit.DataAccess.DocJournalType.ReturnFromBuyer:
                        return CorrectionDocJournal?.DocGoods?.Seller?.Name;
                    case (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Correction:
                        return InvoiceDocJournal?.DocMaster?.DocGoods?.Customer?.Name;
                    default:
                        return "";
                }
            }
        }

        public string SellerName
        {
            get {
                switch (CorrectionDocJournal?.IdDocType)
                {
                    case (decimal)DataContextManagementUnit.DataAccess.DocJournalType.ReturnFromBuyer:
                        return CorrectionDocJournal?.DocGoods?.Customer?.Name;
                    case (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Correction:
                        return InvoiceDocJournal?.DocMaster?.DocGoods?.Seller?.Name;
                    default:
                        return "";
                }
            }
        }

        public string InvoiceNumber => InvoiceDocJournal?.Code;
        public string CorrectionDocJournalNumber => CorrectionDocJournal?.Code;

        public string DocEdoSendStatus
        {
            get {

                if (_edoProcessing == null)
                    return "-";

                switch (_edoProcessing.DocStatus)
                {
                    case (int)Enums.DocEdoSendStatus.Signed:
                        return "Подписан контрагентом";
                    case (int)Enums.DocEdoSendStatus.Rejected:
                        return "Отклонён";
                    case (int)Enums.DocEdoSendStatus.Sent:
                        return "Отправлен";
                    case (int)Enums.DocEdoSendStatus.PartialSigned:
                        return "Подписан с расхождениями";
                    default:
                        return "-";
                }
            }
        }

        public string ActStatusStr
        {
            get {
                return _actStatusStr;
            }
        }

        public UniversalCorrectionDocument Init(AbtDbContext abtContext)
        {
            var collection = (from docEdo in abtContext.DocEdoProcessings
                              where docEdo.IdDoc == CorrectionDocJournal.Id && docEdo.DocType == (int)Enums.DocEdoType.Ucd
                              orderby docEdo.DocDate descending select docEdo);

            if (!(collection?.Any() ?? true))
            {
                EdoProcessing = null;
            }
            else
            {
                if (collection.Any(s => s.DocStatus == (int)Enums.DocEdoSendStatus.Signed))
                    EdoProcessing = collection.FirstOrDefault(s => s.DocStatus == (int)Enums.DocEdoSendStatus.Signed);
                else if (collection.Any(s => s.DocStatus == (int)Enums.DocEdoSendStatus.PartialSigned))
                    EdoProcessing = collection.FirstOrDefault(s => s.DocStatus == (int)Enums.DocEdoSendStatus.PartialSigned);
                else if (collection.Any(s => s.DocStatus == (int)Enums.DocEdoSendStatus.Rejected))
                    EdoProcessing = collection.FirstOrDefault(s => s.DocStatus == (int)Enums.DocEdoSendStatus.Rejected);
                else
                    EdoProcessing = collection.FirstOrDefault();
            }

            if(CorrectionDocJournal?.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Correction)
                IsMarked = (from label in abtContext.DocGoodsDetailsLabels where label.IdDocSale == InvoiceDocJournal.IdDocMaster select label).Count() > 0;
            else
                IsMarked = (from label in abtContext.DocGoodsDetailsLabels where label.IdDocReturn == CorrectionDocJournal.Id select label).Count() > 0;

            _actStatusStr = abtContext.SelectSingleValue($"select name from ref_actions where id = {CorrectionDocJournal.ActStatus}");
            return this;
        }
    }
}
