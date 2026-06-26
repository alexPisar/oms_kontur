using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EdoLiteHonestMarkProcessing.Models
{
    public class EdoLiteDocuments
    {
        [JsonProperty(PropertyName = "id")]
        public string EdoId { get; set; }

        [JsonProperty(PropertyName = "created_at")]
        public int CreatedAtTimeStamp { get; set; }

        [JsonProperty(PropertyName = "date")]
        public int DateTimeStamp { get; set; }

        [JsonProperty(PropertyName = "documents")]
        public EdoLiteDocument[] Documents { get; set; }

        [JsonProperty(PropertyName = "group_id")]
        public string GroupId { get; set; }

        [JsonProperty(PropertyName = "number")]
        public string Number { get; set; }

        [JsonProperty(PropertyName = "sender")]
        public EdoLiteSender Sender { get; set; }

        [JsonProperty(PropertyName = "recipient")]
        public EdoLiteRecipient Recipient { get; set; }

        [JsonProperty(PropertyName = "status")]
        public int Status { get; set; }

        [JsonProperty(PropertyName = "total_price")]
        public string TotalPrice { get; set; }

        [JsonProperty(PropertyName = "total_vat_amount")]
        public string TotalVatAmount { get; set; }

        [JsonProperty(PropertyName = "type")]
        public int DocType { get; set; }

        [JsonProperty(PropertyName = "create_time_stamp")]
        public int CreateTimeStamp { get; set; }

        [JsonProperty(PropertyName = "export_time_stamp")]
        public int ExportTimeStamp { get; set; }

        [JsonProperty(PropertyName = "count")]
        public int Count { get; set; }

        [JsonIgnore]
        public DateTime CreatedAt
        {
            get
            {
                return new UtilitesLibrary.Service.DateTimeUtil().GetDateTimeByTimestamp(CreatedAtTimeStamp);
            }
        }

        [JsonIgnore]
        public DateTime Date
        {
            get
            {
                return new UtilitesLibrary.Service.DateTimeUtil().GetDateTimeByTimestamp(DateTimeStamp);
            }
        }

        [JsonIgnore]
        public DateTime CreateDateTime
        {
            get
            {
                return new UtilitesLibrary.Service.DateTimeUtil().GetDateTimeByTimestamp(CreateTimeStamp);
            }
        }

        [JsonIgnore]
        public DateTime ExportDateTime
        {
            get
            {
                return new UtilitesLibrary.Service.DateTimeUtil().GetDateTimeByTimestamp(ExportTimeStamp);
            }
        }
    }
}
