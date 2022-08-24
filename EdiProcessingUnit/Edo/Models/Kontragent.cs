using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;

namespace EdiProcessingUnit.Edo.Models
{
    public class Kontragent
    {
        public Kontragent(string name, string inn, string kpp)
        {
            Name = name;
            Inn = inn;
            Kpp = kpp;
        }
        public int Index { get; set; }

        public string Name { get; set; }

        public string Inn { get; set; }

        public string Kpp { get; set; }

        public Diadoc.Api.Proto.Address Address { get; set; }

        public X509Certificate2 Certificate { get; set; }

        public string OrgId { get; set; }

        public string FnsParticipantId { get; set; }
    }
}
