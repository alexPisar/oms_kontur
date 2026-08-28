using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reporter.Entities
{
    public class LogisticWeightType : Base.IReportEntity<LogisticWeightType>
    {
        public decimal Gross { get; set; }
    }
}
