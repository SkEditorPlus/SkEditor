using Avalonia.Controls;
using Avalonia.Platform.Storage;
using AvaloniaEdit;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;
using SkEditor.Controls;
using SkEditor.Utilities;
using SkEditor.Utilities.Editor;
using SkEditor.Utilities.Files;
using SkEditor.Utilities.InternalAPI;
using SkEditor.Utilities.InternalAPI;
using SkEditor.Utilities.Parser;
using SkEditor.Utilities.Syntax;
using SkEditor.ViewModels;
using SkEditor.Views.FileTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using File = System.IO.File;

namespace SkEditor.API;

public class Files : IFiles
{
    private List<OpenedFile> OpenedFiles { get; } = [];

    #region Getters/Conditions

    public bool IsFileOpen()
    {
        return GetCurrentOpenedFile()?.IsCustomTab == false;
    }

    public bool IsEditorOpen()
    {
        return GetCurrentOpenedFile()?.IsEditor ?? false;
    }

    public List<OpenedFile> GetOpenedFiles()
    {
        return OpenedFiles;
    }

    public OpenedFile GetCurrentOpenedFile()
    {
        return GetFromTabViewItem(GetCurrentTabViewItem());
    }

    public TabView GetTabView()
    {
        return SkEditorAPI.Windows.GetMainWindow().TabControl;
    }

    public List<TabViewItem> GetOpenedTabs()
    {
        return GetOpenedFiles().Select(source => source.TabViewItem).ToList();
    }

    public List<OpenedFile> GetOpenedEditors()
    {
        return GetOpenedFiles().Where(source => source.IsEditor).ToList();
    }

    public TabViewItem GetCurrentTabViewItem()
    {
        return (TabViewItem)GetTabView().SelectedItem;
    }

    public OpenedFile? GetOpenedFileByPath(string path)
    {
        return GetOpenedFiles().Find(file => file.Path == path);
    }

    #endregion

    #region Saving

    public async Task Save(object entity, bool saveAs)
    {
        var tabItem = GetItem(entity);
        var openedFile = GetOpenedFiles().Find(file => file.TabViewItem == tabItem);

        if (openedFile.IsSaved && !saveAs)
            return;

        var path = GetFromTabViewItem(tabItem).Path;
        if (path == null || saveAs)
        {
            var suggestedFolder = string.IsNullOrEmpty(path)
                ? await SkEditorAPI.Windows.GetMainWindow().StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents)
                : await SkEditorAPI.Windows.GetMainWindow().StorageProvider.TryGetFolderFromPathAsync(path);

            FilePickerFileType skriptFileType = new("Skript") { Patterns = ["*.sk"] };
            FilePickerFileType allFilesType = new("All Files") { Patterns = ["*"] };

            FilePickerSaveOptions saveOptions = new()
            {
                Title = Translation.Get("WindowTitleSaveFilePicker"),
                SuggestedFileName = "",
                DefaultExtension = Path.GetExtension(path) ?? ".sk",
                FileTypeChoices = [skriptFileType, allFilesType],
                SuggestedStartLocation = suggestedFolder
            };

            var file = await SkEditorAPI.Windows.GetMainWindow().StorageProvider.SaveFilePickerAsync(saveOptions);
            if (file is null)
                return;

            var absolutePath = Uri.UnescapeDataString(file.Path.AbsolutePath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            path = absolutePath;

            openedFile.Path = path;
            openedFile.CustomName = Path.GetFileName(path);
            tabItem.Header = openedFile.Header;

            Icon.SetIcon(openedFile);
        }

        try
        {
            var textToWrite = openedFile.Editor?.Text;
            await using StreamWriter writer = new(path, false);
            await writer.WriteAsync(textToWrite);
            writer.Close();
        }
        catch (Exception e)
        {
            await SkEditorAPI.Windows.ShowError(e.Message);
            return;
        }

        openedFile.IsSaved = true;
        openedFile.IsNewFile = false;
    }

    #endregion

    #region Tab Manipulation

    public void AddCustomTab(object header, Control content, bool select = true, IconSource? icon = null)
    {
        var tabItem = new TabViewItem()
        {
            Header = header,
            Content = content
        };

        if (icon != null)
            tabItem.IconSource = icon;

        GetOpenedFiles().Add(new OpenedFile()
        {
            TabViewItem = tabItem,
        });
        (GetTabView().TabItems as IList)?.Add(tabItem);
        if (select)
            GetTabView().SelectedItem = tabItem;
    }

    public async Task<OpenedFile> AddEditorTab(string content, string? path)
    {
        int index = GetOpenedEditors().Count + 1;
        var header = Translation.Get("NewFileNameFormat").Replace("{0}", index.ToString());

        var tabItem = await FileBuilder.Build(header);
        tabItem.Tag = path;

        TextEditor editor = tabItem.Content as TextEditor;
        editor.Text = content;

        var openedFile = new OpenedFile()
        {
            Editor = tabItem.Content as TextEditor,
            Path = path,
            TabViewItem = tabItem,
            CustomName = header,
            IsSaved = !string.IsNullOrEmpty(path),
            IsNewFile = path == null,
        };
        
        Icon.SetIcon(openedFile);

        // Custom Data
        openedFile["Parser"] = new FileParser(editor);
        openedFile["Margin"] = new EditorMargin(openedFile);
        if (tabItem.Content is TextEditor textEditor)
            openedFile["Parser"] = new FileParser(textEditor);

        RemoveWelcomeTab();
        GetOpenedFiles().Add(openedFile);
        (GetTabView().TabItems as IList)?.Add(tabItem);
        await SyntaxLoader.RefreshSyntaxAsync();
        return openedFile;
    }

    public async void NewFile(string content = "")
    {
        await AddEditorTab(content, null);
    }

    public async void OpenFile(string path, bool force = false)
    {
        path = Uri.UnescapeDataString(Path.GetFullPath(path));
        if (!File.Exists(path))
            return;

        var alreadyOpenedFile = GetOpenedFileByPath(path);
        if (alreadyOpenedFile != null)
        {
            Select(alreadyOpenedFile);
            return;
        }

        OpenedFile? openedFile;

        var extension = Path.GetExtension(path);
        var availableTypes = new List<FileTypeData>();
        foreach (var fileType in Registries.FileTypes)
        {
            if (fileType.SupportedExtensions.Contains(extension))
                availableTypes.Add(fileType);
        }

        OpenedFile? BuildFromType(FileTypeData fileType, string path)
        {
            var result = fileType.FileOpener(path) ?? new FileTypeResult(null);
            if (result.Control != null)
            {
                TabViewItem tabViewItem = new()
                {
                    Header = result.Header ?? Path.GetFileName(path),
                    IsSelected = true,
                    Content = result.Control,
                    Tag = string.Empty
                };

                openedFile = new OpenedFile
                {
                    TabViewItem = tabViewItem,
                    Path = path,
                    IsSaved = true
                };

                GetOpenedFiles().Add(openedFile);
                (GetTabView().TabItems as IList)?.Add(tabViewItem);
                return openedFile;
            }

            return null;
        }

        if (availableTypes.Count == 1)
        {
            openedFile = BuildFromType(availableTypes[0], path);
        }
        else if (availableTypes.Count > 1)
        {
            var configuredTypeFullId = SkEditorAPI.Core.GetAppConfig().FileTypeChoices.GetValueOrDefault(extension, null);
            if (configuredTypeFullId != null && !Registries.FileTypes.HasFullKey(configuredTypeFullId))
                configuredTypeFullId = null;

            if (configuredTypeFullId != null)
            {
                var key = RegistryKey.FromFullKey(configuredTypeFullId);
                openedFile = BuildFromType(Registries.FileTypes.GetValue(key), path);
            }
            else
            {

                var selectionVM = new FileTypeSelectionViewModel()
                {
                    FileTypes = availableTypes,
                    SelectedFileType = null,
                };
                await SkEditorAPI.Windows.ShowWindowAsDialog(new FileTypeSelectionWindow { DataContext = selectionVM });

                if (selectionVM.SelectedFileType == null)
                    return;
                if (selectionVM.RememberSelection)
                {
                    SkEditorAPI.Core.GetAppConfig().FileTypeChoices[extension] =
                        Registries.FileTypes.GetValueKey(selectionVM.SelectedFileType).FullKey;
                }

                openedFile = BuildFromType(selectionVM.SelectedFileType, path);
            }
        }
        else
        {
            var content = await File.ReadAllTextAsync(path);
            // binary check
            if (!force && content.Any(c => char.IsControl(c) && !char.IsWhiteSpace(c)))
            {
                var response = await SkEditorAPI.Windows.ShowDialog("BinaryFileTitle", "BinaryFileFound",
                    cancelButtonText: "Cancel", icon: Symbol.Code);
                if (response != ContentDialogResult.Primary)
                    return;
            }

            openedFile = await AddEditorTab(content, path);
        }

        Icon.SetIcon(openedFile);

        if (openedFile == null)
            return;

        SkEditorAPI.Events.FileOpened(openedFile, false);

        RemoveWelcomeTab();
        Select(openedFile);
    }

    private void RemoveWelcomeTab()
    {
        var welcomeTab = GetOpenedFiles().Find(file => file.TabViewItem.Content is WelcomeTabControl);
        if (welcomeTab != null)
        {
            (GetTabView().TabItems as IList)?.Remove(welcomeTab.TabViewItem);
            GetOpenedFiles().Remove(welcomeTab);
        }
    }

    public void Select(object entity)
    {
        GetTabView().SelectedItem = GetItem(entity);
    }

    public async Task Close(object entity)
    {
        var tabViewItem = GetItem(entity);
        var file = GetOpenedFiles().Find(source => source.TabViewItem == tabViewItem);

        if (!file.IsSaved && file is { IsEditor: true, IsNewFile: false })
        {
            var response = await SkEditorAPI.Windows.ShowDialog(
                "Unsaved File",
                "The file '" + file.Name +
                "' is not saved.\n\nAre you sure you want to close it and discard your changes?",
                primaryButtonText: "Yes",
                cancelButtonText: "Cancel", icon: Symbol.SaveLocal);
            if (response != ContentDialogResult.Primary)
                return;
        }

        var canClose = SkEditorAPI.Events.TabClosed(file);
        if (!canClose)
            return;

        if (tabViewItem.Content is TextEditor editor)
            TextEditorEventHandler.ScrollViewers.Remove(editor);

        (GetTabView().TabItems as IList).Remove(tabViewItem);
        OpenedFiles.RemoveAll(opFile => opFile.TabViewItem == tabViewItem);

        if (OpenedFiles.Count == 0)
            AddWelcomeTab();
    }

    public async void BatchClose(IFiles.FileCloseAction closeAction)
    {
        switch (closeAction)
        {
            case IFiles.FileCloseAction.AllExceptCurrent:
                {
                    var currentOpenedFile = GetCurrentOpenedFile();
                    foreach (var openedFile in OpenedFiles.Where(openedFile => openedFile != currentOpenedFile))
                        await Close(openedFile);
                    break;
                }
            case IFiles.FileCloseAction.Unsaved:
                {
                    foreach (var tabViewItem in GetOpenedTabs().Where(tabItem => tabItem.Header.ToString().EndsWith('*')))
                        await Close(tabViewItem);
                    break;
                }
            case IFiles.FileCloseAction.AllRight:
            case IFiles.FileCloseAction.AllLeft:
                {
                    var currentTabViewItem = GetCurrentTabViewItem();
                    var index = GetTabView().TabItems.IndexOf(currentTabViewItem);
                    var openedTabs = GetOpenedTabs();

                    var items = closeAction == IFiles.FileCloseAction.AllRight
                        ? openedTabs.GetRange(index + 1, openedTabs.Count - index - 1)
                        : openedTabs.GetRange(0, index);

                    items.RemoveAll(tab => tab == currentTabViewItem);
                    items.ForEach(async tab => await Close(tab));

                    break;
                }
            case IFiles.FileCloseAction.All:
                {
                    var tabsToClose = GetOpenedFiles().ToList();
                    tabsToClose.ForEach(async tab => await Close(tab));
                    if (GetOpenedFiles().Count == 0)
                        AddWelcomeTab();
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException(nameof(closeAction), closeAction, null);
        }
    }

    #endregion

    public void AddWelcomeTab()
    {
        FluentIcons.Avalonia.Fluent.SymbolIconSource icon = new()
        {
            Symbol = FluentIcons.Common.Symbol.Home,
        };
        AddCustomTab(Translation.Get("WelcomeTabTitle"), new WelcomeTabControl(), icon: icon);
    }

    private OpenedFile GetFromTabViewItem(TabViewItem tabViewItem)
    {
        return OpenedFiles.FirstOrDefault(source => source.TabViewItem == tabViewItem);
    }

    private TabViewItem GetItem(object entity)
    {
        return entity switch
        {
            OpenedFile openedFile => openedFile.TabViewItem,
            TabViewItem item => item,
            string path => GetOpenedFiles().Find(item => item.Path == path).TabViewItem,
            int index => GetOpenedTabs()[index],
            _ => throw new ArgumentException("Given entity is not an OpenedFile, TabViewItem, string or int")
        };
    }
}