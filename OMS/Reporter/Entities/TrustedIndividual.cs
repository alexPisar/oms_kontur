using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reporter.Entities
{
    public class TrustedIndividual : Base.IReportEntity<TrustedIndividual>
    {
        /// <summary>
        /// Основание, по которому физическому лицу доверено принятие товаров (груза)
        /// </summary>
        public string ReasonOfTrust { get; set; }

        /// <summary>
        /// Основание, по которому физическому лицу доверено принятие товаров
        /// </summary>
        public DocumentDetails ReasonOfTrustDocument { get; set; }

        /// <summary>
        /// Иные сведения, идентифицирующие физическое лицо
        /// </summary>
        public string OtherInfo { get; set; }

        /// <summary>
        /// ИНН физического лица, в том числе индивидуального предпринимателя, которому доверен прием
        /// </summary>
        public string PersonInn { get; set; }

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
