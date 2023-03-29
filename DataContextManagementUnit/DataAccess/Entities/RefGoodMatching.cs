using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt
{
    public partial class RefGoodMatching
    {
        public RefGoodMatching()
        {
            OnCreated();
        }

        #region Properties
        public virtual decimal Id { get; set; }
        public virtual decimal IdChannel { get; set; }
        public virtual string CustomerArticle { get; set; }
        public virtual decimal? IdGood { get; set; }
        public virtual int Disabled { get; set; }
        public virtual DateTime? DisabledDatetime { get; set; }
        public virtual DateTime InsertDatetime { get; set; }
        public virtual string InsertUser { get; set; }
        #endregion

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }
}
