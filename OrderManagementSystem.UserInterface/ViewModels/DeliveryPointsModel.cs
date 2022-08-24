using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.ObjectModel;
using DataContextManagementUnit.DataAccess.Contexts.Abt;
using DataContextManagementUnit.DataAccess.Contexts.Edi;
using OrderManagementSystem.UserInterface.ViewModels.Implementations;

namespace OrderManagementSystem.UserInterface.ViewModels
{
    public class DeliveryPointsModel : ListViewModel<RefContractor>
    {
        private LoadWindow downloadWindow;
        private EdiProcessingUnit.UsersConfig _usersConfig;
        private long _idSelectedDataBase;

        public override RelayCommand RefreshCommand => new RelayCommand( (o) => { Refresh(); } );
        public override RelayCommand EditCommand => new RelayCommand((o) => { Edit(); } );
        public override RelayCommand DeleteCommand => new RelayCommand((o) => { Delete(); } );
        public RelayCommand SaveCommand => new RelayCommand((o) => { Save(); } );

        public DeliveryPointsModel(EdiDbContext edi, EdiProcessingUnit.UsersConfig usersConfig)
        {
            _edi = edi;
            _usersConfig = usersConfig;
            SetUsersDatabases();
        }

        public List<RefCompany> Companies { get; set; }
        public RefCompany SelectedCompany { get; set; }

        public List<RefCity> Cities { get; set; }
        public RefCity SelectedCity { get; set; }

        public List<RefDistrict> Districts { get; set; }
        public RefDistrict SelectedDistrict { get; set; }

        public List<RefChannel> Channels { get; set; }
        public RefChannel SelectedChannel { get; set; }

        public List<RefFilial> Databases { get; set; }

        public System.Windows.Window Owner { get; set; }
        public bool Enabled { get; set; }

        private void SaveInDataBase(bool saveAbt = true, bool saveEdi = true)
        {
            _log.Log( System.Reflection.MethodBase.GetCurrentMethod().Name );
            try
            {
                if(saveAbt)
                    _abt.SaveChanges();

                if(saveEdi)
                    _edi.SaveChanges();
            }
            catch (Exception ex)
            {
                Exception innerException = ex.InnerException;

                while (innerException.InnerException != null)
                    innerException = innerException.InnerException;

                _log.Log( innerException ?? ex );

                System.Windows.MessageBox.Show( "Произошла ошибка сохранения в базу данных:" + (innerException?.Message ?? ex.Message) + ".", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error );
            }
        }

        private void SetUsersDatabases()
        {
            using (_abt = new AbtDbContext( _usersConfig.GetConnectionString(), true ))
            {
                var usersSids = _usersConfig.Users.Select( u => u.SID );

                string sql = "select Id, Name, Links from ref_filials where id in(select ID_FILIAL from profiles group by ID_FILIAL) " +
                "and Links in('" + string.Join( "', '", usersSids ) + "')";

                Databases = _abt.Database
                .SqlQuery<RefFilial>( sql )
                .ToList();

                OnPropertyChanged( "Databases" );
            }
        }

        public void SetSelectedDataBase(long idSelectedDataBase)
        {
            _idSelectedDataBase = idSelectedDataBase;
        }

        public override void Refresh()
        {
            _log.Log( System.Reflection.MethodBase.GetCurrentMethod().Name );
            SelectedItem = null;
            Enabled = false;
            OnPropertyChanged( "Enabled" );

            var companies = _edi?.RefCompanies;

            string dataSid = Databases.First( d => d.Id == _idSelectedDataBase ).Links;

            string connectionString = null;
            if (_usersConfig.SelectedUser.SID == dataSid)
            {
                connectionString = _usersConfig.GetConnectionString();
            }
            else
            {
                var user = _usersConfig.Users.FirstOrDefault(u => u.SID == dataSid);

                connectionString = _usersConfig.GetConnectionStringByUser(user);
            }

            if (connectionString == null)
                return;

            downloadWindow = new LoadWindow();
            var loadContext = new LoadModel();
            downloadWindow.DataContext = loadContext;

            if(Owner != null)
                downloadWindow.Owner = Owner;

            downloadWindow.ClosingWindow += (object sender, EventArgs e) => 
            {
                Enabled = true;
                OnPropertyChanged( "Enabled" );
            };
            downloadWindow.Show();

            Task downloadItemsTask = new Task( () => {
                try
                {
                    using (_abt = new AbtDbContext( Properties.Settings.Default.FullDataConnectionString, true ))
                    {
                        ItemsList = new ObservableCollection<RefContractor>( _abt.RefContractors?.ToList() );
                    }

                    using (_abt = new AbtDbContext( connectionString, true ))
                    {
                        var items = _abt?.RefContractors?.ToList() ?? new List<RefContractor>();
                        items.AddRange( ItemsList );

                        ItemsList = new ObservableCollection<RefContractor>( items.Distinct(new DataContextManagementUnit.DataAccess.Contexts.RefContractorQualityComparer()) );

                        string sql = "select Id, Name from REF_CITIES";

                        Cities = _abt.Database
                        .SqlQuery<RefCity>( sql )
                        .ToList();

                        sql = "select Id, Name from REF_DISTRICTS";

                        Districts = _abt.Database
                        .SqlQuery<RefDistrict>( sql )
                        .ToList();

                        sql = "select Id, Name from REF_CHANNELS";

                        Channels = _abt.Database
                        .SqlQuery<RefChannel>( sql )
                        .ToList();

                        sql = "select Id, JURIDICAL_ADDRESS as Address, Inn, Name from REF_CUSTOMERS";
                    }

                    Companies = companies?.ToList();
                }
                finally
                {
                    downloadWindow.SetSuccessFullLoad( loadContext );
                }

                OnPropertyChanged( nameof( ItemsList ) );
                OnPropertyChanged( nameof( Companies ) );
                OnPropertyChanged( nameof( Cities ) );
                OnPropertyChanged( nameof( Districts ) );
                OnPropertyChanged( nameof( Channels ) );
            } );

            downloadItemsTask.Start();
        }

        public void SetOwnerForDownloadWindow()
        {
            downloadWindow.Owner = Owner;
        }

        public void Save()
        {
            _log.Log( System.Reflection.MethodBase.GetCurrentMethod().Name );
            SelectedItem.IdCity = SelectedCity?.Id;
            SelectedItem.IdDistrict = SelectedDistrict?.Id;
            SelectedItem.IdChannel = SelectedChannel?.Id;

            SaveInDataBase(false, true);
        }

        public override void Edit()
        {
            _log.Log( System.Reflection.MethodBase.GetCurrentMethod().Name );
            if (SelectedCompany == null)
            {
                System.Windows.MessageBox.Show( "Не выбрана организация с GLN.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error );
                return;
            }

            SelectedCompany.IdContractor = (long?)SelectedItem.Id;
        }

        public override void Delete()
        {
            if(SelectedCompany != null)
            {
                SelectedCompany.IdContractor = null;
            }
        }

        public void SelectedContractorEvent()
        {
            if (SelectedItem != null)
            {
                SelectedCompany = Companies.FirstOrDefault( s => s.IdContractor == SelectedItem.Id );
                OnPropertyChanged( nameof( SelectedItem ) );
            }
            else
            {
                SelectedCompany = null;
            }

            if (SelectedItem != null)
                OnPropertyChanged( nameof( SelectedCompany ) );
        }
    }
}
