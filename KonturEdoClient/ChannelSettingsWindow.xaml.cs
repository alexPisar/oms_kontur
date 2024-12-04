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
    /// Логика взаимодействия для ChannelSettingsWindow.xaml
    /// </summary>
    public partial class ChannelSettingsWindow : Window
    {
        public ChannelSettingsWindow()
        {
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if ((DataContext as Models.ChannelSettingsModel)?.Save() ?? false)
            {
                DialogResult = true;
                Close();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            ((Models.ChannelSettingsModel)DataContext).AddKeyValue(EdiProcessingUnit.Enums.DocEdoType.Upd);
            KeyValuePairsGridControl.RefreshData();
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            if (KeyValuePairsGridControl.SelectedItem == null)
            {
                MessageBox.Show(
                        "Не выбрана пара ключ - значение УПД для изменения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ((Models.ChannelSettingsModel)DataContext).EditKeyValue(KeyValuePairsGridControl.SelectedItem);
            KeyValuePairsGridControl.RefreshData();
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if(KeyValuePairsGridControl.SelectedItem == null)
            {
                MessageBox.Show(
                        "Не выбрана пара ключ - значение для удаления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ((Models.ChannelSettingsModel)DataContext).RemoveKeyValue(KeyValuePairsGridControl.SelectedItem);
            KeyValuePairsGridControl.RefreshData();
        }

        private void UcdAddButton_Click(object sender, RoutedEventArgs e)
        {
            ((Models.ChannelSettingsModel)DataContext).AddKeyValue(EdiProcessingUnit.Enums.DocEdoType.Ucd);
            UcdKeyValuePairsGridControl.RefreshData();
        }

        private void UcdEditButton_Click(object sender, RoutedEventArgs e)
        {
            if (UcdKeyValuePairsGridControl.SelectedItem == null)
            {
                MessageBox.Show(
                        "Не выбрана пара ключ - значение УКД для изменения.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ((Models.ChannelSettingsModel)DataContext).EditKeyValue(UcdKeyValuePairsGridControl.SelectedItem);
            UcdKeyValuePairsGridControl.RefreshData();
        }

        private void UcdRemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (UcdKeyValuePairsGridControl.SelectedItem == null)
            {
                MessageBox.Show(
                        "Не выбрана пара ключ - значение для удаления.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ((Models.ChannelSettingsModel)DataContext).RemoveKeyValue(UcdKeyValuePairsGridControl.SelectedItem);
            UcdKeyValuePairsGridControl.RefreshData();
        }
    }
}
