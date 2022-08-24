using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt
{
    public partial class DocComissionEdoProcessing
    {
        public DocComissionEdoProcessing()
        {
            MainDocuments = new List<DocEdoProcessing>();
            OnCreated();
        }

        #region Properties

        public virtual string Id { get; set; }

        public virtual decimal? IdDoc { get; set; }

        public virtual string MessageId { get; set; }

        public virtual string EntityId { get; set; }

        public virtual int DocStatus { get; set; }

        public virtual string ErrorMessage { get; set; }

        public virtual string FileName { get; set; }

        public virtual string UserName { get; set; }

        public virtual string SenderInn { get; set; }

        public virtual string ReceiverInn { get; set; }

        public virtual DateTime DocDate { get; set; }

        public virtual DateTime? DeliveryDate { get; set; }

        public virtual int NumberOfReturnDocuments { get; set; }

        public virtual int AnnulmentStatus { get; set; }

        public virtual string AnnulmentFileName { get; set; }

        public virtual int IsMainDocumentError { get; set; }

        #endregion

        #region Navigation Properties
        public virtual List<DocEdoProcessing> MainDocuments { get; set; }
        #endregion

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }
}
