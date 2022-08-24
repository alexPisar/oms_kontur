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

namespace OrderManagementSystem.UserInterface
{
    /// <summary>
    /// Логика взаимодействия для SelectedCompanyWindow.xaml
    /// </summary>
    public partial class SelectedCompanyWindow : Window
    {
        public SelectedCompanyWindow()
        {
            InitializeComponent();
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if(((ViewModels.SelectedCompanyModel)DataContext).SelectedItem == null)
            {
                DevExpress.Xpf.Core.DXMessageBox.Show("Не выбрана компания.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
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
