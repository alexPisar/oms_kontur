using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using System.Threading;
using Newtonsoft.Json.Serialization;

namespace UpdaterConturEdi
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class UpdateWindow : Window
    {
        private readonly string[] configFiles = {"users", "conf/config.json", "DocumentsDataGrid.xml", "DocumentDetailsDataGrid.xml" };

        private const string MainApplicationExeFile = "OrderManagementSystem.UserInterface.exe";

        private string _appPath;
        private string _appVersion;
        private string _appSourceUrl;
        private UpdateModel _context;
        private FileWebService _service;
        private Task _task = null;
        private CancellationTokenSource _cancelToken;
        private bool _cancelLoad = false;

        public UpdateWindow()
        {
            InitializeComponent();
            _context = new UpdateModel();
            DataContext = _context;
            _cancelToken = new CancellationTokenSource(  );

            _context.Text = "Добро пожаловать в мастер обновления приложения КОНТУР EDI. Для продолжения нажмите 'Далее'";
            _context.Progress = 0;
            _context.Maximum = 100;
            _context.IsEnableButton = true;
            _context.ContentButton = "Далее";
            _context.IsVisibleStartUppCheckBox = Visibility.Hidden;

            _appPath = System.IO.Path.GetDirectoryName( System.Reflection.Assembly.GetExecutingAssembly().Location );

            string configPath = null, paramName = null;

            if (File.Exists(_appPath + "\\conf\\config.json"))
            {
                configPath = _appPath + "\\conf\\config.json";
                paramName = "UpdaterFilesLoadReference";
            }
            else if(File.Exists(_appPath + "\\updateConfig.json"))
            {
                configPath = _appPath + "\\updateConfig.json";
                paramName = "appSourceUrl";
            }

            if (configPath == null)
            {
                MessageBox.Show( "Не найден конфигурационный файл!", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error );
                Close();
            }
            else
            {
                _appSourceUrl = GetUrlReferenceAsJson(configPath, paramName);

                _service = new FileWebService( _appSourceUrl );

                var updateInfo = _service.GetAppUpdateInfo();

                if (updateInfo.LockUpdate == true)
                {
                    MessageBox.Show("Возможность обновления заблокирована в базе.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    Close();
                }
                else
                {
                    _appVersion = updateInfo.Version;
                    _context.Text = $"Добро пожаловать в мастер обновления приложения КОНТУР EDI.\nБудет установлена версия {_appVersion}. Для продолжения нажмите 'Далее'";
                }
            }
        }

        private string GetUrlReferenceAsJson(string configPath, string paramName)
        {
            string appSourceUrl = null;
            using (FileStream fs = new FileStream(configPath, FileMode.Open))
            {
                using (StreamReader sr = new StreamReader(fs, Encoding.GetEncoding(1251)))
                {
                    var arrObj = (Newtonsoft.Json.Linq.JObject)JsonConvert.DeserializeObject(sr.ReadToEnd());
                    appSourceUrl = (string)arrObj[paramName];
                }
            }
            return appSourceUrl;
        }

        private void CopyDirectory(string destName, string relativePath, bool isButtonProgress = false)
        {
            _cancelToken?.Token.ThrowIfCancellationRequested();
            var files = _service.GetFilesListByPath( relativePath, _appVersion );
            var directories = _service.GetDirectories( relativePath, _appVersion );

            if (isButtonProgress)
                _context.Maximum = files.Count() + directories.Count();

            foreach(var directory in directories)
            {
                if(!Directory.Exists( destName + "\\" + directory ))
                    Directory.CreateDirectory( destName + "\\" + directory );

                CopyDirectory( destName + "\\" + directory, relativePath == "" ? directory : relativePath + "/" + directory);

                if (isButtonProgress)
                    _context.Progress = _context.Progress + 1;
            }

            foreach (var file in files)
            {
                _cancelToken?.Token.ThrowIfCancellationRequested();

                if (file == "Newtonsoft.Json.dll" && isButtonProgress)
                {
                    _context.Progress = _context.Progress + 1;
                    continue;
                }

                if (configFiles.Any(c => c == (isButtonProgress ? file : relativePath + "/" + file)) && File.Exists(destName + "\\" + file))
                {
                    if (isButtonProgress)
                        _context.Progress = _context.Progress + 1;

                    continue;
                }

                if (isButtonProgress && file == MainApplicationExeFile)
                    continue;

                _context.Text = "Сохранение файла " + destName + "\\" + file;
                var fileBytes = _service.GetFileDataByPath(relativePath + "/" + file, _appVersion);

                File.WriteAllBytes( destName + "\\" + file, fileBytes );

                if (isButtonProgress)
                    _context.Progress = _context.Progress + 1;
            }

            if(isButtonProgress)
            {
                _cancelToken?.Token.ThrowIfCancellationRequested();
                _context.Text = "Сохранение файла " + destName + "\\" + MainApplicationExeFile;
                var fileBytes = _service.GetFileDataByPath(relativePath + "/" + MainApplicationExeFile, _appVersion);
                File.WriteAllBytes(destName + "\\" + MainApplicationExeFile, fileBytes);
                _context.Progress = _context.Progress + 1;

                _context.Text = "Установка успешно завершена.";
                _context.ContentButton = "Готово";
                _context.IsEnableButton = true;
                _context.IsVisibleStartUppCheckBox = Visibility.Visible;
            }
        }

        private void contentButton_Click(object sender, RoutedEventArgs e)
        {
            string path = _appPath + "\\" + MainApplicationExeFile;
            var _token = _cancelToken.Token;
            var factory = new TaskFactory( _token );

            if (_context.ContentButton == "Далее")
            {
                _context.IsEnableButton = false;
                _context.Text = "Идёт инициализация установки...";

                _task = factory.StartNew( () => {
                    try
                    {
                        CopyDirectory( _appPath, "", true );
                    }
                    catch (OperationCanceledException ex)
                    {
                        _context.Text = "Установка была отменена.";
                        _context.ContentButton = "Готово";
                        _context.IsEnableButton = true;
                        _context.IsVisibleCancelButton = Visibility.Hidden;
                        _cancelLoad = true;
                    }
                    catch(System.Net.WebException webEx)
                    {
                        throw new Exception( "Произошла внутренняя ошибка сервера:" + webEx.Message + ",\n StackTrace: " + webEx.StackTrace );
                    }
                    }, _token );

                factory.ContinueWhenAll( new Task[] { _task }, (results) => {
                    if (_cancelLoad)
                        return;

                    if (_task.IsFaulted)
                    {
                        System.Windows.MessageBox.Show( "Произошла ошибка. " + (_task?.Exception?.InnerException?.Message ?? ""), 
                            "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error );

                        _context.Text = "Установка завершена с ошибкой.";
                    }
                    else if (_task.IsCompleted)
                    {
                        _context.Text = "Установка успешно завершена.";
                        _context.IsVisibleStartUppCheckBox = Visibility.Visible;
                    }

                    _context.ContentButton = "Готово";
                    _context.IsVisibleCancelButton = Visibility.Hidden;
                    _context.IsEnableButton = true;
                }
                , _token );
            }
            else
            {
                if(startUppCheckBox.IsChecked == true && startUppCheckBox.Visibility == Visibility.Visible)
                    System.Diagnostics.Process.Start( path );
                
                this.Close();
            }
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            var messageResult = MessageBox.Show( "Вы действительно хотите отменить установку новой версии?",
                            "Запрос отмены", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question );

            if (messageResult != MessageBoxResult.Yes)
                return;

            cancelButton.IsEnabled = false;
            if (_task?.Status == TaskStatus.Running)
            {
                _cancelToken.Cancel();
            }

            if (_task == null)
                Close();
        }
    }
}
