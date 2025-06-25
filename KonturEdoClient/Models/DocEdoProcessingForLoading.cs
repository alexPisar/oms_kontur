using System;
using EdiProcessingUnit.Enums;
using EdiProcessingUnit.HonestMark;
using DataContextManagementUnit.DataAccess.Contexts.Abt;

namespace KonturEdoClient.Models
{
    public class DocEdoProcessingForLoading
    {
        private DocEdoProcessing _docEdoProcessing;

        public DocEdoProcessingForLoading(DocEdoProcessing docEdoProcessing)
        {
            _docEdoProcessing = docEdoProcessing;
        }

        public DateTime DocDate => _docEdoProcessing.DocDate;
        public string ReceiverName => _docEdoProcessing.ReceiverName;
        public string ReceiverInn => _docEdoProcessing.ReceiverInn;
        public string SignStatus
        {
            get {
                switch (_docEdoProcessing.DocStatus)
                {
                    case (int)DocEdoSendStatus.Signed:
                        return "Подписан контрагентом";
                    case (int)DocEdoSendStatus.Rejected:
                        return "Отклонён";
                    case (int)DocEdoSendStatus.Sent:
                        return "Отправлен";
                    case (int)DocEdoSendStatus.PartialSigned:
                        return "Подписан с расхождениями";
                    default:
                        return "-";
                }
            }
        }
        public string AnnullmentStatus
        {
            get {
                switch (_docEdoProcessing.AnnulmentStatus)
                {
                    case (int)AnnulmentDocumentStatus.Rejected:
                        return "Аннулирование отклонено";
                    case (int)AnnulmentDocumentStatus.None:
                        return "-";
                    case (int)AnnulmentDocumentStatus.Requested:
                        return "Запрошено аннулирование";
                    case (int)AnnulmentDocumentStatus.RevokedWaitProcessing:
                        return "Аннулирован";
                    case (int)AnnulmentDocumentStatus.RevokedAndProcessed:
                        return "Аннулирован";
                    case (int)AnnulmentDocumentStatus.Revoked:
                        return "Аннулирован";
                    case (int)AnnulmentDocumentStatus.Error:
                        return "Ошибка аннулирования";
                    default:
                        return "-";
                }
            }
        }

        public string UserName => _docEdoProcessing.UserName;
        public DocEdoProcessing DocEdoProcessing => _docEdoProcessing;
    }
}
