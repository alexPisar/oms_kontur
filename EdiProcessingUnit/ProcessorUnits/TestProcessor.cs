using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataContextManagementUnit.DataAccess.Contexts.Abt;
using EdiProcessingUnit.Infrastructure;
using UtilitesLibrary.ConfigSet;

namespace EdiProcessingUnit.ProcessorUnits
{
	public class ParametersProcessor : EdiProcessor
	{
		internal AbtDbContext _abtDbContext;
		private bool _isTest = Config.GetInstance()?.TestModeEnabled ?? false;

		public override void Run()
		{
			ProcessorName = "TestProcessor";

			//_edi.TestMethod();
		}
	}
}
