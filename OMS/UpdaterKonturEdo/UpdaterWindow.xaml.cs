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
using System.IO;
using System.Threading;
using Newtonsoft.Json;

namespace UpdaterKonturEdo
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class UpdaterWindow : Window
    {
        private readonly string[] configFiles = { "users", "conf/config.json", "Resources/diadok.png" };

        private const string MainApplicationExeFile = "KonturEdoClient.exe";

        private Task _task = null;
        private UpdaterService _service;
        private CancellationTokenSource _cancelToken;
        private string _appPath;
        private string _appSourceUrl;
        private string _appVersion;

        public UpdaterWindow()
        {
            InitializeComponent();

            DataContext = new UpdaterModel();

            _appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            if (!File.Exists(_appPath + "\\updateConfig.json"))
            {
                MessageBox.Show("Не найден конфигурационный файл!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
            else
            {
                _appSourceUrl = GetUrlReferenceAsJson(_appPath + "\\updateConfig.json", "appSourceUrl");

                _service = new UpdaterService(_appSourceUrl);
                var updateInfo = _service.GetAppUpdateInfo();

                if (updateInfo.LockUpdate == true)
                {
                    MessageBox.Show("Возможность обновления заблокирована в базе.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    Close();
                }
                else
                {
                    _appVersion = updateInfo.Version;

                    var inlines = new List<Inline>();
                    inlines.Add(new Run { Text = $"Будет установлена версия {_appVersion}." });
                    inlines.AddRange(startsTextBlock.Inlines);

                    startsTextBlock.Inlines.Clear();
                    startsTextBlock.Inlines.AddRange(inlines);
                    ((UpdaterModel)DataContext).LoadText = "Для начала процесса нажмите кнопку Старт.";
                    ((UpdaterModel)DataContext).ContentButton = "Старт";
                    ((UpdaterModel)DataContext).ProgressMaximum = 100;
                    ((UpdaterModel)DataContext).IsChangeButtonEnabled = true;
                    ((UpdaterModel)DataContext).IsStartButtonEnabled = true;
                    ((UpdaterModel)DataContext).CheckBoxVisibility = Visibility.Hidden;
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

        private void CopyDirectory(UpdaterModel dataContext, string destName, string relativePath, bool isButtonProgress = false)
        {
            _cancelToken?.Token.ThrowIfCancellationRequested();
            var files = _service.GetFilesListByPath(relativePath, _appVersion);
            var directories = _service.GetDirectories(relativePath, _appVersion);

            if (isButtonProgress)
            {
                dataContext.LoadText = "Начало процесса...";
                dataContext.ProgressMaximum = files.Count() + directories.Count();
            }

            foreach (var directory in directories)
            {
                if (!Directory.Exists(destName + "\\" + directory))
                    Directory.CreateDirectory(destName + "\\" + directory);

                CopyDirectory(dataContext, destName + "\\" + directory, relativePath == "" ? directory : relativePath + "/" + directory);

                if (isButtonProgress)
                    dataContext.Progress++;
            }

            foreach (var file in files)
            {
                _cancelToken?.Token.ThrowIfCancellationRequested();

                if (file == "Newtonsoft.Json.dll" && isButtonProgress)
                {
                    dataContext.Progress++;
                    continue;
                }

                if (configFiles.Any(c => c == (isButtonProgress ? file : relativePath + "/" + file)) && File.Exists(destName + "\\" + file))
                {
                    if (isButtonProgress)
                        dataContext.Progress++;

                    continue;
                }

                if (isButtonProgress && file == MainApplicationExeFile)
                    continue;

                dataContext.LoadText = "Сохранение файла " + destName + "\\" + file;
                var fileBytes = _service.GetFileDataByPath(relativePath + "/" + file, _appVersion);

                File.WriteAllBytes(destName + "\\" + file, fileBytes);

                if (isButtonProgress)
                    dataContext.Progress++;
            }

            if (isButtonProgress)
            {
                _cancelToken?.Token.ThrowIfCancellationRequested();
                dataContext.LoadText = "Сохранение файла " + destName + "\\" + MainApplicationExeFile;
                var fileBytes = _service.GetFileDataByPath(relativePath + "/" + MainApplicationExeFile, _appVersion);
                File.WriteAllBytes(destName + "\\" + MainApplicationExeFile, fileBytes);
                dataContext.Progress++;

                dataContext.LoadText = "Установка успешно завершена.";
                dataContext.ContentButton = "Готово";
                dataContext.IsStartButtonEnabled = true;
            }
        }

        private void Close_ButtonClick(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ChangeTabItem_ButtonClick(object sender, RoutedEventArgs e)
        {
            if (tabControl.SelectedIndex == 0)
                tabControl.SelectedIndex = 1;
            else if (tabControl.SelectedIndex == 1)
                tabControl.SelectedIndex = 0;
        }

        private void Cancel_ButtonClick(object sender, RoutedEventArgs e)
        {
            if (_cancelToken == null)
                return;

            var messageResult = MessageBox.Show("Вы действительно хотите отменить установку новой версии?",
                            "Запрос отмены", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (messageResult != MessageBoxResult.Yes)
                return;

            ((UpdaterModel)DataContext).IsCancelButtonEnabled = false;

            if (_task?.Status == TaskStatus.Running)
            {
                _cancelToken.Cancel();
            }

            if (_task == null)
                Close();
        }

        private void Start_ButtonClick(object sender, RoutedEventArgs e)
        {
            if (((UpdaterModel)DataContext).ContentButton == "Старт")
            {
                _cancelToken = new CancellationTokenSource();
                var token = _cancelToken.Token;
                bool cancelLoad = false;
                var factory = new TaskFactory(token);
                var context = (UpdaterModel)DataContext;

                context.IsCancelButtonEnabled = true;
                context.IsStartButtonEnabled = false;
                context.IsChangeButtonEnabled = false;

                _task = factory.StartNew(() =>
                {
                    try
                    {
                        CopyDirectory(context, _appPath, "", true);
                    }
                    catch (OperationCanceledException ex)
                    {
                        context.LoadText = "Установка была отменена пользователем.";
                        cancelLoad = true;
                    }
                    catch (System.Net.WebException webEx)
                    {
                        throw new Exception("Произошла внутренняя ошибка сервера:" + webEx.Message + ",\n StackTrace: " + webEx.StackTrace);
                    }
                }, token);

                factory.ContinueWhenAll(new Task[] { _task }, (results) =>
                {
                    context.IsCancelButtonEnabled = false;

                    if (cancelLoad)
                        return;

                    if (_task.IsFaulted)
                    {
                        MessageBox.Show("Произошла ошибка. " + (_task?.Exception?.InnerException?.Message ?? ""),
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

                        context.LoadText = "Установка завершена с ошибкой.";
                    }
                    else if (_task.IsCompleted)
                    {
                        context.LoadText = "Установка успешно завершена.";
                        context.CheckBoxVisibility = Visibility.Visible;
                    }

                    context.ContentButton = "Готово";
                    context.IsStartButtonEnabled = true;
                }, token);
            }
            else
            {
                if(applicationLaunchCheckBox.Visibility == Visibility.Visible && applicationLaunchCheckBox.IsChecked == true)
                    System.Diagnostics.Process.Start(_appPath + "\\" + MainApplicationExeFile);

                Close();
            }
        }
    }
}
