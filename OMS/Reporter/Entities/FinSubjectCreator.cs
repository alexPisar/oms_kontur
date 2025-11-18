using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reporter.Entities
{
    public class FinSubjectCreator : Base.IReportEntity<FinSubjectCreator>
    {
        /// <summary>
        /// ИНН юридического лица
        /// </summary>
        public string JuridicalEntityInn { get; set; }

        /// <summary>
        /// ИНН физического лица
        /// </summary>
        public string PersonInn { get; set; }

        /// <summary>
        /// Краткое наименование органа исполнительной власти (специализированной уполномоченной организации), выдавшего документ
        /// </summary>
        public string ExecutiveAuthorityOrganization { get; set; }

        /// <summary>
        /// Данные об иностранной организации (иностранном гражданине), не состоящей на учете в налоговых органах
        /// </summary>
        public ForeignOrganizationData ForeignOrganizationData { get; set; }
    }
}
