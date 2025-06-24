using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EdiProcessingUnit.HonestMark.Models
{
    public class ShipmentProduct
    {
        [JsonProperty(PropertyName = "uit_code")]
        public string UitCode { get; set; }

        [JsonProperty(PropertyName = "uitu_code")]
        public string UituCode { get; set; }

        [JsonProperty(PropertyName = "product_description")]
        public string ProductDescription { get; set; }

        [JsonProperty(PropertyName = "product_cost")]
        public decimal ProductCost { get; set; }

        [JsonProperty(PropertyName = "product_tax")]
        public decimal ProductTax { get; set; }
    }
}
