using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using OrderManagementSystem.UserInterface.ViewModels.Implementations;

namespace OrderManagementSystem.UserInterface.ViewModels
{
    public class SettingsModel : ViewModelBase
    {
        private bool _isNeedUpdate;
        private bool _saveWindowSettings;

        public string AccountName { get; set; }
        public string DataBaseUser { get; set; }
        public string AbtDataBaseIpAddress { get; set; }
        public string AbtDataBaseSid { get; set; }
        public string EdiDataBaseIpAddress { get; set; }
        public string EdiDataBaseSid { get; set; }
        public bool IsNeedUpdate
        {
            get 
            {
                return _isNeedUpdate;
            }
            set 
            {
                _isNeedUpdate = value;
                OnPropertyChanged("IsNeedUpdate");
            }
        }
        public bool SaveWindowSettings
        {
            get {
                return _saveWindowSettings;
            }
            set {
                _saveWindowSettings = value;
                OnPropertyChanged("SaveWindowSettings");
            }
        }
        public string UpdaterFilesLoadReference { get; set; }
    }
}
