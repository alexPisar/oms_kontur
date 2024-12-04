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
using OMS.ViewModels;
using OrderManagementSystem.UserInterface.ViewModels;
using DevExpress.Xpf.Ribbon;

namespace OrderManagementSystem.UserInterface
{
	/// <summary>
	/// Логика взаимодействия для GoodsMapView.xaml
	/// </summary>
	public partial class GoodsMapView : DXRibbonWindow
	{
		public GoodsMapView()
		{
			InitializeComponent();
		}

		public GoodsMapView(GoodsMapViewModel viewModel)
		{
			InitializeComponent();
			DataContext = viewModel;
		}

        private void ChangedGln(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            ((GoodsMapViewModel)DataContext).GlnChangedEvent();
        }

        private void GoodsMap_SelectedItemChanged(object sender, DevExpress.Xpf.Grid.SelectedItemChangedEventArgs e)
        {
            ((GoodsMapViewModel)DataContext).OnPropertyChanged("SelectedMapGoodByBuyer");
        }
    }
}
