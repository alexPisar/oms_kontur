using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebService.Models
{
    public class FileParameter
    {
        public FileParameter(string name)
        {
            Name = name;
        }

        public string Name { get; set; }

        public string FileName { get; set; }

        public string ContentType { get; set; }

        public string ContentDispositionType { get; set; }
    }
}
