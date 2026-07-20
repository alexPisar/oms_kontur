using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt
{
    public partial class DocEdoPurchasing
    {
        public DocEdoPurchasing()
        {
            this.Details = new List<DocEdoPurchasingDetail>();
            this.Children = new List<DocEdoPurchasing>();
            OnCreated();
        }

        #region Properties
        public virtual string IdDocEdo { get; set; }

        public virtual string EdoProviderName { get; set; }

        public virtual int? DocStatus { get; set; }

        public virtual string Name { get; set; }

        public virtual int? IdDocType { get; set; }

        public virtual DateTime? CreateDate { get; set; }

        public virtual DateTime? ReceiveDate { get; set; }

        public virtual string TotalPrice { get; set; }

        public virtual string TotalVatAmount { get; set; }

        public virtual string SenderInn { get; set; }

        public virtual string SenderKpp { get; set; }

        public virtual string SenderName { get; set; }

        public virtual string ReceiverInn { get; set; }

        public virtual string ReceiverKpp { get; set; }

        public virtual string ReceiverName { get; set; }

        public virtual decimal? IdDocJournal { get; set; }

        public virtual string SenderEdoId { get; set; }

        public virtual string ReceiverEdoId { get; set; }

        public virtual string SenderEdoOrgName { get; set; }

        public virtual string SenderEdoOrgInn { get; set; }

        public virtual string SenderEdoOrgId { get; set; }

        public virtual string FileName { get; set; }

        public virtual string SignatureFileName { get; set; }

        public virtual string ErrorMessage { get; set; }

        public virtual string UserName { get; set; }

        public virtual string CounteragentEdoBoxId { get; set; }

        public virtual string ParentEntityId { get; set; }

        public virtual string ParentIdDocEdo { get; set; }

        public virtual string DocVersionFormat { get; set; }
        #endregion

        #region Navigation Properties
        public virtual List<DocEdoPurchasingDetail> Details { get; set; }
        public virtual DocEdoPurchasing Parent { get; set; }
        public virtual List<DocEdoPurchasing> Children { get; set; }
        #endregion

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }
}
