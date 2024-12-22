using AvaloniaEdit;
using AvaloniaEdit.TextMate;
using AvaloniaEdit.Highlighting;
using FluentAvalonia.UI.Controls;
using SkEditor.API;
using SkEditor.Utilities.Files;
using SkEditor.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TextMateSharp.Grammars;
using TextMateSharp.Internal.Grammars.Reader;
using TextMateSharp.Internal.Themes.Reader;
using TextMateSharp.Internal.Types;
using TextMateSharp.Registry;
using TextMateSharp.Themes;

namespace SkEditor.Utilities.Syntax;
public static class SyntaxLoader
{
    public static void Load(OpenedFile file)
    {
        int currentTheme = (int)ThemeName.DarkPlus;
        var _registryOptions = new RegistryOptions((ThemeName)currentTheme);
        var _textMateInstallation = file.Editor.InstallTextMate(_registryOptions);

        if (!file.IsSaved) return;

        var extension = Path.GetExtension(file.Path);
        Language language = _registryOptions.GetLanguageByExtension(extension);

        if (language == null)
        {
            var localRegistryOptions = new LocalRegistryOptions();
            var localTextMateInstallation = file.Editor.InstallTextMate(localRegistryOptions);
            localTextMateInstallation.SetGrammar("source.sk");
            localTextMateInstallation.SetTheme(localRegistryOptions.GetDefaultTheme());
            return;
        }

        _textMateInstallation.SetGrammar(_registryOptions.GetScopeByLanguageId(language.Id));
    }
}

class LocalRegistryOptions : IRegistryOptions
{
    public ICollection<string> GetInjections(string scopeName) => null;

    public IRawGrammar GetGrammar(string scopeName)
    {
        string grammarPath = Path.Combine(AppConfig.AppDataFolderPath, "New Syntax Highlighting", "grammars/skript-grammar/skript.tmLanguage.json");
        using StreamReader reader = new(grammarPath);
        return GrammarReader.ReadGrammarSync(reader);
    }

    public IRawTheme GetTheme(string scopeName) => GetDefaultTheme();

    public IRawTheme GetDefaultTheme()
    {
        string themePath = Path.Combine(AppConfig.AppDataFolderPath, "New Syntax Highlighting", "themes/dark_skript.json");
        using StreamReader reader = new(themePath);
        return ThemeReader.ReadThemeSync(reader);
    }
}