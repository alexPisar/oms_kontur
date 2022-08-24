using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileWorker
{
    internal class ExcelColumn
    {
        internal string ColumnName { get; set; }
        internal string PropertyName { get; set; }
        internal int ColumnNumber { get; set; }
        internal ExcelType Type { get; set; }
    }
}
