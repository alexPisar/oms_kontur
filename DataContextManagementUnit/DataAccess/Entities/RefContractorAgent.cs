using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt
{
    public partial class RefContractorAgent
    {
        public RefContractorAgent()
        {
            OnCreated();
        }

        #region Properties
        public virtual decimal? IdContractor { get; set; }

        public virtual decimal? IdManufacturer { get; set; }

        public virtual decimal? IdAgent { get; set; }

        public virtual DateTime? StartDate { get; set; }

        public virtual DateTime? EndDate { get; set; }

        public virtual string SetUser { get; set; }

        public virtual DateTime SetDateTime { get; set; }
        #endregion

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }
}
