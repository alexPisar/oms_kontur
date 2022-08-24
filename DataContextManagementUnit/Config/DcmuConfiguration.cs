using System;
using System.Collections.Generic;
using UtilitesLibrary.Configuration;

namespace DataContextManagementUnit
{
	public class DcmuConfiguration : Configuration<DcmuConfiguration>
	{
		public const string ConfFileName = "dcmu_conf.json";

		public string AbtConnectionString { get; set; }
		public string EdiConnectionString { get; set; }

		private DcmuConfiguration() { }


		[NonSerialized]
		private static volatile DcmuConfiguration _instance;

		[NonSerialized]
		private static readonly object syncRoot = new object();

		public static DcmuConfiguration GetInstance()
		{
			// тут страндартный дабл-чек с блокировкой
			// для создания инстанса синглтона безопасно для многопоточности
			if (_instance == null)
			{
				lock (syncRoot)
				{
					if (_instance == null)
					{
						// а тут паттерн пошёл по пизде.
						// зато красиво
						_instance = new DcmuConfiguration().Load( ConfFileName );

					}
				}
			}

			return _instance;
		}

	}
}
