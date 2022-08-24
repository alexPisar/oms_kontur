using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.Windows.Markup;

namespace Reporter
{
    public abstract class IReporter
    {
        private const string reportsWindowFolderName = "ReportWindows";

        protected IReport _report;

        public abstract void LoadFromXml(string xml);

        public IReport GetReport()
        {
            return _report;
        }

        public void ShowReportWindow()
        {
            string documentFilePath = reportsWindowFolderName + "//" + _report.GetDocumentId() + ".xml";

            var xmlDocument = new XmlDocument();
            xmlDocument.Load(documentFilePath);

            var xmlStream = new System.IO.StringReader(xmlDocument.OuterXml);
            var xmlReader = XmlReader.Create(xmlStream);

            var window = XamlReader.Load(xmlReader) as System.Windows.Window;
            window.DataContext = _report;
            window.ShowDialog();
        }
    }
}
