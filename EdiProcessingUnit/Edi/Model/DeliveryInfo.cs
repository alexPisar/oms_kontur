using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace EdiProcessingUnit.Edi.Model
{
	public class DeliveryInfo
	{
		[XmlElement( ElementName = "requestedDeliveryDateTime" )]
		public string RequestedDeliveryDateTime { get; set; }

		[XmlElement( ElementName = "shipTo" )]
		public Company ShipTo { get; set; }
	}
}
