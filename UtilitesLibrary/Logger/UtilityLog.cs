using System;
using System.Net;
using System.Net.Mail;
using NLog;

namespace UtilitesLibrary.Logger
{
	public class UtilityLog
	{
		private NLog.Logger _logger;

		
		public void Log(string Message)
		{
			_logger.Debug( Message);
		}
		
		public void Log(Exception Exception, string Message)
		{
			_logger.Debug( Exception, Message);
		}

		public void Log(Exception Exception)
		{
			_logger.Debug( Exception );
		}
		
		public void Error(string errorText)
		{
			_logger.Error( errorText );
		}

		public void Error(Exception Exception)
		{
			_logger.Error( Exception );
		}

		public void Mail(Exception Exception)
		{
			Mail( Exception, _mailErrorSubject );
		}

		public void Mail(string message)
		{
			Mail( message, _mailErrorSubject );
		}

		public void Mail(Exception exception, string subject)
		{
			MailAddress from = new MailAddress( _mailUserEmailAddress );
			MailAddress to = new MailAddress( _mailUserEmailAddress );
			MailMessage m = new MailMessage( from, to );
			m.Subject = subject;
			m.Body = GetRecursiveInnerException( exception );
			// DOTO: прикреплять логи к письму
			//m.Attachments.Add( new Attachment( LogManager.Configuration.FindTargetByName("") ) );
			// письмо представляет код html
			//m.IsBodyHtml = true;
			SmtpClient smtp = new SmtpClient( _mailSmtpServerAddress );
			smtp.Credentials = new NetworkCredential( _mailUserLogin, _mailUserPassword );
			smtp.EnableSsl = false;
			smtp.Send( m );
		}


		public void Mail(string message, string subject)
		{
			MailAddress from = new MailAddress( _mailUserEmailAddress );
			MailAddress to = new MailAddress( _mailUserEmailAddress );
			MailMessage m = new MailMessage( from, to );
			m.Subject = subject;
			m.Body = message;
			// DOTO: прикреплять логи к письму
			//m.Attachments.Add( new Attachment( LogManager.Configuration.FindTargetByName("") ) );
			// письмо представляет код html
			//m.IsBodyHtml = true;
			SmtpClient smtp = new SmtpClient( _mailSmtpServerAddress );
			smtp.Credentials = new NetworkCredential( _mailUserLogin, _mailUserPassword );
			smtp.EnableSsl = false;
			smtp.Send( m );
		}

		public string GetRecursiveInnerException(Exception ex)
		{
			Exception realerror = ex;
			while (realerror.InnerException != null)
				realerror = realerror.InnerException;
			return ex.Message + "\r\n" + realerror.Message + "\r\n===StackTrace\r\n" + ex.StackTrace + "\r\n===Source\r\n " + ex.Source;
		}

		[NonSerialized]
		private static volatile UtilityLog _instance;

		[NonSerialized]
		private static readonly object syncRoot = new object();


		public static UtilityLog GetInstance()
		{
			if (_instance == null)
			{
				lock (syncRoot)
				{
					if (_instance == null)
					{
						_instance = new UtilityLog();
					}
				}
			}

			return _instance;
		}

		public void ConfigureMailLogger(
			string MailSmtpServerAddress,
			string MailUserLogin,
			string MailUserPassword,
			string MailUserEmailAddress,
			string MailErrorSubject)
		{
			_mailSmtpServerAddress = MailSmtpServerAddress;
			_mailUserLogin = MailUserLogin;
			_mailUserPassword = MailUserPassword;
			_mailUserEmailAddress = MailUserEmailAddress;
			_mailErrorSubject = MailErrorSubject;
		}


		private string _mailSmtpServerAddress;
		private string _mailUserLogin;
		private string _mailUserPassword;
		private string _mailUserEmailAddress;
		private string _mailErrorSubject;

		private UtilityLog()
		{
			_logger = LogManager.GetCurrentClassLogger();
		}
	}
}
