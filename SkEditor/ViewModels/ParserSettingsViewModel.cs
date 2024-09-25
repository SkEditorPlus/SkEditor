using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using SkEditor.API;

namespace SkEditor.ViewModels;

public partial class ParserSettingsViewModel : ObservableObject
{
    [ObservableProperty] private List<WarningData> _warnings = new();
    
    public partial class WarningData : ObservableObject
    {
        [ObservableProperty] private ParserWarning _warning;
        private bool _isEnabled;
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                OnPropertyChanging();
                _isEnabled = value;
                OnPropertyChanged();
                    
                SkEditorAPI.Logs.Debug($"Setting warning {Warning.Identifier} to {value}");
                var config = SkEditorAPI.Core.GetAppConfig();
                config.IgnoredParserWarnings[Warning.Identifier] = value;
            }
        }
        
        public WarningData(ParserWarning warning, bool isEnabled)
        {
            Warning = warning;
            SkEditorAPI.Logs.Error($"Warn {warning.Identifier} is enabled: {isEnabled}");
            _isEnabled = isEnabled;
            IsEnabled = isEnabled;
        }
    }
}