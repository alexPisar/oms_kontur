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
using UtilitesLibrary.Logger;
using DataContextManagementUnit.DataAccess.Contexts.Abt;
using EdiProcessingUnit;

namespace ProfileManager
{
    /// <summary>
    /// Логика взаимодействия для ChangePasswordWindow.xaml
    /// </summary>
    public partial class ChangePasswordWindow : Window
    {
        private User _user;

        internal UtilityLog _log;

        public ChangePasswordWindow(User user, UtilityLog log)
        {
            InitializeComponent();
            nameTextBox.Text = user.FullName;
            _user = user;
            _log = log;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void changePassword_Click(object sender, RoutedEventArgs e)
        {
            _log.Log($"ChangePassword: смена пароля. Пользователь - {_user.FullName}");
            errorLabel.Content = "";
            currentPassword.Background = string.IsNullOrEmpty(currentPassword.Password) ? Brushes.Pink : Brushes.White;
            newPassword.Background = string.IsNullOrEmpty(newPassword.Password) ? Brushes.Pink : Brushes.White;

            if (string.IsNullOrEmpty(currentPassword.Password) || string.IsNullOrEmpty(newPassword.Password))
                errorLabel.Content = "Необходимо заполнить соответствующие поля";

            if (string.IsNullOrEmpty((string)errorLabel.Content))
            {
                if (newPassword.Password.Length < 6)
                    errorLabel.Content = "Новый пароль не должен быть меньше 6 символов.";

                if (newPassword.Password != confirmedPassword.Password)
                    errorLabel.Content = "Новый и подтверждённый пароль разные.";
            }

            if (!string.IsNullOrEmpty((string)errorLabel.Content))
                return;

            _log.Log("ChangePassword: подключение к базе.");
            var dataConnection = new DataContextManagementUnit.DataBaseConnection(_user.Host, _user.SID, UtilitesLibrary.ConfigSet.Config.GetInstance().GetDataBaseUser(), UtilitesLibrary.ConfigSet.Config.GetInstance().GetDataBasePassword());
            string connectionString = dataConnection.GetConnectionString();

            using (var abtDbContext = new AbtDbContext(connectionString, true))
            {
                try
                {
                    if (abtDbContext.Database.Connection.State != System.Data.ConnectionState.Open)
                        abtDbContext.Database.Connection.Open();

                    string hashedCurrentPassword = new Utils.PasswordUtils().MD5Password(currentPassword.Password);
                    string passwordFromDb = abtDbContext?
                        .Database?
                        .SqlQuery<string>($"select PASSWORD from PROFILES where (ACCOUNT_GLN,PROFILE_GLN,PASSWORD)=" +
                        $"(('{_user.EdiGLN}', '{_user.UserGLN}', '{hashedCurrentPassword}'))").FirstOrDefault();

                    if (string.IsNullOrEmpty(passwordFromDb))
                        throw new Exception("Введён неправильный текущий пароль.");

                    var hashedNewPassword = new Utils.PasswordUtils().MD5Password(newPassword.Password);

                    string sql = $"update PROFILES set PASSWORD = '{hashedNewPassword}' where (ACCOUNT_GLN,PROFILE_GLN,PASSWORD)=" + 
                        $"(('{_user.EdiGLN}', '{_user.UserGLN}', '{hashedCurrentPassword}'))";

                    using (var transaction = abtDbContext.Database.BeginTransaction())
                    {
                        try
                        {
                            abtDbContext.Database.ExecuteSqlCommand(sql);
                            transaction.Commit();
                        }
                        catch (Exception ex)
                        {
                            transaction.Rollback();
                            throw ex;
                        }
                    }

                    _log.Log("ChangePassword: пароль успешно изменён.");
                    errorLabel.Foreground = Brushes.Green;
                    errorLabel.Content = "Пароль успешно изменён!";
                }
                catch(Oracle.ManagedDataAccess.Client.OracleException orEx)
                {
                    MessageBox.Show($"Произошла ошибка БД при изменении пароля.\nOracleException: {orEx.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    _log.Log($"Произошла ошибка БД при изменении пароля.\n " +
                        $"OracleException: {_log.GetRecursiveInnerException(orEx)}");
                }
                catch(Exception ex)
                {
                    MessageBox.Show($"Произошла ошибка при изменении пароля.\nException: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    _log.Log($"Произошла ошибка при изменении пароля.\n " +
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
