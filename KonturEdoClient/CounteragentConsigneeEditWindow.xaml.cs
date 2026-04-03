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
using DataContextManagementUnit.DataAccess.Contexts.Abt;

namespace KonturEdoClient
{
    /// <summary>
    /// Interaction logic for CounteragentConsigneeEditWindow.xaml
    /// </summary>
    public partial class CounteragentConsigneeEditWindow : Window
    {
        public CounteragentConsigneeEditWindow()
        {
            InitializeComponent();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if((DataContext as Models.Base.ListViewModel<RefContractor>)?.SelectedItem == null)
            {
                MessageBox.Show("Ошибка! Не выбран грузополучатель.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
            Close();
        }
    }
}
