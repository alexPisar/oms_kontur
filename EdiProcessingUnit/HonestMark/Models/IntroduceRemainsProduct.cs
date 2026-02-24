using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EdiProcessingUnit.HonestMark.Models
{
    public class IntroduceRemainsProduct
    {
        [JsonProperty(PropertyName = "ki")]
        public string Ki { get; set; }

        [JsonProperty(PropertyName = "kitu")]
        public string Kitu { get; set; }

        [JsonProperty(PropertyName = "country")]
        public string Country { get; set; }
    }
}
