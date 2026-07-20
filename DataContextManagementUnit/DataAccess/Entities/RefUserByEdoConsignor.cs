using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt
{
    public partial class RefUserByEdoConsignor
    {
        public RefUserByEdoConsignor()
        {
            OnCreated();
        }

        #region Properties
        public virtual decimal IdCustomerConsignor { get; set; }

        public virtual string UserName { get; set; }
        #endregion

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }
}
