using System.Collections.Generic;
using System.Xml.Serialization;

namespace EdiProcessingUnit.Edi.Model
{
	[XmlRoot(ElementName="receivingAdvice")]
	public class ReceivingAdvice
	{
		[XmlElement(ElementName="originOrder")]
		public Identificator OriginOrder { get; set; }

		[XmlElement(ElementName="contractIdentificator")]
		public Identificator ContractIdentificator { get; set; }

		[XmlElement(ElementName="despatchIdentificator")]
		public Identificator DespatchIdentificator { get; set; }

		[XmlElement(ElementName="seller")]
		public Company Seller { get; set; }

		[XmlElement(ElementName="buyer")]
		public Company Buyer { get; set; }

		[XmlElement(ElementName="deliveryInfo")]
		public DeliveryInfo DeliveryInfo { get; set; }

		[XmlElement(ElementName="lineItems")]
		public recadvLineItems recadvLineItems { get; set; }

		[XmlAttribute(AttributeName="number")]
		public string Number { get; set; }

		[XmlAttribute(AttributeName="date")]
		public string Date { get; set; }
	}

	public class recadvLineItems
	{
		[XmlElement( ElementName = "lineItem" )]
		public List<recadvLineItem> LineItem { get; set; }

	}


	public class recadvLineItem
	{
		public string gtin { get; set; }
		public string internalBuyerCode { get; set; }
		public string codeOfEgais { get; set; }
		public string externalProductId { get; set; }
		public string lotNumberEgais { get; set; }
		public string orderLineNumber { get; set; }
		public string veterinaryCertificateMercuryId { get; set; }
		public decimal mercuryUnitConversionFactor { get; set; }
		public string description { get; set; }
		public Quantity orderedQuantity { get; set; }
		public Quantity despatchedQuantity { get; set; }
		public Quantity deliveredQuantity { get; set; }
		public Quantity acceptedQuantity { get; set; }
		public Quantity onePlaceQuantity { get; set; }
		public Quantity alternativeQuantity { get; set; }
		public decimal netPrice { get; set; }
		public decimal netPriceWithVAT { get; set; }
		public decimal netAmount { get; set; }
		public string vATRate { get; set; }
		public decimal exciseDuty { get; set; }
		public decimal amount { get; set; }
		[System.Xml.Serialization.XmlArrayItemAttribute( "keyValuePair", IsNullable = false )]
		public KeyValuePair[] additionalInformation { get; set; }
	}

}