using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using OrderManagementSystem.UserInterface.ViewModels.Implementations;
using DataContextManagementUnit.DataAccess.Contexts.Edi;

namespace OrderManagementSystem.UserInterface.ViewModels
{
    public class SelectedCompanyModel : ListViewModel<RefCompany>
    {
        public SelectedCompanyModel(List<RefCompany> companies)
        {
            ItemsList = new ObservableCollection<RefCompany>(companies);
        }
    }
}
