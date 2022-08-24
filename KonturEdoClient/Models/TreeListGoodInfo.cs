using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataContextManagementUnit.DataAccess.Contexts.Abt;

namespace KonturEdoClient.Models
{
    public class TreeListGoodInfo
    {
        public decimal IdDoc { get; set; }
        public decimal IdGood { get; set; }
        public int? Quantity { get; set; } = null;
        public string Name { get; set; }
        public bool NotAllDocumentsMarked { get; set; }
        public bool NotMarked { get; set; }
        public DateTime? InsertDateTime { get; set; } = null;
        public bool IsMarkedCode { get; set; }
    }
}
