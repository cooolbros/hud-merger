﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace hud_merger.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "16.10.0.0")]
    public sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool Download_latest_HUD_file_definitions_file_on_start_up {
            get {
                return ((bool)(this["Download_latest_HUD_file_definitions_file_on_start_up"]));
            }
            set {
                this["Download_latest_HUD_file_definitions_file_on_start_up"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool Extract_required_TF2_HUD_files_on_startup {
            get {
                return ((bool)(this["Extract_required_TF2_HUD_files_on_startup"]));
            }
            set {
                this["Extract_required_TF2_HUD_files_on_startup"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Team Fortress 2")]
        public string Team_Fortress_2_Folder {
            get {
                return ((string)(this["Team_Fortress_2_Folder"]));
            }
            set {
                this["Team_Fortress_2_Folder"] = value;
            }
        }
    }
}
