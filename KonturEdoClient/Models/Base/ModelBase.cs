using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Reflection;

namespace KonturEdoClient.Models.Base
{
    public class ModelBase : INotifyPropertyChanged
    {
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

        /// <summary>Метод для вызова события извещения об изменении списка свойств</summary>
		/// <param name="propList">Последовательность имён свойств</param>
		public void OnPropertyChanged(IEnumerable<string> propList)
        {
            foreach (string _prp in propList.Where(name => !string.IsNullOrWhiteSpace(name)))
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(_prp));
        }

        /// <summary>Метод для вызова события извещения об изменении списка свойств</summary>
        /// <param name="propList">Последовательность свойств</param>
        public void OnPropertyChanged(IEnumerable<PropertyInfo> propList)
        {
            foreach (PropertyInfo _prp in propList)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(_prp.Name));
        }

        /// <summary>Метод для вызова события извещения об изменении всех свойств</summary>
        public void OnAllPropertyChanged() => OnPropertyChanged(GetType().GetProperties());
    }
}
