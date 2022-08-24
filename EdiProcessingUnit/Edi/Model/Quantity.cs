using System;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace EdiProcessingUnit.Edi.Model
{	
	public class Quantity
	{
		[XmlAttribute( AttributeName = "unitOfMeasure" )]
		public string UnitOfMeasure { get; set; }

		[XmlText]
		public string Text { get; set; }
	}
}
