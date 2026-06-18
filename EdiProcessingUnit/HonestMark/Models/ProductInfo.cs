using System;
using Newtonsoft.Json;

namespace EdiProcessingUnit.HonestMark.Models
{
    public class ProductInfo
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }

        [JsonProperty(PropertyName = "gtin")]
        public string Gtin { get; set; }

        [JsonProperty(PropertyName = "brand")]
        public string Brand { get; set; }

        [JsonProperty(PropertyName = "subBrand")]
        public string Subbrand { get; set; }

        [JsonProperty(PropertyName = "privateBrand")]
        public string PrivateBrand { get; set; }

        [JsonProperty(PropertyName = "packageType")]
        public string PackageType { get; set; }

        [JsonProperty(PropertyName = "innerUnitCount")]
        public string InnerUnitCount { get; set; }

        [JsonProperty(PropertyName = "model")]
        public string Model { get; set; }

        [JsonProperty(PropertyName = "productGroupId")]
        public int productGroupId { get; set; }

        [JsonProperty(PropertyName = "productGroup")]
        public string productGroup { get; set; }

        [JsonProperty(PropertyName = "goodTurnFlag")]
        public bool GoodTurnFlag { get; set; }

        [JsonProperty(PropertyName = "goodMarkFlag")]
        public bool GoodMarkFlag { get; set; }

        [JsonProperty(PropertyName = "isKit")]
        public bool IsKit { get; set; }

        [JsonProperty(PropertyName = "isTechGtin")]
        public bool IsTechGtin { get; set; }

        [JsonProperty(PropertyName = "multiplier")]
        public int Multiplier { get; set; }

        [JsonProperty(PropertyName = "tnVedCode")]
        public string TnVedCode { get; set; }

        [JsonProperty(PropertyName = "tnVedCode10")]
        public string TnVedCode10 { get; set; }

        [JsonProperty(PropertyName = "okpd2Group")]
        public string Okpd2Group { get; set; }

        [JsonProperty(PropertyName = "okpd2Code")]
        public string Okpd2Code { get; set; }
    }
}
