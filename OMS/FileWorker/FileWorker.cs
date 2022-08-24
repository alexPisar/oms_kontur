using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileWorker
{
    internal interface IFileWorker
    {
        void SaveFile();

        void ExportData();

        void ImportData<T>() where T : class, new();
    }
}
