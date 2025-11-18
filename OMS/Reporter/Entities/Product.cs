using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reporter.Entities
{
    public class Product : Base.IReportEntity<Product>
    {
        public Product()
        {
            AdditionalInfos = new List<AdditionalInfo>();
        }

        public int Number { get; set; }

        public string Description { get; set; }

        public decimal? Quantity { get; set; }

        public string BarCode { get; set; }

        public decimal? Price { get; set; }

        public decimal? TaxAmount { get; set; }

        public decimal? Subtotal { get; set; }

        public decimal? SubtotalWithVatExcluded { get; set; }

        public List<string> MarkedCodes { get; set; }

        public List<string> TransportPackingIdentificationCode { get; set; }

        public decimal? VatRate { get; set; }

        public string UnitCode { get; set; }

        public string UnitName { get; set; }

        public bool WithoutExcise { get; set; } = true;

        public decimal ExciseSumm { get; set; }

        public string OriginCode { get; set; }

        public string CustomsDeclarationCode { get; set; }

        public string OriginCountryName { get; set; }
        public List<AdditionalInfo> AdditionalInfos { get; set; }
    }
}
