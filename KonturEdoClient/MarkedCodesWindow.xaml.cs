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
using KonturEdoClient.Models;
using DataContextManagementUnit.DataAccess.Contexts.Abt;

namespace KonturEdoClient
{
    /// <summary>
    /// Логика взаимодействия для MarkedCodesWindow.xaml
    /// </summary>
    public partial class MarkedCodesWindow : Window
    {
        public List<DocGoodsDetailsLabels> MarkedCodes { get; set; }

        public List<DocGoodsDetailsLabels> SelectedCodes
        {
            get 
                {
                List<TreeListGoodInfo> selectedItems = treeList.Nodes?
                .Where(n => !((TreeListGoodInfo)n.Content).NotMarked)?
                .SelectMany(n => n.Nodes)?.Where(n => n.IsChecked == true)?
                .Select(n => (TreeListGoodInfo)n.Content)?.ToList() ?? new List<TreeListGoodInfo>();

                var selectedCodes = MarkedCodes.Where(m => selectedItems.Exists(s => m.DmLabel == s.Name && m.IdGood == s.IdGood));
                return selectedCodes.ToList();
            }
        }

        public EventHandler OnReturnCodesToStore;

        public MarkedCodesWindow(bool isReturnButtonEnabled)
        {
            InitializeComponent();
            returnButton.IsEnabled = isReturnButtonEnabled;
        }

        public void SetMarkedItems(AbtDbContext abt, List<DocGoodsDetailsI> details, List<DocGoodsDetailsLabels> markedCodes)
        {
            MarkedCodes = markedCodes;
            treeList.Nodes.Clear();

            foreach (var d in details)
            {
                var detail = new TreeListGoodInfo();

                detail.IdDoc = d.IdDoc;
                detail.IdGood = d.IdGood;
                detail.Quantity = d.Quantity;
                detail.Name = abt.RefGoods?
                    .FirstOrDefault(r => r.Id == d.IdGood)?
                    .Name;

                var marks = markedCodes?
                    .Where(m => m.IdGood == d.IdGood)?
                    .ToList() ?? new List<DocGoodsDetailsLabels>();

                detail.NotAllDocumentsMarked = marks.Count > 0 && marks.Count < d.Quantity;
                detail.NotMarked = marks.Count == 0;
                var detailNode = new DevExpress.Xpf.Grid.TreeListNode(detail);
                detailNode.IsChecked = false;

                if (detail.NotMarked)
                {
                    detailNode.IsCheckBoxEnabled = false;
                }

                treeList.Nodes.Add(detailNode);
                
                foreach(var mark in marks)
                {
                    var markInfo = new TreeListGoodInfo();

                    markInfo.IdDoc = d.IdDoc;
                    markInfo.IdGood = mark.IdGood;
                    markInfo.Name = mark.DmLabel;
                    markInfo.InsertDateTime = mark.InsertDateTime;
                    markInfo.IsMarkedCode = true;

                    var markNode = new DevExpress.Xpf.Grid.TreeListNode(markInfo);
                    markNode.IsChecked = false;
                    detailNode.Nodes.Add(markNode);
                }
            }
            DocumetDetailsDataGrid.RefreshData();
        }

        public void SetMarkedItems(AbtDbContext abt, List<DocGoodsDetail> details, List<DocGoodsDetailsLabels> markedCodes)
        {
            MarkedCodes = markedCodes;
            treeList.Nodes.Clear();

            foreach (var d in details)
            {
                var detail = new TreeListGoodInfo();

                detail.IdDoc = d.IdDoc;
                detail.IdGood = d.IdGood;
                detail.Quantity = d.Quantity;
                detail.Name = abt.RefGoods?
                    .FirstOrDefault(r => r.Id == d.IdGood)?
                    .Name;

                var marks = markedCodes?
                    .Where(m => m.IdGood == d.IdGood)?
                    .ToList() ?? new List<DocGoodsDetailsLabels>();

                detail.NotAllDocumentsMarked = marks.Count > 0 && marks.Count < d.Quantity;
                detail.NotMarked = marks.Count == 0;
                var detailNode = new DevExpress.Xpf.Grid.TreeListNode(detail);
                detailNode.IsChecked = false;

                if (detail.NotMarked)
                {
                    detailNode.IsCheckBoxEnabled = false;
                }

                treeList.Nodes.Add(detailNode);

                foreach (var mark in marks)
                {
                    var markInfo = new TreeListGoodInfo();

                    markInfo.IdDoc = d.IdDoc;
                    markInfo.IdGood = mark.IdGood;
                    markInfo.Name = mark.DmLabel;
                    markInfo.InsertDateTime = mark.InsertDateTime;
                    markInfo.IsMarkedCode = true;

                    var markNode = new DevExpress.Xpf.Grid.TreeListNode(markInfo);
                    markNode.IsChecked = false;
                    detailNode.Nodes.Add(markNode);
                }
            }
            DocumetDetailsDataGrid.RefreshData();
        }

        private void ReturnButton_Click(object sender, RoutedEventArgs e)
        {
            OnReturnCodesToStore?.Invoke(this, e);
        }
    }
}
