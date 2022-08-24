using System;
using UtilitesLibrary.Configuration;

namespace EdiProcessingUnit.Edi
{
	public class EdiTokenCache : Configuration<EdiTokenCache>
	{
		public EdiTokenCache(string AuthToken, string Creator, string PartyId, string BoxId, string LastEventId, string orgGln)
		{
			this.PartyId = PartyId;
            this.BoxId = BoxId;
            this.LastEventId = LastEventId;
            Token = AuthToken;
			TokenCreator = Creator;
			TokenCreationDate = DateTime.Now;
			TokenExpirationDate = DateTime.Now.AddHours( 12 );
            Gln = orgGln;
        }

		public EdiTokenCache() { }
        public string LastEventId { get; set; }
        public string BoxId { get; set; }
        public string PartyId { get; set; }
		public string Token { get; set; }
		public string TokenCreator { get; set; }
		public DateTime TokenCreationDate { get; set; }
		public DateTime TokenExpirationDate { get; set; }
        public string Gln { get; set; }
	}
}
