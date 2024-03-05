using System;
using DataContextManagementUnit.DataAccess.Contexts.Edi;
using EdiProcessingUnit.Edi.Model;
using EdiProcessingUnit.Infrastructure;

namespace EdiProcessingUnit.ProcessorUnits
{
    public class ExecuteEdiProceduresProcessor : EdiProcessor
    {
        public override void Run()
        {
            _ediDbContext.ExecuteProcedure("EDI.TRANSFER_GOOD_MAPPING_FROM_EDI");
        }
    }
}
