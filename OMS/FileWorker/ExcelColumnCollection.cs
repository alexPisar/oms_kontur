using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileWorker
{
    public class ExcelColumnCollection
    {
        private List<ExcelColumn> _columns;

        public ExcelColumnCollection()
        {
            _columns = new List<ExcelColumn>();
        }

        public void AddColumn(string propertyName, string columnName, ExcelType type = ExcelType.String)
        {
            _columns.Add( new ExcelColumn() {
                PropertyName = propertyName,
                ColumnName = columnName,
                Type = type,
                ColumnNumber = 0
            } );
        }

        internal List<ExcelColumn> GetColumns()
        {
            return _columns;
        }
    }
}
