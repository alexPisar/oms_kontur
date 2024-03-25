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

        public DocJournal CorrectionDocJournal { get; set; }
        public object EdoProcessing { get; set; }
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

        public string DocEdoSendStatus
        {
            get {

                if (EdoProcessing as IEnumerable<DocEdoProcessing> != null)
                {
                    _edoProcessing = (EdoProcessing as IEnumerable<DocEdoProcessing>)?.FirstOrDefault();
                    EdoProcessing = _edoProcessing;

                    if (_edoProcessing == null)
                        return "-";
                }
                else if (EdoProcessing as DocEdoProcessing != null)
                {
                    _edoProcessing = EdoProcessing as DocEdoProcessing;
                }
                else
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
    }
}
