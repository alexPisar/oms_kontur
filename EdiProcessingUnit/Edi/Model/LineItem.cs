using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace EdiProcessingUnit.Edi.Model
{

	public class LineItem
	{
		/// <summary>
		/// статус подтверждения строки заказа: Changed - уточнен, Rejected - отклонен, Accepted - подтвержден
		/// </summary>
		[XmlAttribute( AttributeName = "status" )]
		public string Status { get; set; }

		/// <summary>
		/// GTIN товара
		/// </summary>
		[XmlElement( ElementName = "gtin" )]
		public string Gtin { get; set; }

		/// <summary>
		/// внутренний код присвоенный покупателем
		/// </summary>
		[XmlElement( ElementName = "internalBuyerCode" )]
		public string InternalBuyerCode { get; set; }


		/// <summary>
		/// артикул товара (код товара присвоенный продавцом)
		/// </summary>
		[XmlElement( ElementName = "internalSupplierCode" )]
		public string InternalSupplierCode { get; set; }

		/// <summary>
		/// название товара
		/// </summary>
		[XmlElement( ElementName = "description" )]
		public string Description { get; set; }

		/// <summary>
		/// заказанное количество
		/// </summary>
		[XmlElement( ElementName = "orderedQuantity" )]
		public Quantity OrderedQuantity { get; set; }

		/// <summary>
		/// подтвержденнное количество
		/// </summary>
		[XmlElement( ElementName = "confirmedQuantity" )]
		public Quantity AcceptedQuantity { get; set; }

		/// <summary>
		/// заказанное количество
		/// </summary>
		[XmlElement( ElementName = "requestedQuantity" )]
		public Quantity RequestedQuantity { get; set; }

		/// <summary>
		/// цена товара без НДС
		/// </summary>
		[XmlElement( ElementName = "netPrice" )]
		public string NetPrice { get; set; }

		/// <summary>
		/// цена товара с НДС
		/// </summary>
		[XmlElement( ElementName = "netPriceWithVAT" )]
		public string NetPriceWithVAT { get; set; }

		/// <summary>
		/// сумма по позиции без НДС
		/// </summary>
		[XmlElement( ElementName = "netAmount" )]
		public string NetAmount { get; set; }

		/// <summary>
		/// ставка НДС 
		/// (NOT_APPLICABLE - без НДС, 0 - 0%, 10 - 10%, 18 - 18%, 20 - 20%)
		/// </summary>
		[XmlElement( ElementName = "vATRate" )]
		public string VATRate { get; set; }

		/// <summary>
		/// сумма НДС по позиции
		/// </summary>
		[XmlElement( ElementName = "vATAmount" )]
		public string VATAmount { get; set; }

		/// <summary>
		/// сумма по позиции с НДС
		/// </summary>
		[XmlElement( ElementName = "amount" )]
		public string Amount { get; set; }

		/// <summary>
		/// код страны производства
		/// </summary>
		[XmlElement( ElementName = "countryOfOriginISOCode" )]
		public string CountryOfOriginISOCode { get; set; }

	}

}

