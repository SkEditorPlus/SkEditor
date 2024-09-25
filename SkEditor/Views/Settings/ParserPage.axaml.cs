using System;
using Avalonia.Controls;
using Avalonia.Threading;
using AvaloniaEdit;
using CommunityToolkit.Mvvm.Input;
using SkEditor.API;
using SkEditor.Utilities;
using SkEditor.Utilities.Files;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Interactivity;
using SkEditor.Utilities.InternalAPI;
using SkEditor.ViewModels;

namespace SkEditor.Views.Settings;
public partial class ParserPage : UserControl
{
    
    public ParserPage()
    {
        InitializeComponent();

        var config = SkEditorAPI.Core.GetAppConfig();
        DataContext = new ParserSettingsViewModel
        {
            Warnings = Registries.ParserWarnings.Select(warning => new ParserSettingsViewModel.WarningData(
                warning,
                FileParser.IsWarningIgnored(warning.Identifier)
            )).ToList()
        };
            
        Title.BackButton.Command = new RelayCommand(() => SettingsWindow.NavigateToPage(typeof(HomePage)));
    }
}
