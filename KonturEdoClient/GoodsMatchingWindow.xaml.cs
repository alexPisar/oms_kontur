using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DevExpress.Xpf.Ribbon;

namespace KonturEdoClient
{
    /// <summary>
    /// Логика взаимодействия для GoodsMatchingWindow.xaml
    /// </summary>
    public partial class GoodsMatchingWindow : DXRibbonWindow
    {
        public GoodsMatchingWindow()
        {
            InitializeComponent();
        }

        private void LookUpEdit_EditValueChanged(object sender, DevExpress.Xpf.Editors.EditValueChangedEventArgs e)
        {
            ((Models.GoodsMatchingModel)DataContext).Refresh();
        }
    }
}
