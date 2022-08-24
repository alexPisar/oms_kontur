using DevExpress.Xpf.Core;
using System.Collections.Generic;
using System.Linq;
using DevExpress.Xpf.Ribbon;
using OMS.ViewModels;
using OrderManagementSystem.UserInterface.ViewModels.Implementations;

namespace OrderManagementSystem.UserInterface
{
	/// <summary>
	/// Логика взаимодействия для MainWindow.xaml
	/// </summary>
	public partial class MainWindow : DXRibbonWindow
	{
		public MainWindow(DataContextManagementUnit.DataAccess.Contexts.Abt.AbtDbContext abt, EdiProcessingUnit.UsersConfig usersConfig)
		{
			InitializeComponent();

			DataContext = new MainViewModel(abt, usersConfig);
            ((MainViewModel)DataContext).SetInitialCommertialNetworkValues = SetInitialCommertialNetworkValues;
            ((ViewModelBase)DataContext).OnError += ShowError;
		}
		private void ShowError(string errText)
		{
			DXMessageBox.Show( errText, "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error );
		}

        private void DocOrders_SelectedItemChanged(object sender, DevExpress.Xpf.Grid.SelectedItemChangedEventArgs e)
        {
            ((MainViewModel)DataContext).DocLineItems = ((MainViewModel)DataContext).SelectedItem?.DocLineItems?.Where( d => d.IsRemoved != "1" )?.ToList();
            ((MainViewModel)DataContext).IsExportButtonEnabled = ((MainViewModel)DataContext)?.SelectedItem?.Status == 0;
            ((MainViewModel)DataContext).OnPropertyChanged( "IsExportButtonEnabled" );
            ((MainViewModel)DataContext).RefreshDocLines();
        }

        private void CommertialNetworks_ChangeValue(object sender, System.Windows.RoutedEventArgs e)
        {
            var type = ((DevExpress.Xpf.Bars.BarEditItem)sender)?.EditValue?.GetType();

            if (type == null)
            {
                ((MainViewModel)DataContext).SelectedNetworks = new List<string>();
                return;
            }

            if (type == typeof(List<object>))
                ((MainViewModel)DataContext).SelectedNetworks =
                    ((List<object>)((DevExpress.Xpf.Bars.BarEditItem)sender)?.EditValue)?
                    .Cast<string>()?.ToList() ?? new List<string>();
            else if (type == typeof(List<string>))
                ((MainViewModel)DataContext).SelectedNetworks =
                    (List<string>)((DevExpress.Xpf.Bars.BarEditItem)sender)?.EditValue;
        }

        private void SetInitialCommertialNetworkValues(List<string> glns)
        {
            commertialNetworksComboBox.EditValue = glns?.Cast<object>()?.ToList();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (UtilitesLibrary.ConfigSet.Config.GetInstance().SaveWindowSettings)
            {
                DocumentDetailsDataGrid.SaveLayoutToXml(Properties.Settings.Default.OrderDetailsGridLayoutsFileConfigName);
                DocumentsDataGrid.SaveLayoutToXml(Properties.Settings.Default.OrdersDataGridLayoutsFileConfigName);
            }
        }

        private void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (UtilitesLibrary.ConfigSet.Config.GetInstance().SaveWindowSettings)
            {
                if(System.IO.File.Exists(Properties.Settings.Default.OrderDetailsGridLayoutsFileConfigName))
                    DocumentDetailsDataGrid.RestoreLayoutFromXml(Properties.Settings.Default.OrderDetailsGridLayoutsFileConfigName);
                if (System.IO.File.Exists(Properties.Settings.Default.OrdersDataGridLayoutsFileConfigName))
                    DocumentsDataGrid.RestoreLayoutFromXml(Properties.Settings.Default.OrdersDataGridLayoutsFileConfigName);
            }
        }
    }
}