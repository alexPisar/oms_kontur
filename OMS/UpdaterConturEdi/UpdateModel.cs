using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UpdaterConturEdi
{
    public class UpdateModel : INotifyPropertyChanged
    {
        private string _text;
        private double _progress;
        private bool _isEnableButton;
        private System.Windows.Visibility _isVisibleStartUppCheckBox;
        private System.Windows.Visibility _isVisibleCancelButton;
        private double _maximum;
        private string _contentButton;

        public string Text
        {
            get 
            {
                return _text;
            }
            set 
            {
                _text = value;
                OnPropertyChanged( "Text" );
            }
        }
        public double Progress
        {
            get 
            {
                return _progress;
            }
            set 
            {
                _progress = value;
                OnPropertyChanged( "Progress" );
            }
        }
        public bool IsEnableButton
        {
            get 
            {
                return _isEnableButton;
            }
            set 
            {
                _isEnableButton = value;
                OnPropertyChanged( "IsEnableButton" );
            }
        }
        public System.Windows.Visibility IsVisibleStartUppCheckBox
        {
            get 
            {
                return _isVisibleStartUppCheckBox;
            }

            set 
            {
                _isVisibleStartUppCheckBox = value;
                OnPropertyChanged( "IsVisibleStartUppCheckBox" );
            }
        }
        public System.Windows.Visibility IsVisibleCancelButton
        {
            get 
            {
                return _isVisibleCancelButton;
            }
            set 
            {
                _isVisibleCancelButton = value;
                OnPropertyChanged( "IsVisibleCancelButton" );
            }
        }
        public double Maximum
        {
            get 
            {
                return _maximum;
            }
            set 
            {
                _maximum = value;
                OnPropertyChanged( "Maximum" );
            }
        }
        public string ContentButton
        {
            get 
            {
                return _contentButton;
            }
            set 
            {
                _contentButton = value;
                OnPropertyChanged( "ContentButton" );
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
    }
}
