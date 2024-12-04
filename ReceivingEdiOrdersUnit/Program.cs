using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EdiProcessingUnit;
using EdiProcessingUnit.Infrastructure;
using EdiProcessingUnit.ProcessorUnits;
using UtilitesLibrary.Logger;

namespace ReceivingEdiOrdersUnit
{
    class Program
    {
        private static EdiProcessorFactory _processorFactory = new EdiProcessorFactory();
        private static UtilityLog _utilityLog = UtilityLog.GetInstance();
        private static UtilitesLibrary.ConfigSet.Config _config = UtilitesLibrary.ConfigSet.Config.GetInstance();
        private static string _timeStamp => $"[{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}]";
        static void Main(string[] args)
        {
            Console.Title = $"RECEIVING EDI ORDERS UNIT";

            _utilityLog.ConfigureMailLogger(
                _config.MailSmtpServerAddress,
                _config.MailUserLogin,
                _config.MailUserPassword,
                _config.MailUserEmailAddress,
                _config.MailErrorSubject);

            DateTime startStamp = DateTime.Now;
            TimeSpan ts = new TimeSpan();

            try
            {
                foreach (var gln in _config.EdiGlns)
                {
                    MailReporter.Add($"Приём заказов начался для организации с GLN {gln}");

                    try
                    {
                        _processorFactory.OrganizationGln = gln;

                        RunSafe(_processorFactory, new EdiProcessingUnit.WorkingUnits.RelationsProcessor());
                        ExecudeProcessings();
                    }
                    catch (Exception ex)
                    {
                        _utilityLog.Log(ex);
                        MailReporter.Add(ex, Console.Title);
                    }
                }
            }
            finally
            {
                ts = DateTime.Now.Subtract(startStamp);
                MailReporter.Add($"Обработка длилась {ts.Milliseconds} мс");
                MailReporter.Send();
            }
        }

        private static void ExecudeProcessings()
        {
            _utilityLog.Log(_timeStamp + " - Запуск обработчика приёма заказов");

            var ordersProcessor = new EdiProcessingUnit.WorkingUnits.OrdersProcessor();
            RunSafe(_processorFactory, ordersProcessor);
            _utilityLog.Log($"{_timeStamp} - обработчик OrdersProcessor завершил работу.");

            if (!ordersProcessor.WithErrors)
            {
                _utilityLog.Log($"{_timeStamp} - Сохранение LastEventId.");
                _processorFactory.SaveEdiLastEventId();
            }
        }

        public static void RunSafe(EdiProcessorFactory processorFactory, EdiProcessor processor)
        {
            try
            {
                _utilityLog.Log(processor?.GetType()?.Name + ".Run()");
                processorFactory.RunProcessor(processor);
            }
            catch (Exception ex)
            {
                _utilityLog.Log(ex);
                MailReporter.Add(ex, Console.Title);

            }
        }
    }
}