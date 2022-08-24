using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Xml.Serialization;

namespace EdiProcessingUnit.Edi.Model
{
	public class OrderResponse
	{
		public Identificator originOrder { get; set; }
		public Identificator contractIdentificator { get; set; }
		public Identificator blanketOrderIdentificator { get; set; }
		public string orderReason { get; set; }
		public string quantityChangeReason { get; set; }
		public string lateDeliveryReason { get; set; }
		public string freeText { get; set; }
		public string paymentTerms { get; set; }
		public MultipleMessage multipleMessage { get; set; }
		public Company seller { get; set; }
		public Company buyer { get; set; }
		public Company invoicee { get; set; }
		public DeliveryInfo deliveryInfo { get; set; }

		[XmlArrayItem( "keyValuePair", IsNullable = false )]
		public KeyValuePair[] additionalInformation { get; set; }

		public Packages packages { get; set; }
		public LineItems lineItems { get; set; }

		[XmlAttribute()]
		public string number { get; set; }

		[XmlAttribute()]
		public string date { get; set; }

		[XmlAttribute()]
		public string status { get; set; }
	}

	public class MultipleMessage
	{
		public string lastMessage { get; set; }
	}

	public class BuyerContactInfo
	{
		public ContactInfo CEO { get; set; }
		public ContactInfo accountant { get; set; }
		public ContactInfo salesManager { get; set; }
		public ContactInfo orderContact { get; set; }
	}
	
	public class Packages
	{
		public PackagesPackage package { get; set; }
	}

	public class PackagesPackage
	{
		public PackageQuantity packageQuantity { get; set; }
		public PackageMeasurementInfo packageMeasurementInfo { get; set; }
	}

	public class PackageQuantity
	{
		[XmlAttribute()]
		public string typeOfPackage { get; set; }

		[XmlText()]
		public string Value { get; set; }
	}

	public class PackageMeasurementInfo
	{
		public string totalNetWeight { get; set; }
		public string totalGrossWeight { get; set; }
		public string numberOfPalletPlaces { get; set; }
	}

	public class PhysicalDimensions
	{
		public Quantity grossWeight { get; set; }
		public Quantity netWeight { get; set; }
	}

	public class DiscountsAndCharges
	{
		public Discount discount { get; set; }
	}

	public class Discount
	{
		public string calculationSequence { get; set; }
		public string percentage { get; set; }
		public string amountPerUnit { get; set; }
		public string totalAmount { get; set; }
		public QuantityForDiscount quantityForDiscount { get; set; }
	}

	public class QuantityForDiscount
	{
		[XmlAttribute()]
		public string measurementUnitCode { get; set; }
		[XmlText()]
		public string Value { get; set; }
	}

	public class UltimateCustomer
	{
		public string gln { get; set; }
	}

	public class Manufacturer
	{
		public string gln { get; set; }
		public SelfEmployed selfEmployed { get; set; }
	}

	public class SelfEmployed
	{
		public FullName fullName { get; set; }
	}
	

}