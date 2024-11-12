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
        public Kontragent() { }
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

        public string EmchdId { get; set; }

        public string EmchdPersonSurname { get; set; }

        public string EmchdPersonName { get; set; }

        public string EmchdPersonPatronymicSurname { get; set; }

        public string EmchdPersonPosition { get; set; }

        public string EmchdPersonInn { get; set; }

        public DateTime? EmchdBeginDate { get; set; }

        public DateTime? EmchdEndDate { get; set; }

        public Diadoc.Api.Proto.Address Address { get; set; }

        public X509Certificate2 Certificate { get; set; }

        public string OrgId { get; set; }

        public string FnsParticipantId { get; set; }

        public void SetNullEmchdValues()
        {
            this.EmchdId = null;
            this.EmchdBeginDate = null;
            this.EmchdEndDate = null;
            this.EmchdPersonInn = null;
            this.EmchdPersonSurname = null;
            this.EmchdPersonName = null;
            this.EmchdPersonPatronymicSurname = null;
            this.EmchdPersonPosition = null;
        }
    }
}
