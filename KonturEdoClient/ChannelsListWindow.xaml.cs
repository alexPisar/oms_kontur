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
    /// Логика взаимодействия для ChannelsListWindow.xaml
    /// </summary>
    public partial class ChannelsListWindow : Window
    {
        public ChannelsListWindow()
        {
            InitializeComponent();
        }

        private void AddedButton_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as Models.ChannelsListModel).CreateNew();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as Models.ChannelsListModel).Edit();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            (DataContext as Models.ChannelsListModel).Delete();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
