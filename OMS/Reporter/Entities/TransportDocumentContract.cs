using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reporter.Entities
{
    public class TransportDocumentContract : Base.IReportEntity<TransportDocumentContract>
    {
        public TransportDocumentContract()
        {
            Contractors = new List<LogisticOrgInfo>();
        }

        public string DocumentName { get; set; }
        public string DocumentNumber { get; set; }
        public DateTime DocumentDate { get; set; }
        public List<LogisticOrgInfo> Contractors { get; set; }
    }
}
