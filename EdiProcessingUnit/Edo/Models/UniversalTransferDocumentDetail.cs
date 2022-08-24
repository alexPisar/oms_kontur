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

        public DocGoodsDetailsI DocDetailI { get; set; }
        public DocGoodsDetail DocDetail { get; set; }

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
    }
}
