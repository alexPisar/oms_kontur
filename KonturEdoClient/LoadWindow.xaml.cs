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
    /// Логика взаимодействия для LoadWindow.xaml
    /// </summary>
    public partial class LoadWindow : Window
    {
        public LoadWindow()
        {
            InitializeComponent();
        }

        public void SetSuccessFullLoad(LoadModel dataContext, string text = null)
        {
            try
            {
                ((LoadModel)dataContext).Text = text ?? "Загрузка завершена успешно.";
                ((LoadModel)dataContext).PathToImage = "pack://siteoforigin:,,,/Resources/ok.png";
                ((LoadModel)dataContext).OkEnable = true;
                ((LoadModel)dataContext).OnAllPropertyChanged();
            }
            catch (Exception ex)
            {

            }
        }

        private void OK_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void LoadWindow_Closed(object sender, EventArgs e)
        {
            if (Owner != null)
                Owner.IsEnabled = true;
        }

        private void LoadWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (Owner != null)
                Owner.IsEnabled = false;
        }
    }
}
