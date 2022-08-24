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
using System.Security.Cryptography;
using Oracle.ManagedDataAccess.Client;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Sql;
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Ribbon;
using DevExpress.Xpf.Editors;
using OrderManagementSystem.UserInterface.ViewModels;
using DataContextManagementUnit.DataAccess.Contexts.Abt;
using DataContextManagementUnit.DataAccess.Contexts.Edi;
using ProfileManager;

namespace OrderManagementSystem.UserInterface
{
    /// <summary>
    /// Логика взаимодействия для LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : DXRibbonWindow
    {
        private AbtDbContext _abtContext;
        internal UtilitesLibrary.Logger.UtilityLog _log = UtilitesLibrary.Logger.UtilityLog.GetInstance();

        public LoginWindow()
        {
            InitializeComponent();

            DataContext = new EdiProcessingUnit.UsersConfig();

            if (string.IsNullOrEmpty(UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser) ||
                string.IsNullOrEmpty(UtilitesLibrary.ConfigSet.Config.GetInstance().CipherDataBasePassword))
            {
                UtilitesLibrary.ConfigSet.Config.GetInstance().PositionIndex = 22;
                UtilitesLibrary.ConfigSet.Config.GetInstance().ShiftIndex = 1;
                UtilitesLibrary.ConfigSet.Config.GetInstance().CipherDataBasePassword = "\u0016cw4uiccricxh\u001b^wcbZ]wmw7wezrep\\Snr3MWq9d";
                UtilitesLibrary.ConfigSet.Config.GetInstance().DataBaseUser = "edi";
            }

            if (UtilitesLibrary.ConfigSet.Config.GetInstance().IsNeedUpdate)
                GetUpdates();
        }

        private void GetUpdates()
        {
            try
            {
                var fileController = new WebService.Controllers.FileController();
                string actualVersionStr = fileController.GetVersion("KonturEdi");
                string lockUpdate;
                string loadUpdater;

                using (var _ediDbContext = new EdiDbContext())
                {
                    try
                    {
                        if (_ediDbContext.Database.Connection.State != System.Data.ConnectionState.Open)
                            _ediDbContext.Database.Connection.Open();

                        var updateSettings = _ediDbContext.Database.SqlQuery<DataContextManagementUnit.DataAccess.Contexts.Edi.EdiApplicationFromDb>("select Name, LOCK_UPDATE as LockUpdate, LOAD_UPDATER as LoadUpdater from REF_UPDATE_SETTINGS where Name = 'KONTUR_EDI'").FirstOrDefault();
                        lockUpdate = updateSettings?.LockUpdate;
                        loadUpdater = updateSettings?.LoadUpdater;
                    }
                    finally
                    {
                        if (_ediDbContext.Database.Connection.State == System.Data.ConnectionState.Open)
                            _ediDbContext.Database.Connection.Close();
                    }
                }

                if (lockUpdate == "1")
                    return;

                if (actualVersionStr == null)
                    return;

                var appVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                var actualVersion = new Version(actualVersionStr);

                string currentPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

                if (loadUpdater == "1" || !System.IO.File.Exists(currentPath + "\\UpdaterConturEdi.exe"))
                {
                    string updaterAppName = "UpdaterKontur", updaterAppVersion = "1.0.0.0";
                    var files = fileController.GetFilesListFromPath(updaterAppName, updaterAppVersion);

                    foreach (var file in files)
                    {
                        if (file == "Newtonsoft.Json.dll")
                            continue;

                        var fileBytes = fileController.GetFileDataByPath(updaterAppName, updaterAppVersion, file);

                        using (var fileStream = new System.IO.FileStream(currentPath + "\\" + file, System.IO.FileMode.OpenOrCreate))
                        {
                            fileStream.Write(fileBytes, 0, fileBytes.Length);
                            fileStream.Close();
                        }
                    }
                }

                if (actualVersion.CompareTo(appVersion) > 0)
                {
                    var result = new UpdateWindow(actualVersionStr).ShowDialog();

                    if (result == true)
                    {
                        System.Diagnostics.Process.Start(currentPath + "\\UpdaterConturEdi.exe");

                        this.Close();
                    }
                }
            }
            catch(System.Net.WebException webEx)
            {
                MessageBox.Show("Произошла внутренняя ошибка на сервере обновлений! WebException: " + _log.GetRecursiveInnerException(webEx), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                _log.Log("Произошла внутренняя ошибка на сервере обновлений! WebException: " + _log.GetRecursiveInnerException(webEx));
            }
            catch (OracleException orEx)
            {
                MessageBox.Show("Произошла ошибка подключения к базе данных EDI.\n" +
                    "Вероятно, в настройках неправильно задан ip адрес хоста базы EDI, либо имя(SID) сервиса базы данных EDI.\n"+
                    "Настройте правильные параметры для базы EDI. Дополнительная информация:" + _log.GetRecursiveInnerException(orEx), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                _log.Log("Произошла ощибка подключения к базе данных. OracleException: " + _log.GetRecursiveInnerException(orEx));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Произошла ошибка при попытке установить обновления программы! Exception: " + _log.GetRecursiveInnerException(ex), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                _log.Log("Произошла ошибка при попытке установить обновления программы! Exception: " + _log.GetRecursiveInnerException(ex));
            }
        }

        public void OkButton_Click(object sender, EventArgs e)
        {
            if (CheckLogin())
            {
                try
                {
                    string host = ((EdiProcessingUnit.UsersConfig)DataContext).SelectedUser.Host;
                    string sid = ((EdiProcessingUnit.UsersConfig)DataContext).SelectedUser.SID;

                    if (host != UtilitesLibrary.ConfigSet.Config.GetInstance().AbtDataBaseIpAddress || 
                        sid != UtilitesLibrary.ConfigSet.Config.GetInstance().AbtDataBaseSid)
                    {
                        UtilitesLibrary.ConfigSet.Config.GetInstance().AbtDataBaseIpAddress = host;
                        UtilitesLibrary.ConfigSet.Config.GetInstance().AbtDataBaseSid = sid;

                        UtilitesLibrary.ConfigSet.Config.GetInstance().Save(
                            UtilitesLibrary.ConfigSet.Config.GetInstance(),
                            UtilitesLibrary.ConfigSet.Config.ConfFileName);
                    }

                    var mainWindow = new MainWindow(_abtContext, (EdiProcessingUnit.UsersConfig)DataContext );
                    mainWindow.Show();
                }
                catch(Exception ex)
                {
                    MessageBox.Show( "Произошла ошибка при запуске приложения: " + _log.GetRecursiveInnerException( ex ), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error );
                    _log.Log( "Exception: " + _log.GetRecursiveInnerException( ex ) );
                }
                finally
                {
                    this.Close();
                }
            }
        }

        private bool CheckLogin()
        {
            _log.Log( "CheckLogin" );
            try
            {
                _log.Log( "Получение хэшированного пароля." );
                string connectionString = ((EdiProcessingUnit.UsersConfig)DataContext).GetConnectionString();

                string password = MD5Password( Profile_password.Text );
                string result;
                _log.Log( "Хэшированный пароль получен." );

                _log.Log( "Проверка правильности пароля." );

                _abtContext = new AbtDbContext( connectionString, true );

                _log.Log( "AbtDbContext - Контекст получен." );

                result = _abtContext.SelectSingleValue(
                    $"SELECT COUNT(*) FROM PROFILES WHERE (ACCOUNT_GLN,PROFILE_GLN,PASSWORD) = (('" +
                    $"{((EdiProcessingUnit.UsersConfig)DataContext).SelectedUser.EdiGLN}', " +
                    $"'{((EdiProcessingUnit.UsersConfig)DataContext).SelectedUser.UserGLN}', " +
                    $"'{password}'))" );
                

                int value = result != null ? Convert.ToInt32( result ) : 0;

                _log.Log( "Результат проверки: " + value );

                if (value > 0)
                    return true;
                else
                    throw new Exception( "Неверный логин или пароль." );
            }
            catch (OracleException orEx)
            {
                MessageBox.Show("При авторизации произошла ошибка подключения к базе данных трейдера.\n" +
                    "Вероятно, в настройках неправильно задан ip адрес хоста базы, либо имя(SID) сервиса базы данных.\n" +
                    "Настройте правильные параметры для базы трейдера. Дополнительная информация:" + _log.GetRecursiveInnerException(orEx), "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                _log.Log("Произошла ощибка подключения к базе данных. OracleException: " + _log.GetRecursiveInnerException(orEx));
                return false;
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show( "Произошла ошибка: "+ ex.Message, "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error );
                _log.Log( "Exception: " + _log.GetRecursiveInnerException( ex ) );
                return false;
            }
        }

        private string MD5Password(string password)
        {
            string salt = "ePyQqtTpr1CZy0eGrEu@$cf#$%hSHuzZW6c&62xrNr13#9LCxG";
            byte[] hashenc = new MD5CryptoServiceProvider().ComputeHash( Encoding.ASCII.GetBytes( salt + password ) );
            password = "";
            foreach (var b in hashenc)
                password += b.ToString( "x2" );
            return password.ToUpper();
        }

        public void ProfileManagerButton_Click(object sender, EventArgs e)
        {
            var usersConfig = (EdiProcessingUnit.UsersConfig)DataContext;

            var profileManagerWindow = new ProfileWindow(usersConfig, _log);
            profileManagerWindow.ShowDialog();
            Profile_ComboBox.RefreshData();
        }
    }
}
