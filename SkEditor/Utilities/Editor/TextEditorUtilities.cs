using Avalonia;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using System;
using System.Collections.Generic;

namespace SkEditor.Utilities.Editor;
public class TextEditorUtilities
{
    public static SimpleSegment GetWordAtMousePosition(Point pos, TextArea textArea)
    {
        var textView = textArea.TextView;
        if (textView == null) return SimpleSegment.Invalid;

        if (pos.Y < 0) pos = pos.WithY(0);
        if (pos.Y > textView.Bounds.Height) pos = pos.WithY(textView.Bounds.Height);

        pos += textView.ScrollOffset;

        var line = textView.GetVisualLineFromVisualTop(pos.Y);
        if (line == null || line.TextLines == null) return SimpleSegment.Invalid;

        var visualColumn = line.GetVisualColumn(pos, textArea.Selection.EnableVirtualSpace);

        var wordStartVc = line.GetNextCaretPosition(visualColumn + 1, LogicalDirection.Backward, CaretPositioningMode.WordStartOrSymbol, textArea.Selection.EnableVirtualSpace);
        if (wordStartVc == -1) wordStartVc = 0;

        var wordEndVc = line.GetNextCaretPosition(wordStartVc, LogicalDirection.Forward, CaretPositioningMode.WordBorderOrSymbol, textArea.Selection.EnableVirtualSpace);
        if (wordEndVc == -1) wordEndVc = line.VisualLength;

        var relOffset = line.FirstDocumentLine.Offset;
        var wordStartOffset = line.GetRelativeOffset(wordStartVc) + relOffset;
        var wordEndOffset = line.GetRelativeOffset(wordEndVc) + relOffset;

        return new SimpleSegment(wordStartOffset, wordEndOffset - wordStartOffset);
    }

    public static int GetLineNumberFromMousePosition(Point pos, TextArea textArea)
    {
        var textView = textArea.TextView;
        if (textView == null) return -1;

        if (pos.Y < 0) pos = pos.WithY(0);
        if (pos.Y > textView.Bounds.Height) pos = pos.WithY(textView.Bounds.Height);

        pos += textView.ScrollOffset;

        var line = textView.GetVisualLineFromVisualTop(pos.Y);
        if (line == null) return -1;

        return line.FirstDocumentLine.LineNumber;
    }

    public static SimpleSegment GetSegmentBeforeOffset(int offset, TextDocument document)
    {
        if (offset <= 0) return SimpleSegment.Invalid;

        var line = document.GetLineByOffset(offset);
        if (line == null) return SimpleSegment.Invalid;

        var lineText = document.GetText(line.Offset, offset - line.Offset);
        var wordStartPos = lineText.LastIndexOfAny([' ', '\t', '\n', '\r']);
        if (wordStartPos == -1)
        {
            return new SimpleSegment(line.Offset, offset - line.Offset);
        }

        return new SimpleSegment(line.Offset + wordStartPos + 1, offset - line.Offset - wordStartPos - 1);
    }

    public static IEnumerable<SimpleSegment> GetWordOccurrences(string word, TextDocument document)
    {
        string wholeText = document.Text;
        int offset = 0;
        int wordStartPos;

        while ((wordStartPos = wholeText.IndexOf(word, offset, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            yield return new SimpleSegment(wordStartPos, word.Length);
            offset = wordStartPos + 1;
        }

        yield break;
    }
}
