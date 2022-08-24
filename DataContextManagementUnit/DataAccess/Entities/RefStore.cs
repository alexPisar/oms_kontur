using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt
{
    public partial class RefStore
    {
        public RefStore()
        {
            OnCreated();
        }

        #region Properties
        public virtual decimal Id { get; set; }
        public virtual string Name { get; set; }
        public virtual decimal? IdInstance { get; set; }
        public virtual string IsReal { get; set; }
        public virtual string IsConsignment { get; set; }
        public virtual decimal? OldId { get; set; }
        public virtual decimal? SortId { get; set; }
        public virtual string Close { get; set; }
        public virtual int? IdStoreClass { get; set; }
        #endregion

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }
}
