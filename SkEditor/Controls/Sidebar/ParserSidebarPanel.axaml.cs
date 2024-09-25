using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using SkEditor.API;
using SkEditor.Parser;
using SkEditor.Parser.Elements;
using SkEditor.Utilities;
using SkEditor.Utilities.InternalAPI;
using SkEditor.Utilities.Styling;

namespace SkEditor.Controls.Sidebar;

public partial class ParserSidebarPanel : UserControl
{
    public static bool CodeParserEnabled => SkEditorAPI.Core.GetAppConfig().EnableCodeParser;
    public ObservableCollection<SectionNode> Nodes { get; set; } = [];
    public FileParser Parser => SkEditorAPI.Files.GetCurrentOpenedFile()["Parser"] as FileParser;

    public void Refresh(List<SectionNode> nodes)
    {
        Nodes.Clear();
        nodes.ForEach(Nodes.Add);

        var viewModel = (ParserFilterViewModel) DataContext;
        viewModel.AllSections = new ObservableCollection<Node>(nodes);

        UpdateInformationBox();
    }

    public ParserSidebarPanel()
    {
        InitializeComponent();

        DataContext = new ParserFilterViewModel();

        ParserDisabled.IsVisible = !CodeParserEnabled;
        ScrollViewer.IsVisible = CodeParserEnabled;

        if (SkEditorAPI.Core.GetAppConfig().EnableRealtimeCodeParser)
        {
            ParseButton.IsVisible = false;
        }
        else
        {
            ParseButton.IsEnabled = CodeParserEnabled;
            ParseButton.Click += (_, _) => ParseCurrentFile();
        }

        EnableParser.Click += (_, _) =>
        {
            SkEditorAPI.Core.GetAppConfig().EnableCodeParser = true;
            SkEditorAPI.Core.GetAppConfig().Save();

            ParserDisabled.IsVisible = false;
            ScrollViewer.IsVisible = true;
            ParseButton.IsEnabled = true;
        };
        ClearSearch.Click += (_, _) => ClearSearchFilter();
        SearchBox.KeyUp += (_, _) => UpdateSearchFilter();
        TypeFilterBox.SelectionChanged += (_, _) => UpdateSearchFilter();
    }

    public void ParseCurrentFile()
    {
        var parser = SkEditorAPI.Files.GetCurrentOpenedFile()["Parser"] as FileParser;
        if (parser == null)
            return;

        ParseButton.IsEnabled = false;
        parser.Parse();
    }

    public void UpdateSearchFilter()
    {
        Refresh([.. Nodes]);
    }

    public void ClearSearchFilter()
    {
        var viewModel = (ParserFilterViewModel)DataContext;
        viewModel.SearchText = "";
        viewModel.SelectedFilterIndex = 0;
        Refresh([.. Nodes]);
    }

    public void UpdateSectionTypeChoice(List<SectionNode> nodes)
    {
        var structures = new List<Element>();
        foreach (var node in nodes)
            if (node.Element != null && structures.All(el => el.GetType() != node.Element.GetType())) 
                structures.Add(node.Element);

        var viewModel = (ParserFilterViewModel) DataContext;
        var oldIndex = viewModel.SelectedFilterIndex;
        viewModel.AvailableSections = new ObservableCollection<Element>(structures);
        viewModel.SelectedFilterIndex = oldIndex;
    }
    
    public void UpdateInformationBox(bool notifyUnparsing = false)
    {
        if (!CodeParserEnabled)
        {
            Nodes.Clear();
            CannotParseInfoText.IsVisible = false;
            return;
        }

        if (notifyUnparsing)
        {
            Nodes.Clear();
            return;
        }

        if (Nodes.Count == 0)
        {
            CannotParseInfo.IsVisible = true;
            CannotParseInfoText.Text = Translation.Get("CodeParserNoSections");
            return;
        }

        CannotParseInfo.IsVisible = false;
    }

    public class ParserPanel : SidebarPanel
    {
        public override UserControl Content => Panel;
        public override IconSource Icon => new SymbolIconSource() { Symbol = Symbol.Code };
        public override bool IsDisabled => false;

        public readonly ParserSidebarPanel Panel = new();

        public override int DesiredWidth { get; } = 350;
    }
    
}

public record ParserSectionNode(SectionNode SectionNode, FileParser Parser)
{
    public string Name => SectionNode.Key;
    /*public IconSource Icon => Type switch
    {
        ParserSidebarPanel.StructureType.Command => SkEditorAPI.Core.GetApplicationResource("MagicWandIcon") as IconSource,
        ParserSidebarPanel.StructureType.Event => SkEditorAPI.Core.GetApplicationResource("LightingIcon") as IconSource,
        ParserSidebarPanel.StructureType.Function => SkEditorAPI.Core.GetApplicationResource("FunctionIcon") as IconSource,
        ParserSidebarPanel.StructureType.Options => new SymbolIconSource() { Symbol = Symbol.Setting, FontSize = 20 },
        _ => SkEditorAPI.Core.GetApplicationResource("CodeIcon") as IconSource
    };*/

    public string LinesDisplay => $"From {SectionNode.Line} to {SectionNode.FindLastNode().Line}";

    #region Navigation

    public RelayCommand NavigateToCommand => new(NavigateTo);
    public void NavigateTo()
    {
        var editor = Parser.Editor;
        editor.ScrollTo(SectionNode.Line + 1, 0);
        editor.CaretOffset = editor.Document.GetOffset(SectionNode.Line, 0);
        editor.Focus();
    }

    #endregion
    
    #region Section Highlighting

    public LineColorizer Colorizer = new(SectionNode.Line,
        SectionNode.FindLastNode().Line);
        
    public RelayCommand OnSectionPointerEntered => new(HighlightSection);
    public RelayCommand OnSectionPointerExited => new(RemoveHighlight);
        
    public void HighlightSection() => Parser.Editor.TextArea.TextView.LineTransformers.Add(Colorizer);

    public void RemoveHighlight() => Parser.Editor.TextArea.TextView.LineTransformers.Remove(Colorizer);
        
    public class LineColorizer(int from, int to) : DocumentColorizingTransformer
    {
        protected override void ColorizeLine(DocumentLine line)
        {
            if (!line.IsDeleted && line.LineNumber >= from && line.LineNumber <= to)
            {
                ChangeLinePart(line.Offset, line.EndOffset, ApplyChanges);
            }
        }

        private void ApplyChanges(VisualLineElement element)
        {
            element.BackgroundBrush = ThemeEditor.CurrentTheme.SelectionColor;
        }
    }

    #endregion
}