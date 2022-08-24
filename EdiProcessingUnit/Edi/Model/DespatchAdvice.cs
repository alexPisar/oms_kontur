using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace EdiProcessingUnit.Edi.Model
{ 
	[Serializable()]
	[XmlRoot( Namespace = "", IsNullable = false )]
	public class DespatchAdvice
	{
		public Identificator originOrder { get; set; }
		public Identificator orderResponse { get; set; }
		public Identificator contractIdentificator { get; set; }
		public Identificator invoiceIdentificator { get; set; }
		public Identificator blanketOrderIdentificator { get; set; }
		public Identificator egaisRegistrationIdentificator { get; set; }
		public Identificator egaisFixationIdentificator { get; set; }
		public Identificator deliveryNoteIdentificator { get; set; }
		public string invoiceDeliveryMoment { get; set; }
		public string invoiceDeliveryType { get; set; }
		public string quantityChangeReason { get; set; }
		public string lateDeliveryReason { get; set; }
		public string orderReason { get; set; }
		public string paymentTerms { get; set; }
		public Company seller { get; set; }
		public Company buyer { get; set; }
		public Company invoicee { get; set; }
		[XmlElement( ElementName = "deliveryInfo" )]
		public despatchAdviceDeliveryInfo deliveryInfo { get; set; }


		[XmlElement( ElementName = "lineItems" )]
		public despatchAdviceLineItems lineItems { get; set; }
		[XmlAttribute()]
		public string number { get; set; }
		[XmlAttribute()]
		public string date { get; set; }
		[XmlAttribute()]
		public string status { get; set; }
	}
	
	[Serializable()]
	public class despatchAdviceDeliveryInfo
	{
		public Company shipFrom { get; set; }
		public Company shipTo { get; set; }
		public Company ultimateCustomer { get; set; }
		public Company transportation { get; set; }
        public string estimatedDeliveryDateTime { get; set; }
        public string shippingDateTime { get; set; }
    }
	
	[Serializable()]
	public class despatchAdviceLineItems
	{
		public string currencyISOCode { get; set; }
        [XmlElement("lineItem")]
		public despatchAdviceLineItem[] lineItem { get; set; }
		public string totalSumExcludingTaxes { get; set; }
		public string totalVATAmount { get; set; }
		public string totalAmount { get; set; }
		public string totalDiscount { get; set; }
		public string totalAmountWithDiscount { get; set; }
	}

	[Serializable()]
	public class despatchAdviceLineItem
	{
		public string gtin { get; set; }
		public string internalBuyerCode { get; set; }
		public string internalSupplierCode { get; set; }
		public string lotNumberEgais { get; set; }
		public string serialNumber { get; set; }
		public string orderLineNumber { get; set; }
		public string lineNumberInSupplierSystem { get; set; }
		[XmlElement( "typeOfUnit" )]
		public string[] typeOfUnit { get; set; }
		public string description { get; set; }
		public string comment { get; set; }
		public string quantityChangeReason { get; set; }
		public Quantity orderedQuantity { get; set; }
		public Quantity despatchedQuantity { get; set; }
		public Quantity shelfLife { get; set; }
		public string expireDate { get; set; }
		public string expireDateStart { get; set; }
		public string expireDateEnd { get; set; }
		public Company ultimateCustomer { get; set; }
		public string netPrice { get; set; }
		public string netPriceWithVAT { get; set; }
		public string netAmount { get; set; }
		public string vATRate { get; set; }
		public string vATAmount { get; set; }
		public string amount { get; set; }
		public string countryOfOriginISOCode { get; set; }
		public string customsDeclarationNumber { get; set; }
	}	
}