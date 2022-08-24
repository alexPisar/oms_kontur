using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt
{
    public partial class DocEdoProcessing
    {
        public DocEdoProcessing()
        {
            OnCreated();
        }

        #region Properties
        public virtual string Id { get; set; }
        public virtual string MessageId { get; set; }
        public virtual string EntityId { get; set; }
        public virtual string FileName { get; set; }
        public virtual int IsReprocessingStatus { get; set; }
        public virtual string IdComissionDocument { get; set; }
        public virtual int AnnulmentStatus { get; set; }
        public virtual string AnnulmentFileName { get; set; }
        public virtual decimal? IdDoc { get; set; }
        public virtual DateTime DocDate { get; set; }
        public virtual string UserName { get; set; }
        public virtual string ReceiverName { get; set; }
        public virtual string ReceiverInn { get; set; }
        public virtual int DocStatus { get; set; }
        #endregion

        #region Navigation Properties
        public virtual DocComissionEdoProcessing ComissionDocument { get; set; }
        #endregion

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }
}
