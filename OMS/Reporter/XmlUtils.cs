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

        public string SerializeObject<T>(T obj, Encoding encoding = null)
        {
            XmlSerializer ser = new XmlSerializer(typeof(T));
            StringBuilder builder = new StringBuilder();
            var _XmlWriterSettings = new XmlWriterSettings();
            _XmlWriterSettings.Indent = true;
            _XmlWriterSettings.Encoding = encoding ?? Encoding.Unicode;
            _XmlWriterSettings.OmitXmlDeclaration = true;

            if (encoding == null)
                encoding = Encoding.UTF8;

            using (XmlWriter writer = XmlWriter.Create(builder, _XmlWriterSettings))
            {
                ser.Serialize(writer, obj);
            }
            byte[] utf8Bytes = encoding.GetBytes(builder.ToString());
            var ret = encoding.GetString(utf8Bytes);
            return ret;
        }

        public string ParseCertAttribute(string certData, string attributeName)
        {
            string result = String.Empty;
            try
            {
                if (certData == null || certData == "") return result;

                attributeName = attributeName + "=";

                if (!certData.Contains(attributeName)) return result;

                int start = certData.IndexOf(attributeName);

                if (start > 0 && !certData.Substring(0, start).EndsWith(" "))
                {
                    attributeName = " " + attributeName;

                    if (!certData.Contains(attributeName)) return result;
                }

                start = certData.IndexOf(attributeName) + attributeName.Length;

                int length = certData.IndexOf('=', start) == -1 ? certData.Length - start : certData.IndexOf(", ", start) - start;

                if (length == 0) return result;
                if (length > 0)
                {
                    result = certData.Substring(start, length);

                }
                else
                {
                    result = certData.Substring(start);
                }
                return result;

            }
            catch (Exception)
            {
                return result;
            }
        }
    }
}
