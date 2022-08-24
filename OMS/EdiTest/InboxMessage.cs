using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SkbKontur.EdiApi.Client.Types.Common;
using SkbKontur.EdiApi.Client.Types.Messages;
using System.Threading.Tasks;

namespace EdiTest
{
	public class InboxMessage
	{
		public MessageData Data { get; set; }
		public InboxMessageMeta Meta { get; set; }
	}
}
