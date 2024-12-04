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
using KonturEdoClient.Models;

namespace KonturEdoClient
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : DXRibbonWindow
    {
        public MainWindow()
        {
            InitializeComponent();
            organizationsControl.Owner = this;
        }

        private void SelectedDocumentChanged(object sender, DevExpress.Xpf.Grid.SelectedItemChangedEventArgs e)
        {
            ((MainModel)DataContext).SetDocumentDetails();
        }

        private void Filial_Changed(object sender, RoutedEventArgs e)
        {
            ((MainModel)DataContext).SetSelectedFilial((string)((DevExpress.Xpf.Bars.BarEditItem)e.Source).EditValue);
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ((MainModel)DataContext).Dispose();
        }
    }
}
