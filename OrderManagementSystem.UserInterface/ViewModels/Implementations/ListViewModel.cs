using System;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace OrderManagementSystem.UserInterface.ViewModels.Implementations
{
	abstract public class ListViewModel<TEntity> : ViewModelBase
	{
		public ObservableCollection<TEntity> ItemsList { get; set; }
		public TEntity SelectedItem { get; set; }

		public virtual RelayCommand CreateNewCommand { get; set; }
		public virtual RelayCommand DeleteCommand { get; set; }
		public virtual RelayCommand EditCommand { get; set; }
		public virtual RelayCommand RefreshCommand { get; set; }

		public virtual void CreateNew() { }
		public virtual void Delete() { }
		public virtual void Edit() { }
		public virtual void Refresh() { }

	}
}
