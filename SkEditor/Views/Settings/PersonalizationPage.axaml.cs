using Avalonia;
using Avalonia.Controls;
using AvaloniaEdit;
using CommunityToolkit.Mvvm.Input;
using SkEditor.API;
using SkEditor.Utilities;
using SkEditor.ViewModels;
using System.Collections.Generic;
using System.Linq;

namespace SkEditor.Views.Settings;
public partial class PersonalizationPage : UserControl
{
    public PersonalizationPage()
    {
        InitializeComponent();

        DataContext = SkEditorAPI.Core.GetAppConfig();

        AssignCommands();
    }

    private void AssignCommands()
    {
        ThemePageButton.Command = new RelayCommand(() => SettingsWindow.NavigateToPage(typeof(ThemePage)));
        //SyntaxPageButton.Command = new RelayCommand(() => SettingsWindow.NavigateToPage(typeof(FileSyntaxes)));
        Title.BackButton.Command = new RelayCommand(() => SettingsWindow.NavigateToPage(typeof(HomePage)));

        FontButton.Command = new RelayCommand(SelectFont);

        HighlightCurrentLineSwitch.Command = new RelayCommand(() =>
        {
            foreach (TextEditor textEditor in SkEditorAPI.Files.GetOpenedEditors().Select(x => x.Editor))
            {
                textEditor.Options.HighlightCurrentLine = !textEditor.Options.HighlightCurrentLine;
            }
        });
    }

    private async void SelectFont()
    {
        FontSelectionWindow window = new();
        string result = await window.ShowDialog<string>(SkEditorAPI.Windows.GetMainWindow());
        if (result is null)
            return;

        SkEditorAPI.Core.GetAppConfig().Font = result;
        CurrentFont.Description = Translation.Get("SettingsPersonalizationFontDescription").Replace("{0}", result);

        SkEditorAPI.Files.GetOpenedFiles().Where(o => o.IsEditor).ToList().ForEach(i =>
        {
            if (result.Equals("Default"))
            {
                Application.Current.TryGetResource("JetBrainsFont", Avalonia.Styling.ThemeVariant.Default, out object font);
                i.Editor.FontFamily = (Avalonia.Media.FontFamily)font;
            }
            else
            {
                i.Editor.FontFamily = new(result);
            }
        });
    }
}
