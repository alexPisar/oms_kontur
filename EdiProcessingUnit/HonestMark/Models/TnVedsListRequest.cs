using System;
using Newtonsoft.Json;

namespace EdiProcessingUnit.HonestMark.Models
{
    public class TnVedsListRequest
    {
        [JsonProperty(PropertyName = "pg", NullValueHandling = NullValueHandling.Ignore)]
        public string[] ProductGroups { get; set; }

        [JsonProperty(PropertyName = "tnveds", NullValueHandling = NullValueHandling.Ignore)]
        public string[] TnVeds { get; set; }

        [JsonProperty(PropertyName = "page")]
        public int Page { get; set; }

        [JsonProperty(PropertyName = "limit")]
        public int Limit { get; set; }

        [JsonProperty(PropertyName = "sort", NullValueHandling = NullValueHandling.Ignore)]
        public string Sort { get; set; }

        [JsonProperty(PropertyName = "direction", NullValueHandling = NullValueHandling.Ignore)]
        public string Direction { get; set; }
    }
}
