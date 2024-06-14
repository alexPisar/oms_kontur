using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EdiProcessingUnit.Infrastructure;

namespace EdiProcessingUnit.ProcessorUnits
{
    public class DiadocEdoProcessor : EdoProcessor
    {
        public override string ProcessorName => "DiadocEdoProcessor";
        public string OrgInn { get; set; }
        private bool Auth()
        {
            return _edo?.Authenticate(true, null, OrgInn) ?? false;
        }

        public override void Run()
        {
            
        }
    }
}
