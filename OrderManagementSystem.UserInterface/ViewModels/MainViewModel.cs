using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DataContextManagementUnit.DataAccess.Contexts.Abt;
using DataContextManagementUnit.DataAccess.Contexts.Edi;
using EdiProcessingUnit.Edo;
using OrderManagementSystem.UserInterface;
using OrderManagementSystem.UserInterface.ViewModels;
using OrderManagementSystem.UserInterface.ViewModels.Implementations;

namespace OMS.ViewModels
{
	public class MainViewModel : ListViewModel<DocOrder>
	{
        private EdiProcessingUnit.UsersConfig _usersConfig;
        private bool _errorConnectToDb;
        private bool _isNeedRefreshAbt = false;

        public override RelayCommand RefreshCommand => new RelayCommand( (o) => { Refresh(); } );
		public RelayCommand ExportToTraderSingleCommand => new RelayCommand( (o) => { ExportToTraderSingle(); } );
		public RelayCommand ExportToTraderMultiCommand => new RelayCommand( (o) => { ExportToTraderMulti(); } );
		//public RelayCommand ExportSCHFDOPPRCommand => new RelayCommand( (o) => { ExportSCHFDOPPR(); } );
		public RelayCommand OpenGoodsMapCommand => new RelayCommand( (o) => { OpenGoodsMap(); } );
		public RelayCommand OpenOrderViewCommand => new RelayCommand( (o) => { OpenOrderView(); } );
        public RelayCommand SummWithNdsCommand => new RelayCommand( (o) => { SummWithNds(); } );
        public RelayCommand OpenCompanyMapCommand => new RelayCommand( (o) => { OpenCompanyMap(); } );
		public RelayCommand OpenInvoicesCommand => new RelayCommand( (o) => { OpenInvoices(); } );
		public RelayCommand SendGoodsMapCommand => new RelayCommand((o) => { SendGoodsMap(); } );
		public RelayCommand ItemSelectionChangedCommand => new RelayCommand( (o) => { UpdateProps(); } );
		public RelayCommand DocLineItemSelectionChangedCommand => new RelayCommand( (o) => { UpdateProps(); } );
        public RelayCommand DeleteLineCommand => new RelayCommand((o) => { DeleteLine(); } );
        public RelayCommand DeleteOrderCommand => new RelayCommand((o) => { DeleteOrder(); });
        public RelayCommand OpenDeliveryPointsCommand => new RelayCommand((o) => { OpenDeliveryPoints(); } );
        public RelayCommand UpdateSettingsCommand => new RelayCommand((o) => { UpdateSettings(); });
        public RelayCommand OpenJuridicalEntitiesCommand => new RelayCommand((o) => { OpenJuridicalEntities(); });
        public RelayCommand SaveCommand => new RelayCommand((o) => { Save(); });
        public bool MustShowAllOrders { get; set; } = false;
		public DocLineItem SelectedDocLineItem { get; set; }

        private List<DocLineItem> _docLineItems;
        public virtual List<DocLineItem> DocLineItems
        {
            get {
                return _docLineItems;
            }

            set {
                _docLineItems = value;
                OnPropertyChanged( "DocLineItems" );
            }
        }

        public bool IsMainAccount { get; set; }

        public bool IsExportButtonEnabled { get; set; }

        public bool PermittedToMatchingGoods { get; set; }

        private void SendGoodsMap()
		{
			//foreach(var good in SelectedItem.DocLineItems)
			//{
			//	if(_edi.MapGoods.Any(x => x.BarCode == good.Gtin ))
			//	{
			//		var newGoodMap = new MapGood() {
			//			Id = Guid.NewGuid().ToString(),
			//			BarCode = good.Gtin,
			//			IdGood = null,
			//			Name = good.Description
			//		};
			//		_edi.MapGoods.Add( newGoodMap );
			//	}
			//}
			UpdateProps();
		}

		private void OpenInvoices()
		{
			var viewModel = new InvoicExportViewModel( _abt, _edi );
			var InvoicExportWindow = new InvoicExportView();
			InvoicExportWindow.DataContext = viewModel;
			InvoicExportWindow.Activate();
			InvoicExportWindow.Show();
		}

		private void OpenCompanyMap()
		{
			var viewModel = new CompanyMapViewModel( _abt, _edi );
			var GoodsMapWindow = new CompanyMapView( viewModel );
			GoodsMapWindow.Activate();
			GoodsMapWindow.Show();
		}

		private void OpenOrderView()
		{
            try
            {
                if (SelectedItem == null)
                    return;
                var viewModel = new OrderSingleViewModel( _abt, _edi, SelectedItem );
                var GoodsMapWindow = new OrderSingleView( viewModel );
                GoodsMapWindow.Activate();
                GoodsMapWindow.Show();
            }
            catch (Exception ex)
            {
                ShowError( _log.GetRecursiveInnerException( ex ) );
                _log.Log( "Exception: " + _log.GetRecursiveInnerException( ex ) );
            }
        }

		private void OpenGoodsMap()
		{
            if (_errorConnectToDb)
            {
                ShowError("Отсутствует подключение к БД EDI.\nНастройте подключение, и повторите попытку.");
                return;
            }

            try
            {
                var viewModel = new GoodsMapViewModel( _abt, _edi, _usersConfig);
                var GoodsMapWindow = new GoodsMapView( viewModel );
                GoodsMapWindow.Activate();
                GoodsMapWindow.Show();
            }
            catch(Exception ex)
            {
                ShowError( _log.GetRecursiveInnerException( ex ) );
                _log.Log( "Exception: " + _log.GetRecursiveInnerException( ex ) );
            }
		}
		
		public MainViewModel(AbtDbContext abt, EdiProcessingUnit.UsersConfig usersConfig)
		{
			_log.Log( System.Reflection.MethodBase.GetCurrentMethod().Name );
			_abt = abt;
			_edi = new EdiDbContext();
            _usersConfig = usersConfig;
            IsMainAccount = _usersConfig.IsMainAccount;
            ItemsList = new ObservableCollection<DocOrder>();
            DocLineItems = new List<DocLineItem>();
            _errorConnectToDb = false;
            PermittedToMatchingGoods = _usersConfig.IsMainAccount;
            UpdateProps();
		}

        private void SetCommertialNetworks()
        {
            SelectedNetworks = _edi?.ConnectedBuyers?.Select(c => c.Gln)?.ToList() ?? new List<string>();
            CommertialNetworks = _edi.RefCompanies.Where(r => SelectedNetworks.FirstOrDefault(g => r.Gln == g) != null).ToList();
            OnPropertyChanged("CommertialNetworks");
        }

        private void Refresh()
		{
			_log.Log( System.Reflection.MethodBase.GetCurrentMethod().Name );
            /*
				OrderDate.CompareTo(val)
				<0 − OrderDate ранее val
				0 − OrderDate равно val
				>0 − OrderDate позже val
			*/
            List<DocOrder> docs = new List<DocOrder>();
            try
            {
                if (!PermittedToMatchingGoods)
                {
                    PermittedToMatchingGoods = _edi.ConnectedBuyers.Where(c => c.PermittedToMatchingGoods == 1).FirstOrDefault(c => c.Gln == _usersConfig.SelectedUser.UserGLN) != null;
                }

                if (_isNeedRefreshAbt)
                {
                    var connString = _usersConfig.GetConnectionString();
                    _abt = new AbtDbContext(connString, true);
                    _isNeedRefreshAbt = false;
                }

                if (SelectedNetworks == null && CommertialNetworks.Count == 0)
                {
                    SetCommertialNetworks();
                    SetInitialCommertialNetworkValues?.Invoke(SelectedNetworks);
                }

                var glns = _edi?.RefShoppingStores?
                    .Where(r => SelectedNetworks.FirstOrDefault(s => s == r.MainGln) != null)?
                    .Select(b => b.BuyerGln)?.ToList() ?? new List<string>();

                docs = _edi.DocOrders?
                        .Where(x => x.OrderDate != null &&
                        x.OrderDate.Value.CompareTo(DateTo) <= 0 && x.OrderDate.Value.CompareTo(DateFrom) >= 0 
                        && glns.FirstOrDefault(s => s == x.GlnBuyer) != null)
                        .ToList()
                        ?? new List<DocOrder>();

                if (!_usersConfig.IsMainAccount)
                {
                    docs = docs.Where(d => d.ShipTo?.IdContractor != 0 && d.ShipTo?.IdContractor != null && 
                    _abt.RefContractors.FirstOrDefault(r => r.Id == d.ShipTo.IdContractor) != null)?
                    .ToList() ?? new List<DocOrder>();
                }

                if (docs.Count != 0)
                {
                    try
                    {
                        ((System.Data.Entity.Infrastructure.IObjectContextAdapter)_edi)?.
                            ObjectContext?.Refresh(System.Data.Entity.Core.Objects.RefreshMode.StoreWins, docs);
                    }
                    catch (Exception ex)
                    {
                        ShowError(_log.GetRecursiveInnerException(ex));
                        _log.Log("Произошла ошибка при обновлении! Exception: " + _log.GetRecursiveInnerException(ex));
                    }
                }
            }
            catch (Oracle.ManagedDataAccess.Client.OracleException orEx)
            {
                _errorConnectToDb = true;
                ShowError("При получении списка заказов произошла ошибка подключения к базе данных.\n" +
                    "Вероятно, в настройках неправильно задан ip адрес хоста базы EDI, либо имя(SID) сервиса базы данных EDI.\n" +
                    "Настройте правильные параметры для базы EDI. Дополнительная информация:" + _log.GetRecursiveInnerException(orEx));
                _log.Log("Произошла ощибка подключения к базе данных. OracleException: " + _log.GetRecursiveInnerException(orEx));
            }
            catch(Exception ex)
            {
                _errorConnectToDb = true;
                ShowError("При получении списка заказов произошла ошибка подключения к базе данных.\n" +
                    "Вероятно, в настройках неправильно задан ip адрес хоста базы EDI, либо имя(SID) сервиса базы данных EDI.\n" +
                    "Настройте правильные параметры для базы EDI. Дополнительная информация:" + _log.GetRecursiveInnerException(ex));
                _log.Log("Произошла ощибка подключения к базе данных. Exception: " + _log.GetRecursiveInnerException(ex));
            }
            ItemsList = new ObservableCollection<DocOrder>(docs);
            SelectedItem = null;
            DocLineItems = new List<DocLineItem>();
            UpdateProps();
		}

		private void ExportToTraderSingle()
		{
			_log.Log( System.Reflection.MethodBase.GetCurrentMethod().Name );
			try
			{
                if (_errorConnectToDb)
                {
                    ShowError("Отсутствует подключение к БД EDI.\nНастройте подключение, и повторите попытку.");
                    return;
                }

                if (SelectedItem == null)
                {
                    System.Windows.MessageBox.Show( "Не выбран заказ для экспорта!", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error );
                    return;
                }

                _edi.Entry( SelectedItem )?.Reload();

                if(SelectedItem.Status != 0)
                {
                    System.Windows.MessageBox.Show( "Такой заказ уже был экспортирован в трейдер!", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error );
                    return;
                }

                if (DocLineItems.Exists( d => d.IdGood == null || d.IdGood == 0 ))
                {
                    System.Windows.MessageBox.Show( "Ошибка! В списке товаров есть несопоставленные товары!", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                if(SelectedItem?.ShipTo?.IdContractor == null)
                {
                    System.Windows.MessageBox.Show( "Ошибка! GLN получателя не сопоставлен с точкой доставки!", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error );
                    return;
                }

                if (!_usersConfig.IsMainAccount && !_abt.RefContractors.Any( r => r.Id == SelectedItem.ShipTo.IdContractor ))
                {
                    System.Windows.MessageBox.Show( "Ошибка! Для базы по данному профилю не найден такой грузополучатель!", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error );
                    return;
                }

                var dateTimeDeliveryFrom = DateTime.Now.AddMonths(-1);
                if (_edi.DocOrders.Any(d => d.GlnBuyer == SelectedItem.GlnBuyer && d.Number == SelectedItem.Number && d.Status != 0 
                && d.ReqDeliveryDate != null && d.ReqDeliveryDate.Value > dateTimeDeliveryFrom))
                {
                    System.Windows.MessageBox.Show("Заказ с таким номером уже был экспортирован в трейдер!", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                DocOrder order = _edi.DocOrders
					.Where( x => x.Id == SelectedItem.Id )
					.FirstOrDefault();

                ConnectedBuyers connectedBuyer = _edi?
                    .RefShoppingStores?
                    .FirstOrDefault(r => r.BuyerGln == order.GlnBuyer)?
                    .MainShoppingStore;

                decimal? idSeller = (decimal?)_edi.RefCompanies?
                    .Where(r => r.Gln == order.GlnSeller)?
                    .FirstOrDefault()?.IdContractor;

                if (idSeller == null)
                    throw new Exception("Не удалось определить организацию-продавца из заказа.");

                Dictionary<decimal?, DocJournal> docInfosByManufacturers = new Dictionary<decimal?, DocJournal>();
                List<decimal?> docsWithoutManufacturers = new List<decimal?>();

                LoadWindow loadWindow = new LoadWindow();
                LoadModel loadContext = new LoadModel();
                loadWindow.DataContext = loadContext;

                if (_abt.RefContractors.Any( r => r.Id == SelectedItem.ShipTo.IdContractor ))
                {
                    if (connectedBuyer.ExportOrdersByManufacturers == 1)
                        docInfosByManufacturers = DocExportDataByManufacturers(order, idSeller.Value);
                    else
                        docsWithoutManufacturers = DocExportData(order, idSeller.Value);

                    try
                    {
                        _abt.SaveChanges();
                    }
                    catch(Exception ex)
                    {
                        _isNeedRefreshAbt = true;
                        throw ex;
                    }

                    loadWindow.SetSuccessFullLoad( loadContext, "Заказ успешно экспортирован." );
                    loadWindow.Show();
                }
                else if (_usersConfig.IsMainAccount)
                {
                    var connStrings = _usersConfig.GetAllConnectionStrings();

                    string errorText = null;

                    System.Threading.Tasks.Task task = new System.Threading.Tasks.Task( () => {
                        foreach (var connString in connStrings)
                        {
                            if (connString == _usersConfig.GetConnectionString())
                                continue;

                            using (var abtContext = new AbtDbContext( connString, true ))
                            {
                                try
                                {
                                    if (!abtContext.RefContractors.Any( r => r.Id == SelectedItem.ShipTo.IdContractor ))
                                        continue;

                                    if (connectedBuyer.ExportOrdersByManufacturers == 1)
                                    {
                                        var docsToTraider = DocExportDataByManufacturers( order, idSeller.Value, abtContext );
                                        abtContext.SaveChanges();

                                        foreach(var doc in (docsToTraider ?? new Dictionary<decimal?, DocJournal>()))
                                            docInfosByManufacturers.Add(doc.Key, doc.Value);
                                    }
                                    else
                                    {
                                        var idDocsToTrader = DocExportData(order, idSeller.Value, abtContext);
                                        abtContext.SaveChanges();

                                        foreach (var idDoc in (idDocsToTrader ?? new List<decimal?>()))
                                            docsWithoutManufacturers.Add(idDoc);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    _log.Log( ex );
                                    errorText = errorText == null ? "" : errorText + "\r\n";
                                    errorText = errorText + _log.GetRecursiveInnerException( ex );
                                    continue;
                                }
                            }
                        }
                    } );

                    loadWindow.Show();
                    task.Start();

                    System.Threading.Tasks.Task.WaitAll( task );

                    if (errorText != null && docInfosByManufacturers.Count == 0 && docsWithoutManufacturers.Count == 0)
                    {
                        ShowError( errorText );

                        return;
                    }

                    if ((docInfosByManufacturers.Count != 0 && connectedBuyer.ExportOrdersByManufacturers == 1)
                        || (docsWithoutManufacturers.Count != 0 && connectedBuyer.ExportOrdersByManufacturers == 0))
                        loadWindow.SetSuccessFullLoad( loadContext, "Заказ успешно экспортирован." );
                    else
                    {
                        loadWindow.Close();
                        ShowError( "Заказ не был экспортирован в трейдер!" );
                    }
                }

                if (connectedBuyer.ExportOrdersByManufacturers == 1)
                    foreach (var doc in docInfosByManufacturers)
                    {
                        LogOrder newLogOrder = new LogOrder {
                            Id = Guid.NewGuid().ToString(),
                            IdOrder = SelectedItem.Id,
                            IdDocJournal = (long?)doc.Value?.Id,
                            Datetime = GetDateTimeForExportOrder(order),
                            IdManufacturer = doc.Key,
                            OrderStatus = 1
                        };
                        _edi.LogOrders.Add( newLogOrder );
                        order.Status = 1;
                    }
                else if (connectedBuyer.ExportOrdersByManufacturers == 0)
                    foreach (var idDoc in docsWithoutManufacturers)
                    {
                        LogOrder newLogOrder = new LogOrder
                        {
                            Id = Guid.NewGuid().ToString(),
                            IdOrder = SelectedItem.Id,
                            IdDocJournal = (long?)idDoc,
                            Datetime = GetDateTimeForExportOrder(order),
                            OrderStatus = 1
                        };
                        _edi.LogOrders.Add(newLogOrder);
                        order.Status = 1;
                    }

                _edi.SaveChanges();
            }
			catch(Exception ex)
			{
				_log.Log( ex );				
				ShowError( _log.GetRecursiveInnerException(ex) );
				var abtErr = _abt.GetValidationErrors();
				var ediErr = _edi.GetValidationErrors();
			}
			finally
			{
				UpdateProps();
			}
		}
				
		private void ExportToTraderMulti()
		{
			_log.Log( System.Reflection.MethodBase.GetCurrentMethod().Name );
			UpdateProps();

		}
		
		private DocTypeDefaults __docTypeDefaults = null;
		public DocTypeDefaults _docTypeDefaults
		{
			get {
				if(__docTypeDefaults == null)
				{
                    __docTypeDefaults = _abt?
                        .Database?
                        .SqlQuery<DocTypeDefaults>("select " +
                        "to_number(substr(ID_CURRENCY,instr(ID_CURRENCY,'~',1,1)+1,instr(ID_CURRENCY,'~',1,2)-instr(ID_CURRENCY,'~',1,1)-1)) IdCurrency," +
                        "to_number(substr(ID_STORE_RECIEPIENT,instr(ID_STORE_RECIEPIENT,'~',1,1)+1,instr(ID_STORE_RECIEPIENT,'~',1,2)-instr(ID_STORE_RECIEPIENT,'~',1,1)-1)) IdStoreReciepient," +
                        "to_number(substr(ID_PRICE_TYPE,instr(ID_PRICE_TYPE,'~',1,1)+1,instr(ID_PRICE_TYPE,'~',1,2)-instr(ID_PRICE_TYPE,'~',1,1)-1)) IdPriceType," +
                        "to_number(substr(ID_SUBDIVISION,instr(ID_SUBDIVISION,'~',1,1)+1,instr(ID_SUBDIVISION,'~',1,2)-instr(ID_SUBDIVISION,'~',1,1)-1)) IdSubdivision," +
                        "to_number(substr(ID_STORE_SENDER,instr(ID_STORE_SENDER,'~',1,1)+1,instr(ID_STORE_SENDER,'~',1,2)-instr(ID_STORE_SENDER,'~',1,1)-1)) IdStoreSender," +
                        "to_number(substr(ID_CUSTOMER,instr(ID_CUSTOMER,'~',1,1)+1,instr(ID_CUSTOMER,'~',1,2)-instr(ID_CUSTOMER,'~',1,1)-1)) IdCustomer " +
                        "from " +
                        "(select substr(config,instr(config,'ID_CURRENCY')) ID_CURRENCY," +
                        "substr(config,instr(config,'ID_STORE_RECIEPIENT')) ID_STORE_RECIEPIENT," +
                        "substr(config,instr(config,'ID_PRICE_TYPE')) ID_PRICE_TYPE," +
                        "substr(config,instr(config,'ID_SUBDIVISION')) ID_SUBDIVISION," +
                        "substr(config,instr(config,'ID_STORE_SENDER')) ID_STORE_SENDER," +
                        "substr(config,instr(config,'ID_CUSTOMER')) ID_CUSTOMER " +
                        "from (" +
                        "select replace(replace(DEFAULTS,CHR(13)),chr(10),'~') config from " +
                        "REF_DOC_TYPES WHERE id = 1))")?
                        .FirstOrDefault() ?? new DocTypeDefaults();
				}

				return __docTypeDefaults;
				
			}
		}
		public class DocTypeDefaults
		{
            public DocTypeDefaults()
            {
                IdCurrency = 0;
                IdStoreReciepient = 0;
                IdPriceType = 0;
                IdSubdivision = 0;
                IdStoreSender = 0;
                IdCustomer = 0;
            }

            public decimal IdCurrency { get; set; }
			public decimal? IdStoreReciepient { get; set; }
			public decimal IdPriceType { get; set; }
			public decimal IdSubdivision { get; set; }
			public decimal? IdStoreSender { get; set; }
			public decimal IdCustomer { get; set; }
		}

        public void RefreshDocLines()
        {
            if (DocLineItems == null)
                return;

            if (DocLineItems.Count == 0)
                return;

            try
            {
                ((System.Data.Entity.Infrastructure.IObjectContextAdapter)_edi)?.
                    ObjectContext?.Refresh( System.Data.Entity.Core.Objects.RefreshMode.StoreWins, DocLineItems );
            }
            catch (Exception ex)
            {
                ShowError( _log.GetRecursiveInnerException( ex ) );
                _log.Log( "Произошла ошибка при обновлении! Exception: " + _log.GetRecursiveInnerException( ex ) );
            }

            OnPropertyChanged( "DocLineItems" );
        }

		public Dictionary<decimal?, DocJournal> DocExportDataByManufacturers(DocOrder order, decimal idSeller, AbtDbContext abtContext = null)
		{
			_log.Log( System.Reflection.MethodBase.GetCurrentMethod().Name );

            var docJournalsCollection = new Dictionary<decimal?, DocJournal>();

            if (abtContext == null)
                abtContext = _abt;

            var idGoods = order.DocLineItems?
                .Where(i => i.IdGood != null)?
                .Select( l => l.IdGood )?
                .Distinct()?
                .ToList() ?? new List<long?>();

            if (idGoods.Count == 0)
                return new Dictionary<decimal?, DocJournal>();

            var refGoodsManufacturersList = new List<GoodsMapViewModel.DTO_RefGoods>();

            int positionInArrayRefGoods = 0;

            while (positionInArrayRefGoods < idGoods.Count)
            {
                var lengthRefGoods = idGoods.Count - positionInArrayRefGoods > 500 ? 500 : idGoods.Count - positionInArrayRefGoods;

                var refGoodsManufacturersArray = abtContext?
                .Database?
                .SqlQuery<GoodsMapViewModel.DTO_RefGoods>(
                    $"select Id, Id_Manufacturer from abt.ref_goods where Id in({string.Join(", ", idGoods.GetRange(positionInArrayRefGoods, lengthRefGoods))})")?
                    .ToList();

                foreach(var r in refGoodsManufacturersArray)
                    refGoodsManufacturersList.Add(r);

                positionInArrayRefGoods = positionInArrayRefGoods + 500;
            }

            var idManufacturers = refGoodsManufacturersList?
                .GroupBy(r => r.Id_Manufacturer)?
                .Select(g => new { IdManufacturer = g.Key, idGoods = g?.Select(r => r.Id) })?
                .ToList();

            if (idManufacturers == null)
                return new Dictionary<decimal?, DocJournal>();

            var idFilial = Decimal.Parse(abtContext?.SelectSingleValue($"select id_filial from profiles where profile_gln = '{_usersConfig.SelectedUser.UserGLN}'"));

            foreach (var idManufacturer in idManufacturers)
            {
                var newDocJournal = ExportDocWithLineItems(order, abtContext, idFilial, order.DocLineItems.Where(l => idManufacturer.idGoods.Any(id => id == l.IdGood)), idSeller, idManufacturer?.IdManufacturer);
                docJournalsCollection.Add( idManufacturer.IdManufacturer, newDocJournal );
            }
            return docJournalsCollection;

		}

        public DocJournal ExportDocWithLineItems(DocOrder order, AbtDbContext abtContext, decimal idFilial, IEnumerable<DocLineItem> docLineItems, decimal idSeller, decimal? idManufacturer = null)
        {
            DocJournal newDocJournal = new DocJournal();
            newDocJournal.DocGoods = new DocGood();
            newDocJournal.Details = new List<DocGoodsDetail>();

            newDocJournal.DocGoods.IdCurrency = _docTypeDefaults.IdCurrency;
            //newDocJournal.DocGoods.IdStoreReciepient = _docTypeDefaults.IdStoreReciepient;
            newDocJournal.DocGoods.IdPriceType = _docTypeDefaults.IdPriceType;
            newDocJournal.DocGoods.IdSubdivision = _docTypeDefaults.IdSubdivision;
            newDocJournal.DocGoods.IdSeller = idSeller;

            newDocJournal.DocGoods.IdStoreSender = _edi?
                .Database?
                .SqlQuery<decimal>($"select ID_STORE_SENDER from REF_STORE_SENDERS where ID_FILIAL = {idFilial.ToString()}")?
                .FirstOrDefault();

            if (newDocJournal.DocGoods.IdStoreSender == null || newDocJournal.DocGoods.IdStoreSender == 0)
                newDocJournal.DocGoods.IdStoreSender = _docTypeDefaults.IdStoreReciepient;

            long? idCustomer = SelectedItem?.ShipTo?.IdContractor;

            newDocJournal.DocGoods.IdCustomer = (decimal)idCustomer;

            var dateTime = GetDateTimeForExportOrder(order);
            newDocJournal.DocDatetime = dateTime;

            if (idManufacturer != null)
            {
                newDocJournal.DocGoods.IdAgent = abtContext?
                    .RefContractorAgents?
                    .Where( r => r.IdContractor == (decimal)idCustomer
                    && r.IdManufacturer == idManufacturer
                    && r.StartDate != null && r.StartDate < dateTime)?
                     .OrderByDescending(ra => ra.StartDate)?
                     .FirstOrDefault()?
                     .IdAgent ?? 0;

                if (newDocJournal.DocGoods.IdAgent != null)
                {
                    decimal agentId = (decimal)newDocJournal.DocGoods.IdAgent;

                    var agent = abtContext?
                        .RefAgents?
                        .FirstOrDefault(a => a.Id == agentId);

                    if (agent == null)
                    {
                        if (agentId != 0)
                        {
                            agent = abtContext?
                            .RefAgents?
                            .FirstOrDefault(a => a.Id == 0);

                            if (agent == null)
                                newDocJournal.DocGoods.IdAgent = null;
                            else
                                newDocJournal.DocGoods.IdAgent = 0;
                        }
                        else
                            newDocJournal.DocGoods.IdAgent = null;
                    }
                }
            }

            order.ReqDeliveryDate = dateTime.AddDays(1);

            if (order.ReqDeliveryDate?.DayOfWeek == DayOfWeek.Saturday)
            {
                order.ReqDeliveryDate = order.ReqDeliveryDate.Value.AddDays(2);
            }
            else if (order.ReqDeliveryDate?.DayOfWeek == DayOfWeek.Sunday)
            {
                order.ReqDeliveryDate = order.ReqDeliveryDate.Value.AddDays(1);
            }

            newDocJournal.DeliveryDate = order.ReqDeliveryDate;

            var mainGln = _edi.RefShoppingStores?
                .FirstOrDefault(r => r.BuyerGln == SelectedItem.GlnBuyer)?
                .MainGln;

            if (mainGln != null)
            {
                if(idManufacturer == null)
                {
                    var agentByClient = _edi.RefAgentsByEdiClients?
                    .Where(r => r.Gln == mainGln)?
                    .OrderByDescending(r => r.AddedDate)?
                    .FirstOrDefault();

                    if (agentByClient != null)
                        newDocJournal.DocGoods.IdAgent = agentByClient.IdAgent;
                }

                var mapPriceTypes = _edi?.MapPriceTypes?.Where(x => x.GlnCompany == mainGln)?.ToList() ?? new List<MapPriceType>();

                decimal? idPriceType = null;

                if (mapPriceTypes.Count > 0)
                {
                    idPriceType = mapPriceTypes?.SingleOrDefault(x => x.IdFilial == idFilial)?.IdPriceType;

                    if(idPriceType == null)
                        idPriceType = mapPriceTypes?.SingleOrDefault(x => x.IdFilial == null)?.IdPriceType;
                }

                bool isPriceTypeExists = false;

                if (idPriceType != null)
                    isPriceTypeExists = abtContext.SelectSingleValue($"select 1 from ref_price_types where id = {idPriceType}") == "1";

                if(isPriceTypeExists)
                    newDocJournal.DocGoods.IdPriceType = (decimal)idPriceType;
            }

            newDocJournal.Id = 0;
            // генерация ID документа
            string idStr = abtContext.SelectSingleValue( "SELECT abt.seq_objects.NEXTVAL FROM dual" );
            decimal idDec = 0;
            decimal.TryParse( idStr, out idDec );
            newDocJournal.Id = idDec;

            // генерация кода документа
            newDocJournal.Code = abtContext.SelectSingleValue( "SELECT ABT.DOCUMENTS_UTILS.GET_DOC_CODE(1) FROM dual" );

            newDocJournal.IdDocType = 2;
            newDocJournal.CreateInvoice = 1;
            newDocJournal.Comment = order.Number;

            newDocJournal.UserName = "EDI";

            newDocJournal.DocGoods.DocPrecision = true;
            newDocJournal.DocGoods.IdDoc = idDec;

            // генерация списка товаров
            foreach (DocLineItem detail in docLineItems)
            {
                if (detail.IdGood == null)
                    continue;

                if (detail.IsRemoved == "1")
                    continue;

                string itemPrice = abtContext.SelectSingleValue(
                            $"SELECT Documents_Utils.Get_Good_Price" +
                            $"({detail.IdGood}, {newDocJournal.DocGoods.IdPriceType}, 18101, SYSDATE) FROM dual"
                        );

                string defItem = abtContext.SelectSingleValue(
                            $"SELECT DISTINCT rg.ID_DEFAULT_ITEM " +
                            $"FROM ABT.REF_GOODS rg WHERE rg.ID = {detail.IdGood}"
                            );

                itemPrice = string.IsNullOrEmpty( itemPrice ) ? "0": itemPrice;
                defItem = string.IsNullOrEmpty( defItem ) ? "0" : defItem;

                double newDetailPrice = double.Parse ( itemPrice );
                decimal newDetailItemId = decimal.Parse ( defItem );
                int newDetailIdGood = ParseStringToInt(detail.ReqQunatity);

                DocGoodsDetail newDetail = new DocGoodsDetail() {
                    IdDoc = newDocJournal.Id,
                    IdGood = (decimal)detail.IdGood,
                    Quantity = newDetailIdGood,
                    Price = newDetailPrice,
                    IdItem = newDetailItemId,
                    ItemPo = 1,
                };
                detail.IdDocJournal = newDocJournal.Id.ToString();
                newDocJournal.Details.Add( newDetail );
                //abtContext.DocGoodsDetails.Add( newDetail );
            }
            newDocJournal.DocGoods.DocJournal = newDocJournal;
            abtContext.DocJournals.Add( newDocJournal );
            var docJournalTag = new DocJournalTag { IdDoc = newDocJournal.Id, IdTad = 137, TagValue = order.Number };
            abtContext.DocJournalTags.Add(docJournalTag);
            //abtContext.DocGoods.Add( newDocJournal.DocGoods );
            return newDocJournal;
        }

        public List<decimal?> DocExportData(DocOrder order, decimal idSeller, AbtDbContext abtContext = null)
        {
            _log.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);

            if (abtContext == null)
                abtContext = _abt;

            var idFilial = Decimal.Parse(abtContext?.SelectSingleValue($"select id_filial from profiles where profile_gln = '{_usersConfig.SelectedUser.UserGLN}'"));

            var doc = ExportDocWithLineItems(order, abtContext, idFilial, order.DocLineItems, idSeller);
            abtContext.SaveChanges();

            var ids = SplitDoc(doc.Id);
            return ids;
        }

        private List<decimal?> SplitDoc(decimal docId)
        {
            var param = new Oracle.ManagedDataAccess.Client.OracleParameter("ID_DOC", docId);
            param.OracleDbType = Oracle.ManagedDataAccess.Client.OracleDbType.Decimal;

            var ids = _abt.Database.SqlQuery<decimal?>("select distinct id from TABLE(cast(ABT.Split_Doc_EX( :ID_DOC ) as ABT.VT_TMP_REPORTS))", param);
            return ids.ToList();
        }

        private DateTime GetDateTimeForExportOrder(DocOrder order)
        {
            _log.Log(System.Reflection.MethodBase.GetCurrentMethod().Name);

            var dateTime = DateTime.Now;

            try
            {
                if (order.EdiCreationDate != null && order.EdiCreationDate.Value.Date > dateTime)
                {
                    _log.Log($"{System.Reflection.MethodBase.GetCurrentMethod().Name}: получение даты и времени с сервера.");
                    dateTime = _edi.Database.SqlQuery<DateTime?>("select sysdate from dual").FirstOrDefault() ?? DateTime.Now;
                }

                _log.Log($"{System.Reflection.MethodBase.GetCurrentMethod().Name}: дата и время получены успешно - {dateTime.ToString("dd.MM.yyyy HH:mm:ss")}");
            }
            catch(Exception ex)
            {
                dateTime = DateTime.Now;
                _log.Log("Exception: " + _log.GetRecursiveInnerException(ex));
            }
            return dateTime;
        }


        public RelayCommand NextDayCommand => new RelayCommand( (o) => {
			DateFrom = DateFrom.AddDays( 1 );
			DateTo = DateTo.AddDays( 1 );
		} );

		public RelayCommand PrevDayCommand => new RelayCommand( (o) => {
			DateFrom = DateFrom.AddDays( -1 );
			DateTo = DateTo.AddDays( -1 );
		} );

		private DateTime dateFrom = DateTime.Today; // по какую дату загружать документы
		public DateTime DateFrom
		{
			get { return dateFrom; }
			set {
				dateFrom = value;
				OnPropertyChanged( "DateFrom" );
			}
		}

		private DateTime dateTo = DateTime.Now; // с какой даты загружать документы
		public DateTime DateTo
		{
			get { return dateTo; }
			set {
				dateTo = value;
				OnPropertyChanged( "DateTo" );
			}
		}

        public List<RefCompany> CommertialNetworks { get; set; } = new List<RefCompany>();

        public List<string> SelectedNetworks { get; set; } = null;

        public Action<List<string>> SetInitialCommertialNetworkValues;

        private void DeleteLine()
        {
            if(SelectedDocLineItem == null)
            {
                System.Windows.MessageBox.Show( "Ошибка! Не выбран пункт в составе заказа!", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error );
                return;
            }

            SelectedDocLineItem.IsRemoved = "1";
            _edi.SaveChanges();

            DocLineItems = SelectedItem?.DocLineItems?.Where( d => d.IsRemoved != "1" )?.ToList();
            UpdateProps();
        }

        private void DeleteOrder()
        {
            if (SelectedItem == null)
            {
                System.Windows.MessageBox.Show("Не выбран заказ для удаления!", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if ((!_usersConfig.IsMainAccount) && _usersConfig.SelectedUser.UserGLN != _edi?.RefShoppingStores?.FirstOrDefault(r => r.BuyerGln == SelectedItem.GlnBuyer)?.MainGln)
            {
                System.Windows.MessageBox.Show("У пользователя нет прав на удаление заказов данной торговой сети!", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (System.Windows.MessageBox.Show("Вы действительно хотите удалить заказ?" +
                        $"\nДанную операцию нельзя будет отменить.", "Внимание",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Question) == System.Windows.MessageBoxResult.Yes)
            {
                SelectedItem.OrderDate = null;
                SelectedItem.ReqDeliveryDate = null;
                _edi.SaveChanges();

                ItemsList.Remove(SelectedItem);
                SelectedItem = null;
                OnPropertyChanged("SelectedItem");
                OnPropertyChanged("ItemsList");
            }
        }

        private void OpenDeliveryPoints()
        {
            if (_errorConnectToDb)
            {
                ShowError("Отсутствует подключение к БД EDI.\nНастройте подключение, и повторите попытку.");
                return;
            }

            var deliveryPointsWindow = new DeliveryPointsWindow(_edi, _usersConfig);
            deliveryPointsWindow.Show();
            ((DeliveryPointsModel)deliveryPointsWindow.DataContext).SetOwnerForDownloadWindow();
        }

        private void UpdateSettings()
        {
            var settingsWindow = new SettingsWindow(_usersConfig);
            settingsWindow.ShowDialog();

            if (settingsWindow.IsSavedSettings)
            {
                var restart = 
                    System.Windows.MessageBox.Show(
                        "Чтобы изменения вступили в силу, необходимо перезапустить программу.\nПерезапустить сейчас?", 
                        "Перезапуск", 
                        System.Windows.MessageBoxButton.YesNo, 
                        System.Windows.MessageBoxImage.Question) == System.Windows.MessageBoxResult.Yes;

                if(restart)
                {
                    System.Diagnostics.Process.Start(System.Windows.Application.ResourceAssembly.Location);
                    System.Windows.Application.Current.Shutdown();
                }
            }
        }

        private void OpenJuridicalEntities()
        {
            var openJuridicalEntitiesWindow = new JuridicalEntitiesWindow();
            var openJuridicalEntitiesModel = new JuridicalEntitiesModel(_edi);
            openJuridicalEntitiesWindow.DataContext = openJuridicalEntitiesModel;
            openJuridicalEntitiesWindow.Show();
        }

        private void Save()
        {
            try
            {
                _edi.SaveChanges();
            }
            catch (Exception ex)
            {
                _log.Log(ex);
                ShowError(_log.GetRecursiveInnerException(ex));
                var ediErr = _edi.GetValidationErrors();
            }
        }

        private void SummWithNds()
        {
            if(SelectedItem == null)
            {
                System.Windows.MessageBox.Show( "Ошибка! Не выбран заказ для расчёта!", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error );
                return;
            }

            if(!string.IsNullOrEmpty(SelectedItem?.TotalAmount))
            {
                System.Windows.MessageBox.Show( "Ошибка! Сумма с НДС для данного заказа уже рассчитана!", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error );
                return;
            }

            double sum = 0.0;

            foreach(var l in SelectedItem.DocLineItems)
            {
                sum += Convert.ToDouble(l.Amount);
            }

            SelectedItem.TotalAmount = sum.ToString();
            Refresh();
        }

        private void UpdateProps()
		{
			OnPropertyChanged( nameof( ItemsList ) );
			OnPropertyChanged( nameof( SelectedItem ) );
			OnPropertyChanged( nameof( SelectedDocLineItem ) );
            OnPropertyChanged("PermittedToMatchingGoods");
        }

	}
}
