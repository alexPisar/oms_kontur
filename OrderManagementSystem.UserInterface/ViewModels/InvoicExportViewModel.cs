using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataContextManagementUnit.DataAccess.Contexts.Edi;
using DataContextManagementUnit.DataAccess.Contexts.Abt;
using OrderManagementSystem.UserInterface.ViewModels.Implementations;
using System.Collections.ObjectModel;
using EdiProcessingUnit.UnifiedTransferDocument;

namespace OrderManagementSystem.UserInterface.ViewModels
{
	public class InvoicExportViewModel : ListViewModel<ViewInvoicHead>
	{
		public override RelayCommand RefreshCommand => new RelayCommand( (o) => { Refresh(); } );
		public RelayCommand ExportSCHFDOPPRCommand => new RelayCommand( (o) => { ExportSCHFDOPPR(); } );
		
		private void Refresh()
		{
			_log.Log( System.Reflection.MethodBase.GetCurrentMethod().Name );
			Func<ViewInvoicHead, bool> predicate;

			DateTime dtNow = DateTime.Now;
			DateTime compareDate = new DateTime( dtNow.Year, dtNow.Month, dtNow.Day );
			
			predicate = new Func<ViewInvoicHead, bool>( x =>
				/*(x.ActStatus == 10 || x.ActStatus == 11) && 
				string.IsNullOrEmpty(x.ErrorStatus) &&
				x.Deleted == 0 &&
				x.IdDocType == 8 &&
				x.DocDatetime.CompareTo(DateTime.Now.AddDays(-1)) >= 0*/
				//x.DocDatetime.Value.CompareTo( DateTime.Now.AddDays( -1 ) ) >= 0
				true
			);

			List<ViewInvoicHead> docs = new List<ViewInvoicHead>();
			docs = _edi.ViewInvoicHeads.Where( predicate ).ToList();
			ItemsList = new ObservableCollection<ViewInvoicHead>( docs );
			UpdateProps();
		}

		public void ExportSCHFDOPPR()
		{
			_log.Log( System.Reflection.MethodBase.GetCurrentMethod().Name );
			UtdHandler.Create( SelectedItem, _abt,_edi );
			var utd = UtdHandler.AsObject;
			var utlXml = UtdHandler.AsXmlString;
			UpdateProps();
		}

		private void UpdateProps()
		{
			OnPropertyChanged( nameof( ItemsList ) );
			OnPropertyChanged( nameof( SelectedItem ) );
		}

		public InvoicExportViewModel(AbtDbContext AbtDbContext, EdiDbContext EdiDbContext)
		{
			_log.Log( System.Reflection.MethodBase.GetCurrentMethod().Name );
			_abt = AbtDbContext;
			_edi = EdiDbContext;
			
			ItemsList = new ObservableCollection<ViewInvoicHead>();
			Refresh();
			UpdateProps();


		}
	}
}
