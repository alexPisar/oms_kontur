using System;
using DataContextManagementUnit.DataAccess.Contexts.Edi;
using EdiProcessingUnit.Edi.Model;
using EdiProcessingUnit.Infrastructure;

namespace EdiProcessingUnit.ProcessorUnits
{
    public class ExecuteEdiProceduresProcessor : EdiProcessor
    {
        public ExecuteEdiProceduresProcessor()
        {
            ProcessorName = "ExecuteEdiProceduresProcessor";
        }

        public override void Run()
        {
            _ediDbContext.ExecuteProcedure("EDI.TRANSFER_GOOD_MAPPING_FROM_EDI");

            var currentDateTime = DateTime.Now;
            DateTime dateTimeFrom = new DateTime(
                currentDateTime.Year, currentDateTime.Month, currentDateTime.Day, 18, 0, 0);
            DateTime dateTimeTo = new DateTime(
                currentDateTime.Year, currentDateTime.Month, currentDateTime.Day, 19, 0, 0);

            if (currentDateTime > dateTimeFrom && currentDateTime < dateTimeTo)
            {
                _log.Log($"Выполнение метода EveryDayRun.");
                EveryDayRun();
                _log.Log($"Метод EveryDayRun выполнен успешно.");
            }
        }

        private void EveryDayRun()
        {
            _ediDbContext.ExecuteProcedure("EDI.UPDATE_DISABLED_GOODS_MAPPINGS");
        }
    }
}
