using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextManagementUnit.DataAccess
{
    /// <summary>
    /// Типы взаимодействия по заказам контура
    /// </summary>
    public enum OrderTypes
    {
        None,

        /// <summary>
		/// Только ORDERS
		/// </summary>
        Orders,

        /// <summary>
		/// ORDERS - ORDRSP
		/// </summary>
        OrdersOrdrsp
    }

    /// <summary>
    /// Типы взаимодействия по отгрузке контуровских заказов
    /// </summary>
    public enum ShipmentType
    {
        /// <summary>
		/// Только приём RECADV
		/// </summary>
        None,

        /// <summary>
		/// Только DESADV - (RECADV)
		/// </summary>
        Desadv,

        /// <summary>
		/// DESADV + INVOIC - (RECADV - COINVOIC)
		/// </summary>
        DesadvInvoic,

        /// <summary>
		/// DESADV - RECADV - INVOIC - (COINVOIC)
		/// </summary>
        DesadvRecadvInvoic
    }
}
