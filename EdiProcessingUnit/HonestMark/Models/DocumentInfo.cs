using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EdiProcessingUnit.HonestMark.Models
{
    public class DocumentInfo
    {
        [JsonProperty(PropertyName = "number")]
        public string Number { get; set; }

        [JsonProperty(PropertyName = "docDate")]
        public DateTime DocDate { get; set; }

        [JsonProperty(PropertyName = "receivedAt")]
        public DateTime ReceivedAt { get; set; }

        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }

        [JsonProperty(PropertyName = "status")]
        public DocumentProcessStatusesEnum Status { get; set; }

        [JsonProperty(PropertyName = "externalId")]
        public string ExternalId { get; set; }

        [JsonProperty(PropertyName = "senderInn")]
        public string SenderInn { get; set; }

        [JsonProperty(PropertyName = "senderName")]
        public string SenderName { get; set; }

        [JsonProperty(PropertyName = "receiverInn")]
        public string ReceiverInn { get; set; }

        [JsonProperty(PropertyName = "receiverName")]
        public string ReceiverName { get; set; }

        [JsonProperty(PropertyName = "invoiceNumber")]
        public string InvoiceNumber { get; set; }

        [JsonProperty(PropertyName = "invoiceDate")]
        public DateTime InvoiceDate { get; set; }

        [JsonProperty(PropertyName = "total")]
        public int? Total { get; set; }

        [JsonProperty(PropertyName = "vat")]
        public int? Vat { get; set; }

        [JsonProperty(PropertyName = "downloadStatus")]
        public string DownloadStatus { get; set; }

        [JsonProperty(PropertyName = "downloadDesc")]
        public string DownloadDesc { get; set; }

        [JsonProperty(PropertyName = "body")]
        public object Body { get; set; }

        [JsonProperty(PropertyName = "content")]
        public string Content { get; set; }

        [JsonProperty(PropertyName = "input")]
        public bool Input { get; set; }

        [JsonProperty(PropertyName = "pdfFile")]
        public string PdfFile { get; set; }

        [JsonProperty(PropertyName = "errors")]
        public string[] Errors { get; set; }

        [JsonProperty(PropertyName = "docErrors")]
        public string[] DocErrors { get; set; }

        [JsonProperty(PropertyName = "errorMessage")]
        public string ErrorMessage { get; set; }

        [JsonProperty(PropertyName = "atk")]
        public string Atk { get; set; }
    }
}
