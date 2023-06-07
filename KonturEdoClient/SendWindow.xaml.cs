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
using KonturEdoClient.Models;

namespace KonturEdoClient
{
    /// <summary>
    /// Логика взаимодействия для SendWindow.xaml
    /// </summary>
    public partial class SendWindow : Window
    {
        public SendWindow()
        {
            InitializeComponent();
        }

        private void SignAndSaveButton_Click(object sender, RoutedEventArgs e)
        {
            ((SendModel)DataContext).SetButtonsEnabled(false);
            ((SendModel)DataContext).Send(true);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            ((SendModel)DataContext).SetButtonsEnabled(false);
            ((SendModel)DataContext).Send(false);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
