using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaEdit;
using FluentAvalonia.UI.Controls;
using SkEditor.API;
using SkEditor.Utilities;
using SkEditor.Utilities.Docs;
using SkEditor.Utilities.Docs.Local;
using SkEditor.Utilities.Docs.SkUnity;
using SkEditor.Utilities.Styling;
using SkEditor.Utilities.Syntax;
using SkEditor.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Symbol = FluentIcons.Common.Symbol;
using SymbolIcon = FluentIcons.Avalonia.Fluent.SymbolIcon;

namespace SkEditor.Controls.Docs;

public partial class DocElementControl : UserControl
{
    private bool _hasLoadedExamples;
    private readonly DocumentationControl _documentationControl;
    private readonly IDocumentationEntry _entry;

    public DocElementControl(IDocumentationEntry entry, DocumentationControl documentationControl)
    {
        InitializeComponent();

        _documentationControl = documentationControl;
        _entry = entry;

        LoadVisuals(entry);
        LoadPatternsEditor(entry);
        SetupExamples(entry);
        LoadDownloadButton();

        LoadExpressionChangers(entry);

        if (entry.DocType == IDocumentationEntry.Type.Event)
        {
            OtherElementPanel.Children.Add(CreateExpander(Translation.Get("DocumentationControlEventValues"),
                Format(string.IsNullOrEmpty(entry.EventValues) ? Translation.Get("DocumentationControlNoEventValues") : entry.EventValues)));
        }
    }

    private void LoadExpressionChangers(IDocumentationEntry entry)
    {
        if (entry.DocType != IDocumentationEntry.Type.Expression || string.IsNullOrEmpty(entry.Changers)) return;


        var expander = CreateExpander(Translation.Get("DocumentationControlChangers"),
            Format(string.IsNullOrEmpty(entry.Changers) ? Translation.Get("DocumentationControlNoChangers") : entry.Changers));

        var buttons = new StackPanel()
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(2),
            Spacing = 2
        };
        expander.Content = new StackPanel()
        {
            Orientation = Orientation.Vertical,
            Spacing = 3,
            Children =
            {
                new TextBlock() { Text = Translation.Get("DocumentationControlChangerHelp") },
                buttons
            }
        };

        foreach (string raw in entry.Changers.Split("\n"))
        {
            if (!Enum.TryParse(typeof(IDocumentationEntry.Changer), raw, true, out var change))
            {
                buttons.Children.Add(new Button()
                {
                    Content = raw,
                    IsEnabled = false
                });
                continue;
            }
            var changer = (IDocumentationEntry.Changer)change;
            var button = new Button() { Content = raw };
            button.Click += async (_, _) =>
            {
                var firstPattern = GenerateUsablePattern(PatternsEditor.Text.Split("\n")[0]);
                var value = "<value>";
                var format = changer switch
                {
                    IDocumentationEntry.Changer.Set => $"set %s to {value}",
                    IDocumentationEntry.Changer.Add => $"add {value} to %s",
                    IDocumentationEntry.Changer.Remove => $"remove {value} from %s",
                    IDocumentationEntry.Changer.Reset => $"reset %s",
                    IDocumentationEntry.Changer.Clear => $"clear %s",
                    IDocumentationEntry.Changer.Delete => $"delete %s",
                    IDocumentationEntry.Changer.RemoveAll => $"remove all %s",
                    _ => throw new NotImplementedException("Changer not implemented")
                };

                await MainWindow.Instance.Clipboard.SetTextAsync(format.Replace("%s", firstPattern));
            };

            buttons.Children.Add(button);
        }

        OtherElementPanel.Children.Add(expander);
    }

    private void SetupExamples(IDocumentationEntry entry)
    {
        if (!IDocProvider.Providers[entry.Provider].NeedsToLoadExamples)
        {
            LoadExamples(entry);
            return;
        }

        _hasLoadedExamples = false;
        ExamplesEntry.IsExpanded = false;

        ExamplesEntry.Expanded += (_, _) =>
        {
            if (_hasLoadedExamples)
                return;
            _hasLoadedExamples = true;

            LoadExamples(entry);
        };
    }

    private void LoadPatternsEditor(IDocumentationEntry entry)
    {
        PatternsEditor.TextArea.SelectionBrush = ThemeEditor.CurrentTheme.SelectionColor;
        if (SkEditorAPI.Core.GetAppConfig().Font.Equals("Default"))
        {
            Application.Current.TryGetResource("JetBrainsFont", ThemeVariant.Default, out var font);
            PatternsEditor.FontFamily = (FontFamily)font;
        }
        else
        {
            PatternsEditor.FontFamily = new FontFamily(SkEditorAPI.Core.GetAppConfig().Font);
        }
        PatternsEditor.Text = Format(entry.Patterns);
        PatternsEditor.SyntaxHighlighting = DocSyntaxColorizer.CreatePatternHighlighting();
        PatternsEditor.TextArea.TextView.Redraw();
    }

    protected void LoadVisuals(IDocumentationEntry entry)
    {
        NameText.Text = entry.Name;
        Expander.Description = entry.DocType + " from " + entry.Addon;
        Expander.IconSource = IDocumentationEntry.GetTypeIcon(entry.DocType);
        DescriptionText.Text = Format(string.IsNullOrEmpty(entry.Description) ? Translation.Get("DocumentationControlNoDescription") : entry.Description);
        VersionBadge.IconSource = new FontIconSource { Glyph = Translation.Get("DocumentationControlSince", (string.IsNullOrEmpty(entry.Version) ? "1.0.0" : entry.Version)), };

        var uri = IDocProvider.Providers[entry.Provider].GetLink(entry);
        OutsideButton.Content = new StackPanel()
        {
            Orientation = Orientation.Horizontal,
            Spacing = 2,
            Children =
            {
                new TextBlock()
                {
                    Text = "See on " + entry.Provider,
                    VerticalAlignment = VerticalAlignment.Center
                },
                new SymbolIcon()
                {
                    Symbol = Symbol.Open,
                    FontSize = 18,
                    VerticalAlignment = VerticalAlignment.Center
                }
            }
        };
        OutsideButton.Click += (_, _) => Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
        if (uri == null)
        {
            OutsideButton.IsVisible = false;
        }

        LoadAddonBadge(entry);
    }

    private async void LoadAddonBadge(IDocumentationEntry entry)
    {
        SourceBadge.IconSource = new FontIconSource { Glyph = entry.Addon, };

        var color = await IDocProvider.Providers[entry.Provider].GetAddonColor(entry.Addon);
        if (color == null) return;

        SourceBadge.Background = new SolidColorBrush(color.Value);
        SourceBadge.Foreground = color.Value.ToHsl().L < 0.2 ? Brushes.White : Brushes.Black;

        if (entry.Provider == DocProvider.skUnity)
        {
            var skUnityProvider = IDocProvider.Providers[DocProvider.skUnity] as SkUnityProvider;
            SourceBadge.Tapped += (_, _) =>
            {
                var uri = skUnityProvider.GetAddonLink(entry.Addon);
                Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });
            };
        }
    }

    public static Expander CreateExpander(string name, string content)
    {
        var editor = new TextEditor()
        {
            Margin = new Thickness(5),
            FontSize = 16,
            Foreground = (IBrush)GetResource("EditorTextColor"),
            Background = (IBrush)GetResource("EditorBackgroundColor"),
            Padding = new Thickness(10),
            HorizontalScrollBarVisibility = ScrollBarVisibility.Visible,
            IsReadOnly = true,
            Text = content
        };

        editor.TextArea.SelectionBrush = ThemeEditor.CurrentTheme.SelectionColor;

        if (SkEditorAPI.Core.GetAppConfig().Font.Equals("Default"))
        {
            Application.Current.TryGetResource("JetBrainsFont", ThemeVariant.Default, out var font);
            editor.FontFamily = (FontFamily)font;
        }
        else
        {
            editor.FontFamily = new FontFamily(SkEditorAPI.Core.GetAppConfig().Font);
        }

        return new Expander()
        {
            Header = name,
            Content = editor
        };
    }

    private static object GetResource(string key)
    {
        Application.Current.TryGetResource(key, ThemeVariant.Default, out var resource);
        return resource;
    }

    public void DeleteElementFromCache(bool removeFromParent = false)
    {
        var localProvider = LocalProvider.Get();
        localProvider.RemoveElement(_entry);
        if (removeFromParent)
            _documentationControl.RemoveElement(this);
    }

    public async Task DownloadElementToCache()
    {
        List<IDocumentationExample> examples;
        try
        {
            examples = await IDocProvider.Providers[_entry.Provider].FetchExamples(_entry);
        }
        catch (Exception e)
        {
            examples = [];
            await SkEditorAPI.Windows.ShowError(Translation.Get("DocumentationControlErrorExamples", e.Message));
        }

        var localProvider = LocalProvider.Get();
        localProvider.DownloadElement(_entry, examples);
    }

    public async void DownloadButtonClicked(object? sender, RoutedEventArgs args)
    {
        if (_entry.Provider == DocProvider.Local)
        {
            DeleteElementFromCache(true);
            EnableDownloadButton();
        }
        else
        {
            if (await LocalProvider.Get().IsElementDownloaded(_entry))
            {
                DeleteElementFromCache();
                EnableDownloadButton();
            }
            else
            {
                await DownloadElementToCache();
                DisableDownloadButton();
            }
        }
    }

    public void EnableDownloadButton()
    {
        DownloadElementButton.Content = new TextBlock { Text = Translation.Get("DocumentationControlDownload") };
        DownloadElementButton.Classes.Add("accent");
    }

    public void DisableDownloadButton()
    {
        DownloadElementButton.Content = new TextBlock { Text = Translation.Get("DocumentationControlRemove") };
        DownloadElementButton.Classes.Remove("accent");
    }

    public async Task ForceDownloadElement()
    {
        if (await LocalProvider.Get().IsElementDownloaded(_entry)) return;

        await DownloadElementToCache();
        DisableDownloadButton();
    }

    public async void LoadDownloadButton()
    {
        var localProvider = LocalProvider.Get();
        DownloadElementButton.Click += DownloadButtonClicked;
        DownloadElementButton.Classes.Clear();

        if (_entry.Provider == DocProvider.Local)
        {
            DisableDownloadButton();
        }
        else
        {
            if (await localProvider.IsElementDownloaded(_entry))
            {
                DisableDownloadButton();
            }
            else
            {
                EnableDownloadButton();
            }
        }
    }

    public async void LoadExamples(IDocumentationEntry entry)
    {
        var provider = IDocProvider.Providers[entry.Provider];

        // First we setup a small loading bar
        ExamplesEntry.Content = new ProgressBar()
        {
            IsIndeterminate = true,
            Height = 5,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(5)
        };

        // Then we load the examples
        try
        {
            var examples = await provider.FetchExamples(entry);
            ExamplesEntry.Content = new StackPanel()
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(5)
            };

            if (examples.Count == 0)
            {
                ExamplesEntry.Content = new TextBlock()
                {
                    Text = Translation.Get("DocumentationControlNoExamples"),
                    Foreground = Brushes.Gray,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontStyle = FontStyle.Italic
                };
            }

            foreach (IDocumentationExample example in examples)
            {
                var stackPanel = new StackPanel()
                {
                    Orientation = Orientation.Vertical,
                    Margin = new Thickness(0, 2)
                };

                stackPanel.Children.Add(new StackPanel()
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 5,
                    Children =
                    {
                        new TextBlock()
                        {
                            Text = Translation.Get("DocumentationControlExampleAuthor", example.Author),
                            FontWeight = FontWeight.Regular,
                            FontSize = 16,
                            Margin = new Thickness(0, 0, 0, 5)
                        },
                        new InfoBadge() { IconSource = new FontIconSource() { Glyph = example.Votes }, VerticalAlignment = VerticalAlignment.Top }
                    }
                });

                static object GetAppResource(string key)
                {
                    Application.Current.TryGetResource(key, ThemeVariant.Default, out var resource);
                    return resource;
                }

                var textEditor = new TextEditor()
                {
                    Foreground = (IBrush)GetAppResource("EditorTextColor"),
                    Background = (IBrush)GetAppResource("EditorBackgroundColor"),
                    Padding = new Thickness(10),
                    Text = Format(example.Example),
                    IsReadOnly = true,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Margin = new Thickness(0, 0, 0, 10)
                };

                textEditor.TextArea.SelectionBrush = ThemeEditor.CurrentTheme.SelectionColor;

                if (SkEditorAPI.Core.GetAppConfig().Font.Equals("Default"))
                {
                    Application.Current.TryGetResource("JetBrainsFont", ThemeVariant.Default, out var font);
                    textEditor.FontFamily = (FontFamily)font;
                }
                else
                {
                    textEditor.FontFamily = new FontFamily(SkEditorAPI.Core.GetAppConfig().Font);
                }

                // TODO NOTRO: Set the syntax of this editor using the new textmate way :D
                //textEditor.SyntaxHighlighting = SyntaxLoader.GetCurrentSkriptHighlighting();
                stackPanel.Children.Add(textEditor);

                ((StackPanel)ExamplesEntry.Content).Children.Add(stackPanel);
            }
        }
        catch (Exception e)
        {
            ExamplesEntry.Content = new TextBlock()
            {
                Text = Translation.Get("DocumentationControlErrorExamples", e.Message),
                Foreground = Brushes.Red,
                FontWeight = FontWeight.SemiLight,
                TextWrapping = TextWrapping.Wrap
            };
        }
    }

    private static string Format(string input)
    {
        return input.Replace("&gt;", ">").Replace("&lt;", "<").Replace("&amp;", "&")
            .Replace("&quot;", "\"").Replace("&apos;", "'")
            .Replace("&#039;", "'").Replace("&#034;", "\"");
    }

    #region Actions

    private void FilterByThisType(object? sender, RoutedEventArgs e)
    {
        _documentationControl.FilterByType(_entry.DocType);
    }

    private void FilterByThisAddon(object? sender, RoutedEventArgs e)
    {
        _documentationControl.FilterByAddon(_entry.Addon);
    }

    #endregion

    static string GenerateUsablePattern(string pattern)
    {
        var optionalPattern = @"\[([^[\]])*?\]";
        while (Regex.IsMatch(pattern, optionalPattern))
            pattern = Regex.Replace(pattern, optionalPattern, "");

        // Step 2: Select the first option within ()
        pattern = FirstOptionInParenthesesRegex().Replace(pattern, "$1");

        // Step 3: Leave everything within %% untouched (already handled by not modifying it)

        // Trim any extra whitespace
        pattern = WhitespaceRegex().Replace(pattern, " ").Trim();

        return pattern;
    }

    [GeneratedRegex(@"\(([^|]+)\|.*?\)")]
    private static partial Regex FirstOptionInParenthesesRegex();
    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}