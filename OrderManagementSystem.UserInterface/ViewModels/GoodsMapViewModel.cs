using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DataContextManagementUnit.DataAccess.Contexts.Abt;
using DataContextManagementUnit.DataAccess.Contexts.Edi;
using OrderManagementSystem.UserInterface.ViewModels.Implementations;
using FileWorker;

namespace OrderManagementSystem.UserInterface.ViewModels
{
	public class GoodsMapViewModel : ListViewModel<MapGood>
	{
		public override RelayCommand RefreshCommand => new RelayCommand( (o) => { Refresh(); } );
		public override RelayCommand DeleteCommand => new RelayCommand( (o) => { Delete(); } );
		public RelayCommand SaveCommand => new RelayCommand( (o) => { Save(); } );
		public RelayCommand CreateNewCommand => new RelayCommand( (o) => { CreateNew(); } );
        public RelayCommand ImportFromFileCommand => new RelayCommand((o) => { ImportFromFile(); });
		
		public ObservableCollection<DTO_RefGoods> RefGoodList { get; set; } = new ObservableCollection<DTO_RefGoods>();
		public DTO_RefGoods SelectedRefGood { get; set; } = new DTO_RefGoods();

		public List<RefCompany> Companies { get; set; }
        public RefCompany SelectedCompany { get; set; }

		private void DefautInit(AbtDbContext AbtDbContext, EdiDbContext EdiDbContext, EdiProcessingUnit.UsersConfig usersConfig)
		{
			_log.Log( System.Reflection.MethodBase.GetCurrentMethod().Name );
			_abt = AbtDbContext;
			_edi = EdiDbContext;

			RefGoodList = new ObservableCollection<DTO_RefGoods>(
				_abt.Database
					.SqlQuery<DTO_RefGoods>( DTO_RefGoods.Sql )
					.ToList() );

            List<string> Glns;

            if(usersConfig.IsMainAccount)
                Glns = _edi?.ConnectedBuyers?.Select( c => c.Gln )?.ToList();
            else
                Glns = _edi?.ConnectedBuyers?.Where(c => c.PermittedToMatchingGoods == 1)?.Select(c => c.Gln)?.ToList() ?? new List<string>();
            Companies = _edi.RefCompanies.Where( r => Glns.FirstOrDefault( g => r.Gln == g ) != null ).ToList();

			ItemsList = new ObservableCollection<MapGood>();
			UpdateProps();


		}

		public GoodsMapViewModel(AbtDbContext AbtDbContext, EdiDbContext EdiDbContext, EdiProcessingUnit.UsersConfig usersConfig)
		{
			DefautInit( AbtDbContext, EdiDbContext, usersConfig);

            if(!usersConfig.IsMainAccount)
            {
                Companies = Companies?.Where(r => usersConfig.Users.Exists(u => r.Gln == u.UserGLN))?.ToList() ?? new List<RefCompany>();
            }
		}

        public void GlnChangedEvent()
        {
            Refresh();
        }
		
		public class DTO_RefGoods
		{
			public Decimal Id { get; set; }
			public string Code { get; set; }
			public string Name { get; set; }
			public string Bar_Code { get; set; }
			public string Good_Size { get; set; }
			public string Manufacturer { get; set; }
			public Decimal Id_Manufacturer { get; set; }

			public static string Sql =>
@"SELECT rg.id, rg.code, rg.name, rbc.bar_code, rg.GOOD_SIZE,
(SELECT name FROM ABT.REF_CONTRACTORS WHERE ID = rg.id_manufacturer) MANUFACTURER,
rg.ID_MANUFACTURER, rbc.IS_PRIMARY
FROM abt.ref_goods rg, ABT.REF_BAR_CODES RBC
WHERE RBC.id_good = rg.id AND rbc.IS_PRIMARY = 0 AND RBC.ID_GOOD IN (SELECT id_object
FROM REF_GROUP_ITEMS WHERE ID_PARENT IN (SELECT id FROM ABT.REF_GROUPS CONNECT BY PRIOR id = id_parent START WITH ID = 485596300))";
		}
		
		private void Save()
		{
			if (SelectedItem == null)
			{
				return;
			}

            if (SelectedCompany == null)
                return;

			_log.Log( System.Reflection.MethodBase.GetCurrentMethod().Name );

            if (SelectedRefGood != null)
            {
                var idGood = SelectedRefGood.Id;
                SelectedItem.IdGood = idGood;
            }

            var glns = _edi?.RefShoppingStores?
                .Where(r => r.MainGln == SelectedCompany.Gln)?
                .Select(s => s.BuyerGln)?.ToList();
            List<DocOrder> unprocessedDocs = _edi.DocOrders.Where( x => x.Status == 0 && glns.FirstOrDefault(g => g == x.GlnBuyer) != null ).ToList();
            List<long> addedMapGoodsManufacturers = new List<long>();
            foreach(var doc in unprocessedDocs)
                foreach(var item in doc.DocLineItems.Where( i => i.Gtin == SelectedItem.BarCode ))
                {
                    decimal? idManufacturer = null;
                    if (SelectedRefGood != null)
                    {
                        item.IdGood = (long?)SelectedRefGood.Id;
                        item.Manufacturer = SelectedRefGood.Manufacturer;
                        idManufacturer = SelectedRefGood.Id_Manufacturer;
                    }
                    else
                    {
                        item.IdGood = (long?)SelectedItem.IdGood;

                        if(item.IdGood != null && item.IdGood != 0)
                        {
                            var idGood = item.IdGood;
                            var refGood = _abt?.RefGoods?.FirstOrDefault(r => r.Id == idGood);
                            item.Manufacturer = refGood?.Manufacturer?.Name;
                            idManufacturer = refGood?.Manufacturer?.Id;
                        }
                    }

                    if (idManufacturer == null || item.IdGood == null)
                        continue;

                    if (idManufacturer <= 0 || item.IdGood <= 0)
                        continue;

                    if (addedMapGoodsManufacturers.
                        Exists(d => d == item.IdGood))
                        continue;

                    var mapManufacturerGood = _edi?
                        .MapGoodsManufacturers?
                        .FirstOrDefault(m => m.IdGood == item.IdGood);

                    if(mapManufacturerGood == null)
                    {
                        mapManufacturerGood = new MapGoodManufacturer()
                        {
                            IdGood = (decimal)item.IdGood,
                            IdManufacturer = idManufacturer,
                            Name = item.Manufacturer
                        };

                        _edi?.MapGoodsManufacturers?.Add(mapManufacturerGood);
                        addedMapGoodsManufacturers.Add(item.IdGood ?? 0);
                    }
                    else
                    {
                        mapManufacturerGood.IdManufacturer = idManufacturer;
                        mapManufacturerGood.Name = item.Manufacturer;
                    }
                }

			_edi.SaveChanges();
			Refresh();

		}

		private void CreateNew()
		{
			_log.Log( System.Reflection.MethodBase.GetCurrentMethod().Name );
            var newItem = new MapGood() {
                Id = Guid.NewGuid().ToString(),
                BarCode = "",
                IdGood = null,
                Name = "",
                MapGoodByBuyers = new List<MapGoodByBuyer>()
			};

            _edi.MapGoods.Add( newItem );

            var newGlnItemGood = new MapGoodByBuyer() {
                Gln = SelectedCompany?.Gln,
                IdMapGood = newItem.Id,
                MapGood = newItem
            };

            newItem.MapGoodByBuyers.Add( newGlnItemGood );

            ItemsList.Add( newItem );

			SelectedItem = newItem;
			UpdateProps();
		}

        private void ImportFromFile()
        {
            if(SelectedCompany == null)
            {
                System.Windows.MessageBox.Show("Не выбрана торговая сеть для добавления товаров!", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog();

                openFileDialog.Filter = "Excel Files|*.xlsx;*.xls";

                if (openFileDialog.ShowDialog() == true)
                {
                    ExcelColumnCollection columnCollection = new ExcelColumnCollection();

                    columnCollection.AddColumn("Name", "Наименование");
                    columnCollection.AddColumn("IdGood", "Код товара");
                    columnCollection.AddColumn("BarCode", "Штрих-код");

                    var doc = new ExcelDocumentData(columnCollection);

                    List<ExcelDocumentData> docs = new List<ExcelDocumentData>(new ExcelDocumentData[] {
                        doc
                    });

                    ExcelFileWorker worker = new ExcelFileWorker(openFileDialog.FileName, docs);

                    worker.ImportData<MapGood>();

                    bool isAddedMapGoods = false;

                    foreach (var m in doc.Data)
                    {
                        var map = (MapGood)m;

                        if (ItemsList?.ToList()?.Exists(l => l.IdGood == map.IdGood && l.BarCode == map.BarCode) ?? false)
                            continue;

                        var mapGood = ItemsList.FirstOrDefault(l => l.IdGood == null && l.BarCode == map.BarCode);

                        if(mapGood != null)
                        {
                            mapGood.IdGood = map.IdGood;
                            isAddedMapGoods = true;
                            continue;
                        }

                        map.Id = Guid.NewGuid().ToString();
                        map.MapGoodByBuyers = new List<MapGoodByBuyer>();
                        _edi.MapGoods.Add(map);
                        isAddedMapGoods = true;

                        var mapGoodByBuyer = new MapGoodByBuyer()
                        {
                            IdMapGood = map.Id,
                            Gln = SelectedCompany.Gln,
                            MapGood = map
                        };

                        map.MapGoodByBuyers.Add(mapGoodByBuyer);
                    }

                    if(isAddedMapGoods)
                        _edi.SaveChanges();
                }
            }
            catch(Exception ex)
            {
                System.Windows.MessageBox.Show($"Возникла ошибка загрузки товаров.\n {ex.Message}", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                _log.Log($"Ошибка загрузки товаров.\n Exception: {ex.Message}\n Inner Exception: {_log.GetRecursiveInnerException(ex)}");
            }
        }


		private void Delete()
		{
			_log.Log( System.Reflection.MethodBase.GetCurrentMethod().Name );

            var mapGoodsByBuyers = SelectedItem?.MapGoodByBuyers?.ToList() ?? new List<MapGoodByBuyer>();
            foreach (var g in mapGoodsByBuyers)
            {
                SelectedItem.MapGoodByBuyers.Remove( g );
            }

			_edi.MapGoods.Remove( SelectedItem );
			_edi.SaveChanges();
			Refresh();
		}

		private void Refresh()
		{
			_log.Log( System.Reflection.MethodBase.GetCurrentMethod().Name );

            if (SelectedCompany != null)
            {
                List<MapGood> map = _edi.MapGoodsByBuyers?
                    .Where(m => m.Gln == SelectedCompany.Gln)?
                    .Select(m => m.MapGood)?
                    .ToList() 
                    ?? new List<MapGood>();
                ItemsList = new ObservableCollection<MapGood>( map );
            }
			UpdateProps();
		}


		private void UpdateProps()
		{
			OnPropertyChanged( nameof( ItemsList ) );
			OnPropertyChanged( nameof( SelectedItem ) );
			OnPropertyChanged( nameof( RefGoodList ) );
			OnPropertyChanged( nameof( SelectedRefGood ) );
            OnPropertyChanged( nameof( Companies ) );
            OnPropertyChanged( nameof( SelectedCompany ) );
		}
	}
}
