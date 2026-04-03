using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reporter.Entities
{
    public class ElectronicPowerOfAttorney : Base.IReportEntity<ElectronicPowerOfAttorney>
    {
        /// <summary>
        /// Единый регистрационный номер доверенности
        /// </summary>
        public string RegistrationNumber { get; set; }

        /// <summary>
        /// Дата совершения (выдачи) доверенности
        /// </summary>
        public DateTime RegistrationDate { get; set; }

        /// <summary>
        /// Идентифицирующая информация об информационной системе, в которой осуществляется хранение доверенности, необходимая для запроса информации из информационной системы
        /// </summary>
        public string SystemIdentificationInfo { get; set; }

        /// <summary>
        /// Сведения в формате URL об информационной системе, которая предоставляет техническую возможность получения информации о доверенности
        /// </summary>
        public string UrlSystem { get; set; }
    }
}
