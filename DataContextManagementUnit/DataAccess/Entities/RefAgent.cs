using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt
{
    public partial class RefAgent
    {
        public virtual decimal Id { get; set; }
        public virtual string Name { get; set; }
        public virtual string Description { get; set; }
    }
}
