﻿//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан программой.
//     Исполняемая версия:4.0.30319.42000
//
//     Изменения в этом файле могут привести к неправильной работе и будут потеряны в случае
//     повторной генерации кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace OrderManagementSystem.UserInterface.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "15.8.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("User Id=redmine;Password=Ecwiegrool;Data Source=192.168.2.37/orcl")]
        public string FullDataConnectionString {
            get {
                return ((string)(this["FullDataConnectionString"]));
            }
            set {
                this["FullDataConnectionString"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("DocumentsDataGrid.xml")]
        public string OrdersDataGridLayoutsFileConfigName {
            get {
                return ((string)(this["OrdersDataGridLayoutsFileConfigName"]));
            }
            set {
                this["OrdersDataGridLayoutsFileConfigName"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("DocumentDetailsDataGrid.xml")]
        public string OrderDetailsGridLayoutsFileConfigName {
            get {
                return ((string)(this["OrderDetailsGridLayoutsFileConfigName"]));
            }
            set {
                this["OrderDetailsGridLayoutsFileConfigName"] = value;
            }
        }
    }
}
