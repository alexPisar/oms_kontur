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

        public bool IsSavingLastEventId { get; set; }

        public override void Run()
		{
			ProcessorName = "ReceivingAdviceProcessor";

			if (_xmlOrder != null)
				NewInboxMessageHandler( null );

			// получить новые события в ящике
			List<MessageBoxEvent> events = new List<MessageBoxEvent>();
			List<MessageBoxEvent> inboxMessages = new List<MessageBoxEvent>();
			List<MessageBoxEvent> recadvMessages = new List<MessageBoxEvent>();
			events = _edi.MessageBoxEvents;
            IsSavingLastEventId = true;

            if (events.Count() <= 0)
            {
                IsSavingLastEventId = false;
                events = _edi.GetNewEvents();
            }

			if (events.Count() <= 0)
				return;

            try
            {
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
            catch (Exception ex)
            {
                IsSavingLastEventId = false;
                throw ex;
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

            var connectedBuyer = _ediDbContext?
                .RefShoppingStores?
                .FirstOrDefault(r => r.BuyerGln == order.GlnBuyer)?
                .MainShoppingStore;

            bool desadvExportedByManufacturers = connectedBuyer.MultiDesadv == 1 && connectedBuyer.ExportOrdersByManufacturers == 1;

            if (!IsNeedProcessor(order.GlnBuyer, connectedBuyer))
                return;

            IEnumerable<DocLineItem> docLineItems;

            if (desadvExportedByManufacturers)
                docLineItems = from docLineItem in order.DocLineItems
                               where docLineItem.IdDocJournal != null && docLineItem.IdDocJournal != "" && docLineItem.IdGood != null && docLineItem.IsRemoved != "1"
                               select docLineItem;
            else
                docLineItems = order.DocLineItems;

            int RecadvAcceptLineCount = 0;
            long? idDocJournal = null;

            foreach(var item in docLineItems)
			{
				var currentRecadvItem = recadv?
					.recadvLineItems?
					.LineItem?
					.SingleOrDefault(x=>x.gtin.ToString() == item.Gtin) ?? null;

                if (currentRecadvItem == null)
                    currentRecadvItem = recadv?
                        .recadvLineItems?
                        .LineItem?
                        .SingleOrDefault(x => x.internalBuyerCode == item.BuyerCode) ?? null;

                if (currentRecadvItem == null)
					continue;

                if(idDocJournal == null && desadvExportedByManufacturers)
                {
                    idDocJournal = long.Parse(item.IdDocJournal);
                }

				item.RecadvAcceptAmount = currentRecadvItem.amount.ToString();
				item.RecadvAcceptNetAmount = currentRecadvItem.netAmount.ToString();
				item.RecadvAcceptNetPrice = currentRecadvItem.netPrice.ToString();
				item.RecadvAcceptNetPriceVat = currentRecadvItem.netPriceWithVAT.ToString();
				item.RecadvAcceptQuantity = currentRecadvItem.acceptedQuantity.Text.ToString();

				RecadvAcceptLineCount++;
			}

			if (RecadvAcceptLineCount <= 0)
				return;

            if (!desadvExportedByManufacturers)
                order.Status = 4;
            else if(order.Status == 3 && idDocJournal != null)
            {
                var logOrders = (from logOrder in _ediDbContext.LogOrders
                                where logOrder.IdOrder == order.Id && logOrder.OrderStatus >= 3 && logOrder.OrderStatus <= 4 && logOrder.IdDocJournal != null
                                select logOrder)?.ToList() ?? new List<LogOrder>();

                if(logOrders.Count > 0 && logOrders.Exists(l => l.OrderStatus == 3 && l.IdDocJournal == idDocJournal) && 
                    !logOrders.Exists(l => l.OrderStatus == 4 && l.IdDocJournal == idDocJournal))
                {
                    var traderDocsWhenDesadvStatus = logOrders?.Where(l => l.OrderStatus == 3)?.Select(l => l.IdDocJournal.Value)?.Distinct()?.Count() ?? 0;
                    var traderDocsWhenRecadvStatus = logOrders?.Where(l => l.OrderStatus == 4)?.Select(l => l.IdDocJournal.Value)?.Distinct()?.Count() ?? 0;

                    if(traderDocsWhenDesadvStatus <= traderDocsWhenRecadvStatus + 1)
                        order.Status = 4;
                }
            }

            var newLogOrder = new LogOrder
            {
                Id = Guid.NewGuid().ToString(),
                IdOrder = order.Id,
                IdDocJournal = null,
                OrderStatus = 4,
                Datetime = DateTime.Now,
                MessageId = msg.Id,
                CircilationId = null
            };

            if (desadvExportedByManufacturers && idDocJournal != null)
                newLogOrder.IdDocJournal = idDocJournal;

            _ediDbContext.LogOrders.Add( newLogOrder );

			_ediDbContext.SaveChanges();

		}

        /// <summary>
        /// Определяет, нужен ли данный документ в документообороте
        /// </summary>
        /// <param name="gln">ГЛН организации</param>
        protected override bool IsNeedProcessor(string gln, ConnectedBuyers connectedBuyer = null)
        {
            return connectedBuyer.ShipmentExchangeType != (int)DataContextManagementUnit.DataAccess.ShipmentType.None;
        }

    }
}
