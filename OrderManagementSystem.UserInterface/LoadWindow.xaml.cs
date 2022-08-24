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
using DevExpress.Xpf.Core;
using DevExpress.Xpf.Ribbon;
using DevExpress.Xpf.Editors;

namespace OrderManagementSystem.UserInterface
{
    /// <summary>
    /// Логика взаимодействия для LoadWindow.xaml
    /// </summary>
    public partial class LoadWindow : Window
    {
        public EventHandler ClosingWindow;

        public LoadWindow()
        {
            InitializeComponent();
        }

        public void SetSuccessFullLoad(ViewModels.LoadModel dataContext, string text = null)
        {
            try
            {
                ((ViewModels.LoadModel)dataContext).Text = text ?? "Загрузка завершена успешно.";
                ((ViewModels.LoadModel)dataContext).PathToImage = "pack://siteoforigin:,,,/Resources/ok.png";
                ((ViewModels.LoadModel)dataContext).OkEnable = true;
                ((ViewModels.LoadModel)dataContext).PropertyChanged();
            }
            catch(Exception ex)
            {

            }
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            ClosingWindow?.Invoke( sender, e );
        }
    }
}
