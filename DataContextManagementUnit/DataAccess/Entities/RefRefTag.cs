using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt
{
    public partial class RefRefTag
    {
        public RefRefTag()
        {
            OnCreated();
        }

        #region Properties

        public virtual decimal IdTag { get; set; }
        public virtual decimal IdObject { get; set; }
        public virtual string TagValue { get; set; }

        #endregion

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }
}
