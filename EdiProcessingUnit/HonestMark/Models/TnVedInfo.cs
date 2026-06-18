using System;
using Newtonsoft.Json;

namespace EdiProcessingUnit.HonestMark.Models
{
    public class TnVedInfo
    {
        [JsonProperty(PropertyName = "tnved")]
        public string TnVedCode { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "pg")]
        [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public ProductGroupsEnum ProductGroup { get; set; }
    }
}
