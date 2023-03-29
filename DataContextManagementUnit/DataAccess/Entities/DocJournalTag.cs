using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt
{
    public partial class DocJournalTag
    {
        public DocJournalTag()
        {
            OnCreated();
        }

        #region Properties

        public virtual int IdTad { get; set; }
        public virtual decimal IdDoc { get; set; }
        public virtual string TagValue { get; set; }

        #endregion

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }
}
