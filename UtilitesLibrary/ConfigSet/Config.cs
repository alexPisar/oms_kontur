using System;
using UtilitesLibrary.Configuration;

namespace UtilitesLibrary.ConfigSet
{
	public class Config : Configuration<Config>
	{
		public const string ConfFileName = "config.json";

		public bool DebugModeEnabled { get; set; }
		public bool TestModeEnabled { get; set; }
        public bool IsNeedUpdate { get; set; }
        public bool SaveWindowSettings { get; set; }

        public string CertFullPath { get; set; }

        public string AbtDataBaseIpAddress { get; set; }
        public string AbtDataBaseSid { get; set; }

        public string EdiDataBaseIpAddress { get; set; }
        public string EdiDataBaseSid { get; set; }

        public string DataBaseUser { get; set; }
        public string CipherDataBasePassword { get; set; }

        public string EdoLastEventId { get; set; }
		public string EdoBoxId { get; set; }
		public string EdoPartyId { get; set; }
		public string EdoUserName { get; set; }
		public string EdoUserPassword { get; set; }
		public string EdoApiUrl { get; set; }
		public string EdoApiClientId { get; set; }

        public string[] EdiGlns { get; set; }
        public string EdiLastEventId { get; set; }
        public string EdiBoxId { get; set; }
        public string EdiPartyId { get; set; }
        public string EdiUserName { get; set; }
		public string EdiUserPassword { get; set; }
		public string EdiApiUrl { get; set; }
		public string EdiApiClientId { get; set; }
		public int EdiHttpTimeout { get; set; }

		public bool ProxyEnabled { get; set; }
		public string ProxyAddress { get; set; }
		public string ProxyUserName { get; set; }
		public string ProxyUserPassword { get; set; }

		public string MailSmtpServerAddress { get; set; }
		public string MailUserLogin { get; set; }
		public string MailUserPassword { get; set; }
		public string MailUserEmailAddress { get; set; }
		public string MailErrorSubject { get; set; }

        public string UpdaterFilesLoadReference { get; set; }

        public string ConsignorInn { get; set; }

        public int? PositionIndex { get; set; }
        public int? ShiftIndex { get; set; }

        [NonSerialized]
        private string _password = null;

        [NonSerialized]
        private const string _salt = "4rcwnoiherwcez34x]reuhweui5mpqcewe9cw7ed";

        [NonSerialized]
		private static volatile Config _instance;

		[NonSerialized]
		private static readonly object syncRoot = new object();

		private Config() { }

		public static Config GetInstance()
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
						// зато коротко и красиво
						_instance = new Config().Load( ConfFileName );
					}
				}
			}

			return _instance;
		}

        public string GetDataBaseUser()
        {
            return DataBaseUser;
        }

        public string GetDataBasePassword()
        {
            if (_password == null)
            {
                if (!string.IsNullOrEmpty(CipherDataBasePassword))
                {
                    var skitalaBytes = System.Text.Encoding.ASCII.GetBytes(CipherDataBasePassword);
                    var saltData = System.Text.Encoding.ASCII.GetBytes(_salt);

                    int position, shift;
                    GetParametersForPassword(out position, out shift);
                    var bytes = new byte[40];

                    for (int j = 0; j < 8; j++)
                    {
                        for (int k = 0; k < 5; k++)
                        {
                            bytes[j * 5 + k] = skitalaBytes[j + k * 8];
                        }
                    }

                    var passData = new System.Collections.Generic.List<byte>();
                    int i = 0;
                    while (saltData[(position + i * shift) % bytes.Length] != bytes[(position + i * shift) % bytes.Length])
                    {
                        byte b;
                        if (saltData[(position + i * shift) % bytes.Length] > bytes[(position + i * shift) % bytes.Length])
                        {
                            b = (byte)(128 + (int)bytes[(position + i * shift) % bytes.Length] - (int)saltData[(position + i * shift) % bytes.Length]);
                        }
                        else
                        {
                            b = (byte)(bytes[(position + i * shift) % bytes.Length] - saltData[(position + i * shift) % bytes.Length]);
                        }
                        passData.Add(b);
                        i++;
                    }
                    _password = System.Text.Encoding.ASCII.GetString(passData.ToArray());
                }
            }
            return _password;
        }

        public void SetDataBasePassword(string password)
        {
            int position, shift;
            GetParametersForPassword(out position, out shift);

            var passwordData = System.Text.Encoding.ASCII.GetBytes(password);
            var saltData = System.Text.Encoding.ASCII.GetBytes(_salt);

            int i = 0;
            foreach (var p in passwordData)
            {
                var b = (byte)(((int)p + (int)saltData[(position + i * shift) % saltData.Length]) % 128);
                saltData[(position + i * shift) % saltData.Length] = b;
                i++;
            }

            byte[] skitalaBytes = new byte[40];

            for (int j = 0; j < 5; j++)
            {
                for (int k = 0; k < 8; k++)
                {
                    skitalaBytes[8 * j + k] = saltData[j + 5 * k];
                }
            }

            CipherDataBasePassword = System.Text.Encoding.ASCII.GetString(skitalaBytes);
            _password = password;
        }

        public void GenerateParametersForPassword()
        {
            var rand = new Random();
            PositionIndex = rand.Next() % 40;
            ShiftIndex = rand.Next() % 5;
        }

        public void GetParametersForPassword(out int position, out int shift)
        {
            if (PositionIndex == null || ShiftIndex == null)
                GenerateParametersForPassword();

            int[] arrPosition = new int[40]
            {
               36, 39, 38, 2, 15, 16, 8, 26, 31, 21, 28, 5, 25, 9, 27, 18, 4, 29, 33, 34, 14, 35, 24, 0, 6, 10, 7, 23, 11, 13, 22, 1, 19, 17, 32, 3, 20, 12, 30, 37
            };

            int[] arrShift = new int[] { 13, 7, 19, 17, 23 };

            position = arrPosition[PositionIndex.Value];
            shift = arrShift[ShiftIndex.Value];
        }
    }
}
