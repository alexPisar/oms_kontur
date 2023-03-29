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
    /// Логика взаимодействия для RefEdoUpdValuesWindow.xaml
    /// </summary>
    public partial class RefEdoUpdValuesWindow : Window
    {
        private UtilitesLibrary.Logger.UtilityLog _log = UtilitesLibrary.Logger.UtilityLog.GetInstance();
        private bool _isCreated;
        private RefEdoGoodChannel _edoGoodChannel;

        public RefEdoUpdValuesWindow(RefEdoGoodChannel edoGoodChannel, object item = null)
        {
            InitializeComponent();

            _edoGoodChannel = edoGoodChannel;
            if (item == null)
            {
                SaveButton.Content = "Добавить";
                Title = "Добавление пары ключ - значение";

                _isCreated = true;
            }
            else
            {
                SaveButton.Content = "Сохранить";
                NameTextBox.IsEnabled = false;

                if(item as RefEdoUpdValues != null)
                {
                    Item = item as RefEdoUpdValues;
                    Title = "Изменение пары ключ - значение (УПД)";
                    NameTextBox.Text = (Item as RefEdoUpdValues).Key;
                    ValueTextBox.Text = (Item as RefEdoUpdValues).Value;
                }
                else if(item as RefEdoUcdValues != null)
                {
                    Item = item as RefEdoUcdValues;
                    Title = "Изменение пары ключ - значение (УКД)";
                    NameTextBox.Text = (Item as RefEdoUcdValues).Key;
                    ValueTextBox.Text = (Item as RefEdoUcdValues).Value;
                }
            }
        }

        public object Item { get; set; }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(NameTextBox.Text))
            {
                MessageBox.Show(
                        "Имя ключа не может быть пустым.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (Item as RefEdoUpdValues != null)
            {
                var item = Item as RefEdoUpdValues;
                item.Value = ValueTextBox.Text;

                if (_isCreated)
                {
                    item.Key = NameTextBox.Text;
                    if (_edoGoodChannel.EdoValuesPairs.Exists(d => d.Key == item.Key))
                    {
                        MessageBox.Show(
                            "Данное имя ключа уже было ранее добавлено.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    item.IdEdoGoodChannel = _edoGoodChannel.Id;
                    item.EdoGoodChannel = _edoGoodChannel;
                    _edoGoodChannel.EdoValuesPairs.Add(item);
                }
            }
            else if(Item as RefEdoUcdValues != null)
            {
                var item = Item as RefEdoUcdValues;
                item.Value = ValueTextBox.Text;

                if (_isCreated)
                {
                    item.Key = NameTextBox.Text;
                    if (_edoGoodChannel.EdoUcdValuesPairs.Exists(d => d.Key == item.Key))
                    {
                        MessageBox.Show(
                            "Данное имя ключа уже было ранее добавлено.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    item.IdEdoGoodChannel = _edoGoodChannel.Id;
                    item.EdoGoodChannel = _edoGoodChannel;
                    _edoGoodChannel.EdoUcdValuesPairs.Add(item);
                }
            }

            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
