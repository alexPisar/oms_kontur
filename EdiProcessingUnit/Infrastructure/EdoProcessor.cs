using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataContextManagementUnit.DataAccess.Contexts.Abt;
using UtilitesLibrary.ConfigSet;
using UtilitesLibrary.Logger;

namespace EdiProcessingUnit.Infrastructure
{
    abstract public class EdoProcessor
    {
        internal Edo.Edo _edo;
        internal UtilityLog _log = UtilityLog.GetInstance();
        internal Config _conf = Config.GetInstance();

        public virtual string ProcessorName { get; }

        public EdoProcessor Init(Edo.Edo edo)
        {
            _edo = edo;
            return this;
        }

        public EdoProcessor Init()
        {
            _edo = Edo.Edo.GetInstance();
            return this;
        }

        abstract public void Run();
    }
}
