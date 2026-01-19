using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataContextManagementUnit.DataAccess.Contexts.Abt;

namespace KonturEdoClient.Models
{
    public class RefEdoCounteragentConsigneeForLoading
    {
        public RefEdoCounteragentConsignee RefEdoCounteragentConsignee { get; set; }
        public string ConsigneeName { get; set; }
        public string ConsigneeAddress { get; set; }
        public decimal? ConsigneeId => RefEdoCounteragentConsignee?.IdContractorConsignee;
        public decimal? IdCustomerSeller => RefEdoCounteragentConsignee?.IdCustomerSeller;
        public decimal? IdCustomerBuyer => RefEdoCounteragentConsignee?.IdCustomerBuyer;
        public string IdFnsBuyer => RefEdoCounteragentConsignee?.IdFnsBuyer;
        public RefContractor Consignee { get; set; }
    }
}
