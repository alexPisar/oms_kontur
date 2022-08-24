using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EdiProcessingUnit.Edi.Model;

namespace EdiProcessingUnit.Edi.Model
{
	public class CorrectiveInvoice
	{
		public Identificator contractIdentificator { get; set; }
		public string factoringEncription { get; set; }
		public originInvoic originInvoic { get; set; }
		public Identificator originOrder { get; set; }
		public Identificator specificationIdentificator { get; set; }
		public Identificator orderResponse { get; set; }
		public Identificator despatchIdentificator { get; set; }
		public Identificator receivingIdentificator { get; set; }
		public Identificator announcementForReturns { get; set; }
		public Identificator receivingAdviceIdentificatorInBuyerSystem { get; set; }
		public Identificator blanketOrderIdentificator { get; set; }
		//public eDIMessageCorrectiveInvoiceTermsOfPayment termsOfPayment { get; set; }
		public Company seller { get; set; }
		public Company buyer { get; set; }
		public Company invoicee { get; set; }
		public Company deliveryInfo { get; set; }
		public CoinvoicLineItems lineItems { get; set; }
		[System.Xml.Serialization.XmlAttributeAttribute()]
		public string number { get; set; }
		[System.Xml.Serialization.XmlAttributeAttribute()]
		public string date { get; set; }
		[System.Xml.Serialization.XmlAttributeAttribute()]
		public string type { get; set; }
		[System.Xml.Serialization.XmlAttributeAttribute()]
		public string revisionNumber { get; set; }
		[System.Xml.Serialization.XmlAttributeAttribute()]
		public string revisionDate { get; set; }
		[System.Xml.Serialization.XmlAttributeAttribute()]
		public string ucdFunction { get; set; }
	}


	public class CoinvoicLineItems
	{
		public string currencyISOCode { get; set; }
		public CoinvoicLineItem lineItem { get; set; }
		public string totalSumExcludingTaxesDecrease { get; set; }
		public string totalSumExcludingTaxesIncrease { get; set; }
		public string totalVATAmountDecrease { get; set; }
		public string totalVATAmountIncrease { get; set; }
		public string totalAmountDecrease { get; set; }
		public string totalAmountIncrease { get; set; }
		public Change totalSumExcludingTaxesForIVDecrease { get; set; }
		public Change totalSumExcludingTaxesForIVIncrease { get; set; }
		public Change totalVATAmountForIVDecrease { get; set; }
		public Change totalVATAmountForIVIncrease { get; set; }
		public Change totalAmountForIVDecrease { get; set; }
		public Change totalAmountForIVIncrease { get; set; }
	}

	public class CoinvoicLineItem
	{
		public string gtin { get; set; }
		public string internalBuyerCode { get; set; }
		public string internalSupplierCode { get; set; }
		[System.Xml.Serialization.XmlElementAttribute( "typeOfUnit" )]
		public string[] typeOfUnit { get; set; }
		public string description { get; set; }
		public string descriptionColor { get; set; }
		public string descriptionSize { get; set; }
		public string comment { get; set; }
		public Quantity quantityBefore { get; set; }
		public Quantity quantityAfter { get; set; }
		public Quantity quantityIncrease { get; set; }
		public Quantity quantityDecrease { get; set; }
		public Quantity onePlaceQuantity { get; set; }
		public IdentificationMark controlIdentificationMarksBefore { get; set; }
		public IdentificationMark controlIdentificationMarksAfter { get; set; }
		public string netPriceWithVAT { get; set; }
		public string netPriceBefore { get; set; }
		public string netPriceAfter { get; set; }
		public string netPriceIncrease { get; set; }
		public string netPriceDecrease { get; set; }
		public string netAmountBefore { get; set; }
		public string netAmountAfter { get; set; }
		public string netAmountIncrease { get; set; }
		public string netAmountDecrease { get; set; }
		public string exciseDutyBefore { get; set; }
		public string exciseDutyAfter { get; set; }
		public string exciseDutyIncrease { get; set; }
		public string exciseDutyDecrease { get; set; }
		public string vatAmountBefore { get; set; }
		public string vatAmountAfter { get; set; }
		public string vatAmountIncrease { get; set; }
		public string vatAmountDecrease { get; set; }
		public string amountBefore { get; set; }
		public string amountAfter { get; set; }
		public string amountIncrease { get; set; }
		public string amountDecrease { get; set; }
		public string vatRateBefore { get; set; }
		public string vatRateAfter { get; set; }
		public string customsDeclarationNumber { get; set; }
		public string countryOfOriginISOCode { get; set; }
	}

	public class IdentificationMark
	{
		[System.Xml.Serialization.XmlAttributeAttribute()]
		public string type { get; set; }

		[System.Xml.Serialization.XmlTextAttribute()]
		public string Value { get; set; }
	}

	public class originInvoic
	{
		[System.Xml.Serialization.XmlAttributeAttribute()]
		public string number { get; set; }
		[System.Xml.Serialization.XmlAttributeAttribute()]
		public string date { get; set; }
		[System.Xml.Serialization.XmlAttributeAttribute()]
		public string revisionNumber { get; set; }
		[System.Xml.Serialization.XmlAttributeAttribute()]
		public string revisionDate { get; set; }
		[System.Xml.Serialization.XmlAttributeAttribute()]
		public string diadocInvoicId { get; set; }
		[System.Xml.Serialization.XmlAttributeAttribute()]
		public string ediInvoicId { get; set; }
	}

}