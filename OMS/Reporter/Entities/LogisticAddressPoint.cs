using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reporter.Entities
{
    public class LogisticAddressPoint : Base.IReportEntity<LogisticAddressPoint>
    {
        public Enums.LogisticAddressPointOperationEnum Operation { get; set; }
        public string PositionPoint { get; set; }
        public LogisticAddressType AddressPoint { get; set; }
        public LogisticOrgInfo OwnerOrganization { get; set; }
    }
}
