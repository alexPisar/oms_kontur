using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt
{
    public partial class RefUserEdoPermissions
    {
        public RefUserEdoPermissions()
        {
            OnCreated();
        }

        #region Properties
        public virtual string UserName { get; set; }

        public virtual int WorkWithDocuments { get; set; }

        public virtual int ShowMarkedCodes { get; set; }

        public virtual int ReturnMarkedCodes { get; set; }

        public virtual int PermissionCompareGoods { get; set; }

        public virtual int PermissionChannelsList { get; set; }

        public virtual int PermissionChannelsSettings { get; set; }
        #endregion

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }
}
