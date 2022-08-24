using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace EdiProcessingUnit.Edi.Model
{

	public class Identificator
	{
		[XmlAttribute( AttributeName = "number" )]
		public string Number { get; set; }

		[XmlAttribute( AttributeName = "date" )]
		public string Date { get; set; }
	}
}
