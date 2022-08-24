using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EdiProcessingUnit
{
	public static class Constants
	{
		public const string AppName = "EDI PROCESSING UNIT";
		public const int VersionMajor = 1;
		public const int VersionMinor = 2;
		public const int Build = 3;

		public static string Version => $"{VersionMajor}.{VersionMinor} ({Constants.Build})";
	}
}
