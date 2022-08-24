using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt
{
    public partial class RefDistrict
    {
        public RefDistrict()
        {
            OnCreated();
        }

        #region Properties
        public virtual decimal Id { get; set; }

        public virtual string Name { get; set; }
        #endregion

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }
}
