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
            Children = new List<DocEdoProcessing>();
            HonestMarkStatus = 0;
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
        public virtual int DocType { get; set; }
        public virtual string IdParent { get; set; }
        public virtual int HonestMarkStatus { get; set; }
        public virtual string HonestMarkErrorMessage { get; set; }
        #endregion

        #region Navigation Properties
        public virtual DocComissionEdoProcessing ComissionDocument { get; set; }
        public virtual DocEdoProcessing Parent { get; set; }
        public virtual List<DocEdoProcessing> Children { get; set; }
        #endregion

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }
}
