using Avalonia.Threading;
using FluentAvalonia.UI.Controls;
using Newtonsoft.Json;
using Serilog;
using SkEditor.API;
using SkEditor.Utilities;
using SkEditor.Utilities.Styling;
using SkEditor.Utilities.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SkEditor.Views.Marketplace.Types;
public class ThemeWithSyntaxItem : MarketplaceItem
{
    [JsonProperty("themeFile")]
    public string ThemeFileUrl { get; set; }

    [JsonProperty("syntaxFolders")]
    public string[] SyntaxFolders { get; set; }

    public async override void Install()
    {
        //string themeFileName = ThemeFileUrl.Split('/').Last();
        //string themeFilePath = Path.Combine(AppConfig.AppDataFolderPath, "Themes", themeFileName);
        //string baseLocalSyntaxPath = Path.Combine(AppConfig.AppDataFolderPath, "Syntax Highlighting");
        ////string syntaxFilePath = Path.Combine(AppConfig.AppDataFolderPath, "Syntax highlighting", syntaxFileName);

        //List<FileSyntax> installedSyntaxes = [];
        //bool allInstalled = true;
        //allInstalled = allInstalled && await Install(ThemeFileUrl, themeFilePath);
        //foreach (string folder in SyntaxFolders)
        //{
        //    var folderName = folder.Split('/').Last();
        //    string localSyntaxPath = Path.Combine(baseLocalSyntaxPath,
        //        folderName);
        //    Directory.CreateDirectory(localSyntaxPath);
        //    allInstalled = allInstalled && await Install(folder + "/config.json", Path.Combine(localSyntaxPath, "config.json"));
        //    allInstalled = allInstalled && await Install(folder + "/syntax.xshd", Path.Combine(localSyntaxPath, "syntax.xshd"));

        //    if (!allInstalled)
        //        break;

        //    try
        //    {
        //        installedSyntaxes.Add(await SyntaxLoader.LoadSyntax(localSyntaxPath));
        //    }
        //    catch (Exception)
        //    {
        //        SkEditorAPI.Logs.Error($"Failed to load syntax {folderName}!", true);
        //        return;
        //    }
        //}

        //if (!allInstalled)
        //{
        //    await SkEditorAPI.Windows.ShowError(Translation.Get("MarketplaceInstallFailed", ItemName));
        //    return;
        //}

        //string message = Translation.Get("MarketplaceInstallSuccess", ItemName);
        //message += "\n" + Translation.Get("MarketplaceInstallEnableNow");

        //ContentDialogResult result = await SkEditorAPI.Windows.ShowDialog("Success", message,
        //    primaryButtonText: "MarketplaceEnableNow", cancelButtonText: "Okay");

        //if (result == ContentDialogResult.Primary)
        //{
        //    _ = Dispatcher.UIThread.InvokeAsync(async () =>
        //    {
        //        Theme theme = ThemeEditor.LoadTheme(themeFilePath);
        //        await ThemeEditor.SetTheme(theme);
        //    });

        //    foreach (var syntax in installedSyntaxes)
        //    {
        //        SyntaxLoader.SelectSyntax(syntax);
        //    }
        //}

        //MarketplaceWindow.Instance.HideAllButtons();
        //MarketplaceWindow.Instance.ItemView.UninstallButton.IsVisible = true;
    }

    private async Task<bool> Install(string url, string filePath)
    {
        using HttpClient client = new();
        HttpResponseMessage response = await client.GetAsync(url);
        try
        {
            using Stream stream = await response.Content.ReadAsStreamAsync();
            using FileStream fileStream = File.Create(filePath);
            await stream.CopyToAsync(fileStream);
            await stream.DisposeAsync();
        }
        catch (Exception e)
        {
            Log.Error(e, $"Failed to install {ItemName}!");
            await SkEditorAPI.Windows.ShowError(Translation.Get("MarketplaceInstallFailed", ItemName));
            return false;
        }

        return true;
    }

    public override async void Uninstall()
    {
        await UninstallTheme();
        UninstallSyntax();

        MarketplaceWindow.Instance.HideAllButtons();
        MarketplaceWindow.Instance.ItemView.InstallButton.IsVisible = true;

        await SkEditorAPI.Windows.ShowDialog("Success", Translation.Get("MarketplaceUninstallSuccess", ItemName), primaryButtonText: "Okay");
    }

    private async Task UninstallTheme()
    {
        string fileName = ThemeFileUrl.Split('/').Last();

        if (fileName.Equals(ThemeEditor.CurrentTheme.FileName))
            await ThemeEditor.SetTheme(ThemeEditor.Themes.FirstOrDefault(x => x.FileName.Equals("Default.json")) ?? ThemeEditor.GetDefaultTheme());

        ThemeEditor.Themes.Remove(ThemeEditor.Themes.FirstOrDefault(x => x.FileName.Equals(fileName)));
        ThemeEditor.SaveAllThemes();
        File.Delete(Path.Combine(AppConfig.AppDataFolderPath, "Themes", fileName));
    }

    private async void UninstallSyntax()
    {
        //List<string> folders = SyntaxFolders.ToList();
        //var syntaxFolder = Path.Combine(AppConfig.AppDataFolderPath, "Syntax Highlighting");
        //foreach (string folder in folders)
        //{
        //    var folderName = folder.Split('/').Last();
        //    string localSyntaxPath = Path.Combine(syntaxFolder,
        //        folderName);

        //    await SyntaxLoader.UnloadSyntax(localSyntaxPath);
        //    Directory.Delete(localSyntaxPath, true);
        //}
        //SyntaxLoader.CheckConfiguredFileSyntaxes();
        //SyntaxLoader.RefreshAllOpenedEditors();
    }

    public override bool IsInstalled()
    {
        var syntaxFolder = Path.Combine(AppConfig.AppDataFolderPath, "Syntax Highlighting");
        foreach (string folder in SyntaxFolders)
        {
            var folderName = folder.Split('/').Last();
            string localSyntaxPath = Path.Combine(syntaxFolder,
                folderName);
            if (!Directory.Exists(localSyntaxPath))
                return false;
        }

        // Also check if theme is installed
        string themePath = Path.Combine(AppConfig.AppDataFolderPath, ThemeItem.FolderName, Path.GetFileName(ThemeFileUrl));
        if (!File.Exists(themePath))
            return false;

        return true;
    }
}