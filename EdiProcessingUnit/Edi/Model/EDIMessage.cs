using System.Xml.Serialization;

namespace EdiProcessingUnit.Edi.Model
{
	[XmlRoot( ElementName = "eDIMessage" )]
	public class EDIMessage
	{
		[XmlAttribute( AttributeName = "id" )]
		public string Id { get; set; }

		[XmlAttribute( AttributeName = "creationDateTime" )]
		public string CreationDateTime { get; set; }



		[XmlElement( ElementName = "interchangeHeader" )]
		public InterchangeHeader InterchangeHeader { get; set; }

		[XmlElement( ElementName = "order" )]
		public Order Order { get; set; }

		[XmlElement( ElementName = "receivingAdvice" )]
		public ReceivingAdvice ReceivingAdvice { get; set; }

		[XmlElement( ElementName = "orderResponse" )]
		public OrderResponse orderResponse { get; set; }

		[XmlElement( ElementName = "despatchAdvice" )]
		public DespatchAdvice DespatchAdvice { get; set; }

		[XmlElement( ElementName = "invoice" )]
		public invoice Invoice { get; set; }
		
		[XmlElement( ElementName = "correctiveInvoice" )]
		public CorrectiveInvoice correctiveInvoice { get; set; }		
		
	}
}
