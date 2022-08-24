using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace KonturEdoClient.HonestMark.Models
{
    public class ReceiveProduct
    {
        [JsonProperty(PropertyName = "children")]
        public string[] Children { get; set; }

        [JsonProperty(PropertyName = "uit_code")]
        public string UitCode { get; set; }

        [JsonProperty(PropertyName = "uitu_code")]
        public string UituCode { get; set; }

        [JsonProperty(PropertyName = "cost")]
        public decimal Cost { get; set; }

        [JsonProperty(PropertyName = "vat_value")]
        public decimal VatValue { get; set; }

        [JsonProperty(PropertyName = "accepted")]
        public bool Accepted { get; set; }
    }
}
