using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
using SkEditor.API;
using SkEditor.Controls;
using SkEditor.Utilities;
using SkEditor.Utilities.Files;
using SkEditor.Utilities.InternalAPI;
using SkEditor.Utilities.Styling;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace SkEditor.Views;

public partial class MainWindow : AppWindow
{
    public static MainWindow Instance { get; private set; }

    public BottomBarControl GetBottomBar() => BottomBar;

    public MainWindow()
    {
        InitializeComponent();

        WindowStyler.Style(this);

        ThemeEditor.LoadThemes();
        AddEvents();

        Translation.LoadDefaultLanguage();
        Translation.ChangeLanguage(SkEditorAPI.Core.GetAppConfig().Language);

        Instance = this;
    }

    private void AddEvents()
    {
        TabControl.AddTabButtonCommand = new RelayCommand(FileHandler.NewFile);
        TabControl.TabCloseRequested += (sender, e) => FileCloser.CloseFile(e);
        TabControl.SelectionChanged += (sender, e) => SkEditorAPI.Events.TabChanged(e);
        TemplateApplied += OnWindowLoaded;
        Closing += OnClosing;

        Activated += (sender, e) => ChangeChecker.Check();

        KeyDown += (sender, e) =>
        {
            if (e.KeyModifiers == KeyModifiers.Control && e.Key >= Key.D1 && e.Key <= Key.D9)
            {
                FileHandler.SwitchTab((int)e.Key - 35);
            }
            else if (e.KeyModifiers == (KeyModifiers.Control | KeyModifiers.Shift) && e.Key == Key.Oem3)
            {
                TerminalWindow terminal = new();
                terminal.Show();
            }
        };

        AddHandler(DragDrop.DropEvent, FileHandler.FileDropAction);
    }

    public void ReloadUiOfAddons()
    {
        MainMenu.ReloadAddonsMenus();
        BottomBar.ReloadBottomIcons();
        SideBar.ReloadPanels();
    }

    public bool AlreadyClosed { get; set; } = false;
    private async void OnClosing(object sender, WindowClosingEventArgs e)
    {
        if (AlreadyClosed) return;

        ThemeEditor.SaveAllThemes();
        SkEditorAPI.Core.GetAppConfig().Save();

        e.Cancel = true;
        if (!SkEditorAPI.Core.GetAppConfig().EnableSessionRestoring)
        {
            bool anyUnsaved = SkEditorAPI.Files.GetOpenedEditors().Any(x => !x.IsSaved);
            if (!anyUnsaved)
            {
                e.Cancel = false;
                return;
            }

            ContentDialogResult result = await SkEditorAPI.Windows.ShowDialog(Translation.Get("Attention"),
                Translation.Get("ClosingProgramWithUnsavedFiles"), icon: Symbol.ImportantFilled,
                primaryButtonText: "Yes", cancelButtonText: "No");

            if (result == ContentDialogResult.Primary)
            {
                AlreadyClosed = true;
                Close();
            }
        }
        else
        {
            await SessionRestorer.SaveSession();
            AlreadyClosed = true;
            Close();
        }
    }

    private async void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        AddonLoader.Load();
        Utilities.Files.FileTypes.RegisterDefaultAssociations();
        SideBar.ReloadPanels();

        await ThemeEditor.SetTheme(ThemeEditor.CurrentTheme);

        bool sessionFilesAdded = false;
        if (SkEditorAPI.Core.GetAppConfig().EnableSessionRestoring)
            sessionFilesAdded = await SessionRestorer.RestoreSession();

        string[] startupFiles = SkEditorAPI.Core.GetStartupArguments();
        if (startupFiles.Length == 0 && !sessionFilesAdded)
            SkEditorAPI.Files.AddWelcomeTab();
        startupFiles.ToList().ForEach(FileHandler.OpenFile);
        if (SkEditorAPI.Files.GetOpenedFiles().Count == 0)
            SkEditorAPI.Files.AddWelcomeTab();

        Dispatcher.UIThread.Post(async () =>
        {
            DiscordRpcUpdater.Initialize();

            if (SkEditorAPI.Core.GetAppConfig().CheckForUpdates) UpdateChecker.Check();

            Tutorial.ShowTutorial();
            BottomBar.UpdatePosition();
            ChangelogChecker.Check();

            await CrashChecker.CheckForCrash();
        });
    }
}