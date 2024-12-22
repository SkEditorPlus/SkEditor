using Avalonia;
using Avalonia.Input;
using Avalonia.Media;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using SkEditor.API;
using SkEditor.Utilities.Files;
using SkEditor.Utilities.Parser;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SkEditor.Utilities.Editor;

/// <summary>
/// Represent the left margin column of the code editor. This margin is used to display
/// icons on the left of the editor, next to the line numbers.
///
/// Can be accessed from any <see cref="OpenedFile"/> as <code>Margin</code> custom data key (<see cref="OpenedFile.CustomData"/>)
/// </summary>
public class EditorMargin : AbstractMargin
{
    public Dictionary<(int, int), MarginIconData> RenderedIcons { get; } = new();
    public MarginIconData? HoveredIcon { get; set; }

    public OpenedFile File { get; }
    public EditorMargin(OpenedFile file)
    {
        Cursor = new Cursor(StandardCursorType.Arrow);
        File = file;

        file.Editor.TextArea.LeftMargins.Insert(0, this);
        file.Editor.TextChanged += (_, _) => Reload();
        Reload();
    }

    protected override void OnTextViewChanged(TextView? oldTextView, TextView? newTextView)
    {
        if (oldTextView != null)
            oldTextView.VisualLinesChanged -= OnVisualLinesChanged;
        if (newTextView != null)
            newTextView.VisualLinesChanged += OnVisualLinesChanged;

        base.OnTextViewChanged(oldTextView, newTextView);
    }

    public void Reload()
    {
        InvalidateVisual();
        InvalidateMeasure();
    }

    private void OnVisualLinesChanged(object? sender, EventArgs eventArgs)
    {
        Reload();
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var requiredColumns = Registries.MarginIcons.Select(i => i.ColumnKey).Distinct().Count();
        var scale = File.Editor.FontSize / 12;

        return new Size(16 * requiredColumns * scale, 0);
    }

    public override void Render(DrawingContext context)
    {
        context.DrawRectangle(SkEditorAPI.Core.GetApplicationResource("EditorBackgroundColor") as IBrush, null, Bounds);

        var hidden = FoldingCreator.GetHiddenLines(File);
        var lineHeight = File.Editor.FontSize;
        var lineSpacing = lineHeight * 0.345;
        var scrollViewer = TextEditorEventHandler.GetScrollViewer(File.Editor);
        var scale = File.Editor.FontSize / 12;

        for (var line = 1; line <= File.Editor.LineCount; line++)
        {
            if (hidden.Contains(line))
                continue;

            var separator = hidden.Count(h => h < line);
            var y = (lineSpacing + (line - 1) * lineHeight + (line - 1) * lineSpacing - 1) - scrollViewer.Offset.Y
                    - (separator) * (lineSpacing + lineHeight);
            foreach (var icon in Registries.MarginIcons)
            {
                var key = (line, icon.ColumnKey == null ? 0 : Registries.MarginIcons.Select(i => i.ColumnKey).Distinct().ToList().IndexOf(icon.ColumnKey));
                var x = key.Item2 * 16 * scale;

                if (icon.DrawingFunc(new DrawingArgs(context, File, (float)scale, line, (int)y, (int)x)))
                    RenderedIcons[key] = icon;
                else
                    RenderedIcons.Remove(key);
            }
        }

        base.Render(context);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var position = e.GetPosition(this);
        var point = File.Editor.GetPositionFromPoint(position);
        if (point == null)
            return;
        var line = point.Value.Line;
        var x = (int)position.X / 16;

        foreach (var icon in RenderedIcons)
        {
            var l = icon.Key.Item1;
            var c = icon.Key.Item2;
            if (line != l || x != c)
                continue;

            Cursor = new Cursor(StandardCursorType.Hand);
            HoveredIcon = icon.Value;
            return;
        }

        Cursor = new Cursor(StandardCursorType.Arrow);
        HoveredIcon = null;
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        Cursor = new Cursor(StandardCursorType.Arrow);
        HoveredIcon = null;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (HoveredIcon == null)
            return;

        var position = e.GetPosition(this);
        var point = File.Editor.GetPositionFromPoint(position);
        if (point == null)
            return;
        var line = point.Value.Line;

        var args = new ClickedArgs(File, line);
        HoveredIcon.MouseClickFunc(args);
    }
}