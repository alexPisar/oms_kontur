using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using OrderManagementSystem.UserInterface.ViewModels;
using UtilitesLibrary.ConfigSet;

namespace OrderManagementSystem.UserInterface
{
    /// <summary>
    /// Логика взаимодействия для SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        SettingsModel _context;
        private EdiProcessingUnit.UsersConfig _usersConfig;

        public bool IsSavedSettings { get; set; }

        public SettingsWindow(EdiProcessingUnit.UsersConfig usersConfig)
        {
            InitializeComponent();
            IsSavedSettings = false;
            _usersConfig = usersConfig;

            _context = new SettingsModel();
            _context.AccountName = usersConfig?.SelectedUser?.Name;
            _context.AbtDataBaseIpAddress = usersConfig?.SelectedUser?.Host;
            _context.AbtDataBaseSid = usersConfig?.SelectedUser?.SID;
            _context.EdiDataBaseIpAddress = Config.GetInstance().EdiDataBaseIpAddress;
            _context.EdiDataBaseSid = Config.GetInstance().EdiDataBaseSid;
            _context.IsNeedUpdate = Config.GetInstance().IsNeedUpdate;
            _context.SaveWindowSettings = Config.GetInstance().SaveWindowSettings;
            _context.UpdaterFilesLoadReference = Config.GetInstance().UpdaterFilesLoadReference;
            _context.DataBaseUser = Config.GetInstance().GetDataBaseUser();
            DbPasswordBox.Password = Config.GetInstance().GetDataBasePassword();

            DataContext = _context;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _context.OnAllPropertyChanged();
            _usersConfig.SelectedUser.Host = _context.AbtDataBaseIpAddress;
            _usersConfig.SelectedUser.SID = _context.AbtDataBaseSid;
            _usersConfig.Save();

            Config.GetInstance().EdiDataBaseIpAddress = _context.EdiDataBaseIpAddress;
            Config.GetInstance().EdiDataBaseSid = _context.EdiDataBaseSid;
            Config.GetInstance().AbtDataBaseIpAddress = _context.AbtDataBaseIpAddress;
            Config.GetInstance().AbtDataBaseSid = _context.AbtDataBaseSid;
            Config.GetInstance().IsNeedUpdate = _context.IsNeedUpdate;
            Config.GetInstance().SaveWindowSettings = _context.SaveWindowSettings;
            Config.GetInstance().UpdaterFilesLoadReference = _context.UpdaterFilesLoadReference;
            Config.GetInstance().Save(Config.GetInstance(), Config.ConfFileName);

            IsSavedSettings = true;
            Close();
        }
    }
}
