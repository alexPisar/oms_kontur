using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reporter
{
    public interface IReport
    {
        void Parse(string content);

        void Parse(byte[] content);

        string GetXmlContent();
    }
}
