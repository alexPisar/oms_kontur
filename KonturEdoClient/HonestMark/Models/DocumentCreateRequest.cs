using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace KonturEdoClient.HonestMark.Models
{
    public class DocumentCreateRequest
    {
        [JsonProperty(PropertyName = "document_format")]
        public string DocumentFormat { get; set; }

        [JsonProperty(PropertyName = "product_document")]
        public string ProductDocument { get; set; }

        [JsonProperty(PropertyName = "signature")]
        public string Signature { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
    }
}
