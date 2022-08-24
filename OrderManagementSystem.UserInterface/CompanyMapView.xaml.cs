using System.Windows;
using OrderManagementSystem.UserInterface.ViewModels;
using DevExpress.Xpf.Ribbon;

namespace OrderManagementSystem.UserInterface
{
	/// <summary>
	/// Логика взаимодействия для CompanyMapView.xaml
	/// </summary>
	public partial class CompanyMapView : DXRibbonWindow
	{
		public CompanyMapView()
		{
			InitializeComponent();
		}

		public CompanyMapView(CompanyMapViewModel viewModel)
		{
			InitializeComponent();
			DataContext = viewModel;
		}

		private void ListBox_Selected(object sender, RoutedEventArgs e)
		{

		}
	}
}
