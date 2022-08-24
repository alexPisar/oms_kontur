using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reporter.Reporters
{
    public class UniversalTransferDocumentReporter : IReporter
    {
        public override void LoadFromXml(string xml)
        {
            var xmlUtil = new XmlUtils();
            _report = xmlUtil.DeserializeString<UniversalTransferDocumentWithHyphens>(xml);
        }
    }
}
