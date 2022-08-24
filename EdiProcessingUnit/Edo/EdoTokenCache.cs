using System;
using UtilitesLibrary.Configuration;

namespace EdiProcessingUnit.Edo
{
	public class EdoTokenCache : Configuration<EdoTokenCache>
	{
		public EdoTokenCache(string AuthToken, string Creator, string PartyId)
		{
			this.PartyId = PartyId;
			Token = AuthToken;
			TokenCreator = Creator;
			TokenCreationDate = DateTime.Now;
			TokenExpirationDate = DateTime.Now.AddHours( 12 );
		}

		public EdoTokenCache() { }
		public string PartyId { get; set; }
		public string Token { get; set; }
		public string TokenCreator { get; set; }
		public DateTime TokenCreationDate { get; set; }
		public DateTime TokenExpirationDate { get; set; }
	}
}
