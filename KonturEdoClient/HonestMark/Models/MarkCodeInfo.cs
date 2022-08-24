using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace KonturEdoClient.HonestMark.Models
{
    public class MarkCodeInfo
    {
        [JsonProperty(PropertyName = "cisInfo")]
        public MarkInfo CisInfo { get; set; }

        [JsonProperty(PropertyName = "errorMessage")]
        public string ErrorMessage { get; set; }

        [JsonProperty(PropertyName = "errorCode")]
        public string ErrorCode { get; set; }
    }
}
