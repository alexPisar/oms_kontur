using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebService.Models
{
    public class DocOrderInfo
    {
        public string Id { get; set; }
        public decimal IdDoc { get; set; }
        public int? Status { get; set; }
        public string GlnSender { get; set; }
        public string GlnSeller { get; set; }
        public string GlnBuyer { get; set; }
        public string GlnShipTo { get; set; }
        public DateTime? OrderDate { get; set; }
        public DateTime? ReqDeliveryDate { get; set; }
    }
}
