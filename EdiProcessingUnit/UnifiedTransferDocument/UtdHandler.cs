using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataContextManagementUnit.DataAccess.Contexts.Abt;
using DataContextManagementUnit.DataAccess.Contexts.Edi;
using UtilitesLibrary.Service;

namespace EdiProcessingUnit.UnifiedTransferDocument
{
	public static class UtdHandler
	{
		private static string _utdXmlString;
		private static Файл _utdObject;

		public static string AsXmlString => $"<?xml version=\"1.0\" encoding=\"windows-1251\"?>{_utdXmlString}";
		public static Файл AsObject => _utdObject;

		private static string FormatAddress(RefCompany Company)
		{
			return $"{Company.PostalCode}, {Company.City}, {Company.Street}, дом № {Company.House}, этаж {Company.Flat}";
		}

		//private static СвОрг GetOrganization(RefCompany comp)
		//{
		//	if (comp == null)
		//		return null;

		//	return new СвОрг {
		//		ИдСв = new ИдСв {
		//			СвЮЛУч = new СвСвЮЛУч {
		//				НаимОрг = comp.Name,
		//				ИННЮЛ = comp.Inn,
		//				КПП = comp.Kpp,
		//			}
		//		},
		//		Адрес = new Адрес() {
		//			АдрИнф = new АдрИнф() {
		//				КодСтр = "643",
		//				АдрТекст = FormatAddress( comp )
		//				// "680006, Хабаровский край, Хабаровск г, Индустриальная ул, дом № 14, кабинет 67"
		//			}
		//		}
		//	};
		//}


		private static СвОрг GetOrganization(RefContractor contractor)
		{
			if (contractor == null)
				return null;

			return new СвОрг {
				ИдСв = new ИдСв {
					СвЮЛУч = new СвСвЮЛУч {
						НаимОрг = contractor?.Name ?? "",
						// ИННЮЛ = contractor.Inn,
						// КПП = contractor.Kpp,
					}
				},
				Адрес = new Адрес() {
					АдрИнф = new АдрИнф() {
						КодСтр = "643",
						АдрТекст = contractor?.Address ?? ""
						// "680006, Хабаровский край, Хабаровск г, Индустриальная ул, дом № 14, кабинет 67"
					}
				}
			};
		}

		//public static void Create(DocOrder Order)
		//{
		//	using (var _abt = new AbtDbContext())
		//	{
		//		foreach (var logOrder in Order.LogOrders.Where( x => x.IdDocJournal != null ))
		//		{
		//			decimal idDocJournal = decimal.Parse( logOrder.IdDocJournal.ToString() );

		//			DocJournal docJournal = _abt.DocJournals
		//				.Where( x => x.Id == idDocJournal )
		//				.First();

		//			List<DocGoodsDetail> docGoodsDetails = new List<DocGoodsDetail>();

		//			docGoodsDetails = _abt.DocGoodsDetails
		//				.Where( x => x.IdDoc == idDocJournal ).ToList();





		//			var seller = GetOrganization( Order.Seller );
		//			var shipper = GetOrganization( Order.Seller );
		//			var shipTo = GetOrganization( Order.ShipTo );
		//			var buyer = GetOrganization( Order.Buyer );

		//			_utdObject = new Файл() {
		//				ВерсПрог = "Diadoc 1.0", // const
		//				Ид = Guid.NewGuid().ToString(),
		//				ВерсФорм = 5.02M,// const

		//				Документ = new Документ() {
		//					КНД = "1115125", // const
		//					Функция = "СЧФДОП", // const
		//					ДатаИнфПр = DateTime.Now.ToShortDateString(),
		//					ВремИнфПр = DateTime.Now.ToShortTimeString(),
		//					НаимЭконСубСост = Order.Seller.Name,

		//					СвСчФакт = new СвСчФакт() {
		//						НомерСчФ = Order.Number,
		//						ДатаСчФ = DateTime.Now.ToShortDateString(),
		//						КодОКВ = "643", // const
		//						СвПрод = new СвОрг[] { seller },
		//						ГрузОт = new ГрузОт[]
		//						{
		//							new ГрузОт()
		//							{
		//								ГрузОтпр = shipper
		//							}
		//						},
		//						ГрузПолуч = new СвОрг[] { shipTo },
		//						СвПокуп = new СвОрг[] { buyer },
		//						ДопСвФХЖ1 = new ДопСвФХЖ1() {
		//							КурсВал = "1", // const
		//							НаимОКВ = "Российский рубль" // const
		//						},
		//						ИнфПолФХЖ1 = new ФХЖ {
		//							ТекстИнф = new ТекстИнф[]
		//							{
		//								new ТекстИнф()
		//								{
		//									Идентиф ="номер_заказа", // const
		//									Значен = Order.Number
		//								},
		//								new ТекстИнф()
		//								{
		//									Идентиф = "дата_заказа", // const
		//									Значен = Order.EdiCreationSenderDate.Value.ToShortDateString()
		//								}
		//							}
		//						}

		//					},
		//					ТаблСчФакт = new ТаблСчФакт() {
		//						СведТов = GetLineItems( Order.DocLineItems ),
		//						ВсегоОпл = new ВсегоОпл() {
		//							СтТовБезНДСВсего = Order.TotalSumExcludeTax,
		//							СтТовУчНалВсего = Order.TotalAmount,
		//							СумНалВсего = new СумНалич() {
		//								СумНал = Order.TotalVatAmount
		//							}
		//						}
		//					},
		//					Пер = new Пер() {
		//						СвПер = new ПерСвПер() {
		//							СодОпер = "Перадача товаров/услуг",
		//							ДатаПер = "25.06.2019",
		//							ОснПер = new ОснПер[]
		//							{
		//								new ОснПер
		//								{
		//									НаимОсн = "Отсутствует"
		//								}
		//							},
		//							СвЛицПер = new СвЛицПер() {
		//								РабОргПрод = new РабОргПрод() {
		//									Должность = "",
		//									ФИО = new ФИО() {
		//										Фамилия = "Иванова",
		//										Имя = "Илона",
		//										Отчество = "Ивановна"
		//									}
		//								}
		//							}
		//						}
		//					},
		//					Подписант = new Подписант[]
		//					{
		//						new Подписант
		//						{
		//							ОснПолн = "Должностные обязанности",
		//							ОблПолн = "6",
		//							Статус = "2",
		//							ЮЛ = new ЮЛ() {
		//								ИННЮЛ = "123123123",
		//								Должн = "старший оператор",
		//								НаимОрг = "ООО &quot;Тестовая&quot;",
		//								ФИО = new ФИО() {
		//									Фамилия = "Иванова",
		//									Имя = "Илона",
		//									Отчество = "Ивановна"
		//								}
		//							}
		//						}

		//					}
		//				},

		//				СвУчДокОбор = new СвУчДокОбор() {
		//					ИдОтпр = "Тестовый документ",
		//					ИдПол = "Тестовый документ",

		//					СвОЭДОтпр = new СвОЭДОтпр() {
		//						ИдЭДО = "2BM",
		//						ИННЮЛ = "123123123",
		//						НаимОрг = "АО &quot;ПФ &quot;СКБ Контур&quot;"
		//					}
		//				},
		//			};
		//			_utdXmlString = Xml.SerializeEntity( _utdObject );
		//		}

		//	}
		//}

		public static void Create(ViewInvoicHead Doc, AbtDbContext AbtDbContext, EdiDbContext EdiDbContext)
		{
			List<ViewInvoicDetail> docGoodsDetails = new List<ViewInvoicDetail>();
			docGoodsDetails = EdiDbContext.ViewInvoicDetails
				.Where( x => x.IdDocMaster.Value == Doc.Id ).ToList();

			var sellerContractor = AbtDbContext.RefContractors.Where( x => x.Id == Doc.SellerId ).ToList();
			var customerContractor = AbtDbContext.RefContractors.Where( x => x.Id == Doc.CustomerId ).ToList();

			var seller = GetOrganization( sellerContractor.FirstOrDefault() );
			var customer = GetOrganization( customerContractor.FirstOrDefault() );

			_utdObject = new Файл() {
				ВерсПрог = "Diadoc 1.0", // const
				Ид = Guid.NewGuid().ToString(),
				ВерсФорм = 5.02M,

				Документ = new Документ() {
					КНД = "1115125", // const
					Функция = "СЧФДОП", // const
					ДатаИнфПр = DateTime.Now.ToShortDateString(),
					ВремИнфПр = DateTime.Now.ToShortTimeString(),

					СвСчФакт = new СвСчФакт() {
						НомерСчФ = Doc.Code,
						ДатаСчФ = DateTime.Now.ToShortDateString(),
						КодОКВ = "643",// const
						СвПрод = new СвОрг[] { seller },

						СвПокуп = new СвОрг[] { customer },
						ДопСвФХЖ1 = new ДопСвФХЖ1() {
							КурсВал = "1", // const
							НаимОКВ = "Российский рубль" // const
						},

					},
					ТаблСчФакт = new ТаблСчФакт() {
						СведТов = GetLineItems( docGoodsDetails ),
						ВсегоОпл = new ВсегоОпл() {
							СтТовБезНДСВсего = Doc.TotalSumm.ToString(),
							СтТовУчНалВсего = (Doc.TotalSumm+Doc.TaxSumm).ToString(),
							СумНалВсего = new СумНалич() {
								СумНал = Doc.TaxSumm.ToString()
							}
						}
					},
					Пер = new Пер() {
						СвПер = new ПерСвПер() {
							СодОпер = "Перадача товаров/услуг", // const
							ДатаПер = Doc.DeliveryDate.Value.ToShortDateString(),
							ОснПер = new ОснПер[]
							{
								new ОснПер
								{
									НаимОсн = Doc.Dogovor,
									ДатаОсн=Doc.DocDatetime.Value.ToShortDateString(),
									НомОсн=Doc.ZCode,
								}
							},
							СвЛицПер = new СвЛицПер() {
								РабОргПрод = new РабОргПрод() {
									Должность = Doc.StorekeeperProf,
									ФИО = new ФИО() {
										//Фамилия = "Иванова",
										Имя = Doc.Storekeeper,
										//Отчество = "Ивановна"
									}
								}
							}
						}
					},
					
					Подписант = new Подписант[]
					{
						new Подписант
						{
							ОснПолн = $@"{Doc.Dogovor} {Doc.ZCode} {Doc.DocDatetime.Value.ToShortDateString()}",
							ОблПолн = "6", // const
							Статус = "2", // const
							ЮЛ = new ЮЛ() {
								ИННЮЛ = Doc.SellerInn,
								Должн = Doc.StorekeeperProf,
								НаимОрг = Doc.SellerFullName,
								ФИО = new ФИО() {
									Имя = Doc.Storekeeper,
								}
							}
						}

					}
					
				},

				СвУчДокОбор = new СвУчДокОбор() {
					ИдОтпр = "Тестовый документ",
					ИдПол = "Тестовый документ",

					СвОЭДОтпр = new СвОЭДОтпр() {
						ИдЭДО = "2BM",
						ИННЮЛ = Doc.SellerInn,
						НаимОрг = Doc.SellerFullName
					}

				},

			};


			_utdXmlString = Xml.SerializeEntity( _utdObject );

		}




		private static СведТов[] GetLineItems(List<DocLineItem> items)
		{
			List<СведТов> retItems = new List<СведТов>();

			foreach (var item in items)
			{
				СведТов newRetItem = new СведТов() {
					Акциз = new Акциз() { БезАкциз = "без акциза" },
					СумНал = new СумНалич() {
						СумНал = item.VatAmount
					},
					НомСтр = item.LineNumber, // TODO: сделать под это поле в таблице
					КолТов = item.ReqQunatity,
					НаимТов = item.Description,

					СтТовУчНал = item.Amount,
					ЦенаТов = item.NetPrice,
					СтТовБезНДС = item.NetAmount,

					ОКЕИ_Тов = "", // TODO: сделать под это поле в таблице

				};
				retItems.Add( newRetItem );
			}

			return retItems.ToArray();
		}


		private static СведТов[] GetLineItems(List<ViewInvoicDetail> items)
		{
			List<СведТов> retItems = new List<СведТов>();
			ushort i = 1;
			foreach (var item in items)
			{
				СведТов newRetItem = new СведТов() {
					Акциз = new Акциз() {
						БезАкциз = "без акциза"
					},
					СумНал = new СумНалич() {
						СумНал = item.TaxSumm.ToString(),
						//БезНДС = item.SummWithoutTax
					},
					НомСтр = i.ToString(), // TODO: сделать под это поле в таблице
					КолТов = item.Quantity.ToString(),
					НаимТов = item.GName,
					НалСт = item.TaxRate.ToString(),
					СтТовУчНал = item.Summ.ToString(),
					ЦенаТов = item.SummWithoutTax.ToString(),
					СтТовБезНДС = item.PriceWithoutTax.ToString(),
					ДопСведТов = new ДопСведТов() {
						КодТов = item.Code,
					},
					ОКЕИ_Тов = "796", // TODO: сделать под это поле в таблице


				};
				retItems.Add( newRetItem );
				i++;
			}

			return retItems.ToArray();
		}
	}
}
