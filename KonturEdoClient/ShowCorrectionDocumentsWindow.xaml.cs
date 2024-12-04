using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DevExpress.Xpf.Ribbon;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace KonturEdoClient
{
    /// <summary>
    /// Логика взаимодействия для ShowCorrectionDocumentsWindow.xaml
    /// </summary>
    public partial class ShowCorrectionDocumentsWindow : DXRibbonWindow
    {
        public ShowCorrectionDocumentsWindow()
        {
            InitializeComponent();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SendButton_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as Models.CorrectionDocumentsModel)?.SendDocument();
        }

        private void GridControl_SelectedItemChanged(object sender, DevExpress.Xpf.Grid.SelectedItemChangedEventArgs e)
        {
            (DataContext as Models.CorrectionDocumentsModel)?.OnPropertyChanged("CorrectionDocumentDetails");
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            var dataContext = DataContext as Models.CorrectionDocumentsModel;
            try
            {
                var task = dataContext?.Refresh();
                await task;
            }
            catch(Exception ex)
            {
                var log = UtilitesLibrary.Logger.UtilityLog.GetInstance();
                log.Log($"SearchButton Exception: {log.GetRecursiveInnerException(ex)}");

                var errorWindow = new ErrorWindow(
                            "Произошла ошибка загрузки корректировочных документов.",
                            new List<string>(
                                new string[]
                                {
                                    ex.Message,
                                    ex.StackTrace
                                }
                                ));

                errorWindow.ShowDialog();
            }
        }
    }
}
