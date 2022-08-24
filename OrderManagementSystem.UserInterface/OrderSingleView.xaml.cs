using System.Windows;
using OrderManagementSystem.UserInterface.ViewModels;
using DevExpress.Xpf.Ribbon;

namespace OrderManagementSystem.UserInterface
{
	/// <summary>
	/// Логика взаимодействия для OrderSingleView.xaml
	/// </summary>
	public partial class OrderSingleView : DXRibbonWindow
	{
		public OrderSingleView()
		{
			InitializeComponent();
		}

		public OrderSingleView(OrderSingleViewModel viewModel)
		{
			InitializeComponent();
			DataContext = viewModel;
		}

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var orderViewContext = (OrderSingleViewModel)DataContext;

            if (orderViewContext == null)
                return;

            orderViewContext.ExportData();
        }

        private void ResentShippingDocuments_Click(object sender, RoutedEventArgs e)
        {
            var orderViewContext = (OrderSingleViewModel)DataContext;

            if (orderViewContext == null)
                return;

            orderViewContext.ResentShippingDocuments();
        }
    }
}
