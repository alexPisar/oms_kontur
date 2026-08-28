using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reporter.Entities
{
    public class LogisticOrgInfo : Base.IReportEntity<LogisticOrgInfo>
    {
        public string OrgName { get; set; }
        public string OrgInn { get; set; }
    }
}
