using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataContextManagementUnit.DataAccess.Contexts.Abt;
using System.Collections.ObjectModel;
using KonturEdoClient.Models.Implementations;
using FileWorker;

namespace KonturEdoClient.Models
{
    public class GoodsMatchingModel : Base.ListViewModel<RefGoodMatching>
    {
        private decimal? _idFilial = null;
        private AbtDbContext _abt;
        private DTO_RefGoods _selectedRefGood;
        private EdiProcessingUnit.UsersConfig _config;
        private List<EdiProcessingUnit.Edo.Models.UniversalTransferDocument> _docs;

        public Action SaveAction;
        public bool PermissionChannelsList { get; set; }
        public bool PermissionChannelsSettings { get; set; }
        public bool AccountWithChoicesOfFilials => (Filials?.Count ?? 0) > 1;
        public List<EdiProcessingUnit.User> Filials { get; set; } = null;

        public List<RefEdoGoodChannel> EdoGoodChannels { get; set; }
        public RefEdoGoodChannel SelectedEdoGoodChannel { get; set; }

        public List<DTO_RefGoods> RefGoods { get; set; }
        public DTO_RefGoods SelectedRefGood
        {
            get {
                return _selectedRefGood;
            }
            set {
                if(SelectedItem != null)
                    _selectedRefGood = value;
                else
                    _selectedRefGood = null;

                OnPropertyChanged("SelectedRefGood");
            }
        }

        public RelayCommand LoadCommand => new RelayCommand((o) => DefaultInit());
        public RelayCommand ImportFromFileCommand => new RelayCommand((o) => { ImportFromFile(); });
        public RelayCommand OpenChannelsListCommand => new RelayCommand((o) => { OpenChannelsList(); });

        public GoodsMatchingModel(AbtDbContext abt, EdiProcessingUnit.UsersConfig config, List<EdiProcessingUnit.Edo.Models.UniversalTransferDocument> docs)
        {
            _abt = abt;
            _config = config;
            _docs = docs;

            var idFilialStr = _abt?.Database?.SqlQuery<string>("select const_value from ref_const where id = 0")?.FirstOrDefault();

            if (!string.IsNullOrEmpty(idFilialStr))
            {
                decimal idFilial;

                if(decimal.TryParse(idFilialStr, out idFilial))
                    _idFilial = idFilial;
            }

            ItemsList = new ObservableCollection<RefGoodMatching>();
            UpdateProps();
        }

        private void ImportFromFile()
        {
            if (SelectedEdoGoodChannel == null)
            {
                System.Windows.MessageBox.Show(
                    "Не выбрана сеть.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog();

                openFileDialog.Filter = "Excel Files|*.xlsx;*.xls";

                if (openFileDialog.ShowDialog() == true)
                {
                    ExcelColumnCollection columnCollection = new ExcelColumnCollection();

                    columnCollection.AddColumn("IdGood", "Код товара");
                    columnCollection.AddColumn("CustomerArticle", "Код покупателя");

                    var doc = new ExcelDocumentData(columnCollection);

                    List<ExcelDocumentData> docs = new List<ExcelDocumentData>(new ExcelDocumentData[] {
                        doc
                    });

                    ExcelFileWorker worker = new ExcelFileWorker(openFileDialog.FileName, docs);

                    worker.ImportData<RefGoodMatching>();

                    foreach(var r in doc.Data)
                    {
                        var refGoodMatching = r as RefGoodMatching;

                        if (refGoodMatching == null)
                            continue;

                        if (refGoodMatching.IdGood == null)
                            continue;

                        var goodMatchingFromBase = _abt.RefGoodMatchings.FirstOrDefault(g => g.IdChannel == SelectedEdoGoodChannel.IdChannel && g.IdGood == refGoodMatching.IdGood);

                        if (goodMatchingFromBase != null)
                        {
                            goodMatchingFromBase.Disabled = 0;
                            goodMatchingFromBase.DisabledDatetime = null;
                            goodMatchingFromBase.CustomerArticle = refGoodMatching.CustomerArticle;
                            continue;
                        }

                        refGoodMatching.InsertDatetime = DateTime.Now;
                        refGoodMatching.InsertUser = UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser;
                        refGoodMatching.IdChannel = SelectedEdoGoodChannel.IdChannel;
                        refGoodMatching.Disabled = 0;

                        string idStr = _abt.SelectSingleValue("SELECT ABT.SEQ_GOODS_MATCHING.NEXTVAL FROM dual");
                        decimal idDec = 0;

                        if (decimal.TryParse(idStr, out idDec))
                            refGoodMatching.Id = idDec;

                        _abt.RefGoodMatchings.Add(refGoodMatching);
                    }
                    _abt.SaveChanges();

                    var loadContext = new LoadModel();
                    var loadWindow = new LoadWindow();
                    loadWindow.DataContext = loadContext;
                    loadWindow.SetSuccessFullLoad(loadContext, "Импорт успешен.");
                    loadWindow.ShowDialog();
                }
                Refresh();
            }
            catch(Exception ex)
            {
                var errorWindow = new ErrorWindow(
                       "Произошла ошибка испорта из файла.",
                       new List<string>(
                           new string[]
                           {
                                    ex.Message,
                                    ex.StackTrace
                           }
                           ));

                errorWindow.ShowDialog();
                _log.Log("GoodsMatchingModel.ImportFromFile Exception: " + _log.GetRecursiveInnerException(ex));
            }
        }

        private void OpenChannelsList()
        {
            if (!PermissionChannelsList)
            {
                System.Windows.MessageBox.Show(
                    "У пользователя нет прав доступа к списку сетей для сопоставления.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            var channelsListModel = new ChannelsListModel(_abt, _config, _idFilial, EdoGoodChannels);
            channelsListModel.Filials = this.Filials;
            channelsListModel.PermissionChannelsSettings = this.PermissionChannelsSettings;

            var channelsListWindow = new ChannelsListWindow();
            channelsListWindow.DataContext = channelsListModel;

            channelsListWindow.ShowDialog();
            DefaultInit();
        }

        public override void CreateNew()
        {
            if (SelectedEdoGoodChannel == null)
            {
                System.Windows.MessageBox.Show(
                    "Не выбрана сеть.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            try
            {
                var newItem = new RefGoodMatching
                {
                    IdChannel = SelectedEdoGoodChannel.IdChannel,
                    IdGood = null,
                    Disabled = 0,
                    CustomerArticle = "",
                    InsertUser = UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser,
                    InsertDatetime = DateTime.Now
                };

                string idStr = _abt.SelectSingleValue("SELECT ABT.SEQ_GOODS_MATCHING.NEXTVAL FROM dual");
                decimal idDec = 0;

                if (decimal.TryParse(idStr, out idDec))
                    newItem.Id = idDec;
                else
                    throw new Exception("Ошибка вычисления ID");

                ItemsList.Add(newItem);
                _abt.RefGoodMatchings.Add(newItem);
                UpdateProps();
            }
            catch(Exception ex)
            {
                var errorWindow = new ErrorWindow(
                       "Произошла ошибка создания сопоставления.",
                       new List<string>(
                           new string[]
                           {
                                    ex.Message,
                                    ex.StackTrace
                           }
                           ));

                errorWindow.ShowDialog();
                _log.Log("GoodsMatchingModel.CreateNew Exception: " + _log.GetRecursiveInnerException(ex));
            }
        }

        public override void Delete()
        {
            if (SelectedEdoGoodChannel == null)
            {
                System.Windows.MessageBox.Show(
                    "Не выбрана сеть.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (SelectedItem == null)
            {
                System.Windows.MessageBox.Show(
                    "Не выбрано сопоставление для удаления.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (System.Windows.MessageBox.Show("Вы действительно хотите удалить сопоставление?" +
                        $"\nДанную операцию нельзя будет отменить.", "Внимание",
                        System.Windows.MessageBoxButton.YesNo,
                        System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
                return;

            try
            {
                SelectedItem.Disabled = 1;
                SelectedItem.DisabledDatetime = DateTime.Now;
                _abt.SaveChanges();
                Refresh();
            }
            catch (Exception ex)
            {
                var errorWindow = new ErrorWindow(
                       "Произошла ошибка удаления сопоставления.",
                       new List<string>(
                           new string[]
                           {
                                    ex.Message,
                                    ex.StackTrace
                           }
                           ));

                errorWindow.ShowDialog();
                _log.Log("GoodsMatchingModel.Delete Exception: " + _log.GetRecursiveInnerException(ex));
            }
        }

        public override void Refresh()
        {
            try
            {
                if (SelectedEdoGoodChannel == null)
                {
                    System.Windows.MessageBox.Show(
                        "Не выбрана сеть.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    return;
                }

                var items = _abt.RefGoodMatchings.Where(r => r.IdChannel == SelectedEdoGoodChannel.IdChannel && r.Disabled == 0);
                ItemsList = new ObservableCollection<RefGoodMatching>(items);

                if(ItemsList.Count > 0)
                    (_abt as System.Data.Entity.Infrastructure.IObjectContextAdapter)?.ObjectContext?
                        .Refresh(System.Data.Entity.Core.Objects.RefreshMode.StoreWins, items);

                SelectedItem = null;
                SelectedRefGood = null;
                UpdateProps();
            }
            catch(Exception ex)
            {
                var errorWindow = new ErrorWindow(
                       "Произошла ошибка обновления списка сопоставлений.",
                       new List<string>(
                           new string[]
                           {
                                    ex.Message,
                                    ex.StackTrace
                           }
                           ));

                errorWindow.ShowDialog();
                _log.Log("GoodsMatchingModel.Refresh Exception: " + _log.GetRecursiveInnerException(ex));
            }
        }

        public override void Save()
        {
            if (_selectedRefGood != null && SelectedItem != null)
                SelectedItem.IdGood = _selectedRefGood.Id;

            if (SelectedEdoGoodChannel != null && SelectedItem?.IdGood != null)
            {
                var docs = _docs.Where(d => d.RefEdoGoodChannel as RefEdoGoodChannel != null && (d.RefEdoGoodChannel as RefEdoGoodChannel)?.IdChannel == SelectedEdoGoodChannel.IdChannel)
                    .Where(d => d.Details.Any(det => det.IdGood == SelectedItem.IdGood));

                foreach (var doc in docs)
                {
                    var detail = doc.Details.First(det => det.IdGood == SelectedItem.IdGood);

                    detail.GoodMatching = SelectedItem;
                }

                SaveAction?.Invoke();
            }

            _abt?.SaveChanges();
        }

        public void DefaultInit()
        {
            try
            {
                if (AccountWithChoicesOfFilials)
                {
                    var idFilials = _abt.Database.SqlQuery<decimal>("select Id from abt.ref_filials where Links in('" + string.Join("', '", Filials.Select(f => f.SID)) + "')");

                    EdoGoodChannels = _abt?.RefEdoGoodChannels?.Where(r => idFilials.Any(i => i == r.IdFilial))?.ToList() ?? new List<RefEdoGoodChannel>();
                }
                else
                    EdoGoodChannels = _abt?.RefEdoGoodChannels?.Where(r => _idFilial == r.IdFilial || r.PermittedForOtherFilials == 1)?.ToList() ?? new List<RefEdoGoodChannel>();

                ItemsList = new ObservableCollection<RefGoodMatching>();

                RefGoods = _abt.Database.SqlQuery<DTO_RefGoods>(DTO_RefGoods.Sql).ToList();

                SelectedEdoGoodChannel = null;
                SelectedItem = null;
                SelectedRefGood = null;
                UpdateProps();
            }
            catch(Exception ex)
            {
                var errorWindow = new ErrorWindow(
                       "Произошла ошибка.",
                       new List<string>(
                           new string[]
                           {
                                    ex.Message,
                                    ex.StackTrace
                           }
                           ));

                errorWindow.ShowDialog();
                _log.Log("DefaultInit Exception: " + _log.GetRecursiveInnerException(ex));
            }
        }

        public void UpdateProps()
        {
            OnPropertyChanged("EdoGoodChannels");
            OnPropertyChanged("SelectedEdoGoodChannel");
            OnPropertyChanged("ItemsList");
            OnPropertyChanged("SelectedItem");
            OnPropertyChanged("RefGoods");
        }

        public class DTO_RefGoods
        {
            public Decimal Id { get; set; }
            public string Code { get; set; }
            public string Name { get; set; }
            public string Good_Size { get; set; }
            public string Manufacturer { get; set; }

            public static string Sql =>
@"SELECT rg.id, rg.code, rg.name, rg.GOOD_SIZE,
(SELECT name FROM ABT.REF_CONTRACTORS WHERE ID = rg.id_manufacturer) MANUFACTURER 
FROM abt.ref_goods rg, ABT.REF_BAR_CODES RBC
WHERE RBC.id_good = rg.id AND rbc.IS_PRIMARY = 0";// AND RBC.ID_GOOD IN (SELECT id_object
//FROM REF_GROUP_ITEMS WHERE ID_PARENT IN (SELECT id FROM ABT.REF_GROUPS CONNECT BY PRIOR id = id_parent START WITH ID = 485596300))";
        }
    }
}
