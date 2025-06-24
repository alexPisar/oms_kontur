using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EdiProcessingUnit.HonestMark.Models
{
    public class AuthData
    {
        [JsonProperty(PropertyName = "uuid")]
        public string Uid { get; set; }

        [JsonProperty(PropertyName = "data")]
        public string Data { get; set; }
    }
}
