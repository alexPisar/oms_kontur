using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdiProcessingUnit.Enums
{
    public enum DocEdoSendStatus
    {
        /// <summary>
        /// Отклонён
        /// </summary>
        Rejected = -1,

        /// <summary>
        /// Отправлен
        /// </summary>
        Sent,

        /// <summary>
        /// Подписан
        /// </summary>
        Signed = 2,

        ///<summary>
        /// Подписан с расхождениями
        ///</summary>
        PartialSigned
    }
}
