using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reporter.Entities
{
    public class LogisticAddressType : Base.IReportEntity<LogisticAddressType>
    {
        public string Gln { get; set; }
        public string Comment { get; set; }
        public KeyValuePair<string, string>? Coordinates { get; set; } = null;
        public Address Address { get; set; }
    }
}
