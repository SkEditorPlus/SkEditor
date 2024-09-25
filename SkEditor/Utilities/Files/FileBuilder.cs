using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Threading;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using SkEditor.API;
using SkEditor.Utilities.Completion;
using SkEditor.Utilities.Editor;
using SkEditor.Utilities.Styling;
using SkEditor.Views;
using SkEditor.Views.FileTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using SkEditor.Utilities.InternalAPI;

namespace SkEditor.Utilities.Files;

public class FileBuilder
{
    public static readonly Dictionary<string, FileTypes.FileType> OpenedFiles = [];

    public static async Task<TabViewItem?> Build(string header, string path = "", string? content = null)
    {
        var fileType = await GetFileDisplay(path, content);
        if (fileType == null)
            return null;

        TabViewItem tabViewItem = new()
        {
            Header = header,
            IsSelected = true,
            Content = fileType.Display,
            Tag = string.Empty
        };

        MainWindow.Instance.BottomBar.IsVisible = fileType.NeedsBottomBar;

        if (!string.IsNullOrWhiteSpace(path))
        {
            tabViewItem.Tag = Uri.UnescapeDataString(path);

            ToolTip toolTip = new()
            {
                Content = path,
            };

            ToolTip.SetShowDelay(tabViewItem, 1200);
            ToolTip.SetTip(tabViewItem, toolTip);
        }

        if (fileType.IsEditor)
        {
            var editor = fileType.Display as TextEditor;

            SkEditorAPI.Events.FileCreated(editor);
            Dispatcher.UIThread.Post(() => TextEditorEventHandler.CheckForHex(editor));
        }

        OpenedFiles.Remove(header);
        OpenedFiles.Add(header, fileType);
        return tabViewItem;
    }

    private static async Task<FileTypes.FileType?> GetFileDisplay(string path, string? content)
    {
        FileTypes.FileType? fileType = null;
        if (FileTypes.RegisteredFileTypes.ContainsKey(Path.GetExtension(path)))
        {
            var handlers = FileTypes.RegisteredFileTypes[Path.GetExtension(path)];
            if (handlers.Count == 1)
            {
                fileType = handlers[0].Handle(path);
            }
            else
            {
                var ext = Path.GetExtension(path);
                if (SkEditorAPI.Core.GetAppConfig().PreferredFileAssociations.TryGetValue(ext, out string? value))
                {
                    var pref = value;
                    if (pref == "SkEditor")
                    {
                        fileType = handlers.Find(association => !association.IsFromAddon).Handle(path);
                    }
                    else
                    {
                        var preferred = handlers.Find(association => association.IsFromAddon &&
                            association.Addon.Name == value);
                        if (preferred != null)
                        {
                            fileType = preferred.Handle(path);
                        }
                        else
                        {
                            SkEditorAPI.Core.GetAppConfig().PreferredFileAssociations.Remove(ext);
                        }
                    }
                }

                if (fileType == null)
                {
                    var window = new AssociationSelectionWindow(path, handlers);
                    await window.ShowDialog(MainWindow.Instance);
                    var selected = window.SelectedAssociation;
                    if (selected != null)
                    {
                        fileType = selected.Handle(path);
                        if (window.RememberCheck.IsChecked == true)
                        {
                            SkEditorAPI.Core.GetAppConfig().PreferredFileAssociations[ext] = selected.IsFromAddon ? selected.Addon.Name : "SkEditor";
                        }
                        else
                        {
                            SkEditorAPI.Core.GetAppConfig().PreferredFileAssociations.Remove(ext);
                        }
                        SkEditorAPI.Core.GetAppConfig().Save();
                    }
                }
            }
        }

        return fileType ?? await GetDefaultEditor(path, content);
    }

    private static async Task<FileTypes.FileType?> GetDefaultEditor(string path, string? content)
    {
        AppConfig config = SkEditorAPI.Core.GetAppConfig();

        string fileContent = null;
        if (!string.IsNullOrWhiteSpace(path))
        {
            path = Uri.UnescapeDataString(path);
            if (File.Exists(path))
                fileContent = content ?? await File.ReadAllTextAsync(path);
        }

        if (fileContent != null && fileContent.Any(c => char.IsControl(c) && c != '\n' && c != '\r' && c != '\t'))
        {
            var response = await SkEditorAPI.Windows.ShowDialog(
                Translation.Get("BinaryFileTitle"), Translation.Get("BinaryFileFound"),
                new SymbolIconSource() { Symbol = Symbol.Alert });
            if (response != ContentDialogResult.Primary)
                return null;
        }

        var editor = CreateEditor();
        if (!string.IsNullOrWhiteSpace(path))
        {
            path = Uri.UnescapeDataString(path);
            if (File.Exists(path))
            {
                editor.Text = fileContent;
            }
        }

        return new FileTypes.FileType(editor, path, true);
    }

    public static TextEditor CreateEditor(string content = "")
    {
        var editor = new TextEditor
        {
            ShowLineNumbers = true,
            Foreground = (ImmutableSolidColorBrush)Application.Current.FindResource("EditorTextColor"),
            Background = (ImmutableSolidColorBrush)Application.Current.FindResource("EditorBackgroundColor"),
            LineNumbersForeground = (ImmutableSolidColorBrush)Application.Current.FindResource("LineNumbersColor"),
            FontSize = 16,
            WordWrap = SkEditorAPI.Core.GetAppConfig().IsWrappingEnabled,
        };

        editor.ContextFlyout = GetContextMenu(editor);

        if (SkEditorAPI.Core.GetAppConfig().Font.Equals("Default"))
        {
            Application.Current.TryGetResource("JetBrainsFont", Avalonia.Styling.ThemeVariant.Default, out object font);
            editor.FontFamily = (FontFamily)font;
        }
        else
        {
            editor.FontFamily = new FontFamily(SkEditorAPI.Core.GetAppConfig().Font);
        }

        editor.Text = content;
        editor = AddEventHandlers(editor);
        editor = SetOptions(editor);

        return editor;
    }

    private static TextEditor AddEventHandlers(TextEditor editor)
    {
        editor.TextArea.PointerWheelChanged += TextEditorEventHandler.OnZoom;
        editor.TextArea.Loaded += (sender, e) => editor.Focus();
        editor.TextChanged += TextEditorEventHandler.OnTextChanged;
        editor.TextArea.TextEntered += TextEditorEventHandler.DoAutoIndent;
        editor.TextArea.TextEntered += TextEditorEventHandler.DoAutoPairing;
        if (!SkEditorAPI.Core.GetAppConfig().EnableRealtimeCodeParser)
        {
            editor.TextChanged += (_, _) => (SkEditorAPI.Files.GetCurrentOpenedFile()?["Parser"] as FileParser)?.Parse();
        }
        if (SkEditorAPI.Core.GetAppConfig().EnableHexPreview)
        {
            editor.Document.TextChanged += (_, _) => TextEditorEventHandler.CheckForHex(editor);
        }
        editor.TextArea.Caret.PositionChanged += (sender, e) =>
        {
            SkEditorAPI.Windows.GetMainWindow().BottomBar.UpdatePosition();
        };
        editor.TextArea.KeyDown += TextEditorEventHandler.OnKeyDown;
        editor.TextArea.TextView.PointerPressed += TextEditorEventHandler.OnPointerPressed;
        editor.TextArea.SelectionChanged += SelectionHandler.OnSelectionChanged;

        if (SkEditorAPI.Core.GetAppConfig().EnableAutoCompletionExperiment)
        {
            editor.TextChanged += CompletionHandler.OnTextChanged;
            editor.TextArea.AddHandler(Avalonia.Input.InputElement.KeyDownEvent, CompletionHandler.OnKeyDown, handledEventsToo: true, routes: RoutingStrategies.Tunnel);
        }

        editor.TextArea.TextPasting += TextEditorEventHandler.OnTextPasting;

        return editor;
    }

    private static TextEditor SetOptions(TextEditor editor)
    {
        editor.TextArea.TextView.LinkTextForegroundBrush = new ImmutableSolidColorBrush(Color.Parse("#1a94c4"));
        editor.TextArea.TextView.LinkTextUnderline = true;
        editor.TextArea.SelectionBrush = (ImmutableSolidColorBrush)Application.Current.FindResource("SelectionColor");
        editor.TextArea.RightClickMovesCaret = true;

        editor.TextArea.LeftMargins.OfType<LineNumberMargin>().FirstOrDefault().Margin = new Thickness(10, 0);

        editor.Options.AllowScrollBelowDocument = true;
        editor.Options.CutCopyWholeLine = true;

        editor.Options.ConvertTabsToSpaces = SkEditorAPI.Core.GetAppConfig().UseSpacesInsteadOfTabs;
        editor.Options.IndentationSize = SkEditorAPI.Core.GetAppConfig().TabSize;

        editor.TextArea.TextView.CurrentLineBackground = ThemeEditor.CurrentTheme.CurrentLineBackground;
        editor.TextArea.TextView.CurrentLineBorder = new ImmutablePen(ThemeEditor.CurrentTheme.CurrentLineBorder, 2);
        editor.Options.HighlightCurrentLine = SkEditorAPI.Core.GetAppConfig().HighlightCurrentLine;

        return editor;
    }

    public static MenuFlyout GetContextMenu(TextEditor editor)
    {
        var commands = new[]
        {
            new { Header = "MenuHeaderCopy", Command = new RelayCommand(editor.Copy), Icon = Symbol.Copy },
            new { Header = "MenuHeaderPaste", Command = new RelayCommand(editor.Paste), Icon = Symbol.Paste },
            new { Header = "MenuHeaderCut", Command = new RelayCommand(editor.Cut), Icon = Symbol.Cut },
            new { Header = "MenuHeaderUndo", Command = new RelayCommand(() => editor.Undo()), Icon = Symbol.Undo },
            new { Header = "MenuHeaderRedo", Command = new RelayCommand(() => editor.Redo()), Icon = Symbol.Redo },
            new { Header = "MenuHeaderDuplicate", Command = new RelayCommand(() => CustomCommandsHandler.OnDuplicateCommandExecuted(editor.TextArea)), Icon = Symbol.Copy },
            new { Header = "MenuHeaderComment", Command = new RelayCommand(() => CustomCommandsHandler.OnCommentCommandExecuted(editor.TextArea)), Icon = Symbol.Comment },
            new { Header = "MenuHeaderGoToLine", Command = new RelayCommand(() => SkEditorAPI.Windows.ShowWindow(new GoToLineWindow())), Icon = Symbol.Find },
            new { Header = "MenuHeaderTrimWhitespaces", Command = new RelayCommand(() => CustomCommandsHandler.OnTrimWhitespacesCommandExecuted(editor.TextArea)), Icon = Symbol.Remove },
            new { Header = "MenuHeaderDelete", Command = new RelayCommand(editor.Delete), Icon = Symbol.Delete },
            new { Header = "MenuHeaderRefactor", Command = new RelayCommand(() => CustomCommandsHandler.OnRefactorCommandExecuted(editor)), Icon = Symbol.Rename },
        };

        var contextMenu = new MenuFlyout();
        commands.Select(item => new MenuItem
        {
            Header = Translation.Get(item.Header),
            Command = item.Command,
            Icon = new SymbolIcon { Symbol = item.Icon, FontSize = 20 }
        }).ToList().ForEach(item => contextMenu.Items.Add(item));

        return contextMenu;
    }
}