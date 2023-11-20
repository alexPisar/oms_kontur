using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt
{
    public partial class RefAuthoritySignDocuments
    {
        public RefAuthoritySignDocuments()
        {
            OnCreated();
        }

        #region Properties

        public virtual decimal IdCustomer { get; set; }
        public virtual string Surname { get; set; }
        public virtual string Name { get; set; }
        public virtual string PatronymicSurname { get; set; }
        public virtual string Position { get; set; }
        public virtual string Inn { get; set; }
        public virtual string DataBaseUserName { get; set; }
        public virtual string Comment { get; set; }
        public virtual string EmchdId { get; set; }
        public virtual global::System.DateTime? EmchdBeginDate { get; set; }
        public virtual global::System.DateTime? EmchdEndDate { get; set; }

        #endregion

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }
}
