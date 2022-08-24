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
using OrderManagementSystem.UserInterface.ViewModels;
using DataContextManagementUnit.DataAccess.Contexts.Abt;
using DataContextManagementUnit.DataAccess.Contexts.Edi;

namespace OrderManagementSystem.UserInterface
{
    /// <summary>
    /// Логика взаимодействия для DeliveryPointsWindow.xaml
    /// </summary>
    public partial class DeliveryPointsWindow : DXRibbonWindow
    {
        public DeliveryPointsWindow(EdiDbContext edi, EdiProcessingUnit.UsersConfig usersConfig)
        {
            InitializeComponent();
            DataContext = new DeliveryPointsModel(edi, usersConfig );

            var id = ((DeliveryPointsModel)DataContext)?.Databases?.FirstOrDefault(d => d.Links == usersConfig.SelectedUser.SID)?.Id;

            if (id != null && id != 0)
            {
                databasesEditItem.EditValue = (long)id;
                ((DeliveryPointsModel)DataContext).Owner = this;
            }
        }

        private void SelectedContractor(object sender, DevExpress.Xpf.Grid.SelectedItemChangedEventArgs e)
        {
            ((DeliveryPointsModel)DataContext).SelectedContractorEvent();
        }

        private void IdValueChanged(object sender, RoutedEventArgs e)
        {
            ((DeliveryPointsModel)DataContext).SetSelectedDataBase( (long)((DevExpress.Xpf.Bars.BarEditItem)e.Source).EditValue );
            ((DeliveryPointsModel)DataContext).Refresh();
        }
    }
}
