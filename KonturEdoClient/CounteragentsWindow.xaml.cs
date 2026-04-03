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
    /// Interaction logic for CounteragentsWindow.xaml
    /// </summary>
    public partial class CounteragentsWindow : Window
    {
        public CounteragentsWindow()
        {
            InitializeComponent();
        }

        private void ChangeButton_Click(object sender, RoutedEventArgs e)
        {
            var listModel = DataContext as Models.Base.ListViewModel<EdiProcessingUnit.Edo.Models.Kontragent>;

            if (listModel == null)
            {
                DialogResult = false;
                return;
            }

            if(listModel.SelectedItem == null)
            {
                MessageBox.Show("Ошибка! Не выбран контрагент.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                DialogResult = false;
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
