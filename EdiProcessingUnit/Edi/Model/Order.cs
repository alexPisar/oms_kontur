using System.Xml.Serialization;

namespace EdiProcessingUnit.Edi.Model
{
	[XmlRoot( ElementName = "order" )]
	public class Order
	{
		[XmlElement( ElementName = "contractIdentificator" )]
		public Identificator ContractIdentificator { get; set; }

		[XmlElement( ElementName = "seller" )]
		public Company Seller { get; set; }

		[XmlElement( ElementName = "buyer" )]
		public Company Buyer { get; set; }

		[XmlElement( ElementName = "deliveryInfo" )]
		public DeliveryInfo DeliveryInfo { get; set; }

		[XmlElement( ElementName = "comment" )]
		public string Comment { get; set; }

		[XmlElement( ElementName = "lineItems" )]
		public LineItems LineItems { get; set; }

		[XmlAttribute( AttributeName = "number" )]
		public string Number { get; set; }

		[XmlAttribute( AttributeName = "date" )]
		public string Date { get; set; }
	}
}
