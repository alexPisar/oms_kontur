using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdiProcessingUnit.Edi.Model
{
	[System.SerializableAttribute()]
	public class SumForDocument
	{
		[System.Xml.Serialization.XmlAttributeAttribute()]
		public string documentNumber { get; set; }
		
		[System.Xml.Serialization.XmlTextAttribute()]
		public string Value { get; set; }

	}
}
