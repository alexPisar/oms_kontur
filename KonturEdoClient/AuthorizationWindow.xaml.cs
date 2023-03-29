using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DevExpress.Xpf.Ribbon;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Security.Cryptography.X509Certificates;
using DataContextManagementUnit.DataAccess.Contexts.Abt;
using Cryptography.WinApi;
using KonturEdoClient.Models;
using EdiProcessingUnit.Edo;
using UtilitesLibrary.ConfigSet;

namespace KonturEdoClient
{
    /// <summary>
    /// Логика взаимодействия для CertsChangeWindow.xaml
    /// </summary>
    public partial class AuthorizationWindow : DXRibbonWindow
    {
        WinApiCryptWrapper _crypto;
        UtilitesLibrary.Logger.UtilityLog _log = UtilitesLibrary.Logger.UtilityLog.GetInstance();
        X509Certificate2 _consignorCertificate;
        private AbtDbContext _abtDbContext;

        public AuthorizationWindow()
        {
            InitializeComponent();
            _crypto = new WinApiCryptWrapper();
            DataContext = Config.GetInstance();
            authPassword.Text = ((Config)DataContext).GetDataBasePassword();

            if (Config.GetInstance().IsNeedUpdate)
                GetUpdates();
        }

        private void Cancel_Button(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ChangeCertButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorLabel.Content = "";

            var client = new System.Net.WebClient();

            if (Config.GetInstance().ProxyEnabled)
            {
                var webProxy = new System.Net.WebProxy();

                webProxy.Address = new Uri("http://" + Config.GetInstance().ProxyAddress);
                webProxy.Credentials = new System.Net.NetworkCredential(Config.GetInstance().ProxyUserName,
                    Config.GetInstance().ProxyUserPassword);

                client.Proxy = webProxy;
            }

            Utils.XmlSignUtils utils = new Utils.XmlSignUtils(client);

            try
            {
                bool authInHonestMark = false;
                if (_consignorCertificate == null)
                {
                    var crypto = new WinApiCryptWrapper();

                    var personalCertificates = crypto.GetAllGostPersonalCertificates();
                    var consignorInn = Config.GetInstance().ConsignorInn;

                    var certs = personalCertificates.Where(c => consignorInn == utils.GetOrgInnFromCertificate(c) && utils.IsCertificateValid(c) && c.NotAfter > DateTime.Now).OrderByDescending(c => c.NotBefore);
                    _consignorCertificate = certs.FirstOrDefault();

                    if (_consignorCertificate == null)
                        throw new Exception("Не найден сертификат Комитента.");
                }

                try
                {
                    authInHonestMark = HonestMark.HonestMarkClient.GetInstance().Authorization(_consignorCertificate);

                    if (!authInHonestMark)
                        throw new Exception("Не удалось авторизоваться в честном знаке");
                }
                catch (System.Net.WebException webEx)
                {
                    var errorWindow = new ErrorWindow(
                        "Произошла ошибка авторизации в Честном знаке на удалённом сервере.",
                        new List<string>(
                            new string[]
                            {
                                    webEx.Message,
                                    webEx.StackTrace
                            }
                            ));

                    errorWindow.ShowDialog();
                    _log.Log("Ошибка авторизации в Честном знаке: " + _log.GetRecursiveInnerException(webEx));
                    authInHonestMark = false;
                }
                catch (Exception ex)
                {
                    var errorWindow = new ErrorWindow(
                        "Произошла ошибка авторизации в Честном знаке.",
                        new List<string>(
                            new string[]
                            {
                                    ex.Message,
                                    ex.StackTrace
                            }
                            ));

                    errorWindow.ShowDialog();
                    _log.Log("Ошибка авторизации в Честном знаке: " + _log.GetRecursiveInnerException(ex));
                    authInHonestMark = false;
                }

                _log.Log($"Результат авторизации в честном знаке: {authInHonestMark.ToString()}");

                ((Config)DataContext).GenerateParametersForPassword();
                ((Config)DataContext).SetDataBasePassword(authPassword.Text);

                _abtDbContext = new AbtDbContext();

                if (_abtDbContext.Database.Connection.State != System.Data.ConnectionState.Open)
                    _abtDbContext.Database.Connection.Open();

                var appVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                _abtDbContext.ExecuteProcedure("DBMS_APPLICATION_INFO.set_client_info", new Oracle.ManagedDataAccess.Client.OracleParameter("client_info", appVersion));

                MainWindow mainWindow = new MainWindow();
                MainModel mainModel = new MainModel(_abtDbContext, _consignorCertificate, utils, authInHonestMark);

                if (mainModel.Filials.Count == 0)
                    mainWindow.ChangeFilialsBar.IsVisible = false;

                mainWindow.DocTypesBar.EditValue = DataContextManagementUnit.DataAccess.DocJournalType.Invoice;

                ((Config)DataContext).Save((Config)DataContext, Config.ConfFileName);
                mainWindow.DataContext = mainModel;
                mainWindow.Show();
                mainModel.SetOwner(mainWindow);
                mainModel.GetDocuments();

                Close();
            }
            catch (System.Net.WebException webEx)
            {
                var errorWindow = new ErrorWindow(
                            "Произошла ошибка на удалённом сервере.",
                            new List<string>(
                                new string[]
                                {
                                    webEx.Message,
                                    webEx.StackTrace
                                }
                                ));

                errorWindow.ShowDialog();
                _log.Log("WebException: " + _log.GetRecursiveInnerException(webEx));
                _log.Log("ChangeCertButton: вход выполнен с ошибкой.");
            }
            catch (Exception ex)
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
                _log.Log("Exception: " + _log.GetRecursiveInnerException(ex));
                _log.Log("ChangeCertButton: вход выполнен с ошибкой.");
            }
        }

        private void GetUpdates()
        {
            try
            {
                var fileController = new WebService.Controllers.FileController();
                var updaterInfo = fileController.GetUpdateParameters<UpdaterModel>(Properties.Settings.Default.ApplicationName);

                if (updaterInfo.LockUpdate)
                    return;

                var currentPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                if (updaterInfo.LoadUpdater || !System.IO.File.Exists(currentPath + "\\UpdaterKonturEdo.exe"))
                {
                    string updaterAppName = "UpdaterKonturEdo", updaterAppVersion = "1.0.0.0";
                    var files = fileController.GetFilesListFromPath(updaterAppName, updaterAppVersion);

                    foreach(var file in files)
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

                    files = fileController.GetFilesListFromPath(updaterAppName, updaterAppVersion, "Resources");

                    foreach(var file in files)
                    {
                        if (file == "diadok.png")
                            continue;

                        var fileBytes = fileController.GetFileDataByPath(updaterAppName, updaterAppVersion, $"Resources/{file}");
                        using (var fileStream = new System.IO.FileStream(currentPath + "\\Resources\\" + file, System.IO.FileMode.OpenOrCreate))
                        {
                            fileStream.Write(fileBytes, 0, fileBytes.Length);
                            fileStream.Close();
                        }
                    }
                }

                var appVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
                var actualVersion = new Version(updaterInfo.Version);

                if (actualVersion.CompareTo(appVersion) > 0)
                {
                    var updateWindow = new UpdateWindow(updaterInfo.Version);
                    var result = updateWindow.ShowDialog();

                    if (result == true)
                    {
                        System.Diagnostics.Process.Start(currentPath + "\\UpdaterKonturEdo.exe");
                        this.Close();
                    }
                }
            }
            catch(System.Net.WebException webEx)
            {
                var errorWindow = new ErrorWindow(
                            "Произошла ошибка проверки наличия обновления программы на удалённом сервере.",
                            new List<string>(
                                new string[]
                                {
                                    webEx.Message,
                                    webEx.StackTrace
                                }
                                ));

                errorWindow.ShowDialog();

                _log.Log("WebException: " + _log.GetRecursiveInnerException(webEx));
                _log.Log("GetUpdates: не удалось получить информацию о доступном обновлении.");
            }
            catch(Exception ex)
            {
                var errorWindow = new ErrorWindow(
                            "Произошла ошибка проверки наличия обновления программы.",
                            new List<string>(
                                new string[]
                                {
                                    ex.Message,
                                    ex.StackTrace
                                }
                                ));

                errorWindow.ShowDialog();

                _log.Log("Exception: " + _log.GetRecursiveInnerException(ex));
                _log.Log("GetUpdates: не удалось получить информацию о доступном обновлении.");
            }
        }
    }
}
