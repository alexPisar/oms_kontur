using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OrderManagementSystem.UserInterface.ViewModels;
using System.Collections.ObjectModel;
using OrderManagementSystem.UserInterface.ViewModels.Implementations;
using DataContextManagementUnit.DataAccess.Contexts.Edi;
using FileWorker;

namespace OrderManagementSystem.UserInterface.ViewModels
{
    public class JuridicalEntitiesModel : ListViewModel<RefShoppingStore>
    {
        private EdiDbContext _edi;
        private List<RefCompany> _companies = new List<RefCompany>();
        private List<RefShoppingStore> _shoppingStores;

        public override RelayCommand RefreshCommand => new RelayCommand((o) => { Refresh(); });
        public override RelayCommand EditCommand => new RelayCommand((o) => { Edit(); });
        public override RelayCommand CreateNewCommand => new RelayCommand((o) => { CreateNew(); });
        public override RelayCommand DeleteCommand => new RelayCommand((o) => { Delete(); });
        public RelayCommand ImportDataCommand => new RelayCommand((o) => { ImportData(); });

        public RefCompany SelectedBuyer { get; set; }
        public List<RefCompany> Buyers { get; set; }

        public RefCompany SelectedJuridicalEntity { get; set; }
        public List<RefCompany> JuridicalEntities { get {
                return _companies?
                .Where(c => ItemsList.FirstOrDefault(i => i.BuyerGln == c.Gln) != null)?
                .ToList();
            } }

        public JuridicalEntitiesModel(EdiDbContext edi)
        {
            _edi = edi;
            Refresh();
        }

        public void ChangedBuyer()
        {
            if (SelectedBuyer == null)
                return;

            ItemsList = new ObservableCollection<RefShoppingStore>(_shoppingStores.Where(s => s.MainGln == SelectedBuyer.Gln));
            UpdateProperties();
        }

        public override void Refresh()
        {
            _companies = _edi?.RefCompanies?.ToList();
            _shoppingStores = _edi?.RefShoppingStores?.ToList();
            ItemsList = new ObservableCollection<RefShoppingStore>();

            var glns = _edi?.ConnectedBuyers?.Select(b => b.Gln)?.ToList() ?? new List<string>();
            Buyers = _companies?
                .Where(c => glns.FirstOrDefault(g => g == c.Gln) != null)?
                .ToList();

            SelectedBuyer = null;

            UpdateProperties();
        }

        public override void Edit()
        {
            _edi.SaveChanges();
        }

        public override void CreateNew()
        {
            if (SelectedBuyer == null)
            {
                DevExpress.Xpf.Core.DXMessageBox.Show("Не выбрана торговая сеть.",
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            var selectedWindow = new SelectedCompanyWindow();
            var selectedModel = new SelectedCompanyModel(_companies);
            selectedWindow.DataContext = selectedModel;

            if (selectedWindow.ShowDialog() == true)
            {
                if(_shoppingStores.Exists(s => s.BuyerGln == selectedModel.SelectedItem.Gln))
                {
                    DevExpress.Xpf.Core.DXMessageBox.Show("Юр. лицо с таким GLN уже есть.",
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                var newEntity = new RefShoppingStore
                {
                    MainGln = SelectedBuyer.Gln,
                    BuyerGln = selectedModel.SelectedItem.Gln
                };

                _edi.RefShoppingStores.Add(newEntity);
                _shoppingStores.Add(newEntity);
                ItemsList.Add(newEntity);
                UpdateProperties();
            }
        }

        public override void Delete()
        {
            if (SelectedBuyer == null)
            {
                DevExpress.Xpf.Core.DXMessageBox.Show("Не выбрана торговая сеть.", 
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (SelectedJuridicalEntity == null)
            {
                DevExpress.Xpf.Core.DXMessageBox.Show("Не выбрано юр.лицо для удаления.",
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            var dialogResultDeleted = System.Windows.MessageBox.Show("Вы точно хотите удалить выбранное юридическое лицо? \nДанную операцию нельзя будет отменить.",
                "Внимание", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);

            if(dialogResultDeleted == System.Windows.MessageBoxResult.Yes)
            {
                var selectedEntity = ItemsList.FirstOrDefault(s => s.MainGln == SelectedBuyer.Gln 
                && s.BuyerGln == SelectedJuridicalEntity.Gln);

                if (selectedEntity != null)
                {
                    _edi.RefShoppingStores.Remove(selectedEntity);
                    _shoppingStores.Remove(selectedEntity);
                    ItemsList.Remove(selectedEntity);
                    UpdateProperties();
                }
            }
        }

        private void ImportData()
        {
            if (SelectedBuyer == null)
            {
                DevExpress.Xpf.Core.DXMessageBox.Show("Не выбрана торговая сеть.",
                    "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog();

                openFileDialog.Filter = "Excel Files|*.xlsx;*.xls";

                if (openFileDialog.ShowDialog() == true)
                {
                    ExcelColumnCollection columnCollection = new ExcelColumnCollection();

                    columnCollection.AddColumn("BuyerGln", "ГЛН");

                    var doc = new ExcelDocumentData(columnCollection);

                    List<ExcelDocumentData> docs = new List<ExcelDocumentData>(new ExcelDocumentData[] {
                        doc
                    });

                    ExcelFileWorker worker = new ExcelFileWorker(openFileDialog.FileName, docs);

                    worker.ImportData<RefShoppingStore>();

                    if(Array.Exists(doc.Data.Cast<RefShoppingStore>().ToArray(), p => GetGln(p.BuyerGln).Length != 13))
                        throw new Exception("Данные не соответствуют представлению ГЛН.");

                    foreach (RefShoppingStore d in doc.Data)
                    {
                        d.BuyerGln = GetGln(d.BuyerGln);

                        if (_shoppingStores.Exists(s => s.BuyerGln == d.BuyerGln))
                            continue;

                        d.MainGln = SelectedBuyer.Gln;
                        _edi.RefShoppingStores.Add(d);
                        _shoppingStores.Add(d);
                        ItemsList.Add(d);
                    }

                    UpdateProperties();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Возникла ошибка загрузки компаний.\n {ex.Message}", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                _log.Log($"Ошибка загрузки товаров.\n Exception: {ex.Message}\n Inner Exception: {_log.GetRecursiveInnerException(ex)}");
            }
        }

        private string GetGln(string str)
        {
            var stringBuilder = new StringBuilder();
            var bytes = Encoding.UTF8.GetBytes(str);

            foreach (var b in bytes)
            {
                if (b < 48 || b > 57)
                    continue;

                char ch = Encoding.UTF8.GetChars(new byte[] { b }).First();
                stringBuilder.Append(ch);
            }

            return stringBuilder.ToString();
        }

        private void UpdateProperties()
        {
            OnPropertyChanged("ItemsList");
            OnPropertyChanged("JuridicalEntities");
            OnPropertyChanged("SelectedJuridicalEntity");
            OnPropertyChanged("Buyers");
            OnPropertyChanged("SelectedBuyer");
        }
    }
}
