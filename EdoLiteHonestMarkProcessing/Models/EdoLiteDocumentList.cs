using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EdoLiteHonestMarkProcessing.Models
{
    public class EdoLiteDocumentList
    {
        [JsonProperty(PropertyName = "count")]
        public int Count { get; set; }

        [JsonProperty(PropertyName = "items")]
        public EdoLiteDocuments[] Items { get; set; }

        [JsonProperty(PropertyName = "has_next_page")]
        public bool HasNextPage { get; set; }
    }
}
