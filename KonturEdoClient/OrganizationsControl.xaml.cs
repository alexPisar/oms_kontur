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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace KonturEdoClient
{
    /// <summary>
    /// Логика взаимодействия для OrganizationsControl.xaml
    /// </summary>
    public partial class OrganizationsControl : UserControl
    {
        public OrganizationsControl()
        {
            InitializeComponent();
        }

        public Window Owner { get; set; }

        private void SelectItem(object sender, DevExpress.Xpf.Grid.SelectedItemChangedEventArgs e)
        {
            ((Models.MainModel)DataContext).ChangeSelectedOrganization();
        }
    }
}
