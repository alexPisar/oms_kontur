using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KonturEdoClient.Models
{
    public class LoadModel : Base.ModelBase
    {
        private string _text;

        public string PathToImage { get; set; }
        public bool OkEnable { get; set; }
        public string Text
        {
            get 
                {
                return _text;
            }
            set 
                {
                _text = value;
                OnPropertyChanged("Text");
            }
        }

        public LoadModel()
        {
            PathToImage = "pack://siteoforigin:,,,/Resources/download.gif";
            OkEnable = false;
            Text = "Подождите, идёт загрузка данных";
        }
    }
}
