using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt
{
    public partial class DocGoodsDetailsLabels
    {
        #region Properties
        public virtual decimal IdDoc { get; set; }

        public virtual decimal IdGood { get; set; }

        public virtual string DmLabel { get; set; }

        public virtual decimal? IdDocSale { get; set; }

        public virtual string SaleDmLabel { get; set; }

        public virtual DateTime InsertDateTime { get; set; }

        public virtual DateTime? SaleDateTime { get; set; }
        #endregion
    }
}
