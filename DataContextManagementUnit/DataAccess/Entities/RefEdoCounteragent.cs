using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt
{
    public partial class RefEdoCounteragent
    {
        public RefEdoCounteragent()
        {
            OnCreated();
        }

        public virtual decimal IdCustomerSeller { get; set; }
        public virtual decimal IdCustomerBuyer { get; set; }
        public virtual string IdFnsBuyer { get; set; }
        public virtual int ConnectStatus { get; set; }
        public virtual DateTime InsertDatetime { get; set; }
        public virtual string InsertUser { get; set; }
        public virtual decimal? IdFilial { get; set; }
        public virtual int IsConnected { get; set; }

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }
}
