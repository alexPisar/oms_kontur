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

namespace KonturEdoClient
{
    /// <summary>
    /// Логика взаимодействия для ShowDocumentSendHistoryWindow.xaml
    /// </summary>
    public partial class ShowDocumentSendHistoryWindow : Window
    {
        public ShowDocumentSendHistoryWindow()
        {
            InitializeComponent();
        }

        public Action<Models.DocEdoProcessingForLoading> AnnulmentDocument;

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void AnnulmentButton_Click(object sender, RoutedEventArgs e)
        {
            var docEdoProcessingForLoading = (DataContext as Models.Base.ListViewModel<Models.DocEdoProcessingForLoading>)?.SelectedItem;

            AnnulmentDocument?.Invoke(docEdoProcessingForLoading);

            (DataContext as Models.Base.ListViewModel<Models.DocEdoProcessingForLoading>)?.OnPropertyChanged("ItemsList");
            (DataContext as Models.Base.ListViewModel<Models.DocEdoProcessingForLoading>)?.OnPropertyChanged("SelectedItem");
            this.UpdateLayout();
        }
    }
}
