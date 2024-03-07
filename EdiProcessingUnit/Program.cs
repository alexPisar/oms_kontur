using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EdiProcessingUnit.Infrastructure;
using EdiProcessingUnit.ProcessorUnits;
using EdiProcessingUnit.WorkingUnits;
using UtilitesLibrary.Logger;

namespace EdiProcessingUnit
{
	class Program
	{
		private static EdiProcessorFactory _processorFactory = new EdiProcessorFactory();
		private static UtilityLog _utilityLog = UtilityLog.GetInstance();
		private static UtilitesLibrary.ConfigSet.Config _config = UtilitesLibrary.ConfigSet.Config.GetInstance();
        private static string _timeStamp => $"[{DateTime.Now.ToShortDateString()} {DateTime.Now.ToShortTimeString()}]";
        private static bool _applicationLaunchByUser = true;

		static void Main(string[] args)
		{
			DateTime startStamp = DateTime.Now;
			TimeSpan ts = new TimeSpan();

			Console.Title = $"{Constants.AppName} v{Constants.Version}";
			_utilityLog.Log( $"{ Constants.AppName} v{ Constants.Version} Main()" );
			if (args.Contains( "-h" ) || args.Contains( "--help" ))
			{
				return;
			}

			string xmlPath = null;
			int argc = args.Count();

			_utilityLog.ConfigureMailLogger(
				_config.MailSmtpServerAddress,
				_config.MailUserLogin,
				_config.MailUserPassword,
				_config.MailUserEmailAddress,
				_config.MailErrorSubject);
			
			if (argc > 0) // если есть аргумента, то обработаем их
			{
				if (argc % 2 != 0) // если кол-во аргументов нечётное 
					return; // значит надо выйти, 
							// т.к. у каждого параметра должно быть значение, 
							// а где-то нет параметра или значения

				xmlPath = GetParameterValue( args, "-xml", xmlPath );
			}

            if (!_applicationLaunchByUser)
            {
                try
                {
                    foreach (var gln in _config.EdiGlns)
                    {
                        MailReporter.Add($"Обработка началась для организации с GLN {gln}");

                        try
                        {
                            _processorFactory.OrganizationGln = gln;
                            if (xmlPath != null)
                            {
                                StartIncomingHandlersLocally(xmlPath);
                                return;
                            }

                            RunSafe(_processorFactory, new RelationsProcessor());
                            StartHandlers();
                        }
                        catch (Exception ex)
                        {
                            _utilityLog.Log(ex);
                            MailReporter.Add(ex, Console.Title);
                        }
                    }

                    _processorFactory.OrganizationGln = null;
                    _processorFactory.ResetAuth();

                    RunSafe(_processorFactory, new ExecuteEdiProceduresProcessor());
                    _utilityLog.Log($"{_timeStamp} - обработчик ExecuteEdiProceduresProcessor завершил работу.");
                }
                finally
                {
                    ts = DateTime.Now.Subtract(startStamp);
                    MailReporter.Add($"Обработка длилась {ts.Milliseconds} мс");
                    MailReporter.Send();
                }
            }
		}

		private static void StartIncomingHandlersLocally(string XmlPath)
		{
			var files = Directory.GetFiles( XmlPath );
			var _xmlList = new List<string>();

			foreach (string file in files)
				using (FileStream fs = new FileStream( file, FileMode.OpenOrCreate ))
				{
					using (StreamReader sr = new StreamReader( fs ))
					{
						_xmlList.Add( sr.ReadToEnd() );
					}
				}

			RunSafe( _processorFactory, new OrdersProcessor( _xmlList ) );
			//RunSafe( _processorFactory, new ReceivingAdviceProcessor( _xmlList ) );
		}

		private static void StartHandlers()
		{
            _utilityLog.Log(_timeStamp + " - Запуск обработчиков сообщений");
			RunSafe( _processorFactory, new OrdersProcessor() );
            _utilityLog.Log($"{_timeStamp} - обработчик OrdersProcessor завершил работу.");
            RunSafe( _processorFactory, new ReceivingAdviceProcessor() );
            _utilityLog.Log($"{_timeStamp} - обработчик ReceivingAdviceProcessor завершил работу.");

            RunSafe( _processorFactory, new OrderResponsesProcessor() );
            _utilityLog.Log($"{_timeStamp} - обработчик OrderResponsesProcessor завершил работу.");
            RunSafe( _processorFactory, new DespatchAdviceProcessor() );
            _utilityLog.Log($"{_timeStamp} - обработчик DespatchAdviceProcessor завершил работу.");
        }
		
		public static void RunSafe(EdiProcessorFactory processorFactory, EdiProcessor processor)
		{
			try
			{
				_utilityLog.Log( processor.ProcessorName+".Run()" );
				processorFactory.RunProcessor( processor );
			}
			catch (Exception ex)
			{
				_utilityLog.Log( ex );
				MailReporter.Add( ex, Console.Title );

			}
		}
		
		public static string GetParameterValue(string[] args, string parameter, string defaultParameterValue)
		{
			if (args.Contains( parameter ))
			{
				int indexArg = args.ToList().IndexOf( parameter ); // определим индекс параметра
				string val = args[indexArg + 1] ?? ""; // определим значение параметра
				
				if (!string.IsNullOrEmpty( val ))
					return val;
			}
			return defaultParameterValue;
		}

		public static string HelpText => @"
-h            справка
--help        справка
-xml <path>   путь до xml-документа
";

	}
}