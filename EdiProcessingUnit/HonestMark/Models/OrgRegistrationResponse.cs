using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EdiProcessingUnit.HonestMark.Models
{
    public class OrgRegistrationResponse
    {
        [JsonProperty(PropertyName = "inn")]
        public string Inn { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "statusInn")]
        public string StatusInn { get; set; }

        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        [JsonProperty(PropertyName = "is_registered")]
        public bool IsRegistered { get; set; }

        [JsonProperty(PropertyName = "is_kfh")]
        public bool IsKfh { get; set; }

        [JsonProperty(PropertyName = "okopf")]
        public string Okopf { get; set; }

        [JsonProperty(PropertyName = "productGroups")]
        public string[] ProductGroups { get; set; }
    }
}
