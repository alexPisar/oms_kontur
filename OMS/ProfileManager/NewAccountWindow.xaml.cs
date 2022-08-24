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
using EdiProcessingUnit;
using DataContextManagementUnit.DataAccess.Contexts.Abt;
using UtilitesLibrary.Logger;
using EdiProcessingUnit;

namespace ProfileManager
{
    /// <summary>
    /// Логика взаимодействия для NewAccountWindow.xaml
    /// </summary>
    public partial class NewAccountWindow : Window
    {
        private UsersConfig _config;
        private bool _createProfile;

        internal UtilityLog _log;

        public NewAccountWindow(bool isCreateAccount, UsersConfig config, UtilityLog log)
        {
            InitializeComponent();
            DataContext = new User()
            {
                EdiGLN = "4607971729990",
                Host = UtilitesLibrary.ConfigSet.Config.GetInstance().AbtDataBaseIpAddress,
                SID = UtilitesLibrary.ConfigSet.Config.GetInstance().AbtDataBaseSid
            };

            if (!isCreateAccount)
            {
                namePanel.Visibility = Visibility.Collapsed;
                confirmPasswordPanel.Visibility = Visibility.Collapsed;
                orgGlnPanel.Visibility = Visibility.Collapsed;
                Title = "Добавление существующего аккаунта";
                Height = Height - namePanel.Height - confirmPasswordPanel.Height - orgGlnPanel.Height;
            }

            _createProfile = isCreateAccount;
            _config = config;
            _log = log;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AddProfileButton_Click(object sender, RoutedEventArgs e)
        {
            _log.Log("AddProfileButton");

            var user = (User)DataContext;
            errorLabel.Content = "";

            glnTextEdit.Background = string.IsNullOrEmpty(user.UserGLN) ? Brushes.Pink : Brushes.White;
            hostTextEdit.Background = string.IsNullOrEmpty(user.Host) ? Brushes.Pink : Brushes.White;
            sidTextEdit.Background = string.IsNullOrEmpty(user.SID) ? Brushes.Pink : Brushes.White;
            passwordBox.Background = string.IsNullOrEmpty(passwordBox.Password) ? Brushes.Pink : Brushes.White;

            if (string.IsNullOrEmpty(user.SID) || string.IsNullOrEmpty(user.Host) || string.IsNullOrEmpty(user.UserGLN) || string.IsNullOrEmpty(passwordBox.Password))
            {
                errorLabel.Content = "Необходимо заполнить соответствующие поля";
            }

            if (_createProfile)
                CreateProfile(user);
            else
                AddProfile(user);
        }

        private void CreateProfile(User user)
        {
            _log.Log("CreateProfile");

            nameTextEdit.Background = string.IsNullOrEmpty(user.Name) ? Brushes.Pink : Brushes.White;

            if ( string.IsNullOrEmpty(user.Name))
            {
                errorLabel.Content = "Необходимо заполнить соответствующие поля";
            }

            if (string.IsNullOrEmpty((string)errorLabel.Content))
            {
                if (passwordBox.Password.Length < 6)
                    errorLabel.Content = "Пароль не должен быть меньше 6 символов.";

                if (passwordBox.Password != confirmPasswordBox.Password)
                    errorLabel.Content = "Введённый и подтверждённый пароль разные.";
            }
            

            if (!string.IsNullOrEmpty((string)errorLabel.Content))
                return;

            if (_config.Users.Exists(u => u.UserGLN == user.UserGLN))
            {
                errorLabel.Content = "Пользователь с таким GLN уже есть в системе.";
                return;
            }

            _log.Log("CreateProfile: подключение к БД.");
            var dataConnection = new DataContextManagementUnit.DataBaseConnection(user.Host, user.SID, UtilitesLibrary.ConfigSet.Config.GetInstance().GetDataBaseUser(), UtilitesLibrary.ConfigSet.Config.GetInstance().GetDataBasePassword());
            string connectionString = dataConnection.GetConnectionString();

            using (var abtDbContext = new AbtDbContext(connectionString, true))
            {
                try
                {
                    if (abtDbContext.Database.Connection.State != System.Data.ConnectionState.Open)
                        abtDbContext.Database.Connection.Open();

                    var idFilial = abtDbContext?
                        .Database?
                        .SqlQuery<int>(
                            $"select id from ref_filials where links = '{user.SID}' and rownum < 2").FirstOrDefault();

                    string hashedPassword = new Utils.PasswordUtils().MD5Password(passwordBox.Password);

                    string sql = $"insert into profiles(PROFILE_NAME, PASSWORD, ID_FILIAL, ACCOUNT_GLN, PROFILE_GLN) " +
                        $"values('{user.Name}', '{hashedPassword}', {idFilial}, '{user.EdiGLN}', '{user.UserGLN}')";

                    using (var transaction = abtDbContext.Database.BeginTransaction())
                    {
                        try
                        {
                            abtDbContext.Database.ExecuteSqlCommand(sql);
                            transaction.Commit();
                        }
                        catch(Exception ex)
                        {
                            transaction.Rollback();
                            throw ex;
                        }
                    }

                    _config.Users.Add(user);
                    _config.SelectedUser = user;
                    _config.Save();
                    _log.Log("CreateProfile: профиль успешно создан.");

                    Close();
                }
                catch (Oracle.ManagedDataAccess.Client.OracleException orEx)
                {
                    MessageBox.Show($"Произошла ошибка БД при создании профиля {user?.FullName}.\nOracleException: {orEx.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    _log.Log($"Произошла ошибка БД при создании профиля {user?.FullName}.\n " +
                        $"OracleException: {_log.GetRecursiveInnerException(orEx)}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Произошла ошибка при создании профиля {user?.FullName}.\nException: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    _log.Log($"Произошла ошибка при создании профиля {user?.FullName}.\n " +
                        $"Exception: {_log.GetRecursiveInnerException(ex)}");
                }
                finally
                {
                    if (abtDbContext.Database.Connection.State == System.Data.ConnectionState.Open)
                        abtDbContext.Database.Connection.Close();
                }
            }
            
        }

        private void AddProfile(User user)
        {
            _log.Log("AddProfile");

            if (!string.IsNullOrEmpty((string)errorLabel.Content))
                return;

            if (_config.Users.Exists(u => u.UserGLN == user.UserGLN))
            {
                errorLabel.Content = "Пользователь с таким GLN уже есть в системе.";
                return;
            }

            _log.Log("AddProfile: подключение к БД.");
            var dataConnection = new DataContextManagementUnit.DataBaseConnection(user.Host, user.SID, UtilitesLibrary.ConfigSet.Config.GetInstance().GetDataBaseUser(), UtilitesLibrary.ConfigSet.Config.GetInstance().GetDataBasePassword());
            string connectionString = dataConnection.GetConnectionString();

            using (var abtDbContext = new AbtDbContext(connectionString, true))
            {
                try
                {
                    if (abtDbContext.Database.Connection.State != System.Data.ConnectionState.Open)
                        abtDbContext.Database.Connection.Open();

                    var profileNameFromBase = abtDbContext?
                        .Database?
                        .SqlQuery<string>($"select PROFILE_NAME from PROFILES where PROFILE_GLN='{user.UserGLN}'").FirstOrDefault();

                    if (string.IsNullOrEmpty(profileNameFromBase))
                        throw new Exception("Нет такого пользователя в системе.");

                    string hashedPassword = new Utils.PasswordUtils().MD5Password(passwordBox.Password);

                    string sid = user.SID;
                    string host = user.Host;

                    user = abtDbContext?
                        .Database?
                        .SqlQuery<User>($"select PROFILE_NAME as Name, " +
                        $"PROFILE_GLN as UserGLN, " +
                        $"ACCOUNT_GLN as EdiGLN from PROFILES " +
                        $"where (PROFILE_GLN,PASSWORD)=" +
                        $"(('{user.UserGLN}', '{hashedPassword}'))").FirstOrDefault();

                    if (user == null)
                        throw new Exception("Пароль пользователя неверный!");

                    user.SID = sid;
                    user.Host = host;

                    _config.Users.Add(user);
                    _config.SelectedUser = user;
                    _config.Save();
                    _log.Log("AddProfile: профиль успешно добавлен.");

                    Close();
                }
                catch (Oracle.ManagedDataAccess.Client.OracleException orEx)
                {
                    MessageBox.Show($"Произошла ошибка БД при добавлении профиля {user?.FullName}.\nOracleException: {orEx.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    _log.Log($"Произошла ошибка БД при добавлении профиля {user?.FullName}.\n " +
                        $"OracleException: {_log.GetRecursiveInnerException(orEx)}");
                }
                catch(Exception ex)
                {
                    MessageBox.Show($"Произошла ошибка при добавлении профиля {user?.FullName}.\nException: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    _log.Log($"Произошла ошибка при добавлении профиля {user?.FullName}.\n " +
                        $"Exception: {_log.GetRecursiveInnerException(ex)}");
                }
                finally
                {
                    if (abtDbContext.Database.Connection.State == System.Data.ConnectionState.Open)
                        abtDbContext.Database.Connection.Close();
                }
            }
        }
    }
}
