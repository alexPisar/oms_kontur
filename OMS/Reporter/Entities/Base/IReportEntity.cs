using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace Reporter.Entities.Base
{
    public class IReportEntity<T> : MarkupExtension where T : new()
    {
        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return new T();
        }
    }
}
