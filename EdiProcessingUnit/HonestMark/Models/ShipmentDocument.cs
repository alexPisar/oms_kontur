using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EdiProcessingUnit.HonestMark.Models
{
    public class ShipmentDocument : IDocument
    {
        [JsonProperty(PropertyName = "document_num")]
        public string DocumentNumber { get; set; }

        [JsonProperty(PropertyName = "document_date")]
        public string DocumentDate { get; set; }

        [JsonProperty(PropertyName = "transfer_date")]
        public string TransferDate { get; set; }

        [JsonProperty(PropertyName = "receiver_inn")]
        public string ReceiverInn { get; set; }

        [JsonProperty(PropertyName = "sender_inn")]
        public string SenderInn { get; set; }

        [JsonProperty(PropertyName = "to_not_participant")]
        public bool ToNotImportant { get; set; }

        [JsonProperty(PropertyName = "turnover_type")]
        public string TurnOverType { get; set; }

        [JsonProperty(PropertyName = "products")]
        public ShipmentProduct[] Products { get; set; }
    }
}
