using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EdiProcessingUnit.HonestMark.Models
{
    public class DocumentEdoProcessResultInfo
    {
        [JsonProperty(PropertyName = "resultDocId")]
        public string ResultDocId { get; set; }

        [JsonProperty(PropertyName = "resultDocDate")]
        public long? ResultDocDate { get; set; }

        [JsonProperty(PropertyName = "sourceDocId")]
        public string SourceDocId { get; set; }

        [JsonProperty(PropertyName = "sourceDocDate")]
        public long? SourceDocDate { get; set; }

        [JsonProperty(PropertyName = "state")]
        public string State { get; set; }

        [JsonProperty(PropertyName = "code")]
        public HonestMarkProcessResultStatus Code { get; set; }

        [JsonProperty(PropertyName = "description")]
        public string Description { get; set; }

        [JsonProperty(PropertyName = "operations")]
        public ProcessEdoOperation[] Operations { get; set; }
    }
}
