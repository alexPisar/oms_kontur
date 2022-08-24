using System.Windows.Input;

namespace OrderManagementSystem.UserInterface.ViewModels.Implementations
{
	public class CreateViewModel<TEntity> : ViewModelBase
	{
		public TEntity Item { get; set; }

		public virtual ICommand AbortCommand { get; set; }
		public virtual ICommand ConfirmCreationCommand { get; set; }

		public virtual void Abort() { }
		public virtual void ConfirmCreation() { }
	}
}
