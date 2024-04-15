using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using EdiProcessingUnit.Infrastructure;
using SkbKontur.EdiApi.Client.Http.Internal;
using SkbKontur.EdiApi.Client.Http.Messages;
using SkbKontur.EdiApi.Client.Types.Boxes;
using SkbKontur.EdiApi.Client.Types.Common;
using SkbKontur.EdiApi.Client.Types.Messages;
using SkbKontur.EdiApi.Client.Types.Messages.BoxEvents;
using SkbKontur.EdiApi.Client.Types.Messages.BoxEventsContents.Inbox;
using SkbKontur.EdiApi.Client.Types.Messages.BoxEventsContents.Outbox;
using SkbKontur.EdiApi.Client.Types.Organization;
using SkbKontur.EdiApi.Client.Types.Parties;
using UtilitesLibrary.Logger;

namespace EdiProcessingUnit.Edi
{
	public sealed class Edi
	{
		private UtilityLog _log = UtilityLog.GetInstance();
		private EdiTokenCache _cache { get; set; }
		private IWebProxy _webProxy { get; set; }
		private readonly UtilitesLibrary.ConfigSet.Config _config = UtilitesLibrary.ConfigSet.Config.GetInstance();
		private string _authToken => _cache.Token ?? "";
		private string _partyId => _cache.PartyId ?? "";
		private string _actualBoxId;
        private string _lastEventId = null;
        private List<MessageBoxEvent> _messageBoxEvents = new List<MessageBoxEvent>();

		private MessagesEdiApiHttpClient _apiMessages { get; set; }
        //private InternalEdiApiHttpClient _apiInternal { get; set; }

        public string CurrentOrgGln => _cache?.Gln;
        public List<MessageBoxEvent> MessageBoxEvents => _messageBoxEvents ?? new List<MessageBoxEvent>();

        public List<MessageBoxEvent> GetNewEvents()
		{
			MessageBoxEventBatch eventsBatch;

            if(string.IsNullOrEmpty(_lastEventId))
                _lastEventId = _cache.LastEventId;

			bool EmptyIncomingEvents = false;
			
			while (!EmptyIncomingEvents)
			{
				if (string.IsNullOrEmpty(_lastEventId))
					eventsBatch = CallApiSafe( new Func<MessageBoxEventBatch>( () =>
						_apiMessages.GetEvents( _authToken, _actualBoxId, null ) ) );
				else
					eventsBatch = CallApiSafe( new Func<MessageBoxEventBatch>( () =>
						_apiMessages.GetEvents( _authToken, _actualBoxId, _lastEventId) ) );

                _lastEventId = eventsBatch.LastEventId;
				_messageBoxEvents.AddRange( eventsBatch.Events );
				EmptyIncomingEvents = eventsBatch.Events.Count() <= 0;
			}

			return _messageBoxEvents;
		}
		
		public List<MessageBoxEvent> GetNewEventsFromDate(DateTime FromDateTime)
		{
			MessageBoxEventBatch eventsBatch;

            if (string.IsNullOrEmpty(_lastEventId))
                _lastEventId = _cache.LastEventId;

            eventsBatch = CallApiSafe( new Func<MessageBoxEventBatch>( () =>
				_apiMessages.GetEvents( _authToken, _actualBoxId, FromDateTime ) ) );
			_messageBoxEvents.AddRange( eventsBatch.Events );		

            if(eventsBatch.Events.Count() > 0)
                _lastEventId = eventsBatch.LastEventId;

            return _messageBoxEvents;
		}

        public void SaveLastEventId()
        {
            if (!string.IsNullOrEmpty(_lastEventId))
            {
                _cache.LastEventId = _lastEventId;
                _cache.Save(_cache, _cache.Gln);
            }
        }

        public void ResetParameters()
        {
            _cache = null;
            _lastEventId = null;
            _messageBoxEvents = new List<MessageBoxEvent>();
        }

		/// <summary>
		/// Отсылает сообщение по API
		/// </summary>
		/// <param name="EdiMessageBytes">отсылаемое сообщение</param>
		/// <returns>MessageId сообщения, которое было отправлено</returns>
		public OutboxMessageMeta SendMessage(byte[] EdiMessageBytes)
		{
			MessageData msgData = new MessageData();
			msgData.MessageBody = EdiMessageBytes;

			OutboxMessageMeta response = CallApiSafe( new Func<OutboxMessageMeta>( () =>
				_apiMessages.SendMessage( _authToken, _actualBoxId, msgData ) ) );
			
			return response;
		}

		public bool CheckMessageErrors(OutboxMessageMeta response, string addInInformation = "", string addInInformation2 = "")
		{
			MessageBoxEvent MessageUndelivered = null;
			MessageBoxEvent MessageDelivered = null;
			List <MessageBoxEvent> messageBoxEvents = new List<MessageBoxEvent>();
			
			messageBoxEvents = this.GetNewEvents();				

			// часть удачной попытки отправки документа

			List<MessageBoxEvent> MessagesDelivered = messageBoxEvents
				.Where( x => x.EventType == MessageBoxEventType.MessageDelivered &&						
							((MessageDeliveredEventContent)x.EventContent).OutboxMessageMeta.DocumentCirculationId == response.DocumentCirculationId
					)
				.ToList();
			
			if (MessagesDelivered.Count == 1)
				MessageDelivered = MessagesDelivered.First();

			if (MessageDelivered != null)
			{
				// показать ошибки на экране?
				return true;
			}


			// часть неудачной попытки отправки документа

			List<MessageBoxEvent> MessagesUndelivered = messageBoxEvents
				.Where( x => x.EventType == MessageBoxEventType.MessageUndelivered &&
						((MessageUndeliveredEventContent)x.EventContent).OutboxMessageMeta.DocumentCirculationId == response.DocumentCirculationId
					)
				.ToList();


			if (MessagesUndelivered.Count == 1)
				MessageUndelivered = MessagesUndelivered.First();
			
			if (MessageUndelivered != null)
			{
				MessageUndeliveredEventContent evnt = (MessageUndeliveredEventContent)MessageUndelivered?.EventContent;

				string errMsg = $"BoxId={evnt.OutboxMessageMeta.BoxId}\r\n" +
					$"DocumentCirculationId={evnt.OutboxMessageMeta.DocumentCirculationId}\r\n" +
					$"MessageId={evnt.OutboxMessageMeta.MessageId}\r\n" + 
					$"PartyId={MessageUndelivered.PartyId}\r\n" +
					$"EventId={MessageUndelivered.EventId}\r\n" +
					$"EventPointer={MessageUndelivered.EventPointer}\r\n" +
					$"EventDateTime={MessageUndelivered.EventDateTime.ToString( "yyyy-MM-ddTHH:mm:ssZ" )}\r\n" +
					$"EventType={MessageUndelivered.EventType}\r\n";
				
				foreach (string msg in evnt.MessageUndeliveryReasons)
				{
					errMsg += "\r\n" + msg;
				}
				// отправить в почту ошибки
				MailReporter.Add( addInInformation+ "\r\n\r\n" + errMsg+ "\r\n\r\n" + addInInformation2);

				// показать ошибки на экране?
				return false;
			}

			return true;

		}
		


		public MessageData GetBoxMessage(string MessageId)
		{
			MessageData message = CallApiSafe( new Func<MessageData>( () =>
				_apiMessages.GetMessage( _authToken, _actualBoxId, MessageId ) ) );
			return message;
		}

		public MessageData GetBoxMessage(string MessageId, string BoxId)
		{
			MessageData message = CallApiSafe( new Func<MessageData>( () =>
				_apiMessages.GetMessage( _authToken, BoxId, MessageId ) ) );
			return message;
		}

		public MessageData NewInboxMessageEventHandler(object content)
		{
			NewInboxMessageEventContent eventContent = (NewInboxMessageEventContent)content;
			string BoxId = eventContent.InboxMessageMeta.BoxId;
			string MessageId = eventContent.InboxMessageMeta.MessageId;
			MessageData message = CallApiSafe( new Func<MessageData>( () =>
				_apiMessages.GetMessage( _authToken, _actualBoxId, MessageId ) ) );

			InboxMessageMeta messageMeta = CallApiSafe( new Func<InboxMessageMeta>( () =>
				_apiMessages.GetInboxMessageMeta( _authToken, _actualBoxId, MessageId ) ) );

			;

			return message;
		}

		public MessageData NewOutboxMessageEventHandler(object content)
		{
			NewOutboxMessageEventContent eventContent = (NewOutboxMessageEventContent)content;
			string BoxId = eventContent.OutboxMessageMeta.BoxId;
			string MessageId = eventContent.OutboxMessageMeta.MessageId;
			MessageData message = CallApiSafe( new Func<MessageData>( () =>
				_apiMessages.GetMessage( _authToken, _actualBoxId, MessageId ) ) );

			return message;
		}

		public OrganizationCatalogueInfo GetOrganizationCatalogueInfo(string PartyId)
		{
			OrganizationCatalogueInfo OrganizationCatalogueInfo = CallApiSafe( new Func<OrganizationCatalogueInfo>( () =>
				_apiMessages.GetOrganizationCatalogueInfo( _authToken, PartyId ) ) );			

			return OrganizationCatalogueInfo;
		}

		public List<DocumentsSettingsForPartner> GetParties()
		{
			var BoxDocumentsSettings = CallApiSafe( new Func<BoxDocumentsSettings>(
					() => _apiMessages.GetBoxDocumentsSettings( _authToken, _actualBoxId )
				) );

			if (BoxDocumentsSettings.DocumentsSettingsForPartner.Count() <= 0)
				return null;

			//foreach (var partner in BoxDocumentsSettings.DocumentsSettingsForPartner)
			//{
			//	var partnerId = partner.Partner.PartnerId;
			//	var OrganizationCatalogueInfo = CallApiSafe( new Func<OrganizationCatalogueInfo>(
			//		() => _apiMessages.GetOrganizationCatalogueInfo( _authToken, partnerId )
			//	) );

			//	if(OrganizationCatalogueInfo.DeliveryPoints.Count() > 0)
			//	{
			//		foreach (var point in OrganizationCatalogueInfo.DeliveryPoints)
			//		{

			//		}
			//	}
				
			//}
			
			return BoxDocumentsSettings.DocumentsSettingsForPartner.ToList();
		}

		public OrganizationCatalogueInfo GetParties2()
		{
			OrganizationCatalogueInfo parties = CallApiSafe( new Func<OrganizationCatalogueInfo>( () =>
				_apiMessages.GetOrganizationCatalogueInfo( _authToken, _partyId )) );
			return parties;
		}

		private void SetBoxId()
		{
			if (string.IsNullOrEmpty( _actualBoxId ) || _actualBoxId != _cache?.BoxId)
			{
				string BoxId = null;

                if (!string.IsNullOrEmpty(_cache?.BoxId))
                {
                    _actualBoxId = _cache?.BoxId;
                    return;
                }

				if (BoxId == null)
				{
					BoxInfo boxInfo = CallApiSafe( new Func<BoxInfo>( () => _apiMessages.GetMainApiBox( _authToken, _partyId ) ) );
                    _cache.BoxId = boxInfo.Id;
                    _cache.PartyId = boxInfo.PartyId;
                    _cache.Save(_cache, _cache.Gln);
				}

				_actualBoxId = BoxId;
			
			}
		}

		/// <summary>
		/// Получить токен аутентификации
		/// </summary>
		public bool Authenticate(string orgGln)
		{
            if (string.IsNullOrEmpty(orgGln))
                orgGln = _cache?.Gln;

            _cache = new EdiTokenCache().Load(orgGln);
            BoxInfo box;

			if (_cache != null && !IsTokenExpired)
			{
				SetBoxId();
				return true;
			}

			if (_cache == null || IsTokenExpired)
			{
				var conf = UtilitesLibrary.ConfigSet.Config.GetInstance();
				string authToken = (string)CallApiSafe( new Func<object>( () => _apiMessages.Authenticate( conf.EdiUserName, conf.EdiUserPassword ) ) );

				if (string.IsNullOrEmpty( authToken ))
					return false;

                var lastEventId = _cache?.LastEventId;

                if (string.IsNullOrEmpty( _cache?.PartyId ?? "" ))
				{
					box = CallApiSafe( new Func<BoxesInfo>( () => _apiMessages.GetBoxesInfo( authToken ) ) ).Boxes.FirstOrDefault(b => b.Gln == orgGln && !b.IsTest);
					_cache = new EdiTokenCache( authToken, conf.EdiUserName, box.PartyId, box.Id, lastEventId, orgGln);
				}

				_cache = new EdiTokenCache( authToken, conf.EdiUserName, _cache.PartyId, _cache.BoxId, lastEventId, orgGln);
                _actualBoxId = _cache.BoxId;
                _cache.Save( _cache, orgGln);
				SetBoxId();
				return true;
			}
			return false;
		}

		/// <summary>
		/// Истёк ли токен аутентификации
		/// </summary>
		public bool IsTokenExpired => _cache?.TokenExpirationDate < DateTime.Now;
		
		
		private static volatile Edi _instance;
		
		private static readonly object syncRoot = new object();

		private Edi()
		{
			_webProxy = null;

			// если edi хочет ходить через прокси - пусть будет так
			if (_config.ProxyEnabled)
			{
				_webProxy = new WebProxy( _config.ProxyAddress, _config.ProxyEnabled );
				_webProxy.Credentials = new NetworkCredential(
					_config.ProxyUserName,
					_config.ProxyUserPassword
				);				
			}

			//_apiInternal = new InternalEdiApiHttpClient(
			//	_config.EdiApiClientId,
			//	new Uri( _config.EdiApiUrl ),
			//	_config.EdiHttpTimeout,
			//	_webProxy
			//);

			_apiMessages = new MessagesEdiApiHttpClient(
				_config.EdiApiClientId,
				new Uri( _config.EdiApiUrl ),
				_config.EdiHttpTimeout,
				_webProxy
			);

            ServicePointManager.ServerCertificateValidationCallback = delegate (
                object s,
                System.Security.Cryptography.X509Certificates.X509Certificate certificate,
                System.Security.Cryptography.X509Certificates.X509Chain chain,
                System.Net.Security.SslPolicyErrors sslPolicyErrors
            ) {
                return true;
            };

            _log.Log( $"MessagesEdiApiHttpClient .ctor");

		}

		public static Edi GetInstance()
		{
			if (_instance == null)
			{
				lock (syncRoot)
				{
					if (_instance == null)
					{
						_instance = new Edi();
					}
				}
			}

			return _instance;
		}

		private TOut CallApiSafe<TOut>(Func<TOut> CallingDelegate) where TOut : new()
		{
			_log.Log( "SafeCall: " + CallingDelegate.Method.Name );

			TOut ret;
			int tries = 15;
			Exception ex = null;

			while (tries-- >= 0)
			{
				try
				{
					ret = CallingDelegate.Invoke();
					return ret;
				}
				catch (Exception e)
				{
					_log.Error( e );
					ex = e;
				}
			}

			if (ex != null)
				throw ex;

			throw new Exception( "метод не получилось вызвать более 15 раз" );
		}
		



	}
}