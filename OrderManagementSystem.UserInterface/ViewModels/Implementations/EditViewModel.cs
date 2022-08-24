using System.Windows.Input;

namespace OrderManagementSystem.UserInterface.ViewModels.Implementations
{
	abstract public class EditViewModel<TEntity> : ViewModelBase
	{
		public TEntity Item { get; set; }

		public virtual ICommand SaveCommand { get; set; }
		public virtual ICommand AbortCommand { get; set; }

		public virtual void Save() { }
		public virtual void Abort() { }
	}
}
