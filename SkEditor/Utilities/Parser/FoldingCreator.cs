using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Folding;
using AvaloniaEdit.Utils;
using SkEditor.Utilities.Files;
using System.Collections.Generic;
using System.Linq;
using SkEditor.Parser;

namespace SkEditor.Utilities.Parser;
public static class FoldingCreator
{
    public static void CreateFoldings(TextEditor editor, List<Node> structures)
    {
        List<FoldingSection> foldedSections = GetOldFoldedSections(editor);

        FoldingManager foldingManager = FoldingManager.Install(editor.TextArea);
        StyleMarkers(editor);

        void AddFoldings(List<Node> nodes)
        {
            foreach (Node node in nodes)
            {
                if (!node.IsSection)
                    continue;
                
                var section = (SectionNode) node;
                var endingNodeLine = section.FindLastNode().Line;
                
                var start = editor.Document.GetLineByNumber(section.Line);
                var end = editor.Document.GetLineByNumber(endingNodeLine);

                while (string.IsNullOrWhiteSpace(editor.Document.GetText(end).Trim()))
                    end = editor.Document.GetLineByNumber(end.LineNumber - 1);

                FoldingSection foldingSection = foldingManager.CreateFolding(start.Offset, end.EndOffset);

                foldingSection.IsFolded = foldedSections.Any(fs => fs.StartOffset == foldingSection.StartOffset
                                                                   && fs.EndOffset == foldingSection.EndOffset);

                var indentString = editor.Document.GetText(start).TakeWhile(char.IsWhiteSpace).ToArray();
                foldingSection.Title = new string(indentString) + (section.Element?.SectionDisplay() ?? node.Key);
                
                AddFoldings(section.Children);
            }
        }
        
        AddFoldings(structures);
    }

    private static void StyleMarkers(TextEditor editor)
    {
        FoldingMargin margin = editor.TextArea.LeftMargins.OfType<FoldingMargin>().FirstOrDefault();
        margin?.SetValue(FoldingMargin.FoldingMarkerBackgroundBrushProperty, new SolidColorBrush(Color.Parse("#27282a")));
        margin?.SetValue(FoldingMargin.FoldingMarkerBrushProperty, new SolidColorBrush(Color.Parse("#313234")));
        margin?.SetValue(FoldingMargin.SelectedFoldingMarkerBackgroundBrushProperty, new SolidColorBrush(Color.Parse("#3f4042")));
        margin?.SetValue(FoldingMargin.SelectedFoldingMarkerBrushProperty, new SolidColorBrush(Color.Parse("#939395")));
    }

    private static List<FoldingSection> GetOldFoldedSections(TextEditor editor)
    {
        FoldingManager? oldFoldingManager = editor.GetService<FoldingManager>();

        List<FoldingSection> foldedSections = [];
        if (oldFoldingManager != null)
        {
            foldedSections = oldFoldingManager.AllFoldings.Where(folding => folding.IsFolded).ToList();
            FoldingManager.Uninstall(oldFoldingManager);
        }

        return foldedSections;
    }

    public static List<int> GetHiddenLines(OpenedFile file)
    {
        if (file.Editor == null)
            return [];

        List<int> hiddenLines = [];
        FoldingManager? foldingManager = file.Editor.GetService<FoldingManager>();

        if (foldingManager != null)
        {
            foreach (FoldingSection folding in foldingManager.AllFoldings)
            {
                if (folding.IsFolded)
                {
                    var ls = new List<int>();
                    for (var i = folding.StartOffset; i < folding.EndOffset; i++)
                    {
                        var l = file.Editor.Document.GetLineByOffset(i).LineNumber;
                        if (!ls.Contains(l))
                            ls.Add(l);
                    }

                    ls.RemoveAt(0); // Remove the first line of the section
                    hiddenLines.AddRange(ls);
                }
            }
        }

        return hiddenLines.Distinct().ToList();
    }
}
