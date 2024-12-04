using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using DataContextManagementUnit.DataAccess.Contexts.Abt;
using DataContextManagementUnit.DataAccess.Contexts.Edi;
using UtilitesLibrary.Logger;

namespace OrderManagementSystem.UserInterface.ViewModels.Implementations
{
	abstract public class ViewModelBase : INotifyPropertyChanged, IDisposable
	{
		internal UtilityLog _log = UtilityLog.GetInstance();
		internal bool _isLoaded { get; set; } = false;
		public virtual event Action Loaded = null;
		public virtual event Action Unloaded = null;
		public delegate void ErrorHandler(string errorMsg);
		public virtual event ErrorHandler OnError = delegate { };

		internal AbtDbContext _abt { get; set; }
		internal EdiDbContext _edi { get; set; }

		public void ShowError(string errorText)
		{
			OnError( errorText );
		}

		public ViewModelBase()
		{
			// TODO: Add your constructor code here
			// The ctor is always called, initialize view model so that it also works in designer
		}

		public virtual void Initialize()
		{
			// TODO: Add your initialization code here 
			// This method is only called when the application is running
		}
		
		public virtual void OnLoaded()
		{
			if (!_isLoaded)
			{
				// TODO: Add your loaded code here 
				_isLoaded = true;
			}
		}

		public virtual void OnUnloaded()
		{
			if (_isLoaded)
			{
				// TODO: Add your cleanup/unloaded code here 
				_isLoaded = false;
			}
		}

		/// <summary>Событие для извещения об изменения свойства</summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>Метод для вызова события извещения об изменении свойства</summary>
		/// <param name="prop">Изменившееся свойство или список свойств через разделители "\\/\r \n()\"\'-"</param>
		public void OnPropertyChanged([CallerMemberName]string prop = "")
		{
			string[] names = prop.Split( "\\/\r \n()\"\'-".ToArray(), StringSplitOptions.RemoveEmptyEntries );
			if (names.Length != 0)
				foreach (string _prp in names)
					PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( _prp ) );
		}

		/// <summary>Метод для вызова события извещения об изменении списка свойств</summary>
		/// <param name="propList">Последовательность имён свойств</param>
		public void OnPropertyChanged(IEnumerable<string> propList)
		{
			foreach (string _prp in propList.Where( name => !string.IsNullOrWhiteSpace( name ) ))
				PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( _prp ) );
		}

		/// <summary>Метод для вызова события извещения об изменении списка свойств</summary>
		/// <param name="propList">Последовательность свойств</param>
		public void OnPropertyChanged(IEnumerable<PropertyInfo> propList)
		{
			foreach (PropertyInfo _prp in propList)
				PropertyChanged?.Invoke( this, new PropertyChangedEventArgs( _prp.Name ) );
		}

		/// <summary>Метод для вызова события извещения об изменении всех свойств</summary>
		public void OnAllPropertyChanged() => OnPropertyChanged( GetType().GetProperties() );

        /// <summary>Метод для универсального парсинга строки в число</summary>
		/// <param name="text">Строка, которую нужно распарсить</param>
        public int ParseStringToInt(string text)
        {
            decimal result;

            if (decimal.TryParse(text, out result))
            {
                return (int)result;
            }
            else
            {
                _log.Log("Попытка парсинга к типу decimal была неудачной");
                var resDouble = double.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
                
                var res = Convert.ToInt32(resDouble);
                return res;
            }
        }

        /// <summary>Метод для универсального парсинга строки в число с плавающей точкой</summary>
		/// <param name="text">Строка, которую нужно распарсить</param>
        public double ParseStringToDouble(string text)
        {
            double result;

            if (!double.TryParse(text, out result))
            {
                try
                {
                    result = double.Parse(text, System.Globalization.CultureInfo.InvariantCulture);
                }
                catch (Exception ex)
                {

                }
            }

            return result;
        }

        protected virtual void OnDispose() { }

		public void Dispose()
		{
			OnDispose();
		}
	}
}
