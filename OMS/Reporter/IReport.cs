using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reporter
{
    public abstract class IReport
    {
        [NonSerialized]
        internal string documentId;

        public string GetDocumentId()
        {
            return documentId;
        }
    }
}
