using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace UtilitesLibrary.Service
{
	public static class Xml
	{
		public static List<TModel> DeserializeList<TModel>(string rawDocument)
		{
			List<TModel> Documents = new List<TModel>();
			XmlSerializer ser = new XmlSerializer( typeof( TModel ) );
			var stream = new StringReader( rawDocument );

			using (XmlReader reader = XmlReader.Create( stream ))
			{
				Documents.Add( (TModel)ser.Deserialize( reader ) );
			}

			return Documents;
		}

		public static TModel DeserializeEntity<TModel>(string rawDocument)
		{
			XmlSerializer ser = new XmlSerializer( typeof( TModel ) );
			var stream = new StringReader( rawDocument );

			using (XmlReader reader = XmlReader.Create( stream ))
			{
				return (TModel)ser.Deserialize( reader );
			}			
		}

		public static string SerializeEntity<TModel>(TModel obj, Encoding encoding = null)
		{
			XmlSerializer ser = new XmlSerializer( typeof( TModel ) );
			StringBuilder builder = new StringBuilder();
			var _XmlWriterSettings = new XmlWriterSettings();
			_XmlWriterSettings.Encoding = encoding ?? Encoding.Unicode;
			_XmlWriterSettings.OmitXmlDeclaration = true;

            if (encoding == null)
                encoding = Encoding.UTF8;

            using (XmlWriter writer = XmlWriter.Create( builder, _XmlWriterSettings ))
			{
				//writer.Settings.Encoding = Encoding.UTF8;
				ser.Serialize( writer, obj );
			}
			byte[] bytes = encoding.GetBytes( builder.ToString() );
			var ret = encoding.GetString( bytes );
			return ret;

		}

	}
}
