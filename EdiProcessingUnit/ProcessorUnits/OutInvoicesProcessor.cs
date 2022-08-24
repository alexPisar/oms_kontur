using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataContextManagementUnit.DataAccess.Contexts.Abt;
using DataContextManagementUnit.DataAccess.Contexts.Edi;
using EdiProcessingUnit.Edi.Model;
using EdiProcessingUnit.Infrastructure;
using EdiProcessingUnit.UnifiedTransferDocument;
using UtilitesLibrary.ConfigSet;
using EdiProcessingUnit.Edo;

namespace EdiProcessingUnit.ProcessorUnits
{
	public class OutInvoicesProcessor : EdiProcessor
	{
		internal AbtDbContext _abtDbContext;
		private bool _isTest = Config.GetInstance()?.TestModeEnabled ?? false;
		//private Edo.Edo _edo;

		public override void Run()
		{
			ProcessorName = "OutInvoicesProcessor";
			//_edo = Edo.Edo.GetInstance();
		}

		public void SendInvoicFromDesadv(EDIMessage desadvMsg, DocOrder origOrder, DateTime deliveryDate)
		{
			var newEdiMessage = new EDIMessage();
			newEdiMessage.Id = Guid.NewGuid().ToString();
			newEdiMessage.CreationDateTime = GetDate( DateTime.Now );
			newEdiMessage.InterchangeHeader = new InterchangeHeader();
			newEdiMessage.InterchangeHeader.CreationDateTime = GetDate( DateTime.Now );
			newEdiMessage.InterchangeHeader.CreationDateTimeBySender = GetDate( DateTime.Now );
			newEdiMessage.InterchangeHeader.DocumentType = "INVOIC";
			newEdiMessage.InterchangeHeader.IsTest = _isTest ? "1" : "0";
			newEdiMessage.InterchangeHeader.Recipient = desadvMsg.InterchangeHeader.Recipient; // GLN получателя сообщения
			newEdiMessage.InterchangeHeader.Sender = desadvMsg.InterchangeHeader.Sender; // GLN отправителя сообщения
			var newInvoice = new invoice();
			newInvoice.originOrder = desadvMsg.DespatchAdvice.originOrder;
			newInvoice.date = GetDate( deliveryDate );
			newInvoice.orderResponse = new Identificator();
			newInvoice.orderResponse.Number = desadvMsg.DespatchAdvice.number;
			newInvoice.despatchIdentificator = new Identificator();
			newInvoice.despatchIdentificator.Date = desadvMsg.DespatchAdvice.date;
			newInvoice.despatchIdentificator.Number = desadvMsg.DespatchAdvice.number;			
			newInvoice.number = desadvMsg.DespatchAdvice.number;
			newInvoice.seller = desadvMsg.DespatchAdvice.seller;
			newInvoice.buyer = desadvMsg.DespatchAdvice.buyer;
			newInvoice.deliveryInfo = new invoiceDeliveryInfo();
			newInvoice.deliveryInfo.shipTo = desadvMsg.DespatchAdvice.deliveryInfo.shipTo;
			newInvoice.deliveryInfo.ultimateCustomer = desadvMsg.DespatchAdvice.deliveryInfo.ultimateCustomer;
			if (desadvMsg.DespatchAdvice.deliveryInfo.shipFrom == null)
				newInvoice.deliveryInfo.shipFrom = new Company();
			else
				newInvoice.deliveryInfo.shipFrom = desadvMsg?.DespatchAdvice?.deliveryInfo?.shipFrom;
			var seller = _ediDbContext.RefCompanies.Single( x => x.Gln == desadvMsg.DespatchAdvice.seller.gln );
			newInvoice.deliveryInfo.shipFrom.russianAddress = new RussianAddress();
			newInvoice.deliveryInfo.shipFrom.russianAddress.regionISOCode = seller.RegionCode;
			newInvoice.deliveryInfo.shipFrom.gln = seller.Gln;
			newInvoice.deliveryInfo.shipFrom.organization = new Organization();
			newInvoice.deliveryInfo.shipFrom.organization.name = seller.Name;
			newInvoice.deliveryInfo.shipFrom.organization.inn = seller.Inn;
			newInvoice.additionalInformation = new KeyValuePair[] {
				new KeyValuePair(){ key="Номер_заказа", Value=desadvMsg.DespatchAdvice.originOrder.Number },
				new KeyValuePair(){ key="Дата_заказа", Value=desadvMsg.DespatchAdvice.originOrder.Date },
                //new KeyValuePair(){ key="Номер_акта (приемки)", Value=desadvMsg.DespatchAdvice.number },
                new KeyValuePair(){ key="Номер_накладной", Value=desadvMsg.DespatchAdvice.number },
                new KeyValuePair(){ key="GLN_грузополучателя", Value=desadvMsg.DespatchAdvice.deliveryInfo.shipTo.gln }
            };
			newInvoice.lineItems = new invoiceLineItems();
			newInvoice.lineItems.totalAmount = desadvMsg.DespatchAdvice.lineItems.totalAmount;
			newInvoice.lineItems.totalSumExcludingTaxes = desadvMsg.DespatchAdvice.lineItems.totalSumExcludingTaxes;
			newInvoice.lineItems.totalVATAmount = desadvMsg.DespatchAdvice.lineItems.totalVATAmount;
			newInvoice.lineItems.currencyISOCode = desadvMsg.DespatchAdvice.lineItems.currencyISOCode;
			newInvoice.lineItems.lineItem = new List<invoiceLineItemsLineItem>();
			foreach (var item in desadvMsg.DespatchAdvice.lineItems.lineItem)
			{
				var newLineItem = new invoiceLineItemsLineItem();
				newLineItem.amount = item.amount;
				newLineItem.netAmount = item.netAmount;
				newLineItem.netPrice = item.netPrice;
				newLineItem.vATRate = item.vATRate;
				newLineItem.vATAmount = item.vATAmount;
				newLineItem.netPriceWithVAT = item.netPriceWithVAT;
				newLineItem.internalBuyerCode = item.internalBuyerCode;
				newLineItem.description = item.description;
				newLineItem.gtin = item.gtin;
				newLineItem.internalSupplierCode = item.internalSupplierCode;
				newLineItem.quantity = new Quantity();
				newLineItem.quantity.Text = item.despatchedQuantity.Text;
				newLineItem.quantity.UnitOfMeasure = "PCE";
				newLineItem.orderLineNumber = item.orderLineNumber;
				newLineItem.customsDeclarationNumber = item.customsDeclarationNumber;
				newLineItem.countryOfOriginISOCode = item.countryOfOriginISOCode;
                newLineItem.additionalInformation = new KeyValuePair[] 
                {
                    new KeyValuePair(){ key="код_материала", Value=newLineItem.internalBuyerCode },
                    new KeyValuePair(){ key="штрихкод", Value=newLineItem.gtin }
                };
				newInvoice.lineItems.lineItem.Add( newLineItem );
			}
			newEdiMessage.Invoice = newInvoice;
			string rspEdiMsgString = Xml.SerializeObject( newEdiMessage );
			byte[] rspEdiMsgBytes = Encoding.UTF8.GetBytes( rspEdiMsgString );
			// 7. отправим заказ
			var response = _edi.SendMessage( rspEdiMsgBytes );
			var Sended = _edi.CheckMessageErrors( response,
				$"desadv={desadvMsg.DespatchAdvice.number};invoice={newInvoice.number}",
				rspEdiMsgString );
			if (!Sended)
			{
				var ex = new Exception( "УПД не смогла отправится" );
				MailReporter.Add( ex );
				throw ex;
			}			
			MailReporter.Add( $"УПД № {newEdiMessage.Invoice.number} для заказа {newEdiMessage.Invoice.originOrder.Number} " +
				$"от {newEdiMessage.Invoice.originOrder.Date}" );
		}







		//public void SendInvoicFromDesadv(LogOrder log, EDIMessage desadvMsg)
		//{
		//	List<ViewInvoicHead> invoiceList = _ediDbContext.ViewInvoicHeads
		//			.Where( x => x.Id == log.IdDocJournal )
		//			.ToList();
		//	foreach (var invoice in invoiceList)
		//	{
		//		var newEdiMessage = new EDIMessage();

		//		newEdiMessage.Id = Guid.NewGuid().ToString();
		//		newEdiMessage.CreationDateTime = GetDate( DateTime.Now );

		//		newEdiMessage.InterchangeHeader = new InterchangeHeader();
		//		newEdiMessage.InterchangeHeader.CreationDateTime = GetDate( DateTime.Now );
		//		newEdiMessage.InterchangeHeader.CreationDateTimeBySender = GetDate( DateTime.Now );
		//		newEdiMessage.InterchangeHeader.DocumentType = "INVOIC";
		//		newEdiMessage.InterchangeHeader.IsTest = _isTest ? "1" : "0";
		//		newEdiMessage.InterchangeHeader.Recipient = desadvMsg.InterchangeHeader.Recipient; // GLN получателя сообщения
		//		newEdiMessage.InterchangeHeader.Sender = desadvMsg.InterchangeHeader.Sender; // GLN отправителя сообщения

		//		var newInvoice = new invoice();

		//		newInvoice.originOrder = desadvMsg.DespatchAdvice.originOrder;
		//		newInvoice.date = GetDate( DateTime.Now );
		//		newInvoice.utdFunction = "INVDOP";
		//		newInvoice.type = "Original";
		//		newInvoice.number = invoice.Code;

		//		newInvoice.invoicee = new invoicee();
		//		newInvoice.invoicee.gln = desadvMsg.InterchangeHeader.Sender;
		//		newInvoice.invoicee.additionalInfo = new invoiceeAdditionalInfo();
		//		newInvoice.invoicee.additionalInfo.bankName = invoice.SellerBankName;
		//		newInvoice.invoicee.additionalInfo.nameOfAccountant = invoice.Accountant;
		//		newInvoice.invoicee.additionalInfo.correspondentAccountNumber = invoice.SellerCorAccount;


		//		newInvoice.orderResponse = new Identificator();
		//		newInvoice.orderResponse.Number = desadvMsg.DespatchAdvice.number;

		//		newInvoice.despatchIdentificator = new Identificator();
		//		newInvoice.despatchIdentificator.Date = desadvMsg.DespatchAdvice.date;
		//		newInvoice.despatchIdentificator.Number = desadvMsg.DespatchAdvice.number;

		//		newInvoice.number = desadvMsg.DespatchAdvice.number;
		//		newInvoice.seller = desadvMsg.DespatchAdvice.seller;
		//		newInvoice.buyer = desadvMsg.DespatchAdvice.buyer;
		//		newInvoice.deliveryInfo = new invoiceDeliveryInfo();
		//		newInvoice.deliveryInfo.shipTo = desadvMsg.DespatchAdvice.deliveryInfo.shipTo;
		//		newInvoice.deliveryInfo.ultimateCustomer = desadvMsg.DespatchAdvice.deliveryInfo.ultimateCustomer;
		//		if (desadvMsg.DespatchAdvice.deliveryInfo.shipFrom == null)
		//			newInvoice.deliveryInfo.shipFrom = new Company();
		//		else
		//			newInvoice.deliveryInfo.shipFrom = desadvMsg?.DespatchAdvice?.deliveryInfo?.shipFrom;

		//		var buyer = _ediDbContext.RefCompanies.Single( x => x.Gln == desadvMsg.DespatchAdvice.buyer.gln );

		//		newInvoice.deliveryInfo.shipFrom.russianAddress = new RussianAddress();
		//		newInvoice.deliveryInfo.shipFrom.russianAddress.regionISOCode = buyer.RegionCode;
		//		newInvoice.deliveryInfo.shipFrom.gln = buyer.Gln;
		//		newInvoice.deliveryInfo.shipFrom.organization = new Organization();
		//		newInvoice.deliveryInfo.shipFrom.organization.name = buyer.Name;

		//		newInvoice.lineItems = new invoiceLineItems();
		//		newInvoice.lineItems.totalAmount = desadvMsg.DespatchAdvice.lineItems.totalAmount;
		//		newInvoice.lineItems.totalSumExcludingTaxes = desadvMsg.DespatchAdvice.lineItems.totalSumExcludingTaxes;
		//		newInvoice.lineItems.totalVATAmount = desadvMsg.DespatchAdvice.lineItems.totalVATAmount;
		//		newInvoice.lineItems.currencyISOCode = desadvMsg.DespatchAdvice.lineItems.currencyISOCode;



		//		newInvoice.lineItems.lineItem = new List<invoiceLineItemsLineItem>();

		//		foreach (var item in desadvMsg.DespatchAdvice.lineItems.lineItem)
		//		{
		//			var newLineItem = new invoiceLineItemsLineItem();

		//			newLineItem.amount = item.amount;
		//			newLineItem.netAmount = item.netAmount;
		//			newLineItem.netPrice = item.netPrice;
		//			newLineItem.vATRate = item.vATRate;
		//			newLineItem.vATAmount = item.vATAmount;
		//			newLineItem.netPriceWithVAT = item.netPriceWithVAT;
		//			newLineItem.internalBuyerCode = item.internalBuyerCode;
		//			newLineItem.description = item.description;
		//			newLineItem.gtin = item.gtin;
		//			newLineItem.internalSupplierCode = item.internalSupplierCode;
		//			newLineItem.quantity = new Quantity();
		//			newLineItem.quantity.Text = item.despatchedQuantity.Text;
		//			newLineItem.quantity.UnitOfMeasure = "PCE";
		//			newLineItem.orderLineNumber = item.orderLineNumber;
		//			newInvoice.lineItems.lineItem.Add( newLineItem );
		//		}

		//		newEdiMessage.Invoice = newInvoice;


		//		string rspEdiMsgString = Xml.SerializeObject( newEdiMessage );
		//		byte[] rspEdiMsgBytes = Encoding.UTF8.GetBytes( rspEdiMsgString );

		//		// 7. отправим сф
		//		var response = _edi.SendMessage( rspEdiMsgBytes );
		//		var Sended = _edi.CheckMessageErrors( response,
		//			$"desadv={desadvMsg.DespatchAdvice.number};invoice={newInvoice.number}",
		//			rspEdiMsgString );

		//		if (!Sended)
		//		{
		//			throw new Exception( "CA не смогла отправится" );
		//		}
		//	}
		//}

	}
}
