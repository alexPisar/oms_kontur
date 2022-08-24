using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataContextManagementUnit.DataAccess.Contexts.Edi;

namespace OrderManagementSystem.Gen
{
	public class OrderGenerator
	{
		private EdiDbContext _edi;
		private Random _rnd = new Random( (int)DateTime.UtcNow.Ticks );
		private int _orderNumberPrefixLength;
		private int _orderNumberPostfixLength;
		private string _orderNumberPrefix;
		private int _orderNumberEnumerator;


		public OrderGenerator(ref EdiDbContext EdiDbContext)
		{
			_edi = EdiDbContext;
		}

		public List<DocOrder> GetGeneratedOrderList(int count)
		{
			var list = new List<DocOrder>();
			int i = 1;
			while (i != count)
			{

			}
			return list;
		}
		private DocOrder GenerateOrder()
		{
			var order = new DocOrder();

			order.Id = Guid.NewGuid().ToString();
			order.Number = GenerateNumber();
			order.OrderDate = DateTime.Now;
			order.DocType = "";
			order.IsTest = null;
			order.EdiCreationDate = DateTime.Now;
			order.EdiCreationSenderDate = DateTime.Now;
			order.ReqDeliveryDate = DateTime.Now.AddDays(7);
			order.GlnSender = "4610018018674";
			order.GlnSeller = "4607971729990";
			order.GlnBuyer = "4610018019992";
			order.GlnShipTo = "4610018018674";
			order.Comment = "";
			order.CurrencyCode = "RUB";
			order.TotalAmount = null;
			order.TotalVatAmount = null;
			order.TotalSumExcludeTax = "";
			order.Status = 0;

			/*										
			ID							37b31375-610f-4f02-ba93-c790ad8afcfa	string Id
			NUMBER 						AMV00092131								string Number
			ORDER_DATE 					30.09. 2020 00:00:00					DateTime? OrderDate
			DOC_TYPE 					ORDERS									string DocType
			IS_TEST 					(null)									string IsTest
			EDI_CREATION_DATE			16.10. 2020 16:25:30					DateTime? EdiCreationDate
			EDI_CREATION_SENDER_DATE	14.10. 2020 00:17:30					DateTime? EdiCreationSenderDate
			REQ_DELIVERY_DATE			02.10. 2020 22:00:00					DateTime? ReqDeliveryDate
			GLN_SENDER 					4610018018674							string GlnSender
			GLN_SELLER 					4607971729990							string GlnSeller
			GLN_BUYER 					4610018019992							string GlnBuyer
			GLN_SHIP_TO					4610018018674							string GlnShipTo
			COMMENT						579f29fc-0298-11eb-80ee-0050569155a4	string Comment
			CURRENCY_CODE				RUB										string CurrencyCode
			TOTAL_AMOUNT				(null)									string TotalAmount
			TOTAL_VAT_AMOUNT			(null)									string TotalVatAmount
			TOTAL_SUM_EXCLUDE_TAX		9944.50									string TotalSumExcludeTax
			STATUS						0										long Status			
			*/

			return order;
		}

		void GenerateRandomParameters(int PrefixLength = 3, int PostfixLength = 4)
		{
			_orderNumberEnumerator = 1;
			_orderNumberPostfixLength = PostfixLength;
			_orderNumberPrefixLength = PrefixLength;
			int i = 0;
			_orderNumberPrefix = "";
			while (i++ != _orderNumberPrefixLength)
				_orderNumberPrefix += Convert.ToChar( _rnd.Next( 65/*A*/, 90/*Z*/ ) );
		}

		string GenerateNumber()
		{
			string s = _orderNumberPrefix + "_" + _orderNumberEnumerator.ToString( "D" + _orderNumberPostfixLength );
			_orderNumberEnumerator++;
			return s;
		}

	}
}
