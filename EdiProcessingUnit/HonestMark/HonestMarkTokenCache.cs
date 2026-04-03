using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UtilitesLibrary.Configuration;

namespace EdiProcessingUnit.HonestMark
{
    public class HonestMarkTokenCache : Configuration<HonestMarkTokenCache>
    {
        private const string localPath = "honestmark";
        protected override string Path(string FileName) => ConfigFolder + "\\" + localPath + "\\" + FileName;

        public HonestMarkTokenCache()
        {
            string currentDirectoryPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            if (!System.IO.Directory.Exists(currentDirectoryPath + "\\" + ConfigFolder + "\\" + localPath))
                System.IO.Directory.CreateDirectory(currentDirectoryPath + "\\" + ConfigFolder + "\\" + localPath);
        }

        public string Token { get; set; }
        public DateTime TokenCreationDate { get; set; }
        public DateTime TokenExpirationDate { get; set; }
    }
}
