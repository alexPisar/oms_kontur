using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataContextManagementUnit.DataAccess.Contexts.Edi;
using EdiProcessingUnit.Edi.Model;
using EdiProcessingUnit.Infrastructure;
using EdiProcessingUnit.WorkingUnits;
using SkbKontur.EdiApi.Client.Types.Common;
using SkbKontur.EdiApi.Client.Types.Messages;
using SkbKontur.EdiApi.Client.Types.Messages.BoxEvents;
using SkbKontur.EdiApi.Client.Types.Messages.BoxEventsContents.Inbox;
using SkbKontur.EdiApi.Client.Types.Messages.BoxEventsContents.Outbox;

namespace EdiProcessingUnit.ProcessorUnits
{
	public class ReceivingAdviceProcessor : EdiProcessor
	{
		private string _xmlOrder = null;

		public override void Run()
		{
			ProcessorName = "ReceivingAdviceProcessor";

			if (_xmlOrder != null)
				NewInboxMessageHandler( null );

			// получить новые события в ящике
			List<MessageBoxEvent> events = new List<MessageBoxEvent>();
			List<MessageBoxEvent> inboxMessages = new List<MessageBoxEvent>();
			List<MessageBoxEvent> recadvMessages = new List<MessageBoxEvent>();
			events = _edi.GetNewEvents();

			if (events.Count() <= 0)
				events = _edi.GetNewEvents();

			if (events.Count() <= 0)
				return;

			inboxMessages = events.Where( x => x.EventType == MessageBoxEventType.NewInboxMessage).ToList();

			recadvMessages = inboxMessages.Where( x => ((NewInboxMessageEventContent)x.EventContent)
				.InboxMessageMeta.DocumentDetails.DocumentType == DocumentType.Recadv )
				.ToList();

			if(recadvMessages.Count <= 0)
			{
				inboxMessages = events.Where( x => x.EventType == MessageBoxEventType.NewInboxMessage ).ToList();

				recadvMessages = inboxMessages.Where( x => ((NewInboxMessageEventContent)x.EventContent)
					.InboxMessageMeta.DocumentDetails.DocumentType == DocumentType.Recadv )
					.ToList();

			}

			foreach (MessageBoxEvent boxEvent in recadvMessages)
			{
				NewInboxMessageHandler( boxEvent );
			}
		}

		public ReceivingAdviceProcessor() { }

		public ReceivingAdviceProcessor(string XmlOrderPath)
		{
			using (FileStream fs = new FileStream( XmlOrderPath, FileMode.OpenOrCreate ))
			{
				using (StreamReader sr = new StreamReader( fs ))
				{
					_xmlOrder = sr.ReadToEnd();
				}
			}
		}

		private void NewInboxMessageHandler(MessageBoxEvent boxEvent)
		{
			MessageData messageData;
			EDIMessage message;
			string messageBodyString, handledDataBody;

			if (_xmlOrder != null)
			{
				// отсекая ненужный тэг <?xml> в начале
				handledDataBody = string.Join( "\n", _xmlOrder.Split( '\n' ).Skip( 1 ).ToArray() );
			}
			else
			{
				//читаем сообщение 
				messageData = _edi.NewInboxMessageEventHandler( boxEvent.EventContent );
				// транслируем его тело в строку
				messageBodyString = Encoding.UTF8.GetString( messageData.MessageBody, 0, messageData.MessageBody.Length );
				// отсекая ненужный тэг <?xml> в начале
				handledDataBody = string.Join( "\n", messageBodyString.Split( '\n' ).Skip( 1 ).ToArray() );
			}

			// формируем сообщение парся строку
			message = Xml.DeserializeString<EDIMessage>( handledDataBody );

			// если поймали не заказа
			// то покинем данный обработчик
			if (message.ReceivingAdvice == null)
				return;
			
			HandleRecadv( message );
			
		}


		private void HandleRecadv(EDIMessage msg)
		{
			ReceivingAdvice recadv = msg.ReceivingAdvice;

            var docOrders = _ediDbContext
                .DocOrders
                .Where(
                x => x.GlnBuyer == msg.ReceivingAdvice.Buyer.gln &&
                x.GlnSeller == msg.ReceivingAdvice.Seller.gln &&
                x.Number == msg.ReceivingAdvice.OriginOrder.Number);

            DocOrder order = docOrders
				.FirstOrDefault(x => x.Status >= 1 && x.Status < 4 && x.ReqDeliveryDate != null);

            if (order == null)
                order = docOrders.FirstOrDefault();

            if (order == null)
                return;

			var logOrders = order.LogOrders;

			int RecadvAcceptLineCount = 0;

			foreach(var item in order.DocLineItems)
			{
				var currentRecadvItem = recadv?
					.recadvLineItems?
					.LineItem?
					.SingleOrDefault(x=>x.gtin.ToString() == item.Gtin) ?? null;

				if (currentRecadvItem == null)
					continue;

				item.RecadvAcceptAmount = currentRecadvItem.amount.ToString();
				item.RecadvAcceptNetAmount = currentRecadvItem.netAmount.ToString();
				item.RecadvAcceptNetPrice = currentRecadvItem.netPrice.ToString();
				item.RecadvAcceptNetPriceVat = currentRecadvItem.netPriceWithVAT.ToString();
				item.RecadvAcceptQuantity = currentRecadvItem.acceptedQuantity.Text.ToString();

				RecadvAcceptLineCount++;
			}

			if (RecadvAcceptLineCount <= 0)
				return;
			
			order.Status = 4;

			_ediDbContext.LogOrders.Add( new LogOrder 
			{
				Id = Guid.NewGuid().ToString(),
				IdOrder = order.Id,
				IdDocJournal = null,
				OrderStatus = 4,
				Datetime = DateTime.Now,
				MessageId = msg.Id,
				CircilationId = null
			} );

			_ediDbContext.SaveChanges();

		}

	}
}
