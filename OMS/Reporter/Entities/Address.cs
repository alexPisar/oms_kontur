using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reporter.Entities
{
    public class Address : Base.IReportEntity<Address>
    {
        public string CountryCode { get; set; }
        public string CountryName { get; set; }
        public string ForeignTextAddress { get; set; }
        public string RussianIndex { get; set; }
        public string RussianCity { get; set; }
        public string RussianStreet { get; set; }
        public string RussianRegionCode { get; set; }
        public string RussianRegionName { get; set; }
        public string RussianHouse { get; set; }
        public string RussianFlat { get; set; }
    }
}
