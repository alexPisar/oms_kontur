using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace EdiProcessingUnit.HonestMark.Models
{
    public class ProcessEdoOperation
    {
        [JsonProperty(PropertyName = "operationId")]
        public string OperationId { get; set; }

        [JsonProperty(PropertyName = "operationDate")]
        public long? OperationDate { get; set; }

        [JsonProperty(PropertyName = "operationType")]
        public string OperationType { get; set; }

        [JsonProperty(PropertyName = "details")]
        public ProcessEdoOperationDetail Details { get; set; }
    }
}
