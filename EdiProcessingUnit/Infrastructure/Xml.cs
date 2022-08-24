using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace EdiProcessingUnit.Infrastructure
{
	public static class Xml
	{
		public static TModel DeserializeString<TModel>(string rawDocument)
		{
			TModel obj;
			XmlSerializer ser = new XmlSerializer( typeof( TModel ) );
			var stream = new StringReader( rawDocument );

			using (XmlReader reader = XmlReader.Create( stream ))
			{
				obj = (TModel)ser.Deserialize( reader );
			}

			return obj;
		}


		public static string SerializeObject<TModel>(TModel obj)
		{
			XmlSerializer ser = new XmlSerializer( typeof( TModel ) );
			StringBuilder builder = new StringBuilder();
			var _XmlWriterSettings = new XmlWriterSettings();
			_XmlWriterSettings.Encoding = Encoding.Unicode;
			_XmlWriterSettings.OmitXmlDeclaration = true;
			using (XmlWriter writer = XmlWriter.Create( builder, _XmlWriterSettings ))
			{
				ser.Serialize( writer, obj );
			}
			byte[] utf8Bytes = Encoding.UTF8.GetBytes( builder.ToString() );
			var ret = Encoding.UTF8.GetString( utf8Bytes );
			return ret;
		}

	}
}
