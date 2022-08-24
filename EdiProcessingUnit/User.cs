using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdiProcessingUnit
{
    public class User
    {
        public string Name { get; set; }
        public string EdiGLN { get; set; }
        public string UserGLN { get; set; }
        public string SID { get; set; }
        public string Host { get; set; }

        public string FullName => Name + " (" +UserGLN+ ")";
    }
}
