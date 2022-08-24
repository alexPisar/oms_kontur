using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileWorker;
using DataContextManagementUnit.DataAccess.Contexts.Abt;
using DataContextManagementUnit.DataAccess.Contexts.Edi;
using OrderManagementSystem.UserInterface.ViewModels.Implementations;

namespace OrderManagementSystem.UserInterface.ViewModels
{
	public class OrderSingleViewModel : EditViewModel<DocOrder>
	{
        private ConnectedBuyers _connectedBuyer;

        public List<DocJournal> DocJournalList { get; set; } = new List<DocJournal>();
		public RelayCommand DocLineItemSelectionChangedCommand => new RelayCommand( (o) => { UpdateProps(); } );

		public RelayCommand DeleteLogCommand => new RelayCommand( (o) => 
		{
			_edi.LogOrders.Remove( SelectedLogItem );
			_edi.SaveChanges();
			Refresh();
		} );

        public RelayCommand RefreshCommand => new RelayCommand((o) => { RefreshOrdersList(); });
        public RelayCommand DeleteOrderCommand => new RelayCommand((o) => { DeleteOrder(); });

        public bool IsDeleteOrderButtonEnabled => Item.Status == 1;
        public bool IsDocumentShipped => Item.Status >= 3;

        private LogOrder _selectedLogItem;
		public LogOrder SelectedLogItem
		{
			get => _selectedLogItem;
			set {
				if (value != null)
				{
					_selectedLogItem = value;
					OnPropertyChanged( nameof( SelectedLogItem ) );
				}
			}
		}
		
		private DocJournal _selectedDocJournal;
		public DocJournal SelectedDocJournal
		{
			get => _selectedDocJournal;
			set {
				if (value != null)
				{
					_selectedDocJournal = value;
					OnPropertyChanged( nameof( SelectedDocJournal ) );
				}
			}
		}

        private List<DocLineItem> _itemDocLines;
        public List<DocLineItem> ItemDocLines
        {
            get 
            {
                return _itemDocLines;
            }
            set 
            {
                _itemDocLines = value;
            }
        }

        public double TotalQuantity { get; set; }

        public OrderSingleViewModel(AbtDbContext AbtDbContext, EdiDbContext EdiDbContext, DocOrder Item)
		{
			_log.Log( System.Reflection.MethodBase.GetCurrentMethod().Name );
			_abt = AbtDbContext;
			_edi = EdiDbContext;
			this.Item = Item;
            ItemDocLines = Item?.DocLineItems?.Where(i => i.IsRemoved != "1")?.ToList() ?? new List<DocLineItem>();

            TotalQuantity = 0.0;
            if (Item != null)
            {
                foreach (var line in Item.DocLineItems)
                {
                    if (line.IsRemoved == "1")
                        continue;

                    double quality;
                    double.TryParse( line.ReqQunatity, out quality );
                    TotalQuantity += quality;
                }

                _connectedBuyer = _edi?
                    .RefShoppingStores?
                    .FirstOrDefault(r => r.BuyerGln == Item.GlnBuyer)?
                    .MainShoppingStore;
            }

			Refresh();
		}

        public void ExportData()
        {
            _log.Log( $"Экспорт данных заказа." );
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog();

                saveFileDialog.Filter = "Excel Files(.xlsx)|*.xlsx";

                if(saveFileDialog.ShowDialog() == true)
                {
                    ExcelColumnCollection columnCollection = new ExcelColumnCollection();
                    string sheetName = "Лист1";

                    columnCollection.AddColumn( "Description", "Описание", ExcelType.String );
                    columnCollection.AddColumn( "IdGood", "ID товара", ExcelType.String );
                    columnCollection.AddColumn( "Gtin", "Штрих - код", ExcelType.String );
                    columnCollection.AddColumn( "ReqQunatity", "Количество", ExcelType.Double );
                    columnCollection.AddColumn( "NetPrice", "Стоимость единицы товара", ExcelType.Double );
                    columnCollection.AddColumn( "NetPriceVat", "Стоимость с НДС", ExcelType.Double );
                    columnCollection.AddColumn( "Amount", "Сумма с НДС", ExcelType.Double );

                    var docLinesData = new ExcelDocumentData( columnCollection, _itemDocLines.ToArray() );
                    docLinesData.SheetName = sheetName;

                    double totalAmount = 0.0, totalNetAmount = 0.0;

                    foreach(var line in _itemDocLines)
                    {
                        double amount, netAmount;
                        double.TryParse( line.Amount, out amount );
                        double.TryParse( line.NetAmount, out netAmount );

                        totalAmount += amount;
                        totalNetAmount += netAmount;
                    }

                    var result = new DocLineItem[] {
                        new DocLineItem() {
                            Description = "Итог:",
                            Amount = totalAmount.ToString(),
                            NetAmount = totalNetAmount.ToString(),
                            ReqQunatity = TotalQuantity.ToString()
                        }
                    };

                    _log.Log( $"инициализирован итог: сумма {result?.First()?.Amount}, количество {result?.First()?.ReqQunatity}" );

                    var resultData = new ExcelDocumentData( columnCollection, result );
                    resultData.SheetName = sheetName;
                    resultData.ExportColumnNames = false;

                    var docs = new List<ExcelDocumentData>();
                    docs.Add( docLinesData );
                    docs.Add( resultData );

                    var worker = new ExcelFileWorker( saveFileDialog.FileName, docs );

                    worker.ExportRow( 
                        $"Номер заказа: {Item?.Number}, " +
                        $"Дата доставки: {Item?.ReqDeliveryDate}, " +
                        $"Дата получения: {Item?.EdiCreationSenderDate}", sheetName );

                    worker.ExportRow(
                        $"Отправитель:  {Item?.Sender?.Name}, " +
                        $"Получатель:  {Item?.ShipTo?.Name}", sheetName );

                    worker.ExportData();
                    worker.SaveFile();

                    LoadWindow loadWindow = new LoadWindow();
                    LoadModel loadContext = new LoadModel();
                    loadWindow.DataContext = loadContext;

                    loadWindow.SetSuccessFullLoad( loadContext, "Заказ успешно выгружен." );
                    loadWindow.Show();
                }
            }
            catch (Exception ex)
            {
                ShowError( _log.GetRecursiveInnerException( ex ) );
                _log.Log( "При экспорте данных произошла ошибка: " + _log.GetRecursiveInnerException( ex ) );
            }
        }

        public void ResentShippingDocuments()
        {
            try
            {
                if (Item == null)
                    throw new Exception("Ошибка, не определён выбранный заказ.");

                if (_connectedBuyer == null)
                    throw new Exception("Ошибка, не определёна сеть для отправки отгрузки.");

                var logOrders = Item?.LogOrders?.Where(l => l.OrderStatus >= 3 && l.IdDocJournal != null) ?? new List<LogOrder>();

                if(logOrders.Count() == 0)
                {
                    System.Windows.MessageBox.Show("Для данного заказа нет отправленных отгрузок.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                foreach (var log in logOrders)
                    log.IdDocJournal = null;

                if (_connectedBuyer.OrderExchangeType ==
                    (int)DataContextManagementUnit.DataAccess.OrderTypes.OrdersOrdrsp)
                    Item.Status = 2;
                else
                    Item.Status = 1;

                _edi.SaveChanges();

                LoadWindow loadWindow = new LoadWindow();
                LoadModel loadContext = new LoadModel();
                loadWindow.DataContext = loadContext;

                loadWindow.SetSuccessFullLoad(loadContext, "В течение 15 минут документы отправятся.");
                loadWindow.Show();

                OnPropertyChanged(nameof(IsDeleteOrderButtonEnabled));
                OnPropertyChanged(nameof(IsDocumentShipped));
            }
            catch(Exception ex)
            {
                ShowError(_log.GetRecursiveInnerException(ex));
                _log.Log("При изменении статуса документов для повторной отправки отгрузки возникла ошибка: " + _log.GetRecursiveInnerException(ex));
            }
        }

		private void Refresh()
		{
			try
			{
				var logs = Item.LogOrders
					.Where( log => log.OrderStatus == 1 && log.IdDocJournal != null )
					.ToList();
				foreach (var log in logs)
				{
                    if (log?.IdDocJournal == null)
                        continue;
					var doc = _abt.DocJournals?
						.SingleOrDefault( x => log.IdDocJournal == x.Id );
					if (doc != null)
						DocJournalList.Add( doc );
				}

			}
			catch (Exception ex)
			{
				ShowError( ex.Message );
			}			
			UpdateProps();
		}

        private void RefreshOrdersList()
        {
            DocJournalList = new List<DocJournal>();
            Item = _edi.DocOrders.FirstOrDefault(d => d.Id == Item.Id && d.Number == Item.Number);
            _selectedDocJournal = null;
            Refresh();
        }

        private void DeleteOrder()
        {
            if(SelectedDocJournal == null)
            {
                System.Windows.MessageBox.Show("Не выбран трейдер-документ для удаления!", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            var dialogResultDeleted = System.Windows.MessageBox.Show(
                "Вы точно хотите убрать этот трейдер-документ из списка? \nДанную операцию нельзя будет отменить.", "Внимание", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);

            if(dialogResultDeleted == System.Windows.MessageBoxResult.Yes)
            {
                var logs = Item.LogOrders
                    .Where(log => log.OrderStatus == 1 && log.IdDocJournal == SelectedDocJournal.Id)
                    .ToList();

                foreach(var log in logs)
                    log.IdDocJournal = null;

                DocJournalList.Remove(SelectedDocJournal);
                _selectedDocJournal = null;

                if (DocJournalList.Count > 0 && !DocJournalList.Exists(d =>
                !Item.LogOrders.Exists(l => l.IdDocJournal == d.Id && l.OrderStatus >= 3)) &&
                Item.Status < 3)
                    Item.Status = 3;

                _edi.SaveChanges();
                OnPropertyChanged( nameof( IsDeleteOrderButtonEnabled ) );
                UpdateProps();
            }
        }


		private void UpdateProps()
		{
			OnPropertyChanged( nameof( SelectedLogItem ) );
			OnPropertyChanged( nameof( DocJournalList ) );
			OnPropertyChanged( nameof( SelectedDocJournal ) );
			OnPropertyChanged( nameof( Item ) );
		}

	}
}
