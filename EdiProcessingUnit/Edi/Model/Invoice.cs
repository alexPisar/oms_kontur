using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace EdiProcessingUnit.Edi.Model
{
	[Serializable()]	
	public class invoice
	{
		public invoiceOriginInvoic originInvoic { get; set; }
		public Identificator originOrder { get; set; }
		public Identificator orderResponse { get; set; }
		public Identificator contractIdentificator { get; set; }
		public Identificator despatchIdentificator { get; set; }
		public Identificator receivingIdentificator { get; set; }
		public Identificator receivingAdviceIdentificatorInBuyerSystem { get; set; }
		public Identificator blanketOrderIdentificator { get; set; }
		public string factoringEncription { get; set; }
		public Identificator specificationIdentificator { get; set; }
		public invoiceTermsOfPayment termsOfPayment { get; set; }
		public Company seller { get; set; }
		public Company buyer { get; set; }
		public invoicee invoicee { get; set; }
		public invoiceDeliveryInfo deliveryInfo { get; set; }
		public invoicePackages packages { get; set; }
		[XmlArrayItem( "keyValuePair", IsNullable = false )]
		public KeyValuePair[] additionalInformation { get; set; }
		public invoiceLineItems lineItems { get; set; }
		[XmlAttribute()]
		public string number { get; set; }
		[XmlAttribute()]
		public string date { get; set; }
		[XmlAttribute()]
		public string type { get; set; }
		[XmlAttribute()]
		public string revisionNumber { get; set; }
		[XmlAttribute()]
		public string revisionDate { get; set; }
		/// <summary>
		/// функция УПД: INVDOP - СЧФДОП, INV - СЧФ, DOP - ДОП
		/// </summary>
		[XmlAttribute()]
		public string utdFunction { get; set; }
	}

	[Serializable()]
	public class invoiceOriginInvoic
	{
		[XmlAttribute()]
		public string ediInvoicId { get; set; }
		[XmlAttribute()]
		public string diadocInvoicId { get; set; }
	}
	
	[Serializable()]
	public class invoiceTermsOfPayment
	{
		public string interval { get; set; }
		public string intervalLength { get; set; }
	}
	
	[Serializable()]
	public class invoiceDeliveryInfo
	{
		public string actualDeliveryDateTime { get; set; }
		public Identificator waybill { get; set; }
		public Identificator railWaybill { get; set; }
		public Company shipFrom { get; set; }
		public Company shipTo { get; set; }
		public Company ultimateCustomer { get; set; }
		public Company warehouseKeeper { get; set; }
	}

	[Serializable()]
	public class invoicePackages
	{
		public invoicePackagesPackage package { get; set; }
	}

	[Serializable()]
	public class invoicePackagesPackage
	{
		public string parentLevel { get; set; }
		public string packageLevel { get; set; }
		public invoicePackagesPackagePackageQuantity packageQuantity { get; set; }
		public string SSCC { get; set; }
	}

	[Serializable()]
	public class invoicePackagesPackagePackageQuantity
	{
		[XmlAttribute()]
		public string typeOfPackage { get; set; }
		[XmlAttribute()]
		public string packageDescription { get; set; }
		[XmlText()]
		public string Value { get; set; }
	}
	
	[Serializable()]
	public class invoiceLineItems
	{
		public string currencyISOCode { get; set; }
		public string contractualCurrencyISOCode { get; set; }
		public string currencyExchangeRate { get; set; }
		[XmlElement( ElementName = "lineItem" )]
		public List<invoiceLineItemsLineItem> lineItem { get; set; }
		public string totalSumExcludingTaxes { get; set; }
		public string totalAmount { get; set; }
		public string totalVATAmount { get; set; }
		public string totalSumExcludingTaxesInContractualCurrency { get; set; }
		public string totalVATAmountInContractualCurrency { get; set; }
		public string totalAmountInContractualCurrency { get; set; }
		public SumForDocument totalSumExcludingTaxesForDQ { get; set; }
		public SumForDocument totalVATAmountForDQ { get; set; }
		public SumForDocument totalAmountForDQ { get; set; }
		public SumForDocument totalSumExcludingTaxesForIV { get; set; }
		public SumForDocument totalVATAmountForIV { get; set; }
		public SumForDocument totalAmountForIV { get; set; }
	}

	[Serializable()]
	public class invoiceLineItemsLineItem
	{
		public string gtin { get; set; }
		public string internalBuyerCode { get; set; }
		public string internalSupplierCode { get; set; }
		public string orderLineNumber { get; set; }
		public string comment { get; set; }
		public Quantity quantity { get; set; }
		public string excludeFromSummation { get; set; }
		public string description { get; set; }
		[XmlArrayItem( "keyValuePair", IsNullable = false )]
		public KeyValuePair[] additionalInformation { get; set; }
		public string netPrice { get; set; }
		public string netPriceInContractualCurrency { get; set; }
		public string netPriceWithVAT { get; set; }
		public string netAmount { get; set; }
		public string vATRate { get; set; }
		public string vATAmount { get; set; }
		public string amount { get; set; }
		public string netAmountInContractualCurrency { get; set; }
		public string vATAmountInContractualCurrency { get; set; }
		public string amountInContractualCurrency { get; set; }
		public string countryOfOriginISOCode { get; set; }
		public string customsDeclarationNumber { get; set; }


}
	
}

