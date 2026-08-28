using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reporter.Entities
{
    public class LogisticsParticipantType : Base.IReportEntity<LogisticsParticipantType>
    {
        public object Item { get; set; }
        public ContactData Contact { get; set; }
        public Address Address { get; set; }
    }
}
