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
            ((MainViewModel)DataContext).IsExportButtonEnabled = ((MainViewModel)DataContext)?.SelectedItem?.Status == 0 && ((MainViewModel)DataContext)?.SelectedItem?.IsMarkedNotExportable == 0;
            ((MainViewModel)DataContext).OnPropertyChanged( "IsExportButtonEnabled" );
            ((MainViewModel)DataContext).OnPropertyChanged( "IsNotExportable" );
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
            DocumentsDataGrid?.View?.RowCellMenuCustomizations?.Clear();

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

            var rowCellCustomization = DocumentsDataGrid?.View?.RowCellMenuCustomizations;

            if(rowCellCustomization != null)
            {
                rowCellCustomization.Clear();
                var markNotExportableItem = new DevExpress.Xpf.Bars.BarButtonItem
                {
                    Content = "Пометить не экспортируемым",
                    ToolTip = "Сделать невозможным экспорт заказа, но сам заказ оставить в списке"
                };

                var enableMarkNotExportableBinding = new System.Windows.Data.Binding
                {
                    Path = new System.Windows.PropertyPath("View.DataContext.IsExportButtonEnabled"),
                    UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged,
                    Mode = System.Windows.Data.BindingMode.OneWay
                };
                var commandMarkNotExportableBinding = new System.Windows.Data.Binding("View.DataContext.SetNotExportableStateCommand");
                markNotExportableItem.SetBinding(System.Windows.ContentElement.IsEnabledProperty, enableMarkNotExportableBinding);
                markNotExportableItem.SetBinding(DevExpress.Xpf.Bars.BarItem.CommandProperty, commandMarkNotExportableBinding);

                rowCellCustomization.Add(markNotExportableItem);

                var markExportableItem = new DevExpress.Xpf.Bars.BarButtonItem
                {
                    Content = "Пометить экспортируемым",
                    ToolTip = "Сделать снова доступным экспорт заказа"
                };

                var enableMarkExportableBinding = new System.Windows.Data.Binding
                {
                    Path = new System.Windows.PropertyPath("View.DataContext.IsNotExportable"),
                    UpdateSourceTrigger = System.Windows.Data.UpdateSourceTrigger.PropertyChanged,
                    Mode = System.Windows.Data.BindingMode.OneWay
                };
                var commandMarkExportableBinding = new System.Windows.Data.Binding("View.DataContext.SetExportableStateCommand");
                markExportableItem.SetBinding(System.Windows.ContentElement.IsEnabledProperty, enableMarkExportableBinding);
                markExportableItem.SetBinding(DevExpress.Xpf.Bars.BarItem.CommandProperty, commandMarkExportableBinding);

                rowCellCustomization.Add(markExportableItem);
            }
        }
    }
}