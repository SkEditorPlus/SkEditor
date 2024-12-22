using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using SkEditor.API;
using System;
using System.Collections.Generic;
using System.IO;

namespace SkEditor.Utilities;


public partial class AppConfig : ObservableObject
{
    [ObservableProperty] private string _version = string.Empty;
    [ObservableProperty] private bool _firstTime = true;
    [ObservableProperty] private string _language = "English";
    [ObservableProperty] private string _lastUsedPublishService = "Pastebin";
    [ObservableProperty] private string _pastebinApiKey = "";
    [ObservableProperty] private string _codeSkriptPlApiKey = "";
    [ObservableProperty] private bool _useSkriptGui = false;

    [ObservableProperty] private bool _isDiscordRpcEnabled = true;
    [ObservableProperty] private bool _isWrappingEnabled = false;
    [ObservableProperty] private bool _isAutoIndentEnabled = false;
    [ObservableProperty] private bool _isPasteIndentationEnabled = false;
    [ObservableProperty] private bool _isAutoPairingEnabled = false;
    [ObservableProperty] private bool _isAutoSaveEnabled = false;
    [ObservableProperty] private string _currentTheme = "Default.json";
    [ObservableProperty] private Dictionary<string, string> _fileSyntaxes = [];
    [ObservableProperty] private string _font = "Default";
    [ObservableProperty] private bool _highlightCurrentLine = true;
    [ObservableProperty] private bool _useSpacesInsteadOfTabs = false;
    [ObservableProperty] private int _tabSize = 4;
    [ObservableProperty] private bool _checkForUpdates = true;
    [ObservableProperty] private bool _checkForChanges = false;
    [ObservableProperty] private bool _isProjectSingleClickEnabled = true;
    [ObservableProperty] private bool _isDevModeEnabled = false;

    /// <summary>
    /// Represent the width of panels via their ID (<see cref="Registries.SidebarPanels"/>
    /// </summary>
    public Dictionary<string, int> SidebarPanelSizes { get; set; } = [];

    /// <summary>
    /// Represent the (saved) choices the user made for file types.
    /// </summary>
    public Dictionary<string, string> FileTypeChoices { get; set; } = [];

    public HashSet<string> AddonsToDisable { get; set; } = [];
    public HashSet<string> AddonsToDelete { get; set; } = [];
    public HashSet<string> AddonsToUpdate { get; set; } = [];

    public Dictionary<string, object> CustomOptions { get; set; } = [];
    public Dictionary<string, string> PreferredFileAssociations { get; set; } = [];

    public bool EnableAutoCompletionExperiment { get; set; } = false;
    public bool EnableProjectsExperiment { get; set; } = false;
    public bool EnableHexPreview { get; set; } = false;
    public bool EnableCodeParser { get; set; } = false;
    public bool EnableFolding { get; set; } = false;
    public bool EnableBetterPairing { get; set; } = false;
    public bool EnableSessionRestoring { get; set; } = false;
    public bool EnableRealtimeCodeParser { get; set; } = false;
    public bool EnableSkDoc { get; set; } = false;

    public string SkUnityAPIKey { get; set; } = "";
    public string SkriptMCAPIKey { get; set; } = "";

    public static string AppDataFolderPath { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SkEditor");

    public static string SettingsFilePath { get; set; } = Path.Combine(AppDataFolderPath, "settings.json");

    public static AppConfig Load()
    {
        string settingsFilePath = SettingsFilePath;

        if (!File.Exists(settingsFilePath) || string.IsNullOrWhiteSpace(File.ReadAllText(settingsFilePath)))
        {
            return LoadDefaultSettings();
        }

        try
        {
            return JsonConvert.DeserializeObject<AppConfig>(File.ReadAllText(settingsFilePath)) ?? new AppConfig();
        }
        catch (JsonException)
        {
            return LoadDefaultSettings();
        }
    }

    public void Save()
    {
        string jsonContent = JsonConvert.SerializeObject(this, Formatting.Indented);
        File.WriteAllText(SettingsFilePath, jsonContent);
    }

    public static AppConfig LoadDefaultSettings()
    {
        if (!Directory.Exists(AppDataFolderPath)) Directory.CreateDirectory(AppDataFolderPath);

        AppConfig defaultSettings = new();

        string jsonContent = JsonConvert.SerializeObject(defaultSettings, Formatting.Indented);
        File.WriteAllText(SettingsFilePath, jsonContent);

        return defaultSettings;
    }

    /// <summary>
    /// Set up a new custom option.
    /// </summary>
    public void SetUpNewOption(string optionName, object defaultValue)
    {
        CustomOptions.TryAdd(optionName, defaultValue);
    }

    /// <summary>
    /// Set value of a custom option.
    /// </summary>
    public void SetOption(string optionName, object value)
    {
        if (CustomOptions.ContainsKey(optionName))
        {
            CustomOptions[optionName] = value;
        }
    }

    /// <summary>
    /// Get value of a custom option.
    /// </summary>
    public object GetOption(string optionName)
    {
        return CustomOptions.GetValueOrDefault(optionName);
    }

    /// <summary>
    /// Get value of an option by name.
    /// </summary>
    public T GetOptionValue<T>(string optionName)
    {
        return (T)GetType().GetProperty(optionName).GetValue(this);
    }

    /// <summary>
    /// Set value of an option by name.
    /// </summary> 
    public void SetOptionValue<T>(string optionName, T value)
    {
        GetType().GetProperty(optionName).SetValue(this, value);
    }
}