using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt
{
    public partial class RefEdoGoodChannel
    {
        public RefEdoGoodChannel()
        {
            EdoValuesPairs = new List<RefEdoUpdValues>();
            EdoUcdValuesPairs = new List<RefEdoUcdValues>();
            OnCreated();
        }

        #region Properties
        public virtual string Id { get; set; }
        public virtual decimal IdChannel { get; set; }
        public virtual string Name { get; set; }
        public virtual string EdiGln { get; set; }
        public virtual DateTime CreateDateTime { get; set; }
        public virtual string UserName { get; set; }
        public virtual int PermittedForOtherFilials { get; set; }
        public virtual decimal IdFilial { get; set; }
        public virtual string NumberUpdId { get; set; }
        public virtual string OrderNumberUpdId { get; set; }
        public virtual string OrderDateUpdId { get; set; }
        public virtual string DetailBuyerCodeUpdId { get; set; }
        public virtual string DetailBarCodeUpdId { get; set; }
        public virtual string DocReturnNumberUcdId { get; set; }
        public virtual string DocReturnDateUcdId { get; set; }
        #endregion

        #region Navigation Properties

        public virtual List<RefEdoUpdValues> EdoValuesPairs { get; set; }
        public virtual List<RefEdoUcdValues> EdoUcdValuesPairs { get; set; }

        #endregion

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }
}
