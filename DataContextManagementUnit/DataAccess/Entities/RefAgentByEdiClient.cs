using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextManagementUnit.DataAccess.Contexts.Edi
{
    public partial class RefAgentByEdiClient
    {
        public RefAgentByEdiClient()
        {
            OnCreated();
        }

        #region Properties
        public virtual string Gln { get; set; }
        public virtual decimal IdAgent { get; set; }
        public virtual DateTime? AddedDate { get; set; }
        #endregion

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }
}
