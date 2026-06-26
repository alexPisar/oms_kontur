using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EdoLiteHonestMarkProcessing.Models
{
    public class EdoLiteDocument
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "created_at")]
        public int CreatedAt { get; set; }

        [JsonProperty(PropertyName = "date")]
        public int Date { get; set; }

        [JsonProperty(PropertyName = "number")]
        public string Number { get; set; }

        [JsonProperty(PropertyName = "processed_at")]
        public int ProcessedAt { get; set; }

        [JsonProperty(PropertyName = "status")]
        public int Status { get; set; }

        [JsonProperty(PropertyName = "total_price")]
        public string TotalPrice { get; set; }

        [JsonProperty(PropertyName = "total_vat_amount")]
        public string TotalVatAmount { get; set; }

        [JsonProperty(PropertyName = "type")]
        public int Type { get; set; }
    }
}
