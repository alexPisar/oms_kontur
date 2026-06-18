using System;
using Newtonsoft.Json;

namespace EdiProcessingUnit.HonestMark.Models.Base
{
    public class SearchResult<T>
    {
        [JsonProperty(PropertyName = "total")]
        public int Total { get; set; }

        [JsonProperty(PropertyName = "results")]
        public T[] Results { get; set; }

        [JsonProperty(PropertyName = "errorCode")]
        public string ErrorCode { get; set; }
    }
}
