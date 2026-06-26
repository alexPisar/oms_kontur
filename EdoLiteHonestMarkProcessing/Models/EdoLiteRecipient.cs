using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EdoLiteHonestMarkProcessing.Models
{
    public class EdoLiteRecipient
    {
        [JsonProperty(PropertyName = "id")]
        public long Id { get; set; }

        [JsonProperty(PropertyName = "inn")]
        public long Inn { get; set; }

        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
    }
}
