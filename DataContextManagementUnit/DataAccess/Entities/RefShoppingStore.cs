using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextManagementUnit.DataAccess.Contexts.Edi
{
    public partial class RefShoppingStore
    {
        public RefShoppingStore()
        {
            OnCreated();
        }

        #region Properties
        public virtual string MainGln { get; set; }

        public virtual string BuyerGln { get; set; }
        #endregion
        #region Navigation Properties
        public virtual ConnectedBuyers MainShoppingStore { get; set; }
        #endregion
        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }
}
