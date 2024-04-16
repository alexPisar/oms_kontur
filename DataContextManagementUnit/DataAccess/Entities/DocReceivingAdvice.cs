using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextManagementUnit.DataAccess.Contexts.Edi
{
    public partial class DocReceivingAdvice
    {
        public DocReceivingAdvice()
        {
            OnCreated();
        }

        #region Properties

        public virtual string MessageId { get; set; }
        public virtual string IdOrder { get; set; }
        public virtual string RecadvNumber { get; set; }
        public virtual global::System.DateTime? RecadvDate { get; set; }
        public virtual long? IdDocJournal { get; set; }
        public virtual string TotalAmount { get; set; }
        public virtual string TotalVatAmount { get; set; }
        public virtual string TotalSumExcludeTax { get; set; }

        #endregion

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }
}
