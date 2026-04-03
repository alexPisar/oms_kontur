using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EdiProcessingUnit.HonestMark.Models
{
    public class IntroduceRemainsDocument
    {
        [JsonProperty(PropertyName = "trade_participant_inn")]
        public string TradeParticipantInn { get; set; }

        [JsonProperty(PropertyName = "products_list")]
        public IntroduceRemainsProduct[] Products { get; set; }
    }
}
