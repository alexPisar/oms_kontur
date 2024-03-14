using System.Linq;
using DataContextManagementUnit.DataAccess.Contexts.Edi;
using DataContextManagementUnit.DataAccess.Contexts.Abt;
using EdiProcessingUnit.Infrastructure;
using System.Collections.Generic;
using EdiProcessingUnit.Edi.Model;
using System;
using System.Text;
using UtilitesLibrary.ConfigSet;

namespace EdiProcessingUnit.WorkingUnits
{
	public class OrderResponsesProcessor : EdiProcessor
	{
		internal AbtDbContext _abtDbContext;
		private bool _isTest = Config.GetInstance()?.TestModeEnabled ?? false;
        private List<string> _xmlList = null;
        private List<DocOrder> _ordersListForSentResponses = null;
        private bool _isSentResponseForSomeOrders => _ordersListForSentResponses != null && _ordersListForSentResponses?.Count > 0;

        public OrderResponsesProcessor(List<string> xmlList) => _xmlList = xmlList;
        public OrderResponsesProcessor() { }
        public OrderResponsesProcessor(List<DocOrder> orders) => _ordersListForSentResponses = orders;

        public override void Run()
		{
			ProcessorName = "OrderResponsesProcessor";

            if(_xmlList != null)
            {
                foreach(var xml in _xmlList)
                {
                    var ordRespFromXml = Xml.DeserializeString<EDIMessage>(xml);

                    if (ordRespFromXml.orderResponse == null)
                        continue;

                    var order = _ediDbContext.DocOrders.FirstOrDefault(d => d.Number == ordRespFromXml.orderResponse.originOrder.Number &&
                    d.GlnSeller == ordRespFromXml.orderResponse.seller.gln && d.GlnBuyer == ordRespFromXml.orderResponse.buyer.gln);

                    var ordrspBytes = Encoding.UTF8.GetBytes(xml);
                    SkbKontur.EdiApi.Client.Types.Messages.OutboxMessageMeta response = _edi.SendMessage(ordrspBytes);
                    bool Sended = _edi.CheckMessageErrors(response, $"order={ordRespFromXml.orderResponse.originOrder.Number};doc={ordRespFromXml.orderResponse.number}", xml);

                    if (Sended && order != null)
                        order.Status = 2;
                }

                _ediDbContext.SaveChanges();
                return;
            }

            List<string> connectionStringList = new UsersConfig().GetAllConnectionStrings();//_ediDbContext.ConnStrings.ToList();
			if (connectionStringList.Count <= 0)
				return;

            // 1. получаем доки из бд
            List<DocOrder> docs = new List<DocOrder>();

            var dateTimeDeliveryFrom = DateTime.Now.AddMonths(-1);

            if (_isSentResponseForSomeOrders)
                docs = _ordersListForSentResponses;
            else
                docs = _ediDbContext.DocOrders
                    .Where( doc => doc.Status == 1 && doc.ReqDeliveryDate != null && doc.GlnSeller == _edi.CurrentOrgGln &&
                    doc.ReqDeliveryDate.Value > dateTimeDeliveryFrom)
                    .ToList();

            List<LogOrder> orderLogs = new List<LogOrder>();
            foreach (var doc in docs)
            {
                if (!IsNeedProcessor( doc.GlnBuyer ))
                    continue;

                foreach (var item in doc.LogOrders)
                {
                    /*
                    0	Новый
                    1	Экспортирован
                    2	Отобран
                    3	Отправлен
                    4	Принят
                    5	Принят с расхождением
                    6	Отправлена корректировка
                    7	Закрыт
                    */
                    if (item.OrderStatus != 1)
                        continue;

                    if (item.IdDocJournal != null /*&& item.IdManufacturer != null*/)
                        orderLogs.Add( item );
                }
            }

            if (orderLogs.Count <= 0)
                return;

            var orderLogsByOrders = orderLogs.GroupBy( l => l.IdOrder );

            foreach (var connStr in connectionStringList)
			{
				using (_abtDbContext = new AbtDbContext( connStr, true ))
				{
                    bool isAbtDataBaseError = false;

                    foreach (var orderLogsByOrder in orderLogsByOrders)
                    {
                        // дёргаем заказ
                        var orderId = orderLogsByOrder.Key;
                        DocOrder originalEdiDocOrder = _ediDbContext.DocOrders.FirstOrDefault( d => d.Id == orderId );

                        var connectedBuyer = _ediDbContext?
                            .RefShoppingStores?
                            .FirstOrDefault(r => r.BuyerGln == originalEdiDocOrder.GlnBuyer)?
                            .MainShoppingStore;

                        if (!IsNeedProcessor( originalEdiDocOrder.GlnBuyer, connectedBuyer ))
                            continue;

                        try
                        {
                            List<DocJournal> traderDocs = new List<DocJournal>();

                            if (orderLogsByOrder.All(l => l.IdManufacturer == null))
                            {
                                foreach(var log in orderLogsByOrder)
                                {
                                    List<DocJournal> trDocs = null;

                                    try
                                    {
                                        // 3. получаем привязанные к логам документы из трейдера
                                        trDocs = _abtDbContext?
                                            .DocJournals?
                                            .Where(doc => log.IdDocJournal == doc.Id)?
                                            .ToList();
                                    }
                                    catch(Exception ex)
                                    {
                                        _log.Log(ex);
                                        MailReporter.Add(ex, Console.Title);
                                        isAbtDataBaseError = true;
                                        break;
                                    }

                                    if (trDocs == null)
                                        continue;

                                    foreach (var t in trDocs)
                                        if (!traderDocs.Contains(t))
                                        {
                                            traderDocs.Add(t);
                                        }
                                }
                            }
                            else
                            {
                                var logsGroupsByManufacturers = orderLogsByOrder.GroupBy( l => l.IdManufacturer );

                                bool existsManufacturerWithEarlyStatus = false;
                                foreach (var logsGroupsByManufacturer in logsGroupsByManufacturers)
                                {
                                    bool existsDocumentWithNeedStatus = false;
                                    // 2. пробегаемся по доступным логам
                                    foreach (LogOrder log in logsGroupsByManufacturer)
                                    {
                                        List<DocJournal> trDocs = null;

                                        try
                                        {
                                            // 3. получаем привязанные к логам документы из трейдера
                                            trDocs = _abtDbContext?
                                                .DocJournals?
                                                .Where( doc => log.IdDocJournal == doc.Id)?
                                                .ToList();
                                        }
                                        catch(Exception ex)
                                        {
                                            _log.Log(ex);
                                            MailReporter.Add(ex, Console.Title);
                                            isAbtDataBaseError = true;
                                            break;
                                        }

                                        if (trDocs == null)
                                            continue;

                                        foreach (var t in trDocs)
                                            if ((!traderDocs.Contains( t )) && t.ActStatus >= 4)
                                            {
                                                traderDocs.Add( t );
                                                existsDocumentWithNeedStatus = true;
                                            }
                                    }

                                    if (isAbtDataBaseError)
                                        break;

                                    existsManufacturerWithEarlyStatus = existsManufacturerWithEarlyStatus || !existsDocumentWithNeedStatus;
                                }

                                if (existsManufacturerWithEarlyStatus)
                                    continue;
                            }

                            if (isAbtDataBaseError)
                                break;

                            if (traderDocs.Exists( tr => tr.ActStatus < 4 ))
                                continue;

                            if (traderDocs.Count == 0)
                                continue;

                            List<LineItem> orderResponseLineItems = new List<LineItem>();

                            // пробегаем по всем документам
                            foreach (DocJournal traderDoc in traderDocs)
                            {
                                // 5. сформируем список товаров для отправки
                                foreach (DocGoodsDetail detail in traderDoc.Details)
                                {
                                    LineItem newOrdrspeLineItem = new LineItem();
                                    DocLineItem origLineItem = originalEdiDocOrder.DocLineItems
                                        .FirstOrDefault( x => x.IdGood == detail.IdGood );

                                    if (origLineItem == null)
                                    {
                                        string sql = "select RBC.BAR_CODE FROM ABT.REF_GOODS RG, ABT.REF_BAR_CODES RBC " +
                                            "WHERE RBC.ID_GOOD = RG.ID and RG.ID=" + detail.IdGood;

                                        List<string> barCodes = _abtDbContext?.Database?
                                        .SqlQuery<string>(sql)?
                                        .ToList() ?? new List<string>();

                                        if (barCodes.Count == 0)
                                            continue;

                                        origLineItem = originalEdiDocOrder.DocLineItems?
                                            .Where(x => barCodes.Exists(b => b == x.Gtin))?
                                            .FirstOrDefault();

                                        if (origLineItem != null)
                                            origLineItem.IdGood = (long)detail.IdGood;
                                    }

                                    if (origLineItem == null && connectedBuyer?.IncludedBuyerCodes == 1)
                                    {
                                        var refBarCode = _abtDbContext?.RefBarCodes?.FirstOrDefault(r => r.IdGood == detail.IdGood && r.IsPrimary == false);

                                        IEnumerable<MapGoodByBuyer> buyerItems = null;

                                        if (refBarCode != null)
                                            buyerItems = (from mapGoodByBuyer in _ediDbContext.MapGoodsByBuyers
                                                          where mapGoodByBuyer.Gln == connectedBuyer.Gln
                                                          join mapGood in _ediDbContext.MapGoods on mapGoodByBuyer.IdMapGood equals (mapGood.Id)
                                                          where mapGood.IdGood == detail.IdGood && mapGood.BarCode == refBarCode.BarCode
                                                          select mapGoodByBuyer) as IEnumerable<MapGoodByBuyer> ?? new List<MapGoodByBuyer>();

                                        if (refBarCode == null || buyerItems == null || buyerItems.Count() == 0)
                                            buyerItems = (from mapGoodByBuyer in _ediDbContext.MapGoodsByBuyers
                                                          where mapGoodByBuyer.Gln == connectedBuyer.Gln
                                                          join mapGood in _ediDbContext.MapGoods on mapGoodByBuyer.IdMapGood equals (mapGood.Id)
                                                          where mapGood.IdGood == detail.IdGood
                                                          select mapGoodByBuyer) as IEnumerable<MapGoodByBuyer> ?? new List<MapGoodByBuyer>();

                                        if (buyerItems.Count() == 0)
                                            buyerItems = (from mapGoodByBuyer in _ediDbContext.MapGoodsByBuyers
                                                          where mapGoodByBuyer.Gln == connectedBuyer.Gln
                                                          join mapGood in _ediDbContext.MapGoods on mapGoodByBuyer.IdMapGood equals (mapGood.Id)
                                                          where mapGood.BarCode == refBarCode.BarCode
                                                          select mapGoodByBuyer) as IEnumerable<MapGoodByBuyer> ?? new List<MapGoodByBuyer>();

                                        if (buyerItems.Count() == 0)
                                            continue;

                                        var buyerCodes = buyerItems?
                                        .Where(x => !string.IsNullOrEmpty(x.BuyerCode))?
                                        .Select(s => s.BuyerCode) ?? new List<string>();

                                        if (buyerCodes.Count() == 0)
                                            continue;

                                        buyerCodes = buyerCodes.Distinct();

                                        origLineItem = originalEdiDocOrder.DocLineItems?
                                            .Where(x => buyerCodes.Any(b => b == x.BuyerCode))?
                                            .FirstOrDefault();

                                        if (origLineItem != null)
                                        {
                                            origLineItem.IdGood = (long)detail.IdGood;

                                            if (!string.IsNullOrEmpty(refBarCode?.BarCode))
                                                origLineItem.Gtin = refBarCode.BarCode;
                                        }
                                        else
                                            continue;
                                    }

                                    if (origLineItem == null)
                                        continue;
                                    double UnitsCount = 0,// отправляемое кол-во
                                        VATRate = 0,// ставка НДС
                                        NetPrice = 0,// цена товара без ндс
                                        NetPriceWithVAT = 0,// цена товара с НДС
                                        Amount = 0,// сумма по позиции с НДС
                                        VATAmount = 0,// сумма НДС по позиции
                                        NetAmount = 0,// сумма по позиции без НДС

                                        OrderedUnitsCount = 0,// отправляемое кол-во
                                        OrderedVATRate = 0,// ставка НДС
                                        OrderedNetPrice = 0,// цена товара без ндс
                                        OrderedNetPriceWithVAT = 0,// цена товара с НДС
                                        OrderedAmount = 0,// сумма по позиции с НДС
                                        OrderedVATAmount = 0,// сумма НДС по позиции
                                        OrderedNetAmount = 0;// сумма по позиции без НДС

                                    /* ********************РАСЧЁТЫ ЗАКАЗА************************ */
                                    double.TryParse( origLineItem.ReqQunatity, out OrderedUnitsCount );
                                    double.TryParse( origLineItem.VatRate, out OrderedVATRate );
                                    double.TryParse( origLineItem.NetPrice, out OrderedNetPrice );
                                    double.TryParse( origLineItem.NetPriceVat, out OrderedNetPriceWithVAT );
                                    double.TryParse( origLineItem.Amount, out OrderedAmount );
                                    double.TryParse( origLineItem.VatAmount, out OrderedVATAmount );
                                    double.TryParse( origLineItem.NetAmount, out OrderedNetAmount );
                                    /* ********************РАСЧЁТЫ ПОЗИЦИИ************************ */
                                    UnitsCount = detail.Quantity;
                                    double.TryParse( origLineItem.VatRate, out VATRate );
                                    if (detail.Price >= 0)
                                    {
                                        NetPriceWithVAT = detail.Price;
                                        NetPrice = Math.Round(NetPriceWithVAT / (100 + VATRate) * 100 , 2);
                                    }
                                    else
                                    {
                                        double.TryParse( newOrdrspeLineItem.NetPrice, out NetPrice );
                                        NetPriceWithVAT = Math.Round(NetPrice / 100 * (100 + VATRate), 2);
                                    }
                                    Amount = Math.Round( NetPriceWithVAT * UnitsCount, 2 );
                                    VATAmount = Math.Round( Amount * VATRate / (100 + VATRate), 2 );
                                    NetAmount = Amount - VATAmount;
                                    /* *********************************************************** */
                                    newOrdrspeLineItem.Amount = Amount.ToString();
                                    newOrdrspeLineItem.NetAmount = NetAmount.ToString();
                                    newOrdrspeLineItem.NetPrice = NetPrice.ToString();
                                    newOrdrspeLineItem.VATRate = VATRate.ToString();
                                    newOrdrspeLineItem.VATAmount = VATAmount.ToString();
                                    newOrdrspeLineItem.NetPriceWithVAT = NetPriceWithVAT.ToString();
                                    newOrdrspeLineItem.Description = origLineItem.Description;
                                    newOrdrspeLineItem.Gtin = origLineItem.Gtin;
                                    newOrdrspeLineItem.InternalBuyerCode = origLineItem.BuyerCode;
                                    newOrdrspeLineItem.RequestedQuantity = new Quantity();
                                    newOrdrspeLineItem.RequestedQuantity.Text = origLineItem.ReqQunatity;
                                    newOrdrspeLineItem.OrderedQuantity = new Quantity();
                                    newOrdrspeLineItem.OrderedQuantity.Text = origLineItem.ReqQunatity;
                                    newOrdrspeLineItem.AcceptedQuantity = new Quantity();
                                    newOrdrspeLineItem.AcceptedQuantity.Text = UnitsCount.ToString();

                                    origLineItem.OrdrspAmount = Amount.ToString();
                                    origLineItem.OrdrspNetAmount = NetAmount.ToString();
                                    origLineItem.OrdrspNetPrice = NetPrice.ToString();
                                    origLineItem.OrdrspNetPriceVat = NetPriceWithVAT.ToString();
                                    origLineItem.OrdrspQuantity = UnitsCount.ToString();

                                    newOrdrspeLineItem.Status = "Accepted";

                                    if (UnitsCount <= 0 || NetPrice <= 0 || Amount <= 0)
                                        newOrdrspeLineItem.Status = "Rejected";
                                    if (Amount != OrderedAmount)
                                        newOrdrspeLineItem.Status = "Changed";
                                    if (NetPrice != OrderedNetPrice)
                                        newOrdrspeLineItem.Status = "Changed";
                                    if (VATRate != OrderedVATRate)
                                        newOrdrspeLineItem.Status = "Changed";
                                    if (NetAmount != OrderedNetAmount)
                                        newOrdrspeLineItem.Status = "Changed";
                                    orderResponseLineItems.Add( newOrdrspeLineItem );
                                }
                            }
                            double
                                totalSumExcludingTaxes = 0, // сумма респонса без НДС
                                totalVATAmount = 0, // сумма НДС
                                totalAmount = 0; // общая сумма респонса, на которую начисляется НДС (125/86)

                            if (traderDocs.Count == 0)
                                continue;

                            /* ********************РАСЧЁТЫ ОТВЕТА************************ */
                            totalSumExcludingTaxes = orderResponseLineItems.Sum( x => double.Parse( x.NetAmount ) );
                            totalVATAmount = orderResponseLineItems.Sum( x => double.Parse( x.VATAmount ) );
                            totalAmount = orderResponseLineItems.Sum( x => double.Parse( x.Amount ) );
                            /* *********************************************************** */
                            var responseNumber = traderDocs.First().Code;
                            EDIMessage rspEdiMsg = new EDIMessage();
                            rspEdiMsg.Id = Guid.NewGuid().ToString();
                            rspEdiMsg.CreationDateTime = GetDate( DateTime.Now );
                            rspEdiMsg.InterchangeHeader = new InterchangeHeader();
                            rspEdiMsg.InterchangeHeader.CreationDateTime = GetDate( DateTime.Now );
                            rspEdiMsg.InterchangeHeader.CreationDateTimeBySender = GetDate( DateTime.Now );
                            rspEdiMsg.InterchangeHeader.DocumentType = "ORDRSP";
                            rspEdiMsg.InterchangeHeader.IsTest = _isTest ? "1" : "0";
                            rspEdiMsg.InterchangeHeader.Recipient = originalEdiDocOrder.GlnShipTo; // GLN получателя сообщения
                            rspEdiMsg.InterchangeHeader.Sender = originalEdiDocOrder.GlnSeller; // GLN отправителя сообщения
                            rspEdiMsg.orderResponse = new OrderResponse();
                            rspEdiMsg.orderResponse.date = GetOrderDate( traderDocs.First().DocDatetime );
                            rspEdiMsg.orderResponse.number = responseNumber;
                            rspEdiMsg.orderResponse.originOrder = new Identificator();
                            rspEdiMsg.orderResponse.originOrder.Number = originalEdiDocOrder.Number;
                            rspEdiMsg.orderResponse.originOrder.Date = GetOrderDate( originalEdiDocOrder.OrderDate.Value );
                            rspEdiMsg.orderResponse.seller = new Company();
                            rspEdiMsg.orderResponse.seller.gln = originalEdiDocOrder.GlnSeller;
                            rspEdiMsg.orderResponse.buyer = new Company();
                            rspEdiMsg.orderResponse.buyer.gln = originalEdiDocOrder.GlnBuyer;
                            rspEdiMsg.orderResponse.deliveryInfo = new DeliveryInfo();
                            rspEdiMsg.orderResponse.deliveryInfo.ShipTo = new Company();
                            rspEdiMsg.orderResponse.deliveryInfo.ShipTo.gln = originalEdiDocOrder.GlnShipTo;
                            rspEdiMsg.orderResponse.deliveryInfo.RequestedDeliveryDateTime = GetDate( originalEdiDocOrder.ReqDeliveryDate.Value );
                            rspEdiMsg.orderResponse.lineItems = new LineItems();
                            rspEdiMsg.orderResponse.lineItems.LineItem = orderResponseLineItems;
                            rspEdiMsg.orderResponse.lineItems.TotalAmount = totalAmount.ToString();
                            rspEdiMsg.orderResponse.lineItems.TotalSumExcludingTaxes = totalSumExcludingTaxes.ToString();
                            rspEdiMsg.orderResponse.lineItems.TotalVATAmount = totalVATAmount.ToString();
                            rspEdiMsg.orderResponse.lineItems.CurrencyISOCode = originalEdiDocOrder.CurrencyCode;
                            rspEdiMsg.orderResponse.status = "Accepted";

                            if (orderResponseLineItems.Any( x => x.Status == "Changed" ))
                            {
                                rspEdiMsg.orderResponse.status = "Changed";
                                rspEdiMsg.orderResponse.quantityChangeReason = "изменена цена или количество";
                            }

                            if (orderResponseLineItems.Count <= 0)
                            {
                                rspEdiMsg.orderResponse.status = "Rejected";
                                rspEdiMsg.orderResponse.quantityChangeReason = "отсутствие товара на складе или иные причины";
                            }
                            rspEdiMsg.orderResponse.additionalInformation = new KeyValuePair[2]
                            {
                                    new KeyValuePair(){ key="номер_заказа", Value=originalEdiDocOrder.Number },
                                    new KeyValuePair(){ key="дата_заказа", Value=GetOrderDate( originalEdiDocOrder.OrderDate.Value ) },
                            };
                            string rspEdiMsgString = Xml.SerializeObject( rspEdiMsg );
                            byte[] rspEdiMsgBytes = Encoding.UTF8.GetBytes( rspEdiMsgString );
                            SkbKontur.EdiApi.Client.Types.Messages.OutboxMessageMeta response = _edi.SendMessage( rspEdiMsgBytes );
                            bool Sended = _edi.CheckMessageErrors( response, $"order={originalEdiDocOrder.Number};doc={responseNumber}", rspEdiMsgString );
                            if (Sended)
                            {
                                MailReporter.Add( $"Подтверждение № {rspEdiMsg.orderResponse.number} для заказа {rspEdiMsg.orderResponse.originOrder.Number} " +
                                    $"от {rspEdiMsg.orderResponse.originOrder.Date}" );

                                foreach (var tr in traderDocs)
                                {
                                    originalEdiDocOrder.Status = 2;
                                    _ediDbContext.LogOrders.Add( new LogOrder {
                                        Id = Guid.NewGuid().ToString(),
                                        IdOrder = orderId,
                                        IdDocJournal = (long?)tr.Id,
                                        OrderStatus = 2,
                                        Datetime = DateTime.Now,
                                        MessageId = response.MessageId,
                                        CircilationId = response.DocumentCirculationId
                                    } );
                                }
                            }
                            _ediDbContext.SaveChanges();
                        }
                        catch (Exception ex)
                        {
                            MailReporter.Add( _log.GetRecursiveInnerException( ex )
                                + $"\r\n===ORDER"
                                + $"\r\n Number={originalEdiDocOrder.Number}"
                                + $"\r\n Buyer ={originalEdiDocOrder.Buyer.IdContractor} {originalEdiDocOrder.GlnBuyer} {originalEdiDocOrder.Buyer.Name}"
                                + $"\r\n Seller={originalEdiDocOrder.Seller.IdContractor} {originalEdiDocOrder.GlnSeller} {originalEdiDocOrder.Seller.Name}"
                                + $"\r\n ShipTo={originalEdiDocOrder.ShipTo.IdContractor} {originalEdiDocOrder.GlnShipTo} {originalEdiDocOrder.NameShipTo}"
                                + $"\r\n Sender={originalEdiDocOrder.Sender.IdContractor} {originalEdiDocOrder.GlnSender} {originalEdiDocOrder.Sender.Name}"
                                );
                        }
                        
                    }//foreach (var orderLogsByOrder in orderLogsByOrders)
                } // using (_abtDbContext = new AbtDbContext())
			}// foreach (string connStr in connectionStringList)
		}
		/// <summary>
		/// Возвращает строку в формате даты по спецификации ISO 8601
		/// </summary>
		/// <param name="Date">input date</param>
		/// <returns>string 00.00.0000T00:00:00.000Z from date</returns>
		private string GetDate(DateTime Date)
		{
			return Date.ToString( "yyyy-MM-ddTHH:mm:ssZ" );
		}
		private string GetOrderDate(DateTime Date)
		{
			return Date.ToString( "yyyy-MM-dd" );
		}

        /// <summary>
        /// Определяет, нужен ли данный документ в документообороте
        /// </summary>
        /// <param name="gln">ГЛН организации</param>
        protected override bool IsNeedProcessor(string gln, ConnectedBuyers connectedBuyer = null)
        {
            if(connectedBuyer == null)
                connectedBuyer = _ediDbContext?
                    .RefShoppingStores?
                    .FirstOrDefault( r => r.BuyerGln == gln )?
                    .MainShoppingStore;

            if (connectedBuyer == null)
                return false;

            return _isSentResponseForSomeOrders || connectedBuyer.OrderExchangeType == (int)DataContextManagementUnit.DataAccess.OrderTypes.OrdersOrdrsp;
        }
    }
}