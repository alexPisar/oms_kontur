using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EdiProcessingUnit.Infrastructure;
using UtilitesLibrary.Logger;


namespace SendEdoDocumentsProcessingUnit
{
    class Program
    {
        private static UtilityLog _utilityLog = UtilityLog.GetInstance();
        private static UtilitesLibrary.ConfigSet.Config _config = UtilitesLibrary.ConfigSet.Config.GetInstance();

        static void Main(string[] args)
        {
            Console.Title = $"SEND EDO DOCUMENTS PROCESSING UNIT";

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
                var result = (new Processors.ClientEdoProcessor().Init() as Processors.ClientEdoProcessor).TestSend().Result;
                //RunSafe(new Processors.ClientEdoProcessor());
            }
            finally
            {
                ts = DateTime.Now.Subtract(startStamp);
                MailReporter.Add($"Обработка длилась {ts.Milliseconds} мс");
                MailReporter.Send();
            }
        }

        public static void RunSafe(EdoProcessor processor)
        {
            try
            {
                _utilityLog.Log(processor.ProcessorName + ".Run()");
                processor.Init();
                processor.Run();
            }
            catch (Exception ex)
            {
                _utilityLog.Log(ex);
                MailReporter.Add(ex, Console.Title);
            }
        }
    }
}
