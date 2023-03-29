using System.Collections.ObjectModel;
using KonturEdoClient.Models.Implementations;

namespace KonturEdoClient.Models.Base
{
    public class ListViewModel<TEntity> : ModelBase
    {
        protected UtilitesLibrary.Logger.UtilityLog _log = UtilitesLibrary.Logger.UtilityLog.GetInstance();

        public ObservableCollection<TEntity> ItemsList { get; set; }
        public TEntity SelectedItem { get; set; }

        public virtual RelayCommand CreateNewCommand => new RelayCommand((o) => CreateNew());
        public virtual RelayCommand DeleteCommand => new RelayCommand((o) => Delete());
        public virtual RelayCommand EditCommand => new RelayCommand((o) => Edit());
        public virtual RelayCommand RefreshCommand => new RelayCommand((o) => Refresh());
        public virtual RelayCommand SaveCommand => new RelayCommand((o) => Save());

        public virtual void CreateNew() { }
        public virtual void Delete() { }
        public virtual void Edit() { }
        public virtual void Refresh() { }
        public virtual void Save() { }
    }
}
