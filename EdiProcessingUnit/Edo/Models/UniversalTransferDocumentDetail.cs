using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataContextManagementUnit.DataAccess.Contexts.Abt;

namespace EdiProcessingUnit.Edo.Models
{
    public class UniversalTransferDocumentDetail
    {
        private decimal? _vat = null;
        private decimal? _subtotal = null;
        private decimal? _price = null;
        private string _barCode = null;
        private string _countryCode = null;
        private List<string> _labels = null;
        private bool _mappedNotRequired = true;
        private RefGood _good;

        public DocGoodsDetailsI DocDetailI { get; set; }
        public DocGoodsDetail DocDetail { get; set; }
        public RefGoodMatching GoodMatching{ get; set; }
        public decimal IdGood { get; set; }

        public bool NotMapped
        {
            set {
                _mappedNotRequired = value;
            }
            get {
                return GoodMatching == null && !_mappedNotRequired;
            }
        }

        public string Product { get { return DocDetailI?.Good?.Name ?? DocDetail?.Good?.Name; } }

        public decimal? Quantity { get { return DocDetailI?.Quantity ?? DocDetail?.Quantity; } }

        public decimal? Vat {
            get 
                {
                if (DocDetailI != null && _vat == null)
                    _vat = (decimal)Math.Round(DocDetailI.TaxSumm * DocDetailI.Quantity, 2);

                return _vat;
            }
        }

        public decimal? Subtotal
        {
            get 
                {
                if (DocDetailI != null && _subtotal == null)
                    _subtotal = Math.Round(DocDetailI.Quantity * (decimal)DocDetailI.Price, 2);
                else if (DocDetail != null && _subtotal == null)
                    _subtotal = Math.Round(DocDetail.Quantity * (decimal)DocDetail.Price, 2);

                return _subtotal;
            }
        }

        public decimal? Price
        {
            get 
                {
                if(DocDetailI != null && _price == null)
                    _price = (decimal)Math.Round(DocDetailI.Price - DocDetailI.TaxSumm, 2);
                else if (DocDetail != null && _price == null)
                    _price = (decimal)Math.Round(DocDetail.Price, 2);

                return _price;
            }
        }

        public string ItemVendorCode
        {
            get 
                {
                if (_barCode == null && (DocDetailI?.Good?.BarCodes?.Count() ?? 0) > 0)
                    _barCode = DocDetailI?.Good?.BarCodes?.FirstOrDefault(b => b.IdGood == DocDetailI?.IdGood && (!b.IsPrimary ?? false))?.BarCode;
                else if(_barCode == null && (DocDetail?.Good?.BarCodes?.Count() ?? 0) > 0)
                    _barCode = DocDetail?.Good?.BarCodes?.FirstOrDefault(b => b.IdGood == DocDetail?.IdGood && (!b.IsPrimary ?? false))?.BarCode;

                return _barCode;
            }
        }

        public string BuyerCode
        {
            get {
                return GoodMatching?.CustomerArticle;
            }
        }

        public string CountryCode
        {
            get {
                return _countryCode;
            }
        }

        public List<string> Labels
        {
            get {
                return _labels;
            }
        }

        public RefGood Good
        {
            get {
                return _good;
            }
        }

        public UniversalTransferDocumentDetail SetBarCodeFromDataBase(AbtDbContext abtContext)
        {
            if (_good != null && string.IsNullOrEmpty(_barCode))
            {
                _barCode = abtContext?.RefBarCodes?
                    .FirstOrDefault(b => b.IdGood == _good.Id && b.IsPrimary == false)?
                    .BarCode;
            }

            return this;
        }

        public UniversalTransferDocumentDetail Init(AbtDbContext abtContext, RefEdoGoodChannel edoGoodChannel = null)
        {
            _barCode = this.ItemVendorCode;

            if(!string.IsNullOrEmpty(edoGoodChannel?.DetailBuyerCodeUpdId))
            {
                var idChannel = edoGoodChannel.IdChannel;

                GoodMatching = abtContext.RefGoodMatchings.FirstOrDefault(r => r.IdChannel == idChannel && r.IdGood == this.IdGood && r.Disabled == 0);

                if (GoodMatching == null)
                {
                    DateTime? docDateTime = null;

                    if(DocDetailI != null)
                        docDateTime = DocDetailI.DocJournal.DocMaster.DocDatetime.Date;
                    else if (DocDetail != null)
                        docDateTime = DocDetail.DocJournal.DocDatetime.Date;

                    if(docDateTime != null)
                        GoodMatching = abtContext.RefGoodMatchings.FirstOrDefault(r => r.DisabledDatetime != null && r.IdChannel == idChannel &&
                        r.IdGood == this.IdGood && r.Disabled == 1 && r.DisabledDatetime.Value >= docDateTime.Value);
                }

                if (GoodMatching == null)
                    throw new Exception("Не все товары сопоставлены с кодами покупателя.");

                if (string.IsNullOrEmpty(GoodMatching?.CustomerArticle))
                    throw new Exception("Не для всех товаров заданы коды покупателя.");
            }

            if (DocDetailI != null)
            {
                _good = DocDetailI.Good;
                _countryCode = abtContext.SelectSingleValue("select NUM_CODE from REF_COUNTRIES where id in" +
                    $"(select ID_COUNTRY from REF_GOODS where ID = {DocDetailI.IdGood})");

                var idDoc = DocDetailI.DocJournal.IdDocMaster;
                _labels = abtContext?.Database?
                    .SqlQuery<string>($"select DM_LABEL from doc_goods_details_labels where id_doc_sale = {idDoc} and id_good = {DocDetailI.IdGood}")?
                    .ToList() ?? new List<string>();
            }
            else if (DocDetail != null)
            {
                _good = DocDetail.Good;
                _countryCode = abtContext.SelectSingleValue("select NUM_CODE from REF_COUNTRIES where id in" +
                    $"(select ID_COUNTRY from REF_GOODS where ID = {DocDetail.IdGood})");

                _labels = abtContext?.Database?
                    .SqlQuery<string>($"select DM_LABEL from doc_goods_details_labels where id_doc_sale = {DocDetail.IdDoc} and id_good = {DocDetail.IdGood}")?
                    .ToList() ?? new List<string>();
            }

            return this;
        }
    }
}
