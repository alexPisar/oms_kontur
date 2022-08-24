using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace EdiProcessingUnit.Edi.Model
{

	public class InterchangeHeader
	{
		[XmlElement( ElementName = "sender" )]
		public string Sender { get; set; }

		[XmlElement( ElementName = "recipient" )]
		public string Recipient { get; set; }

		[XmlElement( ElementName = "documentType" )]
		public string DocumentType { get; set; }

		[XmlElement( ElementName = "isTest" )]
		public string IsTest { get; set; }

		[XmlElement( ElementName = "creationDateTime" )]
		public string CreationDateTime { get; set; }

		[XmlElement( ElementName = "creationDateTimeBySender" )]
		public string CreationDateTimeBySender { get; set; }
		
	}
}
