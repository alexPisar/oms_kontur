using System.ComponentModel.DataAnnotations.Schema;

namespace EdiProcessingUnit.Edo.Models
{
    public class UniversalAccountingTransferDocumentV2 : UniversalTransferDocumentV2
    {
        [NotMapped]
        public override string GlnShipTo { get; set; }

        [NotMapped]
        public override string OrderNumber { get; set; }

        [NotMapped]
        public override decimal IdDocMaster => 0;
    }
}
