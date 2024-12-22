using FluentAvalonia.UI.Controls;
using Newtonsoft.Json;
using Serilog;
using SkEditor.API;
using SkEditor.Utilities;
using SkEditor.Utilities.Syntax;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace SkEditor.Views.Marketplace.Types;
public class SyntaxItem : MarketplaceItem
{
    [JsonProperty("syntaxFolders")]
    public string[] ItemSyntaxFolders { get; set; }

    [JsonIgnore]
    public const string FolderName = "Syntax Highlighting";

    public async override void Install()
    {
        //string baseLocalSyntaxPath = Path.Combine(AppConfig.AppDataFolderPath, FolderName);

        //List<FileSyntax> installedSyntaxes = [];
        //bool allInstalled = true;
        //foreach (string folder in ItemSyntaxFolders)
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
        //    foreach (var syntax in installedSyntaxes)
        //    {
        //        SyntaxLoader.SelectSyntax(syntax);
        //    }

        //    SyntaxLoader.RefreshAllOpenedEditors();
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
        //List<string> folders = ItemSyntaxFolders.ToList();
        //var syntaxFolder = Path.Combine(AppConfig.AppDataFolderPath, FolderName);
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

        //MarketplaceWindow.Instance.HideAllButtons();
        //MarketplaceWindow.Instance.ItemView.InstallButton.IsVisible = true;

        //await SkEditorAPI.Windows.ShowDialog("Success", Translation.Get("MarketplaceUninstallSuccess", ItemName), primaryButtonText: "Okay");
    }

    public override bool IsInstalled()
    {
        var syntaxFolder = Path.Combine(AppConfig.AppDataFolderPath, FolderName);
        foreach (string folder in ItemSyntaxFolders)
        {
            var folderName = folder.Split('/').Last();
            string localSyntaxPath = Path.Combine(syntaxFolder,
                folderName);
            if (!Directory.Exists(localSyntaxPath))
                return false;
        }

        return true;
    }
}