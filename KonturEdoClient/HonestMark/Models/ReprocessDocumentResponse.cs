using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace KonturEdoClient.HonestMark.Models
{
    public class ReprocessDocumentResponse
    {
        [JsonProperty(PropertyName = "code")]
        public string Code { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "sourceDocId")]
        public string SourceDocId { get; set; }

        [JsonProperty(PropertyName = "resultDocId")]
        public string ResultDocId { get; set; }

        [JsonProperty(PropertyName = "nextTimeToReprocess")]
        public DateTime NextTimeToReprocess { get; set; }

        [JsonProperty(PropertyName = "error")]
        public string Error { get; set; }

        [JsonProperty(PropertyName = "error_description")]
        public string ErrorDescription { get; set; }
    }
}
