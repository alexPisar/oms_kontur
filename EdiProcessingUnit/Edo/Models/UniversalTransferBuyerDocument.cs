using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataContextManagementUnit.DataAccess.Contexts.Abt;

namespace EdiProcessingUnit.Edo.Models
{
    public class UniversalTransferBuyerDocument
    {
        public DocJournal DocJournal { get; set; }
        public DocEdoProcessing DocEdoProcessing { get; set; }
        public string SellerName { get; set; }
        public string SellerInn { get; set; }
        public string SellerKpp { get; set; }
        public string BuyerName { get; set; }
        public string BuyerInn { get; set; }
        public string BuyerKpp { get; set; }
        public string BuyerEdoId { get; set; }
    }
}
