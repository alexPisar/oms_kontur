using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EdiProcessingUnit;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ProfileManager.Models
{
    public class ProfileModel : INotifyPropertyChanged
    {
        public UsersConfig ProfileConfig { get; set; }
        public string DataBaseUser { get; set; }

        /// <summary>Событие для извещения об изменения свойства</summary>
		public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>Метод для вызова события извещения об изменении свойства</summary>
		/// <param name="prop">Изменившееся свойство или список свойств через разделители "\\/\r \n()\"\'-"</param>
		public void OnPropertyChanged([CallerMemberName]string prop = "")
        {
            string[] names = prop.Split("\\/\r \n()\"\'-".ToArray(), StringSplitOptions.RemoveEmptyEntries);
            if (names.Length != 0)
                foreach (string _prp in names)
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(_prp));
        }
    }
}
