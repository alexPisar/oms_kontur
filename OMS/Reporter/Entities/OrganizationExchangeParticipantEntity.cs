using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reporter.Entities
{
    public class OrganizationExchangeParticipantEntity : Base.IReportEntity<OrganizationExchangeParticipantEntity>
    {
        /// <summary>
        /// ИНН организации
        /// </summary>
        public string JuridicalInn { get; set; }

        /// <summary>
        /// КПП организации
        /// </summary>
        public string JuridicalKpp { get; set; }

        /// <summary>
        /// Полное наименование организации
        /// </summary>
        public string OrgName { get; set; }
    }
}
