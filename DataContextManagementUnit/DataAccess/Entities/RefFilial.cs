using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataContextManagementUnit.DataAccess.Contexts.Abt
{
    public partial class RefFilial
    {
        public RefFilial()
        {

        }

        public long Id { get; set; }

        public string Name { get; set; }

        public string Links { get; set; }

        public string Ip { get; set; }
    }
}
