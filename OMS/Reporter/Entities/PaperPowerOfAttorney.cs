using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reporter.Entities
{
    public class PaperPowerOfAttorney : Base.IReportEntity<PaperPowerOfAttorney>
    {
        /// <summary>
        /// Внутренний номер доверенности
        /// </summary>
        public string InternalNumber { get; set; }

        /// <summary>
        /// Дата совершения (выдачи) доверенности
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// Сведения, идентифицирующие доверителя
        /// </summary>
        public string IdentificationInfo { get; set; }

        /// <summary>
        /// Фамилия
        /// </summary>
        public string Surname { get; set; }

        /// <summary>
        /// Имя
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Отчество
        /// </summary>
        public string Patronymic { get; set; }
    }
}
