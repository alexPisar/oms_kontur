using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using DataContextManagementUnit.DataAccess.Contexts.Abt;

namespace OrderManagementSystem.UserInterface.Infrastructure
{
    public class DocJournalByRecadv
    {
        private DocJournal _docJournal;
        private DocJournal _invoiceDocJournal;
        private double _docJournalTotalAmount;
        private int _docJournalTotalQuantity;

        public DocJournal InvoiceDocJournal
        {
            get {
                if (_invoiceDocJournal == null && _docJournal != null)
                    _invoiceDocJournal = _docJournal?.DocJournals?.FirstOrDefault(j => j.IdDocType == 8);

                return _invoiceDocJournal;
            }
        }

        public long IdDocJournal { get; set; }
        public string OrderNumber { get; set; }

        public string InvoicNumber => InvoiceDocJournal?.Code;

        public string DocJournalNumber => _docJournal?.Code;

        public string BuyerName { get; set; }
        public string ShipToName { get; set; }
        public double DocJournalTotalAmount => _docJournalTotalAmount;
        public int DocJournalTotalQuantity => _docJournalTotalQuantity;
        public string RecadvTotalAmount { get; set; }
        public int RecadvTotalQuantity { get; set; }

        public DocJournal GetDocJournal(IEnumerable<DocJournal> docJournals = null)
        {
            if (_docJournal == null && docJournals != null)
            {
                _docJournal = docJournals.FirstOrDefault(j => j.Id == IdDocJournal);

                if(_docJournal != null)
                {
                    _docJournalTotalAmount = InvoiceDocJournal?.DocGoodsI?.TotalSumm ?? 0;
                    _docJournalTotalQuantity = InvoiceDocJournal?.DocGoodsDetailsIs?.Sum(g => g.Quantity) ?? 0;
                }
            }

            return _docJournal;
        }
    }
}
