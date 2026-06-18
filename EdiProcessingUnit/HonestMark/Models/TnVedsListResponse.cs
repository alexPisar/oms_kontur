using System;
using Newtonsoft.Json;

namespace EdiProcessingUnit.HonestMark.Models
{
    public class TnVedsListResponse
    {
        [JsonProperty(PropertyName = "tnveds")]
        public TnVedInfo[] TnVeds { get; set; }

        [JsonProperty(PropertyName = "total")]
        public long Total { get; set; }

        [JsonProperty(PropertyName = "last")]
        public bool Last { get; set; }

        [JsonProperty(PropertyName = "errorMessage")]
        public string ErrorMessage { get; set; }

        [JsonProperty(PropertyName = "errorCode")]
        public string ErrorCode { get; set; }
    }
}
