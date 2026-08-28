using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reporter.Entities
{
    public class SupplyPoint : Base.IReportEntity<SupplyPoint>
    {
        public DateTime DateTime { get; set; }
        public bool IsUtcUsed { get; set; }
        public LogisticAddressType Address { get; set; }
    }
}
