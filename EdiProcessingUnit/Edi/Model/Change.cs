namespace EdiProcessingUnit.Edi.Model
{
	public class Change
	{
		[System.Xml.Serialization.XmlAttributeAttribute()]
		public string documentNumber { get; set; }

		[System.Xml.Serialization.XmlTextAttribute()]
		public string Value { get; set; }
	}
}
