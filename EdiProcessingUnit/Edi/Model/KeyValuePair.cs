using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdiProcessingUnit.Edi.Model
{
	public class KeyValuePair
	{
		[System.Xml.Serialization.XmlAttributeAttribute()]
		public string key { get; set; }
		[System.Xml.Serialization.XmlTextAttribute()]
		public string Value { get; set; }
	}
}
