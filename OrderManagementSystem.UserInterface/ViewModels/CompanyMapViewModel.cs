using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataContextManagementUnit.DataAccess.Contexts.Abt;
using DataContextManagementUnit.DataAccess.Contexts.Edi;
using OrderManagementSystem.UserInterface.ViewModels.Implementations;
using System.Collections.ObjectModel;

namespace OrderManagementSystem.UserInterface.ViewModels
{
	public class CompanyMapViewModel : ListViewModel<RefCompany>
	{
		public RelayCommand SaveCommand => new RelayCommand( (o) => { Save(); } );
		public override RelayCommand RefreshCommand => new RelayCommand( (o) => { Refresh(); } );
		public RelayCommand ContractorSelectionChangedCommand => new RelayCommand( (o) => { ContractorSelectionChanged(); } );
		
		public ObservableCollection<ViewRefFilial> FilialsList { get; set; } = new ObservableCollection<ViewRefFilial>();
		public ObservableCollection<ViewRefContractor> RefContractorList { get; set; } = new ObservableCollection<ViewRefContractor>();
		
		public ViewRefFilial SelectedFilial { get; set; }
		public ViewRefContractor SelectedRefContractor { get; set; }

		private void Refresh()
		{
			_log.Log( System.Reflection.MethodBase.GetCurrentMethod().Name );
			ItemsList = new ObservableCollection<RefCompany>( _edi.RefCompanies.ToList() );
			UpdateProps();
		}

		private void ContractorSelectionChanged()
		{
			SelectedFilial = FilialsList.SingleOrDefault(x => x.Id == (SelectedItem?.IdDbFilial ?? 0) );
			SelectedRefContractor = RefContractorList.SingleOrDefault(x => x.Id == (SelectedItem?.IdContractor ?? 0) );

			UpdateProps();
		}

		private void Save()
		{
			_log.Log( System.Reflection.MethodBase.GetCurrentMethod().Name );

			if(SelectedRefContractor != null)
				SelectedItem.IdContractor = (long?)SelectedRefContractor.Id;

			if (SelectedFilial != null)
				SelectedItem.IdDbFilial = SelectedFilial.Id;

			_edi.SaveChanges();

			UpdateProps();
		}

		public CompanyMapViewModel(AbtDbContext AbtDbContext, EdiDbContext EdiDbContext)
		{
			_log.Log( System.Reflection.MethodBase.GetCurrentMethod().Name );
			_abt = AbtDbContext;
			_edi = EdiDbContext;

			FilialsList = new ObservableCollection<ViewRefFilial>(_edi.ViewRefFilials.ToList());
			RefContractorList = new ObservableCollection<ViewRefContractor>( _edi.ViewRefContractors.ToList() );

			UpdateProps();
		}

		private void UpdateProps()
		{
			OnPropertyChanged( nameof( SelectedRefContractor ) );
			OnPropertyChanged( nameof( SelectedItem ) );
			OnPropertyChanged( nameof( SelectedFilial ) );

			OnPropertyChanged( nameof( RefContractorList ) );
			OnPropertyChanged( nameof( FilialsList ) );
			OnPropertyChanged( nameof( ItemsList ) );
		}

	}
}
