﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataContextManagementUnit.DataAccess.Contexts.Abt;

namespace EdiProcessingUnit.Edo.Models
{
    public partial class UniversalTransferDocument
    {
        private DocJournal _docJournal;
        private IEnumerable<UniversalTransferDocumentDetail> _details;
        private decimal? _docJournalId;
        private string _shipper;
        private string _consignee;
        private string _buyerInnKpp;
        private string _buyerName;
        private string _buyerAddress;
        private object _status;
        private object _edoProcessing;

        public IEnumerable<UniversalTransferDocumentDetail> Details
        {
            get 
                {
                return _details;
            }
        }

        public decimal? DocJournalId
        {
            get { return _docJournalId; }
            set { _docJournalId = value; }
        }

        public DocJournal DocJournal
        {
            get {
                return _docJournal;
            }
            set {
                if(value?.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice)
                    _details = from detail in value?.DocGoodsDetailsIs select new UniversalTransferDocumentDetail() { DocDetailI = detail };
                else if(value?.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Translocation)
                    _details = from detail in value?.Details select new UniversalTransferDocumentDetail() { DocDetail = detail };
                _docJournal = value;
            }
        }

        public decimal? CurrentDocJournalId
        {
            get {
                if (DocJournal == null)
                    return null;

                if (DocJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice)
                    return DocJournal.IdDocMaster;
                else
                    return DocJournal.Id;
            }
        }

        public double Total { get; set; }

        public double TotalWithVatExcluded { get; set; }

        public double Vat { get; set; }

        public string DocJournalNumber { get; set; }

        public string DocumentNumber { get; set; }

        public object ProcessingStatus
        {
            get {
                if(_status != null)
                {
                    if (_status as IQueryable<DocComissionEdoProcessing> != null)
                    {
                        var query = _status as IQueryable<DocComissionEdoProcessing>;

                        if(query.Any(s => s.DocStatus == 2))
                            _status = query.FirstOrDefault(s => s.DocStatus == 2);
                        else if (query.Any(s => s.DocStatus == 1))
                            _status = query.FirstOrDefault(s => s.DocStatus == 1);
                        else
                            _status = query.FirstOrDefault();
                    }
                    else if (_status as IEnumerable<DocComissionEdoProcessing> != null)
                    {
                        var collection = _status as IEnumerable<DocComissionEdoProcessing>;

                        if(collection.Any(s => s.DocStatus == 2))
                            _status = collection.FirstOrDefault(s => s.DocStatus == 2);
                        else if (collection.Any(s => s.DocStatus == 1))
                            _status = collection.FirstOrDefault(s => s.DocStatus == 1);
                        else
                            _status = collection.FirstOrDefault();
                    }
                    else if (_status as DocComissionEdoProcessing == null)
                        return null;
                }

                return _status;
            }
            set {
                _status = value;
            }
        }

        public object EdoProcessing
        {
            get {
                if (_edoProcessing as IQueryable<DocEdoProcessing> != null)
                {
                    var query = _edoProcessing as IQueryable<DocEdoProcessing>;

                    if(query.Any(s => s.DocStatus == (int)Enums.DocEdoSendStatus.Signed && (s.AnnulmentStatus == 0 || s.AnnulmentStatus == -1)))
                        _edoProcessing = query.FirstOrDefault(s => s.DocStatus == (int)Enums.DocEdoSendStatus.Signed && (s.AnnulmentStatus == 0 || s.AnnulmentStatus == -1));
                    else if (query.Any(s => s.DocStatus == (int)Enums.DocEdoSendStatus.Signed))
                        _edoProcessing = query.FirstOrDefault(s => s.DocStatus == (int)Enums.DocEdoSendStatus.Signed);
                    else if (query.Any(s => s.DocStatus == (int)Enums.DocEdoSendStatus.Rejected))
                        _edoProcessing = query.FirstOrDefault(s => s.DocStatus == (int)Enums.DocEdoSendStatus.Rejected);
                    else
                        _edoProcessing = query.FirstOrDefault();
                }
                else if (_edoProcessing as IEnumerable<DocEdoProcessing> != null)
                {
                    var collection = _edoProcessing as IEnumerable<DocEdoProcessing>;

                    if (collection.Any(s => s.DocStatus == (int)Enums.DocEdoSendStatus.Signed && (s.AnnulmentStatus == 0 || s.AnnulmentStatus == -1)))
                        _edoProcessing = collection.FirstOrDefault(s => s.DocStatus == (int)Enums.DocEdoSendStatus.Signed && (s.AnnulmentStatus == 0 || s.AnnulmentStatus == -1));
                    else if (collection.Any(s => s.DocStatus == (int)Enums.DocEdoSendStatus.Signed))
                        _edoProcessing = collection.FirstOrDefault(s => s.DocStatus == (int)Enums.DocEdoSendStatus.Signed);
                    else if (collection.Any(s => s.DocStatus == (int)Enums.DocEdoSendStatus.Rejected))
                        _edoProcessing = collection.FirstOrDefault(s => s.DocStatus == (int)Enums.DocEdoSendStatus.Rejected);
                    else
                        _edoProcessing = collection.FirstOrDefault();
                }
                else if (_edoProcessing as DocEdoProcessing == null)
                    return null;

                return _edoProcessing;
            }
            set {
                _edoProcessing = value;
            }
        }

        public string DocEdoSendStatus {
            get {
                if (EdoProcessing == null)
                    return "-";

                var edoProc = (DocEdoProcessing)EdoProcessing;

                if (edoProc.AnnulmentStatus == 2 || edoProc.AnnulmentStatus == 3 || edoProc.AnnulmentStatus == 4)
                    return "Аннулирован";

                if (edoProc.AnnulmentStatus == 1)
                    return "Запрошено аннулирование";

                switch (edoProc.DocStatus)
                {
                    case (int)Enums.DocEdoSendStatus.Signed:
                        return "Подписан контрагентом";
                    case (int)Enums.DocEdoSendStatus.Rejected:
                        return "Отклонён";
                    case (int)Enums.DocEdoSendStatus.Sent:
                        return "Отправлен";
                    default:
                        return "-";
                }
                
            }
        }

        public string OrgId { get; set; }

        public RefContractor SellerContractor
        {
            set {
                _shipper = value?.Name + ", " + value?.Address;
            }
        }

        public RefContractor BuyerContractor
        {
            set {
                _consignee = value?.Name + ", " + value?.Address;
            }
        }

        public RefCustomer BuyerCustomer
        {
            set {
                _buyerInnKpp = value?.Inn + "/" + value?.Kpp;
                _buyerName = value?.Name;
                _buyerAddress = value?.Address;
            }
        }

        public RefStore StoreSender
        {
            set {
                _shipper = value?.Name;
            }
        }

        public RefStore StoreRecipient
        {
            set {
                _consignee = value?.Name;
            }
        }

        public string SenderInnKpp { get; set; }

        public string SenderName { get; set; }

        public string SenderAddress { get; set; }

        public string BuyerInnKpp {
            get 
                {
                return _buyerInnKpp;
            }
        }

        public string BuyerName {
            get 
                {
                return _buyerName;
            }
        }

        public string BuyerAddress {
            get 
                {
                return _buyerAddress;
            }
        }

        public string Shipper {
            get 
                {
                return _shipper;
            }
        }

        public string Consignee {
            get 
                {
                return _consignee;
            }
        }

        public bool IsMarked { get; set; }
    }
}
