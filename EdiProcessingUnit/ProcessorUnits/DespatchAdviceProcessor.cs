using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataContextManagementUnit.DataAccess.Contexts.Abt;
using DataContextManagementUnit.DataAccess.Contexts.Edi;
using EdiProcessingUnit.Edi.Model;
using EdiProcessingUnit.Infrastructure;
using UtilitesLibrary.ConfigSet;

namespace EdiProcessingUnit.ProcessorUnits
{
	public class DespatchAdviceProcessor : EdiProcessor
	{
		internal AbtDbContext _abtDbContext;
		private bool _isTest = Config.GetInstance()?.TestModeEnabled ?? false;
        private List<string> _xmlList = null;

        public DespatchAdviceProcessor(List<string> xmlList) => _xmlList = xmlList;
        public DespatchAdviceProcessor() { }

        public override void Run()
		{
			OutInvoicesProcessor outInvoiceProcessor =
				(OutInvoicesProcessor)new EdiProcessorFactory()
				.CreateProcessor( new OutInvoicesProcessor(), "OutInvoicesProcessor" );

            if(_xmlList != null)
            {
                foreach(var xml in _xmlList)
                {
                    var desadvFromXml = Xml.DeserializeString<EDIMessage>(xml);

                    if (desadvFromXml.DespatchAdvice == null)
                        continue;

                    var order = _ediDbContext.DocOrders.FirstOrDefault(d => d.Number == desadvFromXml.DespatchAdvice.originOrder.Number &&
                    d.GlnSeller == desadvFromXml.DespatchAdvice.seller.gln && d.GlnBuyer == desadvFromXml.DespatchAdvice.buyer.gln);

                    SendDesadvMessage(order, desadvFromXml.DespatchAdvice.lineItems.lineItem.ToList(), new List<DocJournal>(), outInvoiceProcessor, DateTime.Now, $"13/1-{desadvFromXml.DespatchAdvice.originOrder.Number}");

                    order.Status = 3;
                }
                _ediDbContext.SaveChanges();

                return;
            }

			List<LogOrder> desadvLogs=new List<LogOrder>();
			List<DocJournal> traderDocs=new List<DocJournal>();
			List<despatchAdviceLineItem> desadvLineItems = new List<despatchAdviceLineItem>();
			despatchAdviceLineItem newDesadvLineItem = new despatchAdviceLineItem();
			DocLineItem desadvLineItem= new DocLineItem();
			DocOrder origOrder = new DocOrder();
            int docStatusWhenIsSent = 5;

            ProcessorName = "DespatchAdviceProcessor";
			List<string> connectionStringList = new UsersConfig().GetAllConnectionStrings();//_ediDbContext.ConnStrings.ToList();
            if (connectionStringList.Count <= 0)
				return;

            // 1. получаем доки из бд
            List<DocOrder> docs = new List<DocOrder>();

            var dateTimeDeliveryFrom = DateTime.Now.AddMonths(-2);
            docs = _ediDbContext.DocOrders
                .Where( doc => doc.Status < 3 && doc.Status > 0 && doc.ReqDeliveryDate != null && doc.GlnSeller == _edi.CurrentOrgGln &&
                doc.ReqDeliveryDate.Value > dateTimeDeliveryFrom)
                .ToList();

            foreach (var doc in docs)
            {
                var clientGln = doc.GlnBuyer;
                ConnectedBuyers connectedBuyer = _ediDbContext?
                    .RefShoppingStores?
                    .FirstOrDefault( r => r.BuyerGln == doc.GlnBuyer )?
                    .MainShoppingStore;

                if (connectedBuyer == null)
                    continue;

                if (connectedBuyer.ShipmentExchangeType == (int)DataContextManagementUnit.DataAccess.ShipmentType.None)
                    continue;

                if (connectedBuyer.OrderExchangeType ==
                    (int)DataContextManagementUnit.DataAccess.OrderTypes.OrdersOrdrsp && doc.Status < 2)
                    continue;

                var logOrders = doc?.LogOrders ?? new List<LogOrder>();
                foreach (var log in logOrders)
                {
                    if (connectedBuyer.OrderExchangeType ==
                    (int)DataContextManagementUnit.DataAccess.OrderTypes.OrdersOrdrsp && log.OrderStatus < 2)
                        continue;

                    if (log.OrderStatus > 2)
                        continue;

                    if (log.IdDocJournal == null)
                        continue;

                    if (!logOrders.Exists(l => l.IdDocJournal == log.IdDocJournal && l.OrderStatus > 2))
                        desadvLogs.Add(log);
                }
            }

            if (desadvLogs.Count <= 0)
                return;

            var desadvLogsByOrders = desadvLogs.GroupBy( l => l.IdOrder );

            foreach (var connStr in connectionStringList)
			{
				using (_abtDbContext = new AbtDbContext( connStr, true ))
				{
                    List<RefCountry> Countries = null;
                    bool isAbtDataBaseError = false;

                    foreach (var desadvLogsByOrder in desadvLogsByOrders)
                    {
                        traderDocs = new List<DocJournal>();
                        DateTime dateTime = DateTime.Now;

                        // 1. дёргаем начальный заказ
                        origOrder = _ediDbContext.DocOrders.FirstOrDefault( d => d.Id == desadvLogsByOrder.Key );

                        var connectedBuyer = _ediDbContext?.RefShoppingStores?
                            .FirstOrDefault(r => r.BuyerGln == origOrder.GlnBuyer)?
                            .MainShoppingStore;

                        DateTime? deliveryDate = origOrder?.ReqDeliveryDate;

                        docStatusWhenIsSent = connectedBuyer.DocStatusSendDesadv ?? 5;

                        // 2. пробегаемся по доступным логам
                        foreach (LogOrder log in desadvLogsByOrder)
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
                            catch (Exception ex)
                            {
                                _log.Log(ex);
                                MailReporter.Add(ex, Console.Title);
                                isAbtDataBaseError = true;
                                break;
                            }

                            if (trDocs == null)
                                continue;

                            dateTime = log?.Datetime ?? dateTime;

                            foreach (var t in trDocs)
                                if (!traderDocs.Contains( t ))
                                    traderDocs.Add( t );

                        }//foreach (var log in desadvLogs)

                        if (isAbtDataBaseError)
                            break;

                        if (traderDocs.Exists(doc => doc.ActStatus < docStatusWhenIsSent) && connectedBuyer.MultiDesadv == 0)
                            continue;

                        if (traderDocs.Count == 0)
                            continue;

                        if (connectedBuyer.SendTomorrow == 1)
                        {
                            if (traderDocs.Count == 1)
                            {
                                var trDoc = traderDocs.First();

                                if (trDoc.DeliveryDate != null)
                                    deliveryDate = trDoc.DeliveryDate;
                            }

                            if (deliveryDate != null)
                            {
                                DateTime dateTimeWhenCanSend = new DateTime(
                                    deliveryDate.Value.Year,
                                    deliveryDate.Value.Month,
                                    deliveryDate.Value.Day,
                                    9, 0, 0);

                                var currentDateTime = DateTime.Now;

                                if (currentDateTime < dateTimeWhenCanSend)
                                    continue;
                            }
                        }

                        if (Countries == null)
                            Countries = _abtDbContext?.RefCountries?.ToList();

                        // пробегаем по всем документам

                        desadvLineItems = new List<despatchAdviceLineItem>();
                        bool existsTraderDocsWithLessStatus = false;
                        bool existsTraderDocsNotDelivered = false;

                        foreach (DocJournal traderDoc in traderDocs)
                        {
                            if (traderDoc.ActStatus < docStatusWhenIsSent)
                            {
                                existsTraderDocsWithLessStatus = true;
                                continue;
                            }

                            if(connectedBuyer.MultiDesadv == 1)
                            {
                                desadvLineItems = new List<despatchAdviceLineItem>();

                                if (traderDoc.DeliveryDate != null)
                                    deliveryDate = traderDoc.DeliveryDate;
                                else
                                    deliveryDate = origOrder?.ReqDeliveryDate;
                            }

                            if (connectedBuyer.SendTomorrow == 1 && deliveryDate != null && connectedBuyer.MultiDesadv == 1)
                            {
                                DateTime dateTimeWhenCanSend = new DateTime(
                                    deliveryDate.Value.Year,
                                    deliveryDate.Value.Month,
                                    deliveryDate.Value.Day,
                                    9, 0, 0);

                                var currentDateTime = DateTime.Now;

                                if (currentDateTime < dateTimeWhenCanSend)
                                {
                                    existsTraderDocsNotDelivered = true;
                                    continue;
                                }
                            }

                            var traderInvoice = _abtDbContext?
                                    .DocJournals?
                                    .Where(tr => tr.IdDocMaster == traderDoc.Id)?
                                    .FirstOrDefault();

                            // 5. сформируем список товаров для отправки
                            foreach (DocGoodsDetail detail in traderDoc.Details)
                            {
                                newDesadvLineItem = new despatchAdviceLineItem();

                                desadvLineItem = origOrder.DocLineItems
                                    .FirstOrDefault( x => x.IdGood == detail.IdGood
                                        && x.IdDocJournal == detail.IdDoc.ToString() );

                                if(desadvLineItem == null)
                                    desadvLineItem = origOrder.DocLineItems.FirstOrDefault(x => x.IdGood == detail.IdGood);

                                List<string> barCodes = new List<string>();

                                if (desadvLineItem == null)
                                {
                                    string sql = "select RBC.BAR_CODE FROM ABT.REF_GOODS RG, ABT.REF_BAR_CODES RBC " +
                                        "WHERE RBC.ID_GOOD = RG.ID and RG.ID=" + detail.IdGood;

                                    barCodes = _abtDbContext?.Database?
                                        .SqlQuery<string>(sql)?
                                        .ToList() ?? new List<string>();

                                    if (barCodes.Count == 0)
                                        continue;

                                    desadvLineItem = origOrder.DocLineItems
                                        .Where(x => barCodes.Exists(b => b == x.Gtin))?
                                        .FirstOrDefault();

                                    if(desadvLineItem != null)
                                        desadvLineItem.IdGood = (long)detail.IdGood;
                                }

                                if (desadvLineItem == null)
                                {
                                    if (connectedBuyer.IncludedBuyerCodes != 1)
                                        continue;

                                    var refBarCode = _abtDbContext?.RefBarCodes?.FirstOrDefault(r => r.IdGood == detail.IdGood && r.IsPrimary == false);

                                    IEnumerable<MapGoodByBuyer> buyerItems = null;

                                    if(refBarCode != null)
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
                                    desadvLineItem = origOrder.DocLineItems
                                        .Where(x => buyerCodes.Any(b => b == x.BuyerCode))?
                                        .FirstOrDefault();

                                    if (desadvLineItem != null)
                                    {
                                        desadvLineItem.IdGood = (long)detail.IdGood;

                                        if(!string.IsNullOrEmpty(refBarCode?.BarCode))
                                            desadvLineItem.Gtin = refBarCode.BarCode;
                                    }
                                    else
                                        continue;
                                }

                                double UnitsCount = 0,// отправляемое кол-во
                                    VATRate = 0,// ставка НДС
                                    NetPrice = 0,// цена товара без ндс
                                    NetPriceWithVAT = 0,// цена товара с НДС
                                    Amount = 0,// сумма по позиции с НДС
                                    VATAmount = 0,// сумма НДС по позиции
                                    NetAmount = 0;// сумма по позиции без НДС

                                /* ********************РАСЧЁТЫ ПОЗИЦИИ************************ */
                                if(traderDoc.ActStatus < docStatusWhenIsSent)
                                    UnitsCount = 0;
                                else
                                    UnitsCount = detail.Quantity;

                                if(desadvLineItem.VatRate != null)
                                {
                                    if (desadvLineItem.VatRate == "NOT_APPLICABLE")
                                        VATRate = 0;
                                    else
                                        VATRate = double.Parse( desadvLineItem.VatRate );
                                }
                                else
                                {
                                    VATRate = traderInvoice?
                                        .DocGoodsDetailsIs?
                                        .Where(ds => ds.IdGood == detail.IdGood)?
                                        .FirstOrDefault()?
                                        .TaxRate ?? 0;
                                }

                                if (connectedBuyer.PriceIncludingVat == 1)
                                {
                                    NetPriceWithVAT = detail.Price;
                                    Amount = Math.Round(NetPriceWithVAT * UnitsCount, 2);

                                    var taxRate = traderInvoice?
                                        .DocGoodsDetailsIs?
                                        .Where(ds=> ds.IdGood == detail.IdGood)?
                                        .FirstOrDefault()?
                                        .TaxRate ?? 0;

                                    var taxSumm = Amount * taxRate / (taxRate + 100);

                                    VATAmount = Math.Round(taxSumm, 3);
                                    VATAmount = Math.Round(VATAmount, 2, MidpointRounding.AwayFromZero);

                                    if(UnitsCount > 0)
                                        NetPrice = Math.Round((Amount - VATAmount) / UnitsCount, 2, MidpointRounding.AwayFromZero);
                                    else
                                        NetPrice = Math.Round(NetPriceWithVAT * 100 / (taxRate + 100), 2);
                                }
                                else
                                {
                                    NetPrice = detail.Price;
                                    NetPriceWithVAT = Math.Round( NetPrice / 100 * (100 + VATRate), 2 );
                                    Amount = Math.Round( NetPriceWithVAT * UnitsCount, 2 );
                                    VATAmount = Math.Round( Amount * VATRate / (100 + VATRate), 2 );
                                    NetAmount = Amount - VATAmount;
                                }
                                /* *********************************************************** */

                                newDesadvLineItem.amount = Amount.ToString();
                                newDesadvLineItem.netAmount = NetAmount.ToString();
                                newDesadvLineItem.netPrice = NetPrice.ToString();
                                newDesadvLineItem.vATRate = VATRate.ToString();
                                newDesadvLineItem.vATAmount = VATAmount.ToString();
                                newDesadvLineItem.netPriceWithVAT = NetPriceWithVAT.ToString();
                                newDesadvLineItem.internalBuyerCode = desadvLineItem.BuyerCode;

                                newDesadvLineItem.description = desadvLineItem.Description;
                                newDesadvLineItem.gtin = desadvLineItem.Gtin;

                                if (UnitsCount <= 0 || NetPrice <= 0 || Amount <= 0)
                                    continue;

                                newDesadvLineItem.internalSupplierCode = desadvLineItem.IdGood.ToString();
                                newDesadvLineItem.orderedQuantity = new Quantity();
                                newDesadvLineItem.orderedQuantity.Text = desadvLineItem.ReqQunatity;
                                newDesadvLineItem.orderedQuantity.UnitOfMeasure = desadvLineItem.UnitOfMeasure;
                                newDesadvLineItem.despatchedQuantity = new Quantity();
                                newDesadvLineItem.despatchedQuantity.Text = UnitsCount.ToString();
                                newDesadvLineItem.despatchedQuantity.UnitOfMeasure = desadvLineItem.UnitOfMeasure;
                                newDesadvLineItem.orderLineNumber = desadvLineItem.LineNumber;

                                RefCountry country = Countries?.Single( x => x.Id == detail.Good.IdCountry );

                                newDesadvLineItem.customsDeclarationNumber = detail.Good.CustomsNo;
                                newDesadvLineItem.countryOfOriginISOCode = detail.Good?.Country?.Code ?? "";

                                // сформировать список
                                desadvLineItems.Add( newDesadvLineItem );
                            }

                            if (connectedBuyer.MultiDesadv == 1)
                                SendDesadvMessage(origOrder,
                                    desadvLineItems,
                                    new List<DocJournal>(new DocJournal[]
                                    {
                                        traderDoc
                                    }),
                                    outInvoiceProcessor,
                                    deliveryDate ?? dateTime,
                                    traderInvoice?.Code,
                                    deliveryDate);
                            
                        }//foreach (var traderDoc in traderDocs)

                        if (connectedBuyer.MultiDesadv == 0)
                        {
                            SendDesadvMessage(origOrder,
                                desadvLineItems,
                                traderDocs,
                                outInvoiceProcessor,
                                dateTime, null, deliveryDate);

                            origOrder.Status = 3;
                        }

                        if (!(existsTraderDocsWithLessStatus || existsTraderDocsNotDelivered))
                            origOrder.Status = 3;

                        _ediDbContext.SaveChanges();
                    }
				} // using (_abtDbContext = new AbtDbContext())

			}// foreach (string connStr in connectionStringList)
		}//void	
        
        private void SendDesadvMessage(DocOrder origOrder, 
            List<despatchAdviceLineItem> desadvLineItems, 
            List<DocJournal> traderDocs,
            OutInvoicesProcessor outInvoiceProcessor,
            DateTime ordersDateTime,
            string invoiceNumber = null,
            DateTime? deliveryDate = null)
        {
            if (deliveryDate == null)
                deliveryDate = origOrder?.ReqDeliveryDate;

            double
                                    totalSumExcludingTaxes = 0, // сумма респонса без НДС
                                    totalVATAmount = 0, // сумма НДС
                                    totalAmount = 0; // общая сумма респонса, на которую начисляется НДС (125/86)

            /* ********************РАСЧЁТЫ ОТВЕТА************************ */

            totalSumExcludingTaxes = desadvLineItems.Sum(x => double.Parse(x.netAmount));
            totalVATAmount = desadvLineItems.Sum(x => double.Parse(x.vATAmount));
            totalAmount = desadvLineItems.Sum(x => double.Parse(x.amount));

            /* *********************************************************** */

            // 6. сформируем заказ для отправки
            string reseiveNumber;

            EDIMessage desadvEdiMsg = new EDIMessage();

            desadvEdiMsg.Id = Guid.NewGuid().ToString();
            desadvEdiMsg.CreationDateTime = GetDate(DateTime.Now);

            desadvEdiMsg.InterchangeHeader = new InterchangeHeader();
            desadvEdiMsg.InterchangeHeader.CreationDateTime = GetDate(origOrder.EdiCreationDate.Value);
            desadvEdiMsg.InterchangeHeader.CreationDateTimeBySender = GetDate(DateTime.Now);
            desadvEdiMsg.InterchangeHeader.DocumentType = "DESADV";
            desadvEdiMsg.InterchangeHeader.IsTest = _isTest ? "1" : "0";
            desadvEdiMsg.InterchangeHeader.Recipient = origOrder.GlnShipTo; // GLN получателя сообщения
            desadvEdiMsg.InterchangeHeader.Sender = origOrder.GlnSeller; // GLN отправителя сообщения

            if (!string.IsNullOrEmpty(invoiceNumber))
            {
                reseiveNumber = invoiceNumber;
                desadvEdiMsg.CreationDateTime = GetDate(ordersDateTime);
                desadvEdiMsg.InterchangeHeader.CreationDateTime = GetDate(ordersDateTime);
            }
            else
                reseiveNumber = traderDocs.First().Code;

            desadvEdiMsg.DespatchAdvice = new DespatchAdvice();
            desadvEdiMsg.DespatchAdvice.number = reseiveNumber;
            desadvEdiMsg.DespatchAdvice.date = GetDate(DateTime.Now);
            desadvEdiMsg.DespatchAdvice.originOrder = new Identificator();
            desadvEdiMsg.DespatchAdvice.originOrder.Number = origOrder.Number;
            desadvEdiMsg.DespatchAdvice.originOrder.Date = GetDate(origOrder.OrderDate.Value);
            desadvEdiMsg.DespatchAdvice.orderResponse = new Identificator();
            desadvEdiMsg.DespatchAdvice.orderResponse.Date = GetDate(ordersDateTime);
            desadvEdiMsg.DespatchAdvice.seller = new Company();
            desadvEdiMsg.DespatchAdvice.seller.gln = origOrder.GlnSeller;
            desadvEdiMsg.DespatchAdvice.buyer = new Company();
            desadvEdiMsg.DespatchAdvice.buyer.gln = origOrder.GlnBuyer;
            desadvEdiMsg.DespatchAdvice.deliveryInfo = new despatchAdviceDeliveryInfo();
            desadvEdiMsg.DespatchAdvice.deliveryInfo.shipTo = new Company();
            desadvEdiMsg.DespatchAdvice.deliveryInfo.shipTo.gln = origOrder.GlnShipTo;
            desadvEdiMsg.DespatchAdvice.deliveryInfo.shippingDateTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ssZ");

            if(deliveryDate != null)
                desadvEdiMsg.DespatchAdvice.deliveryInfo.estimatedDeliveryDateTime = deliveryDate.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");

            desadvEdiMsg.DespatchAdvice.status = "Accepted";
            if (desadvLineItems.Count <= 0)
            {
                desadvEdiMsg.DespatchAdvice.status = "Rejected";
                desadvEdiMsg.DespatchAdvice.quantityChangeReason = "отсутствие товара на складе или иные причины";
            }
            desadvEdiMsg.DespatchAdvice.lineItems = new despatchAdviceLineItems();
            desadvEdiMsg.DespatchAdvice.lineItems.lineItem = desadvLineItems.ToArray();
            desadvEdiMsg.DespatchAdvice.lineItems.totalAmount = totalAmount.ToString();
            desadvEdiMsg.DespatchAdvice.lineItems.totalSumExcludingTaxes = totalSumExcludingTaxes.ToString();
            desadvEdiMsg.DespatchAdvice.lineItems.totalVATAmount = totalVATAmount.ToString();
            desadvEdiMsg.DespatchAdvice.lineItems.currencyISOCode = origOrder.CurrencyCode;
            string desadvEdiMsgString = Xml.SerializeObject(desadvEdiMsg);
            byte[] desadvEdiMsgBytes = Encoding.UTF8.GetBytes(desadvEdiMsgString);
            SkbKontur.EdiApi.Client.Types.Messages.OutboxMessageMeta response = _edi.SendMessage(desadvEdiMsgBytes);
            bool Sended = _edi.CheckMessageErrors(response, $"order={origOrder.Number};doc={reseiveNumber}", desadvEdiMsgString);
            if (Sended)
            {
                try
                {
                    var shipmentExchangeType = _ediDbContext?
                        .RefShoppingStores?
                        .FirstOrDefault(r => r.BuyerGln == origOrder.GlnBuyer)?
                        .MainShoppingStore?
                        .ShipmentExchangeType ?? (int)DataContextManagementUnit.DataAccess.ShipmentType.None;

                    if (shipmentExchangeType ==
                        (int)DataContextManagementUnit.DataAccess.ShipmentType.DesadvInvoic)
                    {
                        if(deliveryDate == null)
                            deliveryDate = DateTime.Now;

                        if (traderDocs.Count == 1)
                        {
                            var trDoc = traderDocs.First();

                            if(trDoc.DeliveryDate != null && trDoc.DeliveryDate.Value.Date > deliveryDate.Value)
                                deliveryDate = trDoc.DeliveryDate.Value;
                        }

                        outInvoiceProcessor.SendInvoicFromDesadv(desadvEdiMsg, origOrder, deliveryDate.Value);
                    }
                }
                catch (Exception ex)
                {
                    _log.Log(ex);
                    MailReporter.Add(ex);
                }

                foreach (var tr in traderDocs)
                {
                    _ediDbContext.LogOrders.Add(new LogOrder
                    {
                        Id = Guid.NewGuid().ToString(),
                        IdOrder = origOrder.Id,
                        IdDocJournal = (long?)tr.Id,
                        OrderStatus = 3,
                        Datetime = DateTime.Now,
                        MessageId = response.MessageId,
                        CircilationId = response.DocumentCirculationId
                    });
                }
                MailReporter.Add($"Отгрузка № {desadvEdiMsg.DespatchAdvice.number} для заказа {desadvEdiMsg.DespatchAdvice.originOrder.Number} " +
                    $"от {desadvEdiMsg.DespatchAdvice.originOrder.Date}");
            }
        }
    }//class
}//using
