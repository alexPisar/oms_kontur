using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt
{
    public partial class RefEdoUcdValues
    {
        public RefEdoUcdValues()
        {
            OnCreated();
        }

        #region Properties

        public virtual string IdEdoGoodChannel { get; set; }
        public virtual string Key { get; set; }
        public virtual string Value { get; set; }
        public virtual decimal IdDocType { get; set; }

        #endregion

        #region Navigation Properties

        public virtual RefEdoGoodChannel EdoGoodChannel { get; set; }

        #endregion

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }
}
