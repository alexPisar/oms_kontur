using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EdiProcessingUnit.Infrastructure;
using UtilitesLibrary.Logger;

namespace EdoLiteHonestMarkProcessing
{
    public class Program
    {
        private static UtilityLog _utilityLog = UtilityLog.GetInstance();
        private static UtilitesLibrary.ConfigSet.Config _config = UtilitesLibrary.ConfigSet.Config.GetInstance();

        static void Main(string[] args)
        {
            Console.Title = $"EDO LITE DOCUMENTS PROCESSING UNIT";

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
                new Processors.EdoLiteProcessor().Run();
            }
            finally
            {
                ts = DateTime.Now.Subtract(startStamp);
                MailReporter.Add($"Обработка длилась {ts.Milliseconds} мс");
                MailReporter.Send();
            }
        }
    }
}
