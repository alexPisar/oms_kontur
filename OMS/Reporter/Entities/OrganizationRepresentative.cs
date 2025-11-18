using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reporter.Entities
{
    public class OrganizationRepresentative : Base.IReportEntity<OrganizationRepresentative>
    {
        /// <summary>
        /// Должность
        /// </summary>
        public string Position { get; set; }

        /// <summary>
        /// Иные сведения, идентифицирующие физическое лицо
        /// </summary>
        public string OtherInfo { get; set; }

        /// <summary>
        /// Наименование организации
        /// </summary>
        public string OrgName { get; set; }

        /// <summary>
        /// ИНН юридического лица, которому доверен прием
        /// </summary>
        public string OrgInn { get; set; }

        /// <summary>
        /// Основание, по которому организации доверено принятие товаров (груза) 
        /// </summary>
        public string ReasonOrgTrust { get; set; }

        /// <summary>
        /// Основание, по которому организации доверено принятие товаров
        /// </summary>
        public DocumentDetails ReasonOrgTrustDocument { get; set; }

        /// <summary>
        /// Основание полномочий представителя организации на принятие товаров (груза)
        /// </summary>
        public string ReasonTrustPerson { get; set; }

        /// <summary>
        /// Основание полномочий представителя организации на принятие товаров
        /// </summary>
        public DocumentDetails ReasonTrustPersonDocument { get; set; }

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
