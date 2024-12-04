using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace KonturEdoClient.HonestMark.Models
{
    public class AuthRequest
    {
        [JsonProperty(PropertyName = "uuid")]
        public string Uid { get; set; }

        [JsonProperty(PropertyName = "data")]
        public string Data { get; set; }

        [JsonProperty(PropertyName = "inn")]
        public string Inn { get; set; }
    }
}
