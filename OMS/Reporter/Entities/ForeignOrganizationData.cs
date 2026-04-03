using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reporter.Entities
{
    public class ForeignOrganizationData : Base.IReportEntity<ForeignOrganizationData>
    {
        /// <summary>
        /// Идентификация статуса
        /// </summary>
        public string StatusId { get; set; }

        /// <summary>
        /// Код страны
        /// </summary>
        public string CountryCode { get; set; }

        /// <summary>
        /// Наименование страны
        /// </summary>
        public string CountryName { get; set; }

        /// <summary>
        /// Наименование иностранной организации полное/Фамилия, имя, отчество (при наличии) иностранного гражданина
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Идентификатор иностранной организации (иностранного гражданина)
        /// </summary>
        public string IdentificationInfo { get; set; }

        /// <summary>
        /// Иные сведения для однозначной идентификации иностранной организации (иностранного гражданина)
        /// </summary>
        public string OtherInfo { get; set; }
    }
}
