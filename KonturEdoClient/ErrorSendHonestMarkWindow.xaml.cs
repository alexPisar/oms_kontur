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
    /// Логика взаимодействия для ErrorSendHonestMarkWindow.xaml
    /// </summary>
    public partial class ErrorSendHonestMarkWindow : Window
    {
        public ErrorSendHonestMarkWindow(string messageText)
        {
            InitializeComponent();
            textContent.Text = messageText;
        }

        public void YesButtonClick(object sender, EventArgs e)
        {
            DialogResult = true;
            Close();
        }

        public void NoButtonClick(object sender, EventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
