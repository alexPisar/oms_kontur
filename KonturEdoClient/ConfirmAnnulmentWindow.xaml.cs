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
    /// Логика взаимодействия для ConfirmAnnulmentWindow.xaml
    /// </summary>
    public partial class ConfirmAnnulmentWindow : Window
    {
        private AnnulmentRequestDialogResult _result;

        public string RejectReasonText => rejectReasonTextBox.Text;

        public AnnulmentRequestDialogResult Result => _result;

        public ConfirmAnnulmentWindow()
        {
            InitializeComponent();

            revokeRadioButton.IsChecked = true;
            rejectPanel.IsEnabled = false;

            _result = AnnulmentRequestDialogResult.None;
        }

        private void RevokeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            rejectPanel.IsEnabled = false;
        }

        private void RejectRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            rejectPanel.IsEnabled = true;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            if (revokeRadioButton.IsChecked == true)
            {
                _result = AnnulmentRequestDialogResult.Revoke;
            }
            else if (rejectRadioButton.IsChecked == true)
            {
                if (string.IsNullOrEmpty(RejectReasonText))
                {
                    MessageBox.Show("Не указана причина отказа.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _result = AnnulmentRequestDialogResult.Reject;
            }

            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            _result = AnnulmentRequestDialogResult.None;
            Close();
        }
    }

    public enum AnnulmentRequestDialogResult
    {
        None,
        Revoke,
        Reject
    }
}
