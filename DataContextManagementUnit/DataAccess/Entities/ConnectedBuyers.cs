using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextManagementUnit.DataAccess.Contexts.Edi
{
    public partial class ConnectedBuyers
    {
        public ConnectedBuyers()
        {
            OnCreated();
        }

        #region Properties
        public string Gln { get; set; }

        public int? OrderExchangeType { get; set; }

        public int? ShipmentExchangeType { get; set; }

        public int? MultiDesadv { get; set; }

        public int? PriceIncludingVat { get; set; }

        public int? DocStatusSendDesadv { get; set; }

        public int? SendTomorrow { get; set; }

        public int? PermittedToMatchingGoods { get; set; }

        public int? ExportOrdersByManufacturers { get; set; }

        public int? IncludedBuyerCodes { get; set; }

        public int? UseSplitDocProcedure { get; set; }
        #endregion

        #region Navigation Properties
        public virtual List<RefShoppingStore> ShoppingStores { get; set; }
        #endregion

        #region Extensibility Method Definitions

        partial void OnCreated();

        #endregion
    }
}
