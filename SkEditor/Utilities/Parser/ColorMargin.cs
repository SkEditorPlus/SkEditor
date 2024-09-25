using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using CommunityToolkit.Mvvm.Input;
using SkEditor.API;
using SkEditor.Parser;
using SkEditor.Utilities.Editor;
using SkEditor.Utilities.InternalAPI;
using SkEditor.Utilities.Parser.Elements;
using SkEditor.Utilities.Styling;
using SkEditor.Views;
using Color = System.Drawing.Color;

namespace SkEditor.Utilities.Parser;

public class ColorMargin : AbstractMargin
{
    public FileParser Parser { get; set; }
    public Node? HoveredNode { get; set; }
    
    public ColorMargin(FileParser parser)
    {
        Cursor = new Cursor(StandardCursorType.Arrow);
        Parser = parser;
        
        Parser.OnParsed += (_, _) => Reload();
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
    }

    private void OnVisualLinesChanged(object? sender, EventArgs eventArgs)
    {
        Reload();
    }
    
    protected override Size MeasureOverride(Size availableSize)
    {
        return new Size(Padding * 2 + ColorSquareSize, 0);
    }

    private const int Padding = 2;
    private const int ColorSquareSize = 10; // for font size 12
    public override void Render(DrawingContext context)
    {
        context.DrawRectangle(SkEditorAPI.Core.GetApplicationResource("EditorBackgroundColor") as IBrush, null, Bounds);

        var lineHeight = Parser.Editor.FontSize;
        var lineSpacing = lineHeight * 0.345;
        var squareSize = (ColorSquareSize / (double) 12) * lineHeight;
        
        void DrawNode(Node node)
        {
            var x = Padding;
            if (node.Element is ExprProviderElement element)
            {
                foreach (var color in element.Colors)
                {
                   
                }
                var y = lineSpacing + (node.Line - 1) * lineHeight + (node.Line - 1) * lineSpacing - 1;
                var scrollViewer = TextEditorEventHandler.GetScrollViewer(Parser.Editor);
                y -= scrollViewer.Offset.Y;
                    
                DrawColors(x, (int) y, (int) squareSize, context, element.Colors.Select(c => c.Color).ToArray());
                    
                x += (int) squareSize + Padding;
            }
            
            if (node is SectionNode sectionNode)
                foreach (var child in sectionNode.Children)
                    DrawNode(child);
        }
        
        foreach (var node in Parser.ParsedNodes)
            DrawNode(node);

        base.Render(context);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        var position = e.GetPosition(this);
        var viewer = TextEditorEventHandler.GetScrollViewer(Parser.Editor);
        var line = (int) (position.Y / (Parser.Editor.FontSize + Parser.Editor.FontSize * 0.345) + viewer.Offset.Y / Parser.Editor.FontSize) + 1;
        HoveredNode = Parser.FindNodeAtLine(line, true);
        Cursor = HoveredNode is { Element: ExprProviderElement { Colors.Count: > 0 } } ? new Cursor(StandardCursorType.Hand) : new Cursor(StandardCursorType.Arrow);
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        HoveredNode = null;
        Cursor = new Cursor(StandardCursorType.Arrow);
    }

    protected override async void OnPointerPressed(PointerPressedEventArgs e)
    {
        if (HoveredNode is { Element: ExprProviderElement { Colors.Count: > 0 } } node)
        {
            
        }
    }

    /// <summary>
    /// Draw the given colors array into one single square.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    /// <param name="size"></param>
    /// <param name="context"></param>
    /// <param name="colors"></param>
    private void DrawColors(int x, int y, int size, DrawingContext context, Color[] colors)
    {
        Avalonia.Media.Color ConvertColor(Color color)
        {
            return new Avalonia.Media.Color(color.A, color.R, color.G, color.B);
        }
        
        if (colors.Length == 0)
            return;
        
        if (colors.Length == 1)
        {
            var brush = new SolidColorBrush(ConvertColor(colors[0]));
            context.DrawRectangle(brush, null, new Rect(x, y, size, size));
            return;
        }
        
        if (colors.Length == 2)
        {
            var brush1 = new SolidColorBrush(ConvertColor(colors[0]));
            var brush2 = new SolidColorBrush(ConvertColor(colors[1]));
            // Draw two triangles
            var firstTriangle = new[]
            {
                new Point(x, y),
                new Point(x + size, y),
                new Point(x, y + size)
            };
            var secondTriangle = new[]
            {
                new Point(x + size, y),
                new Point(x, y + size),
                new Point(x + size, y + size)
            };

            context.DrawGeometry(brush1, null, new PolylineGeometry(firstTriangle, true));
            context.DrawGeometry(brush2, null, new PolylineGeometry(secondTriangle, true));
            
            return;
        }
        
        if (colors.Length == 3)
        {
            var brush1 = new SolidColorBrush(ConvertColor(colors[0]));
            var brush2 = new SolidColorBrush(ConvertColor(colors[1]));
            var brush3 = new SolidColorBrush(ConvertColor(colors[2]));
            // Draw three triangles
            var firstTriangle = new[]
            {
                new Point(x, y),
                new Point(x + size, y),
                new Point(x, y + size)
            };
            var secondTriangle = new[]
            {
                new Point(x + size, y),
                new Point(x, y + size),
                new Point(x + size, y + size)
            };
            var thirdTriangle = new[]
            {
                new Point(x, y),
                new Point(x + size, y),
                new Point(x + size, y + size)
            };

            context.DrawGeometry(brush1, null, new PolylineGeometry(firstTriangle, true));
            context.DrawGeometry(brush2, null, new PolylineGeometry(secondTriangle, true));
            context.DrawGeometry(brush3, null, new PolylineGeometry(thirdTriangle, true));
            
            return;
        }

        if (colors.Length == 4)
        {
            var brush1 = new SolidColorBrush(ConvertColor(colors[0]));
            var brush2 = new SolidColorBrush(ConvertColor(colors[1]));
            var brush3 = new SolidColorBrush(ConvertColor(colors[2]));
            var brush4 = new SolidColorBrush(ConvertColor(colors[3]));
            
            var center = new Point(x + size / 2, y + size / 2);
            var firstTriangle = new[] { new Point(x, y), new Point(x + size, y), center };
            var secondTriangle = new[] { new Point(x + size, y), new Point(x + size, y + size), center };
            var thirdTriangle = new[] { new Point(x + size, y + size), new Point(x, y + size), center };
            var fourthTriangle = new[] { new Point(x, y + size), new Point(x, y), center };

            context.DrawGeometry(brush1, null, new PolylineGeometry(firstTriangle, true));
            context.DrawGeometry(brush2, null, new PolylineGeometry(secondTriangle, true));
            context.DrawGeometry(brush3, null, new PolylineGeometry(thirdTriangle, true));
            context.DrawGeometry(brush4, null, new PolylineGeometry(fourthTriangle, true));
            
            return;
        }
        
        // For more than 4 colors, we draw columns of colors with equidistant spacing
        var columnWidth = size / colors.Length;
        for (var i = 0; i < colors.Length; i++)
        {
            var brush = new SolidColorBrush(ConvertColor(colors[i]));
            context.DrawRectangle(brush, null, new Rect(x + i * columnWidth, y, columnWidth, size));
        }
    }
    
}