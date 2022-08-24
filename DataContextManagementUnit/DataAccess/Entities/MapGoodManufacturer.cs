using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextManagementUnit.DataAccess.Contexts.Edi
{
    public partial class MapGoodManufacturer
    {
        public MapGoodManufacturer()
        {
            OnCreated();
        }

        #region Properties
        public virtual decimal IdGood { get; set; }

        public virtual decimal? IdManufacturer { get; set; }

        public virtual string Name { get; set; }
        #endregion

        #region Extensibility Method Definitions
        partial void OnCreated();
        #endregion
    }
}
