using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace Reporter
{
    public class XmlUtils
    {
        public T DeserializeString<T>(string rawDocument)
        {
            T obj;
            XmlSerializer ser = new XmlSerializer(typeof(T));
            var stream = new StringReader(rawDocument);

            using (XmlReader reader = XmlReader.Create(stream))
            {
                obj = (T)ser.Deserialize(reader);
            }

            return obj;
        }

        public string SerializeObject<T>(T obj)
        {
            XmlSerializer ser = new XmlSerializer(typeof(T));
            StringBuilder builder = new StringBuilder();
            var _XmlWriterSettings = new XmlWriterSettings();
            _XmlWriterSettings.Encoding = Encoding.Unicode;
            _XmlWriterSettings.OmitXmlDeclaration = true;
            using (XmlWriter writer = XmlWriter.Create(builder, _XmlWriterSettings))
            {
                ser.Serialize(writer, obj);
            }
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(builder.ToString());
            var ret = Encoding.UTF8.GetString(utf8Bytes);
            return ret;
        }
    }
}
