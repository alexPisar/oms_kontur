using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataContextManagementUnit.DataAccess.Contexts.Abt;

namespace KonturEdoClient.Models
{
    public class CounteragentConsigneesModel:Base.ListViewModel<RefEdoCounteragentConsigneeForLoading>
    {
        private AbtDbContext _abt;
        private RefEdoCounteragent _counteragent;
        private IEnumerable<RefContractor> _deliveryPoints;

        public CounteragentConsigneesModel(RefEdoCounteragent counteragent, AbtDbContext abt)
        {
            _abt = abt;
            _counteragent = counteragent;
        }

        public override void Refresh()
        {
            if (_counteragent == null)
                return;

            var consignees = (from refEdoCounteragentConsignee in _abt.RefEdoCounteragentConsignees
                             where refEdoCounteragentConsignee.IdCustomerBuyer == _counteragent.IdCustomerBuyer &&
                             refEdoCounteragentConsignee.IdCustomerSeller == _counteragent.IdCustomerSeller
                             join refContr in _abt.RefContractors on refEdoCounteragentConsignee.IdContractorConsignee equals refContr.Id
                             select new RefEdoCounteragentConsigneeForLoading
                             {
                                 RefEdoCounteragentConsignee = refEdoCounteragentConsignee,
                                 ConsigneeName = refContr.Name,
                                 ConsigneeAddress = refContr.Address,
                                 Consignee = refContr
                             }).Where(r => r.RefEdoCounteragentConsignee.IdFnsBuyer.ToUpper() == _counteragent.IdFnsBuyer.ToUpper());

            ItemsList = new System.Collections.ObjectModel.ObservableCollection<RefEdoCounteragentConsigneeForLoading>(consignees);
            SelectedItem = null;
            _deliveryPoints = _abt.RefContractors.Where(r => r.DefaultCustomer == _counteragent.IdCustomerBuyer).ToList();

            OnPropertyChanged("ItemsList");
            OnPropertyChanged("SelectedItem");
        }

        public override void CreateNew()
        {
            var counteragentConsigneeEditModel = new Base.ListViewModel<RefContractor>();
            var consignees = _deliveryPoints.Where(d => !ItemsList.Any(r => r?.Consignee?.Id == d.Id));
            counteragentConsigneeEditModel.ItemsList = new System.Collections.ObjectModel.ObservableCollection<RefContractor>(consignees);

            var counteragentConsigneeEditWindow = new CounteragentConsigneeEditWindow();
            counteragentConsigneeEditWindow.DataContext = counteragentConsigneeEditModel;

            if (counteragentConsigneeEditWindow.ShowDialog() == true)
            {
                var idContractorConsignee = counteragentConsigneeEditModel.SelectedItem.Id;

                if (!_abt.RefEdoCounteragentConsignees.Any(r => r.IdContractorConsignee == idContractorConsignee && r.IdCustomerSeller == _counteragent.IdCustomerSeller))
                {
                    _abt.RefEdoCounteragentConsignees.Add(
                        new RefEdoCounteragentConsignee
                        {
                            IdContractorConsignee = idContractorConsignee,
                            IdCustomerBuyer = _counteragent.IdCustomerBuyer,
                            IdCustomerSeller = _counteragent.IdCustomerSeller,
                            IdFnsBuyer = _counteragent.IdFnsBuyer
                        });
                }
                else
                {
                    var refEdoCounteragentConsignee = _abt.RefEdoCounteragentConsignees.First(r => r.IdContractorConsignee == idContractorConsignee && r.IdCustomerSeller == _counteragent.IdCustomerSeller);
                    refEdoCounteragentConsignee.IdCustomerBuyer = _counteragent.IdCustomerBuyer;
                    refEdoCounteragentConsignee.IdFnsBuyer = _counteragent.IdFnsBuyer;
                }

                _abt.SaveChanges();
                Refresh();
            }
        }

        public override void Delete()
        {
            if(SelectedItem?.RefEdoCounteragentConsignee == null)
            {
                System.Windows.MessageBox.Show("Ошибка! Не выбран грузополучатель для удаления.", "Ошибка", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                return;
            }

            if (System.Windows.MessageBox.Show("Вы действительно хотите удалить грузополучателя?", "Внимание", System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Question) != System.Windows.MessageBoxResult.Yes)
                return;

            var refEdoCounteragentConsignee = _abt.RefEdoCounteragentConsignees.FirstOrDefault(r => r.IdCustomerSeller == SelectedItem.IdCustomerSeller &&
            r.IdCustomerBuyer == SelectedItem.IdCustomerBuyer && r.IdContractorConsignee == SelectedItem.ConsigneeId);

            if (refEdoCounteragentConsignee != null)
                _abt.RefEdoCounteragentConsignees.Remove(refEdoCounteragentConsignee);

            Refresh();
        }


        public RefEdoCounteragent Counteragent => _counteragent;
        public IEnumerable<RefContractor> DeliveryPoints => _deliveryPoints;
    }
}
