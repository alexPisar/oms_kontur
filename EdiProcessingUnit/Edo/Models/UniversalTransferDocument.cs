using System;
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
        private string _shipper, _shipperName, _shipperAddress;
        private string _consignee, _consigneeName, _consigneeAddress;
        private string _buyerInnKpp, _buyerInn, _buyerKpp;
        private string _buyerName;
        private string _buyerAddress;
        private object _status;
        private object _edoProcessing;
        private object _refEdoGoodChannel;
        private object _channel;
        private decimal? _idChannel = null;
        private string _actStatusForSendFromTraderStr;

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
                    _details = from detail in value?.DocGoodsDetailsIs select new UniversalTransferDocumentDetail() { DocDetailI = detail, IdGood = detail.IdGood };
                else if(value?.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Translocation)
                    _details = from detail in value?.Details select new UniversalTransferDocumentDetail() { DocDetail = detail, IdGood = detail.IdGood };
                _docJournal = value;
            }
        }

        public decimal? IdSubdivision
        {
            get {
                if (_docJournal == null)
                    return null;

                if (_docJournal.IdDocType == (decimal)DataContextManagementUnit.DataAccess.DocJournalType.Invoice)
                    return _docJournal?.DocMaster?.DocGoods?.IdSubdivision;
                else
                    return _docJournal?.DocGoods?.IdSubdivision;
            }
        }

        public object RefEdoGoodChannel
        {
            set {
                _refEdoGoodChannel = value;
            }
            get {
                if(_refEdoGoodChannel != null)
                {
                    if (_refEdoGoodChannel as IQueryable<RefEdoGoodChannel> != null)
                    {
                        var query = _refEdoGoodChannel as IQueryable<RefEdoGoodChannel>;
                        _refEdoGoodChannel = query.FirstOrDefault();
                    }
                    else if (_refEdoGoodChannel as IEnumerable<RefEdoGoodChannel> != null)
                    {
                        var collection = _refEdoGoodChannel as IEnumerable<RefEdoGoodChannel>;
                        _refEdoGoodChannel = collection.FirstOrDefault();
                    }
                    else if (_refEdoGoodChannel as RefEdoGoodChannel == null)
                        return null;
                }

                return _refEdoGoodChannel;
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

        public int? ActStatus { get; set; }

        public string ActStatusStr
        {
            get {
                if (ActStatus == null)
                    return null;

                if (ActStatus == 2)
                    return "В резерве";
                else if (ActStatus == 3)
                    return "Отборка";
                else if (ActStatus == 4)
                    return "Отобран";
                else if (ActStatus == 5)
                    return "Вывезен";
                else if (ActStatus == 6)
                    return "Подтверждён";
                else return null;
            }
        }

        public double Total { get; set; }

        public double TotalWithVatExcluded { get; set; }

        public double Vat { get; set; }

        public string DocJournalNumber { get; set; }

        public string DocumentNumber { get; set; }

        public object EdoProcessing
        {
            get {
                if (_edoProcessing as IQueryable<DocEdoProcessing> != null)
                {
                    var query = _edoProcessing as IQueryable<DocEdoProcessing>;

                    if (!query.Any())
                    {
                        _edoProcessing = null;
                        return null;
                    }

                    if(query.Any(s => s.DocStatus == (int)Enums.DocEdoSendStatus.Signed && (s.AnnulmentStatus == 0 || s.AnnulmentStatus == -1)))
                        _edoProcessing = query.FirstOrDefault(s => s.DocStatus == (int)Enums.DocEdoSendStatus.Signed && (s.AnnulmentStatus == 0 || s.AnnulmentStatus == -1));
                    else if (query.Any(s => s.DocStatus == (int)Enums.DocEdoSendStatus.PartialSigned && (s.AnnulmentStatus == 0 || s.AnnulmentStatus == -1)))
                        _edoProcessing = query.FirstOrDefault(s => s.DocStatus == (int)Enums.DocEdoSendStatus.PartialSigned && (s.AnnulmentStatus == 0 || s.AnnulmentStatus == -1));
                    else if (query.Any(s => s.DocStatus == (int)Enums.DocEdoSendStatus.Signed))
                        _edoProcessing = query.FirstOrDefault(s => s.DocStatus == (int)Enums.DocEdoSendStatus.Signed);
                    else if (query.Any(s => s.DocStatus == (int)Enums.DocEdoSendStatus.PartialSigned))
                        _edoProcessing = query.FirstOrDefault(s => s.DocStatus == (int)Enums.DocEdoSendStatus.PartialSigned);
                    else if (query.Any(s => s.DocStatus == (int)Enums.DocEdoSendStatus.Rejected))
                        _edoProcessing = query.FirstOrDefault(s => s.DocStatus == (int)Enums.DocEdoSendStatus.Rejected);
                    else if (query.Any(s => s.DocStatus == (int)Enums.DocEdoSendStatus.Sent && (s.AnnulmentStatus == 0 || s.AnnulmentStatus == -1)))
                        _edoProcessing = query.FirstOrDefault(s => s.DocStatus == (int)Enums.DocEdoSendStatus.Sent && (s.AnnulmentStatus == 0 || s.AnnulmentStatus == -1));
                    else
                        _edoProcessing = query.FirstOrDefault();
                }
                else if (_edoProcessing as IEnumerable<DocEdoProcessing> != null)
                {
                    var collection = _edoProcessing as IEnumerable<DocEdoProcessing>;

                    if (!collection.Any())
                    {
                        _edoProcessing = null;
                        return null;
                    }

                    if (collection.Any(s => s.DocStatus == (int)Enums.DocEdoSendStatus.Signed && (s.AnnulmentStatus == 0 || s.AnnulmentStatus == -1)))
                        _edoProcessing = collection.FirstOrDefault(s => s.DocStatus == (int)Enums.DocEdoSendStatus.Signed && (s.AnnulmentStatus == 0 || s.AnnulmentStatus == -1));
                    else if (collection.Any(s => s.DocStatus == (int)Enums.DocEdoSendStatus.PartialSigned && (s.AnnulmentStatus == 0 || s.AnnulmentStatus == -1)))
                        _edoProcessing = collection.FirstOrDefault(s => s.DocStatus == (int)Enums.DocEdoSendStatus.PartialSigned && (s.AnnulmentStatus == 0 || s.AnnulmentStatus == -1));
                    else if (collection.Any(s => s.DocStatus == (int)Enums.DocEdoSendStatus.Signed))
                        _edoProcessing = collection.FirstOrDefault(s => s.DocStatus == (int)Enums.DocEdoSendStatus.Signed);
                    else if (collection.Any(s => s.DocStatus == (int)Enums.DocEdoSendStatus.PartialSigned))
                        _edoProcessing = collection.FirstOrDefault(s => s.DocStatus == (int)Enums.DocEdoSendStatus.PartialSigned);
                    else if (collection.Any(s => s.DocStatus == (int)Enums.DocEdoSendStatus.Rejected))
                        _edoProcessing = collection.FirstOrDefault(s => s.DocStatus == (int)Enums.DocEdoSendStatus.Rejected);
                    else if (collection.Any(s => s.DocStatus == (int)Enums.DocEdoSendStatus.Sent && (s.AnnulmentStatus == 0 || s.AnnulmentStatus == -1)))
                        _edoProcessing = collection.FirstOrDefault(s => s.DocStatus == (int)Enums.DocEdoSendStatus.Sent && (s.AnnulmentStatus == 0 || s.AnnulmentStatus == -1));
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

                if (edoProc.Children.Count > 0)
                    return "Корректирован";

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
                    case (int)Enums.DocEdoSendStatus.PartialSigned:
                        return "Подписан с расхождениями";
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
                _shipperName = value?.Name;
                _shipperAddress = value?.Address;
            }
        }

        public RefContractor BuyerContractor
        {
            set {
                _consignee = value?.Name + ", " + value?.Address;
                _consigneeName = value?.Name;
                _consigneeAddress = value?.Address;
                _idChannel = value?.IdChannel;
            }
        }

        public RefCustomer BuyerCustomer
        {
            set {
                _buyerInnKpp = value?.Inn + "/" + value?.Kpp;
                _buyerInn = value?.Inn;
                _buyerKpp = value?.Kpp;
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

        public string BuyerInn
        {
            get {
                return _buyerInn;
            }
        }

        public string BuyerKpp
        {
            get {
                return _buyerKpp;
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

        public string ShipperName
        {
            get {
                return _shipperName;
            }
        }

        public string ShipperAddress
        {
            get {
                return _shipperAddress;
            }
        }

        public string Consignee {
            get 
                {
                return _consignee;
            }
        }

        public string ConsigneeName
        {
            get {
                return _consigneeName;
            }
        }

        public string ConsigneeAddress
        {
            get {
                return _consigneeAddress;
            }
        }

        public bool IsMarked { get; set; }

        public bool IsMarkedDocumentProcessed
        {
            get
            {
                if (!IsMarked)
                    return false;

                var docEdoProcessing = EdoProcessing as DocEdoProcessing;
                return docEdoProcessing != null && docEdoProcessing?.HonestMarkStatus == (int)HonestMark.DocEdoProcessingStatus.Processed;
            }
        }

        public bool IsMarkedDocumentProcessingError
        {
            get
            {
                if (!IsMarked)
                    return false;

                var docEdoProcessing = EdoProcessing as DocEdoProcessing;
                return docEdoProcessing != null && docEdoProcessing?.HonestMarkStatus == (int)HonestMark.DocEdoProcessingStatus.ProcessingError;
            }
        }

        public static AbtDbContext DbContext { get; set; }

        public object Channel
        {
            get 
                {
                if (_idChannel == null || DbContext == null)
                    return null;

                if (_channel == null)
                    _channel = from ch in DbContext.RefChannels where ch.Id == _idChannel.Value select ch;

                if (_channel as RefChannel != null)
                    return _channel;
                else if (_channel as IQueryable<RefChannel> != null)
                    _channel = (_channel as IQueryable<RefChannel>)?.FirstOrDefault();
                else if (_channel as IEnumerable<RefChannel> != null)
                    _channel = (_channel as IEnumerable<RefChannel>)?.FirstOrDefault();
                else
                {
                    _idChannel = null;
                    _channel = null;
                }

                return _channel;
            }
        }
        public string ChannelName => (Channel as RefChannel)?.Name;
        //public int ActStatusForSendFromTrader => Int32.Parse(_actStatusForSendFromTraderStr ?? "5");
        public string ActStatusForSendFromTraderStr
        {
            get {
                return _actStatusForSendFromTraderStr;
            }

            set {
                _actStatusForSendFromTraderStr = value;
            }
        }

        public string OrderNumber { get; set; }

        public string GlnShipTo { get; set; }

        public UniversalTransferDocument Init(AbtDbContext abtContext)
        {
            RefEdoGoodChannel refEdoGoodChannel = null;
            var idChannel = this.DocJournal?.DocMaster?.DocGoods?.Customer?.IdChannel;
            if (idChannel != null && idChannel != 99001)
                refEdoGoodChannel = (from r in abtContext.RefContractors
                                     where r.IdChannel == idChannel
                                     join s in (from c in abtContext.RefContractors
                                                where c.DefaultCustomer != null
                                                join refEdo in abtContext.RefEdoGoodChannels on c.IdChannel equals (refEdo.IdChannel)
                                                select new { RefEdoGoodChannel = refEdo, RefContractor = c })
                                                on r.DefaultCustomer equals (s.RefContractor.DefaultCustomer)
                                     where s != null
                                     select s.RefEdoGoodChannel)?.FirstOrDefault();

            this.DocJournal = this.DocJournal;
            _details = this.Details.Select(u => u.Init(abtContext, refEdoGoodChannel)).ToList();

            if (refEdoGoodChannel != null)
            {
                refEdoGoodChannel.EdoValuesPairs.ToList();
                RefEdoGoodChannel = refEdoGoodChannel;
                if (!string.IsNullOrEmpty(refEdoGoodChannel.OrderNumberUpdId))
                {
                    var docJournalTag = abtContext.DocJournalTags.FirstOrDefault(t => t.IdDoc == this.DocJournal.IdDocMaster && t.IdTad == 137);

                    if (docJournalTag == null)
                        throw new Exception("Отсутствует номер заказа покупателя.");

                    this.OrderNumber = docJournalTag.TagValue ?? string.Empty;
                }

                if (!string.IsNullOrEmpty(refEdoGoodChannel.GlnShipToUpdId))
                {
                    var shipToGlnJournalTag = abtContext.DocJournalTags.FirstOrDefault(t => t.IdDoc == this.DocJournal.IdDocMaster && t.IdTad == 222);

                    if (shipToGlnJournalTag == null)
                        throw new Exception("Не указан GLN грузополучателя!");

                    this.GlnShipTo = shipToGlnJournalTag.TagValue ?? string.Empty;
                }
            }

            return this;
        }
    }
}
