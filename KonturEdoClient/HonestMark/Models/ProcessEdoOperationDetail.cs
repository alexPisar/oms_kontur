using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace KonturEdoClient.HonestMark.Models
{
    public class ProcessEdoOperationDetail
    {
        [JsonProperty(PropertyName = "productGroups")]
        public string[] ProductGroups { get; set; }

        [JsonProperty(PropertyName = "successful")]
        public bool Successful { get; set; }

        [JsonProperty(PropertyName = "documentType")]
        public string DocumentType { get; set; }

        [JsonProperty(PropertyName = "documentName")]
        public string DocumentName { get; set; }

        [JsonProperty(PropertyName = "documentDateTime")]
        public string DocumentDateTime { get; set; }

        [JsonProperty(PropertyName = "errors")]
        public EdoProcessingError[] Errors { get; set; }
    }
}
