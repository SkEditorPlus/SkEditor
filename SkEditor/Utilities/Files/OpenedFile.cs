using System;
using Avalonia.Controls;
using AvaloniaEdit;
using FluentAvalonia.UI.Controls;
using SkEditor.API;
using SkEditor.Utilities.Parser;
using System.Collections.Generic;
using SkEditor.Utilities.InternalAPI;

namespace SkEditor.Utilities.Files;

public class OpenedFile
{

    #region Text Files Properties

    public TextEditor? Editor { get; set; }
    public string? Path { get; set; }
    public bool IsNewFile { get; set; } = false;

    private bool _saved;
    public bool IsSaved
    {
        get => _saved;
        set
        {
            _saved = value;
            if (TabViewItem != null)
                TabViewItem.Header = Header;
        }
    }

    #endregion

    #region Custom Tabs Properties

    public bool IsCustomTab => Editor == null;
    public Control? CustomControl => IsCustomTab ? TabViewItem.Content as Control : null;
    public string? CustomName = null;

    #endregion

    public TabViewItem TabViewItem { get; set; }

    #region Accessors
    
    [Obsolete("Use indexers instead.")]
    public FileParser? FileParser => this["Parser"] as FileParser;

    public bool IsEditor => Editor != null;
    public string? Name => Path == null ? CustomName : System.IO.Path.GetFileName(Path);
    public string? Header => Name + (IsSaved || (SkEditorAPI.Core.GetAppConfig().IsAutoSaveEnabled && Path != null) ? "" : " •");

    #endregion

    #region Custom Data

    public List<CustomFileData> CustomData { get; } = [];
    public object? this[string key]
    {
        get
        {
            var data = CustomData.Find(d => d.Key == key);
            return data?.Value;
        }
        set
        {
            var data = CustomData.Find(d => d.Key == key);
            if (data != null)
                CustomData.Remove(data);
            CustomData.Add(new CustomFileData(key, value));
        }
    }

    #endregion
}