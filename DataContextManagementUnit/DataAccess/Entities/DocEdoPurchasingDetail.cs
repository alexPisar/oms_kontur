using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt
{
    public partial class DocEdoPurchasingDetail
    {
        public DocEdoPurchasingDetail()
        {
            OnCreated();
        }

        #region Properties
        public virtual string IdDocEdoPurchasing { get; set; }

        public virtual string Description { get; set; }

        public virtual decimal? Quantity { get; set; }

        public virtual string BarCode { get; set; }

        public virtual decimal? Price { get; set; }

        public virtual decimal? TaxAmount { get; set; }

        public virtual decimal? Subtotal { get; set; }

        public virtual decimal? IdGood { get; set; }

        public virtual int DetailNumber { get; set; }

        public virtual string Gtin { get; set; }

        public virtual decimal? QuantityMark { get; set; }
        #endregion

        #region Navigation Properties
        public virtual DocEdoPurchasing EdoDocument { get; set; }
        #endregion

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }
}
