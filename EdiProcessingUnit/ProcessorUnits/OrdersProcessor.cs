using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using DataContextManagementUnit.DataAccess.Contexts.Edi;
using EdiProcessingUnit.Edi.Model;
using EdiProcessingUnit.Infrastructure;
using SkbKontur.EdiApi.Client.Types.Common;
using SkbKontur.EdiApi.Client.Types.Messages.BoxEvents;

namespace EdiProcessingUnit.WorkingUnits
{
	public sealed class OrdersProcessor : EdiProcessor
	{
		private List<string> _xmlList = null;
        private bool _withErrors;

        public bool WithErrors => _withErrors;
        public OrdersProcessor() { }		
		public OrdersProcessor(List<string> Xmls) => _xmlList = Xmls;	
		public override void Run()
		{
			ProcessorName = "OrdersProcessor";
			string handledDataBody;
			if (_xmlList != null || (_xmlList?.Count ?? 0) > 0)
			{
				foreach (var ord in _xmlList)
				{
                    handledDataBody = ord;
					DataBodyHandler( handledDataBody );
				}
				return;
			}			
			List<MessageBoxEvent> events = new List<MessageBoxEvent>();
            _withErrors = false;

            try
            {
                events = _edi.GetNewEvents();// получить новые события в ящике 
                                             //if (events.Count() <= 0)
                                             //	events = _edi.GetNewEventsFromDate( DateTime.Today );
            }
            catch(Exception ex)
            {
                _withErrors = true;
                MailReporter.Add(_log.GetRecursiveInnerException(ex));
                throw ex;
            }

            if (events.Count() <= 0)
				return;
			var incomingMessages = events.Where( x => x.EventType == MessageBoxEventType.NewInboxMessage ).ToList();
			foreach (MessageBoxEvent boxEvent in incomingMessages)
			{
				try
				{
					NewInboxMessageHandler( boxEvent.EventContent );
				}
				catch(Exception ex)
				{
                    _withErrors = true;
                    MailReporter.Add( _log.GetRecursiveInnerException(ex)
						+ "\r\n BoxId=" + boxEvent.BoxId
						+ "\r\n EventId=" + boxEvent.EventId
						+ "\r\n EventDateTime=" + boxEvent.EventDateTime
						+ "\r\n EventPointer=" + boxEvent.EventPointer
						+ "\r\n PartyId=" + boxEvent.PartyId
						);
				}
			}
        }

		private void NewInboxMessageHandler(object EventContent)
		{
			MessageData messageData;
			string messageBodyString, handledDataBody;			
			messageData = _edi.NewInboxMessageEventHandler( EventContent );//читаем сообщение 			
			messageBodyString = Encoding.UTF8.GetString( messageData.MessageBody, 0, messageData.MessageBody.Length );// транслируем его тело в строку			
			handledDataBody = string.Join( "\n", messageBodyString.Split( '\n' ).Skip( 1 ).ToArray() );// отсекая ненужный тэг <?xml> в начале
			DataBodyHandler( handledDataBody );
		}

		private void DataBodyHandler(string handledDataBody)
		{
			if (handledDataBody.Contains( "UNH+" ))
				return; // если поймали заказ в формате EANCOM то выходим ибо не умеем обрабатывать
			EDIMessage message = Xml.DeserializeString<EDIMessage>( handledDataBody );			
			if (message.Order == null)
				return; // если поймали не заказа то покинем данный обработчик
			DocOrder order;
			if (_ediDbContext.DocOrders.Any( x => x.Id == message.Id ))
				return; // проверим нет ли такого уже в БД и если нет то проверим валидность заказа
            string glnMainBuyer;
            order = ValidateNewMessage( message, out glnMainBuyer);
			order = HandleAbsentGoods( order, glnMainBuyer );
			_ediDbContext.LogOrders.Add( new LogOrder {
				Id = Guid.NewGuid().ToString(),
				IdOrder = order.Id,
				Datetime = DateTime.Now,
				OrderStatus = 0
			} );			
			_ediDbContext.DocOrders.Add( order );// добавим в БД
			MailReporter.Add( $"Заказ № {order.Number} от {order.EdiCreationDate} GLN=[{order.GlnBuyer}/{order.GlnSender}/{order.GlnShipTo}], Id{{{order.Id}}}, {{{order.TotalAmount}}}" );
			_ediDbContext.SaveChanges();
		}

		
		private DocOrder ValidateNewMessage(EDIMessage msg, out string glnMainReceiver)
		{			
			DocOrder newOrder = null;
            glnMainReceiver = null;
            if (msg.Order != null) // проверим есть ли тело заказа во входящем xml и продолжим если есть
            {
                RelationsProcessor relationsProcessor = (RelationsProcessor)new RelationsProcessor().Init( _edi, _ediDbContext );
                List<DocLineItem> orderLineItems = new List<DocLineItem>();
                DocLineItem newItem = null;
                string  newItemId,
                        IdOrder = msg.Id,
                        newItemGtin,
                        newItemBuyerCode,
                        newItemDescription,
                        newItemAmount,
                        newItemReqQunatity,
                        newItemUnitOfMeasure,
                        newItemNetPrice,
                        newItemNetPriceVat,
                        newItemNetAmount,
                        newItemVatRate,
                        newItemVatAmount,
                        newItemRegionIsoCode,
                        newItemManufacturer = null,
                        newOrderDocType = "",
                        newOrderIsTest = "",
                        newOrderGlnSender = "",
                        newOrderGlnSeller = "",
                        newOrderGlnBuyer = "",
                        newOrderGlnShipTo = "",
                        newOrderNameShipTo = "",
                        newOrderAddressShipTo = "",
                        newOrderComment = "",
                        newOrderCurrencyCode = "",
                        newOrderTotalAmount = "",
                        newOrderTotalVatAmount = "",
                        newOrderTotalSumExcludeTax = "",
                        newOrderNumber = "",
                        newOrderMainGlnReceiver = null;
                DateTime? newOrderEdiCreationDate = null,
                          newOrderDate = null,
                          newOrderEdiCreationSenderDate = null,
                          newOrderReqDeliveryDate = null;
                long?  newItemIdGood = 0,
                       newItemIdPriceType = 0;
                InterchangeHeader header = msg.InterchangeHeader;
                Order order = msg.Order;
                LineItems lineInfo = order.LineItems;
                DeliveryInfo deliveryInfo = order.DeliveryInfo;
                if (header != null)
                {
                    newOrderDocType = header.DocumentType;
                    newOrderIsTest = header.IsTest;
                    newOrderGlnSender = header.Sender;
                    newOrderEdiCreationDate = DateTime.Parse( header.CreationDateTime );
                    newOrderEdiCreationSenderDate = DateTime.Parse( header.CreationDateTimeBySender );
                }
                else
                    throw new NullReferenceException( "Заголовок входящего заказа отсутствует" );

                if (order != null)
                {
                    if (order.Seller != null)
                    {
                        newOrderGlnSeller = order.Seller.gln;
                        relationsProcessor.AddNewCompany( ConvertCompany( order.Seller ) );
                    }
                    if (order.Buyer != null)
                    {
                        newOrderGlnBuyer = order.Buyer.gln;
                        relationsProcessor.AddNewCompany( ConvertCompany( order.Buyer ) );
                    }
                    newOrderDate = DateTime.ParseExact(order.Date, "yyyy-MM-dd", CultureInfo.InvariantCulture );
                    newOrderNumber = order.Number;
                    newOrderComment = order.Comment;
                }
                else
                    throw new NullReferenceException( "Тело входящего заказа отсутствует" );
                if (deliveryInfo != null)
                {
                    if (deliveryInfo.ShipTo != null)
                    {
                        newOrderGlnShipTo = deliveryInfo.ShipTo.gln;
                        relationsProcessor.AddNewCompany( ConvertCompany( deliveryInfo.ShipTo ) );
                        newOrderNameShipTo = deliveryInfo.ShipTo?.organization?.name;

                        if (deliveryInfo.ShipTo.russianAddress != null)
                        {
                            if (!string.IsNullOrEmpty(deliveryInfo.ShipTo.russianAddress.city))
                                newOrderAddressShipTo = deliveryInfo.ShipTo.russianAddress.city;

                            if (!string.IsNullOrEmpty(deliveryInfo.ShipTo.russianAddress.settlement))
                                newOrderAddressShipTo = string.IsNullOrEmpty(newOrderAddressShipTo) ? deliveryInfo.ShipTo.russianAddress.settlement
                                    : $"{newOrderAddressShipTo},{deliveryInfo.ShipTo.russianAddress.settlement}";

                            if (!string.IsNullOrEmpty(deliveryInfo.ShipTo.russianAddress.street))
                                newOrderAddressShipTo = string.IsNullOrEmpty(newOrderAddressShipTo) ? deliveryInfo.ShipTo.russianAddress.street
                                    : $"{newOrderAddressShipTo},{deliveryInfo.ShipTo.russianAddress.street}";

                            if (!string.IsNullOrEmpty(deliveryInfo.ShipTo.russianAddress.house))
                                newOrderAddressShipTo = $"{newOrderAddressShipTo},{deliveryInfo.ShipTo.russianAddress.house}";

                            if (!string.IsNullOrEmpty(deliveryInfo.ShipTo.russianAddress.flat))
                                newOrderAddressShipTo = $"{newOrderAddressShipTo},{deliveryInfo.ShipTo.russianAddress.flat}";
                        }
                    }

                    if(!string.IsNullOrEmpty(deliveryInfo.RequestedDeliveryDateTime))
                        newOrderReqDeliveryDate = DateTime.Parse( deliveryInfo.RequestedDeliveryDateTime );
                }
                else
                    throw new NullReferenceException( "Информация доставки входящего заказа отсутствует" );
                if (lineInfo != null)
                {
                    newOrderCurrencyCode = lineInfo.CurrencyISOCode;
                    newOrderTotalAmount = lineInfo.TotalAmount;
                    newOrderTotalVatAmount = lineInfo.TotalVATAmount;
                    newOrderTotalSumExcludeTax = lineInfo.TotalSumExcludingTaxes;
                }
                else
                    throw new NullReferenceException( "Информация о перечне товаров входящего заказа отсутствует" );
                newItemIdPriceType = (long?)_ediDbContext?
                    .MapPriceTypes?
                    .Where( x => x.GlnCompany == newOrderGlnSender )? // TODO: надо уточнить точно ли Sender тут нужен
                    .FirstOrDefault()?
                    .IdPriceType ?? null;

                if (!string.IsNullOrEmpty(newOrderGlnBuyer))
                {
                    newOrderMainGlnReceiver = _ediDbContext?.RefShoppingStores?
                        .Where(r => r.BuyerGln == newOrderGlnBuyer)?
                        .Select(g => g.MainGln)?
                        .FirstOrDefault();

                    if (string.IsNullOrEmpty(newOrderMainGlnReceiver) && !string.IsNullOrEmpty(newOrderGlnSender))
                    {
                        var senderAsBuyer = _ediDbContext?.ConnectedBuyers?.FirstOrDefault(c => c.Gln == newOrderGlnSender);
                        var buyerCompany = _ediDbContext?.RefCompanies?.FirstOrDefault(r => r.Gln == newOrderGlnBuyer);

                        if (senderAsBuyer != null && buyerCompany != null)
                        {
                            var newEntity = new RefShoppingStore
                            {
                                MainGln = newOrderGlnSender,
                                BuyerGln = newOrderGlnBuyer
                            };

                            _ediDbContext?.RefShoppingStores.Add(newEntity);
                            newOrderMainGlnReceiver = newOrderGlnSender;
                        }
                    }
                }

                glnMainReceiver = newOrderMainGlnReceiver;

                if (string.IsNullOrEmpty(newOrderNameShipTo))
                {
                    var shipToCompany = _ediDbContext?.RefCompanies?.FirstOrDefault(r => r.Gln == newOrderGlnShipTo);
                    newOrderNameShipTo = shipToCompany?.Name;
                }

                // пробегаемся по списку заказываемого
                foreach (var item in msg.Order.LineItems.LineItem)
                {
                    newItemIdGood = null;
                    newItemReqQunatity = "";
                    newItemUnitOfMeasure = "";
                    newItemGtin = "";
                    newItemBuyerCode = "";
                    newItemDescription = "";
                    newItemAmount = "";
                    newItemNetPrice = "";
                    newItemNetPriceVat = "";
                    newItemNetAmount = "";
                    newItemVatRate = "";
                    newItemVatAmount = "";
                    newItemRegionIsoCode = "";
                    newItemId = Guid.NewGuid().ToString();
                    IdOrder = msg.Id ?? "";
                    newItemManufacturer = null;
                    if (item != null) // проверим не пустой ли попался элемент списка если не пустой, то заполнить значения
                    {
                        if(item.RequestedQuantity != null)
                        {
                            newItemReqQunatity = item.RequestedQuantity.Text ?? "";
                            newItemUnitOfMeasure = item.RequestedQuantity.UnitOfMeasure ?? "";
                        }
                        else
                            throw new NullReferenceException( "Информация о количестве заказываемого входящего заказа отсутствует" );
                        /*
						    <netPrice>			// цена товара без НДС
							<netPriceWithVAT>	// цена товара с НДС
							<netAmount>			// сумма по позиции без НДС
							<vATRate>			// ставка НДС (NOT_APPLICABLE - без НДС, 0 - 0%, 10 - 10%, 18 - 18%, 20 - 20%)
							<vATAmount>			// сумма НДС по позиции
							<amount>			// сумма по позиции с НДС	
						*/
                        newItemGtin = item.Gtin ?? "";
                        newItemBuyerCode = item.InternalBuyerCode ?? "";
                        newItemDescription = item.Description ?? "";
                        newItemAmount = item.Amount ?? ""; // сумма по позиции с НДС
                        newItemNetPrice = item.NetPrice ?? ""; // цена товара без НДС
                        newItemNetPriceVat = item.NetPriceWithVAT ?? ""; // цена товара с НДС
                        newItemNetAmount = item.NetAmount ?? "";// сумма по позиции без НДС
                        newItemVatRate = item.VATRate ?? "";// ставка НДС
                        newItemVatAmount = item.VATAmount ?? "";// сумма НДС по позиции
                        newItemRegionIsoCode = item.CountryOfOriginISOCode ?? "";
                    }
                    // TODO: понять как забить сюда значения и реализовать
                    if (!string.IsNullOrEmpty(newOrderMainGlnReceiver))
                    {
                        var goodsByBuyer = _ediDbContext?
                            .MapGoodsByBuyers?
                            .Where( m => m.Gln == newOrderMainGlnReceiver )?
                            .ToList() ?? new List<MapGoodByBuyer>();

                        var mapGood = goodsByBuyer?.Select( g => g.MapGood )?
                            .Where( l => l.BarCode == newItemGtin )?
                            .FirstOrDefault();

                        if (mapGood != null)
                            newItemIdGood = (long?)mapGood?.IdGood;
                    }
                    else
                    {
                        newItemIdGood = (long?)_ediDbContext?
                            .MapGoods?
                            .Where( x => x.BarCode == newItemGtin )?
                            .FirstOrDefault()?
                            .IdGood ?? null;
                    }

                    if(newItemIdGood != null && newItemIdGood != 0)
                        newItemManufacturer = _ediDbContext?
                            .MapGoodsManufacturers?
                            .FirstOrDefault(m => m.IdGood == newItemIdGood)?
                            .Name;

                    newItem = new DocLineItem() { // определеим новый пункт заказа со всеми параметрами
                        Id = newItemId,
                        IdOrder = IdOrder,
                        IdGood = newItemIdGood,
                        IdPriceType = newItemIdPriceType,
                        IdDocJournal = null, // при создании заказа в БД мы не имеем номер документа
                        Gtin = newItemGtin,
                        BuyerCode = newItemBuyerCode,
                        Description = newItemDescription,
                        ReqQunatity = newItemReqQunatity,
                        UnitOfMeasure = newItemUnitOfMeasure,
                        Amount = newItemAmount,// сумма по позиции с НДС
                        NetPrice = newItemNetPrice,// цена товара без НДС
                        NetPriceVat = newItemNetPriceVat,// цена товара с НДС
                        NetAmount = newItemNetAmount,// сумма по позиции без НДС
                        VatRate = newItemVatRate,// ставка НДС
                        VatAmount = newItemVatAmount,// сумма НДС по позиции
                        RegionIsoCode = newItemRegionIsoCode,
                        Manufacturer = newItemManufacturer
                    };
                    orderLineItems.Add( newItem ); // после определения добавим к списку который потом запихаем в заказа
                }
                if (orderLineItems.Count <= 0)      // если что-то пошло не так и список товаров пустой то вывалимся в ошибку		
                    throw new NullReferenceException( "Заказываемые товары входящего заказа отсутствуют" );

                if (string.IsNullOrEmpty(newOrderTotalAmount) && orderLineItems.All(l => !string.IsNullOrEmpty(l.Amount)))
                {
                    try
                    {
                        double calculatedTotalAmount = orderLineItems.Sum(l => double.Parse(l.Amount));
                        newOrderTotalAmount = Math.Round(calculatedTotalAmount, 2).ToString();
                    }
                    catch(Exception ex)
                    {
                        newOrderTotalAmount = null;
                    }
                }

                newOrder = new DocOrder() { // определим тело заказа куда и список товаров запихаем
                    Id = IdOrder,
                    DocType = newOrderDocType,
                    IsTest = newOrderIsTest,
                    EdiCreationDate = newOrderEdiCreationDate,
                    EdiCreationSenderDate = newOrderEdiCreationSenderDate,
                    ReqDeliveryDate = newOrderReqDeliveryDate,
                    OrderDeliveryDate = newOrderReqDeliveryDate,
                    GlnSender = newOrderGlnSender,
                    GlnSeller = newOrderGlnSeller,
                    GlnBuyer = newOrderGlnBuyer,
                    GlnShipTo = newOrderGlnShipTo,
                    NameShipTo = newOrderNameShipTo,
                    AddressShipTo = newOrderAddressShipTo,
                    Comment = newOrderComment,
                    Number = newOrderNumber,
                    OrderDate = newOrderDate,
                    CurrencyCode = newOrderCurrencyCode,
                    TotalAmount = newOrderTotalAmount,
                    TotalVatAmount = newOrderTotalVatAmount,
                    TotalSumExcludeTax = newOrderTotalSumExcludeTax,
                    Status = 0,

                    DocLineItems = orderLineItems,
                };
            }
            // если где-то что-то пошло не так? то вывалится либо ошибка либо Null
            // которые необходимо поймать и обработать если всё прошло хорошо то вывалится готовый заказ
            return newOrder;
		}
		
		private DocOrder HandleAbsentGoods(DocOrder order, string glnMainBuyer = null)
		{
            if(string.IsNullOrEmpty(glnMainBuyer))
                glnMainBuyer = _ediDbContext?.RefShoppingStores?
                    .Where(r => r.BuyerGln == order.GlnBuyer)?
                    .Select(g => g.MainGln)?
                    .FirstOrDefault();

            ConnectedBuyers connectedBuyer = null;

            if(!string.IsNullOrEmpty(glnMainBuyer))
                connectedBuyer = _ediDbContext?.ConnectedBuyers?.FirstOrDefault(c => c.Gln == glnMainBuyer);

            var map = _ediDbContext?.MapGoodsByBuyers?
                .Where(m => m.Gln == glnMainBuyer)?
                .Select(l => l.MapGood)?
                .ToList() ?? new List<MapGood>();

			foreach (DocLineItem item in order.DocLineItems)
			{
                if (map.Any( x => x.BarCode == item.Gtin ))
                {
                    var newItem = map.Single( x => x.BarCode == item.Gtin );
                    if (newItem.IdGood != null)
                        item.IdGood = (long)newItem.IdGood;

                    if (connectedBuyer?.IncludedBuyerCodes == 1 && !string.IsNullOrEmpty(item.BuyerCode))
                    {
                        var glnItemGood = newItem.MapGoodByBuyers.FirstOrDefault(m => m.Gln == glnMainBuyer);

                        if (glnItemGood != null && string.IsNullOrEmpty(glnItemGood?.BuyerCode))
                            glnItemGood.BuyerCode = item.BuyerCode;
                    }
                }
                else
                {
                    var mapGood = new MapGood 
                    {
                        Id = Guid.NewGuid().ToString(),
                        BarCode = item.Gtin,
                        Name = item.Description,
                        MapGoodByBuyers = new List<MapGoodByBuyer>()
                    };

                    _ediDbContext.MapGoods.Add(mapGood);

                    var glnItemGood = new MapGoodByBuyer() {
                        Gln = glnMainBuyer,
                        IdMapGood = mapGood.Id,
                        MapGood = mapGood
                    };

                    if (connectedBuyer?.IncludedBuyerCodes == 1)
                        glnItemGood.BuyerCode = item.BuyerCode;

                    mapGood.MapGoodByBuyers.Add( glnItemGood );
                }				
			}
			return order;
		}

		private RefCompany ConvertCompany(Company organization)
		{
			string name = "-",
				kpp = "",
				inn = "",
				gln = organization.gln,
				city = "",
				region = "",
				street = "",
				house = "",
				flat = "",
				postal = "";
			if (organization.organization != null)
			{
				var partyInf = organization.organization;
				name = partyInf.name;
				kpp = partyInf.kpp;
				inn = partyInf.inn;
			}
			if (organization.russianAddress != null)
			{			
				var addrInf = organization.russianAddress;
				city = addrInf.city;
				region = addrInf.regionISOCode;
				street = addrInf.street;
				house = addrInf.house;
				flat = addrInf.flat;
				postal = addrInf.postalCode;				
			}
			var refcomp = new RefCompany() {
				Gln = gln,
				Name = name,
				Kpp = kpp,
				Inn = inn,
				City = city,
				RegionCode = region,
				Street = street,
				House = house,
				Flat = flat,
				PostalCode = postal,
				LastSync = DateTime.Now,
			};
			return refcomp;
		}

		
	}
}
