using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace UpdaterKonturEdo
{
    public class UpdaterModel : INotifyPropertyChanged
    {
        private int _progress;
        private int _progressMaximum;
        private string _loadText;
        private string _contentButton;
        private bool _isCancelButtonEnabled;
        private bool _isStartButtonEnabled;
        private bool _isChangeButtonEnabled;
        private bool _isTabItemEnabled;
        private System.Windows.Visibility _checkBoxVisibility;

        public int Progress
        {
            get {
                return _progress;
            }
            set {
                _progress = value;
                OnPropertyChanged("Progress");
            }
        }

        public int ProgressMaximum
        {
            get {
                return _progressMaximum;
            }
            set {
                _progressMaximum = value;
                OnPropertyChanged("ProgressMaximum");
            }
        }

        public string LoadText
        {
            get {
                return _loadText;
            }
            set {
                _loadText = value;
                OnPropertyChanged("LoadText");
            }
        }

        public bool IsCancelButtonEnabled
        {
            get {
                return _isCancelButtonEnabled;
            }
            set {
                _isCancelButtonEnabled = value;
                OnPropertyChanged("IsCancelButtonEnabled");
            }
        }

        public bool IsStartButtonEnabled
        {
            get {
                return _isStartButtonEnabled;
            }
            set {
                _isStartButtonEnabled = value;
                OnPropertyChanged("IsStartButtonEnabled");
            }
        }

        public bool IsChangeButtonEnabled
        {
            get {
                return _isChangeButtonEnabled;
            }
            set {
                _isChangeButtonEnabled = value;
                OnPropertyChanged("IsChangeButtonEnabled");
            }
        }

        public bool IsTabItemEnabled
        {
            get {
                return _isTabItemEnabled;
            }
            set {
                _isTabItemEnabled = value;
                OnPropertyChanged("IsTabItemEnabled");
            }
        }

        public string ContentButton
        {
            get {
                return _contentButton;
            }
            set {
                _contentButton = value;
                OnPropertyChanged("ContentButton");
            }
        }

        public System.Windows.Visibility CheckBoxVisibility
        {
            get {
                return _checkBoxVisibility;
            }
            set {
                _checkBoxVisibility = value;
                OnPropertyChanged("CheckBoxVisibility");
            }
        }

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
