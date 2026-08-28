using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reporter.Entities
{
    public class ContactData : Base.IReportEntity<ContactData>
    {
        public string Phone { get; set; }
        public string Email { get; set; }
        public string OtherData { get; set; }
        public List<AdditionalInfo> TextData { get; set; }
    }
}
