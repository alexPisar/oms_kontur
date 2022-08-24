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
using System.Windows.Navigation;
using System.Windows.Shapes;
using DevExpress.Xpf.Ribbon;
using DevExpress.Xpf.Core;
using ProfileManager.Models;
using EdiProcessingUnit;
using UtilitesLibrary.Logger;

namespace ProfileManager
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class ProfileWindow : Window
    {
        internal UtilityLog _log;

        public ProfileWindow()
        {
            InitializeComponent();

            _log = UtilityLog.GetInstance();

            var context = new ProfileModel()
            {
                ProfileConfig = new EdiProcessingUnit.UsersConfig(),
                DataBaseUser = UtilitesLibrary.ConfigSet.Config.GetInstance().GetDataBaseUser()
            };
            DbPasswordBox.Password = UtilitesLibrary.ConfigSet.Config.GetInstance().GetDataBasePassword();

            DataContext = context;
        }

        public ProfileWindow(UsersConfig config, UtilityLog log)
        {
            InitializeComponent();

            _log = log;

            var context = new ProfileModel()
            {
                ProfileConfig = config,
                DataBaseUser = UtilitesLibrary.ConfigSet.Config.GetInstance().GetDataBaseUser()
            };
            DbPasswordBox.Password = UtilitesLibrary.ConfigSet.Config.GetInstance().GetDataBasePassword();

            DataContext = context;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ((ProfileModel)DataContext).OnPropertyChanged("ProfileConfig");
            ((ProfileModel)DataContext).OnPropertyChanged("ProfileConfig.SelectedUser");
            ((ProfileModel)DataContext).OnPropertyChanged("ProfileConfig.SelectedUser.Name");
            ((ProfileModel)DataContext).OnPropertyChanged("ProfileConfig.SelectedUser.Host");
            ((ProfileModel)DataContext).OnPropertyChanged("ProfileConfig.SelectedUser.SID");

            ((ProfileModel)DataContext).ProfileConfig.Save();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ChangePasswordButton_Click(object sender, RoutedEventArgs e)
        {
            if (((ProfileModel)DataContext)?.ProfileConfig?.SelectedUser == null)
            {
                MessageBox.Show("Не выбран пользователь!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            new ChangePasswordWindow(((ProfileModel)DataContext).ProfileConfig.SelectedUser, _log).ShowDialog();
        }

        private void CreateNewAccountButton_Click(object sender, RoutedEventArgs e)
        {
            _log.Log("Открытие окна создания профайла");
            new NewAccountWindow(true, ((ProfileModel)DataContext).ProfileConfig, _log).ShowDialog();
            ((ProfileModel)DataContext).OnPropertyChanged("ProfileConfig");
            ((ProfileModel)DataContext).OnPropertyChanged("ProfileConfig.Users");
            usersComboBox.RefreshData();
        }

        private void AddAccountButton_Click(object sender, RoutedEventArgs e)
        {
            _log.Log("Открытие окна создания профайла");
            new NewAccountWindow(false, ((ProfileModel)DataContext).ProfileConfig, _log).ShowDialog();
            ((ProfileModel)DataContext).OnPropertyChanged("ProfileConfig");
            ((ProfileModel)DataContext).OnPropertyChanged("ProfileConfig.Users");
            usersComboBox.RefreshData();
        }
    }
}
