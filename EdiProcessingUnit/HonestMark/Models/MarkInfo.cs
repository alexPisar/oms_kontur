using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EdiProcessingUnit.HonestMark.Models
{
    public class MarkInfo
    {
        [JsonProperty(PropertyName = "requestedCis")]
        public string RequestedCis { get; set; }

        [JsonProperty(PropertyName = "cis")]
        public string Cis { get; set; }

        [JsonProperty(PropertyName = "gtin")]
        public string Gtin { get; set; }

        [JsonProperty(PropertyName = "productName")]
        public string ProductName { get; set; }

        [JsonProperty(PropertyName = "productGroupId")]
        public int? ProductGroupId { get; set; }

        [JsonProperty(PropertyName = "productGroup")]
        public string ProductGroup { get; set; }

        [JsonProperty(PropertyName = "brand")]
        public string Brand { get; set; }

        [JsonProperty(PropertyName = "producedDate")]
        public DateTime ProducedDate { get; set; }

        [JsonProperty(PropertyName = "emissionDate")]
        public DateTime EmissionDate { get; set; }

        [JsonProperty(PropertyName = "emissionType")]
        public string EmissionType { get; set; }

        [JsonProperty(PropertyName = "packageType")]
        public string PackageType { get; set; }

        [JsonProperty(PropertyName = "ownerInn")]
        public string OwnerInn { get; set; }

        [JsonProperty(PropertyName = "ownerName")]
        public string OwnerName { get; set; }

        [JsonProperty(PropertyName = "status")]
        public string Status { get; set; }

        [JsonProperty(PropertyName = "statusEx")]
        public string StatusEx { get; set; }

        [JsonProperty(PropertyName = "producerInn")]
        public string ProducerInn { get; set; }

        [JsonProperty(PropertyName = "producerName")]
        public string ProducerName { get; set; }
    }
}
