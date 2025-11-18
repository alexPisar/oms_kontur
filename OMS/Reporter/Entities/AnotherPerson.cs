using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reporter.Entities
{
    public class AnotherPerson : Base.IReportEntity<AnotherPerson>
    {
        /// <summary>
        /// Представитель организации, или физическое лицо, которому доверено принятие товаров (груза)
        /// </summary>
        public object Item { get; set; }
    }
}
