using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataContextManagementUnit.DataAccess.Contexts.Abt;

namespace EdiProcessingUnit.Edo.Models
{
    public class UniversalCorrectionDocumentDetail
    {
        private string _barCode = null;
        private RefGood _good;
        private string _countryCode = null;
        private string _countryName = null;
        private string _customsNo = null;
        private bool _honestMarkGood;

        public decimal IdGood { get; set; }
        public DocGoodsDetailsI DocDetailsI { get; set; }
        public DocGoodsDetail DocDetail { get; set; }
        public DocGoodsDetailsI BaseDetail { get; set; }
        public RefGoodMatching GoodMatching { get; set; }
        public int BaseIndex { get; set; }

        public string ItemVendorCode
        {
            get
            {
                if (_barCode == null)
                {
                    if ((DocDetail?.Good?.BarCodes?.Count() ?? 0) > 0)
                        _barCode = DocDetail?.Good?.BarCodes?.FirstOrDefault(b => b.IdGood == DocDetail?.IdGood && (!b.IsPrimary ?? false))?.BarCode;
                    else if((DocDetailsI?.Good?.BarCodes?.Count() ?? 0) > 0)
                        _barCode = DocDetailsI?.Good?.BarCodes?.FirstOrDefault(b => b.IdGood == DocDetailsI?.IdGood && (!b.IsPrimary ?? false))?.BarCode;
                }

                return _barCode;
            }
        }

        public RefGood Good
        {
            get
            {
                return _good;
            }
        }

        public string CountryCode
        {
            get
            {
                return _countryCode;
            }
        }

        public string CountryName
        {
            get
            {
                return _countryName;
            }
        }

        public string CustomsNo
        {
            get
            {
                return _customsNo;
            }
        }

        public List<string> OriginalMarkedCodes { get; set; }
        public List<string> CorrectedMarkedCodes { get; set; }

        public string BuyerCode
        {
            get
            {
                return GoodMatching?.CustomerArticle;
            }
        }

        public bool HonestMarkGood => _honestMarkGood;

        public UniversalCorrectionDocumentDetail SetBarCodeFromDataBase(AbtDbContext abtContext)
        {
            if (_good != null && string.IsNullOrEmpty(_barCode))
            {
                _barCode = abtContext?.RefBarCodes?
                    .FirstOrDefault(b => b.IdGood == _good.Id && b.IsPrimary == false)?
                    .BarCode;
            }

            return this;
        }

        public UniversalCorrectionDocumentDetail Init(AbtDbContext abtContext, DocJournal invoiceDocJournal, bool isMarked = false, RefEdoGoodChannel edoGoodChannel = null)
        {
            _barCode = ItemVendorCode;

            BaseDetail = invoiceDocJournal.DocGoodsDetailsIs.FirstOrDefault(d => d.IdGood == this.IdGood);

            if (BaseDetail == null)
                throw new Exception($"Не найден товар {this.IdGood} из корректировки к документу {invoiceDocJournal?.Code}");

            BaseIndex = invoiceDocJournal.DocGoodsDetailsIs.IndexOf(BaseDetail) + 1;

            if (!string.IsNullOrEmpty(edoGoodChannel?.DetailBuyerCodeUpdId))
            {
                var idChannel = edoGoodChannel.IdChannel;

                GoodMatching = abtContext.RefGoodMatchings.FirstOrDefault(r => r.IdChannel == idChannel && r.IdGood == this.IdGood && r.Disabled == 0);

                if (GoodMatching == null)
                {
                    DateTime? docDateTime = null;

                    if (invoiceDocJournal?.DocMaster != null)
                        docDateTime = invoiceDocJournal.DocMaster?.DocDatetime.Date;

                    if (docDateTime != null)
                        GoodMatching = abtContext.RefGoodMatchings.FirstOrDefault(r => r.DisabledDatetime != null && r.IdChannel == idChannel &&
                        r.IdGood == this.IdGood && r.Disabled == 1 && r.DisabledDatetime.Value >= docDateTime.Value);
                }

                if (GoodMatching == null)
                    throw new Exception("Не все товары сопоставлены с кодами покупателя.");

                if (string.IsNullOrEmpty(GoodMatching?.CustomerArticle))
                    throw new Exception("Не для всех товаров заданы коды покупателя.");
            }

            if(DocDetailsI != null)
                _good = DocDetailsI.Good;
            else if (DocDetail != null)
            {
                _good = DocDetail.Good;
                _honestMarkGood = abtContext.RefItems.Any(r => r.IdName == 30071 && r.IdGood == this.IdGood && r.Quantity == 1);
            }

            if(_good != null)
            {
                var country = abtContext.RefCountries.FirstOrDefault(c => c.Id == _good.IdCountry);

                if(country != null)
                {
                    _countryCode = country.NumCode?.ToString();
                    _countryName = country.Name;
                }
            }

            _customsNo = _good?.CustomsNo;

            if(isMarked)
            {
                var originalLabels = (from label in abtContext.DocGoodsDetailsLabels
                                      where label.IdDocSale == invoiceDocJournal.IdDocMaster && label.IdGood == this.IdGood
                                      select label)?.ToList();

                var correctedLabels = originalLabels?.Where(o => o.IdDocReturn == null);

                OriginalMarkedCodes = originalLabels?.Select(o => o.DmLabel)?.ToList() ?? new List<string>();
                CorrectedMarkedCodes = correctedLabels?.Select(c => c.DmLabel)?.ToList() ?? new List<string>();
            }

            return this;
        }
    }
}
