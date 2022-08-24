using System;
using System.Collections.Generic;

namespace EdiProcessingUnit.Infrastructure
{
	public static class MailReporter
	{
		private static List<string> _list;
		private static string _timeStamp => DateTime.Now.ToShortDateString() +" "+ DateTime.Now.ToShortTimeString();
		private static string _stamp => $"[{_timeStamp}] ";

		public static void Add(string msg) => _list.Add(_stamp + msg );
		public static void Add(Exception ex)=>_list.Add( _stamp + 
			UtilitesLibrary.Logger.UtilityLog.GetInstance().GetRecursiveInnerException(ex) );
		public static void Add(Exception ex, string msg) => _list.Add( _stamp + msg + 
			UtilitesLibrary.Logger.UtilityLog.GetInstance().GetRecursiveInnerException( ex ) );
		public static void Send()
		{
			if (_list.Count <= 1 + UtilitesLibrary.ConfigSet.Config.GetInstance().EdiGlns.Length)
				return;
			string message = "";
			foreach(var msg in _list)			
				message += msg+"\r\n";
			UtilitesLibrary.Logger.UtilityLog.GetInstance().Mail( message, "KONTUR_MAIL_REPORT" );
		}
		static MailReporter()
		{
			_list = new List<string>();
		}
	}
}
