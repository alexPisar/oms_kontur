using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextManagementUnit.DataAccess.Contexts.Edi
{
    public partial class MapGoodByBuyer
    {
        public MapGoodByBuyer()
        {
            OnCreated();
        }

        #region Properties
        public virtual string Gln { get; set; }

        public virtual string IdMapGood { get; set; }
        #endregion

        #region Navigation Properties
        public virtual MapGood MapGood { get; set; }
        #endregion

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }
}
