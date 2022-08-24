using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt
{
    public partial class RefCustomer
    {
        public RefCustomer()
        {
            OnCreated();
        }

        #region Properties
        public virtual decimal Id { get; set; }

        public virtual string Phones { get; set; }

        public virtual string PostalAddress { get; set; }

        public virtual string Name { get; set; }

        public virtual string Address { get; set; }

        public virtual string Inn { get; set; }

        public virtual string Kpp { get; set; }

        public virtual string Okpo { get; set; }

        public virtual string Okonh { get; set; }

        public virtual string Director { get; set; }

        public virtual string AccountAnt { get; set; }

        public virtual decimal? IdCity { get; set; }

        public virtual string Contact { get; set; }

        public virtual decimal? IdContractor { get; set; }
        #endregion

        #region Navigation Properties
        public virtual RefContractor Contractor { get; set; }
        #endregion

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }
}
