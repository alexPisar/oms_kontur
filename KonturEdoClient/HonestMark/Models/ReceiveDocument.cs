using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace KonturEdoClient.HonestMark.Models
{
    public class ReceiveDocument : IDocument
    {
        [JsonProperty(PropertyName = "release_order_number")]
        public string ReleaseOrderNumber { get; set; }

        [JsonProperty(PropertyName = "document_number")]
        public string DocumentNumber { get; set; }

        [JsonProperty(PropertyName = "document_date")]
        public string DocumentDate { get; set; }

        [JsonProperty(PropertyName = "accept_all")]
        public bool AcceptAll { get; set; }

        [JsonProperty(PropertyName = "reject_all")]
        public bool RejectAll { get; set; }

        [JsonProperty(PropertyName = "transfer_date")]
        public string TransferDate { get; set; }

        [JsonProperty(PropertyName = "acceptance_date")]
        public string AcceptanceDate { get; set; }

        [JsonProperty(PropertyName = "trade_recipient_inn")]
        public string TradeReceiverInn { get; set; }

        [JsonProperty(PropertyName = "trade_sender_inn")]
        public string TradeSenderInn { get; set; }

        [JsonProperty(PropertyName = "turnover_type")]
        public string TurnoverType { get; set; }

        [JsonProperty(PropertyName = "products")]
        public ReceiveProduct Products { get; set; }
    }
}
