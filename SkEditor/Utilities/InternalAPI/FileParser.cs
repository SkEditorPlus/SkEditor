using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Rendering;
using SkEditor.API;
using SkEditor.Parser;
using SkEditor.Parser.Elements;
using SkEditor.TextMarkers;
using SkEditor.Utilities.Files;
using SkEditor.Utilities.Parser;

namespace SkEditor.Utilities.InternalAPI;

/// <summary>
/// Class that is attached to every opened TabViewItem it its
/// content is a TextEditor. Every parse stuff that uses the new
/// parser version (v2) should be done through this class!
/// </summary>
public class FileParser
{
    
    public readonly TextMarkerService TextMarkerService;
    public readonly HintGenerator HintGenerator;
    public ParsingContext LastContext = new() { Debug = SkEditorAPI.Core.IsDeveloperMode() };
   
    public FileParser(TextEditor editor)
    {
        Editor = editor;
        
        TextMarkerService = new TextMarkerService(Editor);
        Editor.TextArea.TextView.ElementGenerators.Add(HintGenerator = new HintGenerator());
        Editor.TextChanged += (sender, args) =>
        {
            IsParsed = false;
        };
        /*Editor.PointerHover += (sender, args) =>
        {
            var position = Editor.GetPositionFromPoint(args.GetPosition(Editor));
            if (position == null)
                return;
            var tooltip = LastContext.Tooltips.FirstOrDefault(t => t.Line == position.Value.Line 
                                                                   && t.StartingIndex <= position.Value.Column
                                                                   && t.StartingIndex + t.Length >= position.Value.Column);
            if (tooltip == null)
            {
                CurrentTooltip = null;
                return;
            } else if (CurrentTooltip == null)
            {
                SkEditorAPI.Logs.Debug("Showing tooltip at line " + position.Value.Line + " with length " + tooltip.Length);

                var flyout = new Flyout
                {
                    Content = tooltip.Tooltip,
                    Placement = PlacementMode.LeftEdgeAlignedTop
                };
                flyout.ShowAt(editor);
                
                CurrentTooltip = (tooltip, flyout);
            } else if (CurrentTooltip.Value.Item1 == tooltip)
            {
                SkEditorAPI.Logs.Debug("Updating location of tooltip at line " + position.Value.Line);
                
                var flyout = CurrentTooltip.Value.Item2;
                flyout.ShowAt(editor);
            } else
            {
                SkEditorAPI.Logs.Debug("Hiding tooltip at line " + position.Value.Line);
                FlyoutBase.SetAttachedFlyout(editor, null);
            }
        };*/
    }
    
    public EventHandler OnParsed;
    public TextEditor Editor { get; private set; }
    public bool IsParsed { get; private set; } = false;
    
    public List<Node> ParsedNodes { get; private set; } = new();
    public List<TooltipInformation> Tooltips { get; private set; } = new();
    
    public (TooltipInformation, Flyout)? CurrentTooltip { get; private set; }

    public void Parse()
    {
        var lines = Editor.Document.Lines
            .Select(line => Editor.Document.GetText(line));
        ParsedNodes = SectionParser.Parse(lines.ToArray());
        SkEditorAPI.Logs.Debug($"Parsed {ParsedNodes.Count} pure nodes, now parsing elements ...");

        LastContext = new ParsingContext { Debug = SkEditorAPI.Core.IsDeveloperMode() };
        ElementParser.ParseNodes(ParsedNodes, LastContext);
        
        IsParsed = true;
        OnParsed?.Invoke(this, EventArgs.Empty);
        SkEditorAPI.Addons.GetSelfAddon().ParserPanel.Panel.Refresh(ParsedNodes.OfType<SectionNode>().ToList());
        SkEditorAPI.Addons.GetSelfAddon().ParserPanel.Panel.UpdateSectionTypeChoice(ParsedNodes.OfType<SectionNode>().ToList());
        
        SkEditorAPI.Logs.Debug($"Parsed {ParsedNodes.Count} nodes, with {LastContext.Warnings.Count} warnings! [{LastContext.ParsedNodes.Count}]");

        HintGenerator.Controls.Clear();
        TextMarkerService.RemoveAll(marker => true);
        Editor.TextArea.TextView.Redraw();
        
        foreach (var pair in LastContext.Warnings)
        {
            var node = pair.Item1;
            var warning = pair.Item2;
            if (!IsWarningIgnored(warning.Identifier))
            {
                SkEditorAPI.Logs.Debug("Warning: " + warning.Message + " at line " + node.Line);
                var line = Editor.Document.GetLineByNumber(node.Line);
            
                var marker = TextMarkerService.Create(line.Offset, line.Length);
                marker.MarkerTypes = TextMarkerTypes.SquigglyUnderline;
                marker.MarkerColor = Colors.Orange;
            
                var offset = line.Offset + line.Length;
                var panel = ParseContent(warning.Message);
                panel.Margin = new Thickness(5, 0, 0, 0);
                HintGenerator.Controls.Add((offset, panel));
            }
            else
            {
                SkEditorAPI.Logs.Debug("Ignoring warning: " + warning + " at line " + node.Line);
            }
            Editor.TextArea.TextView.Redraw();
        }
        
        Tooltips.Clear();
        Tooltips.AddRange(LastContext.Tooltips);
        //SkEditorAPI.Logs.Debug($"Gathered {Tooltips.Count} tooltips.");
        
        if (SkEditorAPI.Core.GetAppConfig().EnableFolding) 
            FoldingCreator.CreateFoldings(Editor, ParsedNodes);

        void VisitNode(Node node, int indent = 0)
        {
            if (node is SectionNode sectionNode)
            {
                foreach (var child in sectionNode.Children)
                {
                    VisitNode(child, indent + 2);
                }
            }
        }

        if (false)
        {
            foreach (var node in ParsedNodes)
            {
                VisitNode(node);
            }
        }
    }

    public StackPanel ParseContent(string input)
    {
        return new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Children =
            {
                new TextBlock {
                    Text = input, 
                    Foreground = Brushes.Orange,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Margin = new Thickness(15, 0, 0, 0),
                    FontStyle = FontStyle.Italic,
                    Opacity = 0.6
                }
            }
        };
        
        /*
        var regex = new Regex(@"<line:(\d+)>");
        var matches = regex.Matches(input);
        
        var lastMatch = 0;
        foreach (Match match in matches)
        {
            var index = match.Index;
            var length = match.Length;
            var line = int.Parse(match.Groups[1].Value);
            
            var text = input.Substring(lastMatch, index - lastMatch);
            var textBlock = new TextBlock { Text = text, 
                Foreground = Brushes.Orange,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(15, 0, 0, 0),
                FontStyle = FontStyle.Italic,
                Opacity = 0.6
            };
            panel.Children.Add(textBlock);
            
            var button = new Button()
            {
                Content = $"line {line}",
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                Padding = new Thickness(1),
                Foreground = Brushes.CornflowerBlue,
                VerticalAlignment = VerticalAlignment.Center,
            };
            button.Click += (sender, args) =>
            {
                SkEditorAPI.Logs.Debug("Tapped on line " + line);
                Editor.ScrollToLine(line);
                Editor.ScrollToHorizontalOffset(0);
                Editor.CaretOffset = Editor.Document.GetLineByNumber(line).Offset;
                Editor.Focus();
            };
            panel.Children.Add(button);
            
            lastMatch = index + length;
        }
        
        if (!string.IsNullOrEmpty(input.Substring(lastMatch)))
        {
            var text = input.Substring(lastMatch);
            var textBlock = new TextBlock { Text = text, 
                Foreground = Brushes.Orange,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(15, 0, 0, 0),
                FontStyle = FontStyle.Italic,
                Opacity = 0.6
            };
            panel.Children.Add(textBlock);
        }
        
        return panel;*/
    }

    public Node? FindNodeAtLine(int line, bool deep = false)
    {
        if (line < 0)
            return null;
        if (!deep)
        {
            return ParsedNodes.FirstOrDefault(node => node.Line == line);
        }
        else
        {
            Node? result = null;
            void VisitNode(Node node)
            {
                if (node.Line == line)
                {
                    result = node;
                    return;
                }
                if (node is SectionNode sectionNode)
                {
                    foreach (var child in sectionNode.Children)
                    {
                        VisitNode(child);
                    }
                }
            }

            foreach (var node in ParsedNodes)
            {
                VisitNode(node);
            }

            return result;
        }
    }

    public void ReplaceReference(CodeReference reference, string content)
    {
        var line = reference.Node.Line;
        var lineObj = Editor.Document.GetLineByNumber(line);
        var offset = lineObj.Offset + reference.Position + reference.Node.Indent;
        var length = reference.Length;
        
        Editor.Document.Replace(offset, length, content);
        
        Parse();
    }
    
    public static bool IsWarningIgnored(string warning)
    {
        var dict = SkEditorAPI.Core.GetAppConfig().IgnoredParserWarnings;
        return dict.ContainsKey(warning) && dict[warning];
    }
}

public class HintGenerator : VisualLineElementGenerator, IComparer<(int, Control)>
{
    public readonly List<(int, Control)> Controls = [];

    /// <summary>
    /// Gets the first interested offset using binary search
    /// </summary>
    /// <returns>The first interested offset.</returns>
    /// <param name="startOffset">Start offset.</param>
    public override int GetFirstInterestedOffset(int startOffset)
    {
        int pos = Controls.BinarySearch((startOffset, null), this);
        if (pos < 0)
            pos = ~pos;
        if (pos < Controls.Count)
            return Controls[pos].Item1;
        else
            return -1;
    }

    public override VisualLineElement ConstructElement(int offset)
    {
        int pos = Controls.BinarySearch((offset, null), this);
        if (pos >= 0)
            return new InlineObjectElement(0, Controls[pos].Item2);
        else
            return null;
    }

    public int Compare((int, Control) x, (int, Control) y)
    {
        return x.Item1.CompareTo(y.Item1);
    }
}