using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace EdiProcessingUnit.Edi.Model
{

	public class LineItems
	{
		[XmlElement( ElementName = "currencyISOCode" )]
		public string CurrencyISOCode { get; set; }

		[XmlElement( ElementName = "lineItem" )]
		public List<LineItem> LineItem { get; set; }
		
		[XmlElement( ElementName = "totalSumExcludingTaxes" )]
		public string TotalSumExcludingTaxes { get; set; }

		[XmlElement( ElementName = "totalVATAmount" )]
		public string TotalVATAmount { get; set; }

		[XmlElement( ElementName = "totalAmount" )]
		public string TotalAmount { get; set; }


	}
}
