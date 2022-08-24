using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KonturEdoClient.Models
{
    public class UpdaterModel
    {
        public string Version { get; set; }
        public bool LockUpdate { get; set; }
        public bool LoadUpdater { get; set; }
    }
}
