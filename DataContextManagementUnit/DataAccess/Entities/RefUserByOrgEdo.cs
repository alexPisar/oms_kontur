using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt
{
    public partial class RefUserByOrgEdo
    {
        public RefUserByOrgEdo()
        {
            OnCreated();
        }

        #region Properties
        public decimal IdCustomer { get; set; }

        public string UserName { get; set; }
        #endregion

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }
}
