using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using AvaloniaEdit.Utils;
using SkEditor.API;

namespace SkEditor.TextMarkers;

/// <summary>
/// Handles the text markers for a code editor.
/// </summary>
public sealed class TextMarkerService : DocumentColorizingTransformer, IBackgroundRenderer, ITextMarkerService, ITextViewConnect
{
    TextSegmentCollection<TextMarker> markers;
    TextDocument document;
    
    Dictionary<ITextMarker, Flyout> _shownMarkers = [];
		
    public TextMarkerService(TextEditor editor)
    {
        if (editor == null)
            throw new ArgumentNullException("document");
        
        this.document = editor.Document;
        this.markers = new TextSegmentCollection<TextMarker>(editor.Document);
        
        editor.TextArea.TextView.BackgroundRenderers.Add(this);
        editor.TextArea.TextView.LineTransformers.Add(this);
        
        var services = editor.Document.GetService<IServiceContainer>();
        services?.AddService(typeof(ITextMarkerService), this);
        
        editor.PointerMoved += (sender, args) =>
        {
            var position = args.GetPosition(editor);
            var pos = editor.GetPositionFromPoint(position);
            if (pos == null)
                return;
            
            var offset = editor.Document.GetOffset(pos.Value.Location);

            var anyMarkers = GetMarkersAtOffset(offset).ToList();
            if (anyMarkers.Count > 0)
            {
                var marker = anyMarkers[0];
                if (_shownMarkers.ContainsKey(marker))
                    return;
                
                if (marker.ToolTip != null)
                {
                    var flyout = new Flyout { Placement = PlacementMode.Pointer, ShowMode = FlyoutShowMode.TransientWithDismissOnPointerMoveAway};
                    FlyoutBase.SetAttachedFlyout(editor, flyout);
                    flyout.Content = marker.ToolTip();
                    flyout.ShowAt(editor, true);
                    flyout.Closed += (sender, args) =>
                    {
                        _shownMarkers.Remove(marker);
                    };
                    _shownMarkers[marker] = flyout;
                }
            } else {
                foreach (var flyout in _shownMarkers.Values)
                    flyout.Hide();
                _shownMarkers.Clear();
            }
        };
    }
		
    #region ITextMarkerService
    public ITextMarker Create(int startOffset, int length)
    {
        if (markers == null)
            throw new InvalidOperationException("Cannot create a marker when not attached to a document");
			
        int textLength = document.TextLength;
        if (startOffset < 0 || startOffset > textLength)
            throw new ArgumentOutOfRangeException("startOffset", startOffset, "Value must be between 0 and " + textLength);
        if (length < 0 || startOffset + length > textLength)
            throw new ArgumentOutOfRangeException("length", length, "length must not be negative and startOffset+length must not be after the end of the document");
			
        TextMarker m = new TextMarker(this, startOffset, length);
        markers.Add(m);
        // no need to mark segment for redraw: the text marker is invisible until a property is set
        return m;
    }
		
    public IEnumerable<ITextMarker> GetMarkersAtOffset(int offset)
    {
        if (markers == null)
            return Enumerable.Empty<ITextMarker>();
        else
            return markers.FindSegmentsContaining(offset);
    }
		
    public IEnumerable<ITextMarker> TextMarkers {
        get { return markers ?? Enumerable.Empty<ITextMarker>(); }
    }
		
    public void RemoveAll(Predicate<ITextMarker> predicate)
    {
        if (predicate == null)
            throw new ArgumentNullException("predicate");
        if (markers != null) {
            foreach (TextMarker m in markers.ToArray()) {
                if (predicate(m))
                    Remove(m);
            }
        }
    }
		
    public void Remove(ITextMarker marker)
    {
        if (marker == null)
            throw new ArgumentNullException("marker");
        TextMarker m = marker as TextMarker;
        if (markers != null && markers.Remove(m)) {
            Redraw(m);
            m.OnDeleted();
        }
    }
		
    /// <summary>
    /// Redraws the specified text segment.
    /// </summary>
    internal void Redraw(ISegment segment)
    {
        if (RedrawRequested != null)
            RedrawRequested(this, EventArgs.Empty);
    }
		
    public event EventHandler RedrawRequested;
    #endregion
		
    #region DocumentColorizingTransformer
    protected override void ColorizeLine(DocumentLine line)
    {
        if (markers == null)
            return;
        int lineStart = line.Offset;
        int lineEnd = lineStart + line.Length;
        foreach (TextMarker marker in markers.FindOverlappingSegments(lineStart, line.Length)) {
            Brush foregroundBrush = null;
            if (marker.ForegroundColor != null) {
                foregroundBrush = new SolidColorBrush(marker.ForegroundColor.Value);
            }
            ChangeLinePart(
                Math.Max(marker.StartOffset, lineStart),
                Math.Min(marker.EndOffset, lineEnd),
                element => {
                    if (foregroundBrush != null) {
                        element.TextRunProperties.SetForegroundBrush(foregroundBrush);
                    }
                    Typeface tf = element.TextRunProperties.Typeface;
                    element.TextRunProperties.SetTypeface(new Typeface(
                        tf.FontFamily,
                        marker.FontStyle ?? tf.Style,
                        marker.FontWeight ?? tf.Weight,
                        tf.Stretch
                    ));
                }
            );
        }
    }
    #endregion
		
    #region IBackgroundRenderer
    public KnownLayer Layer {
        get {
            // draw behind selection
            return KnownLayer.Selection;
        }
    }
		
    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (textView == null)
            throw new ArgumentNullException("textView");
        if (drawingContext == null)
            throw new ArgumentNullException("drawingContext");
        if (markers == null || !textView.VisualLinesValid)
            return;
        var visualLines = textView.VisualLines;
        if (visualLines.Count == 0)
            return;
        int viewStart = visualLines.First().FirstDocumentLine.Offset;
        int viewEnd = visualLines.Last().LastDocumentLine.EndOffset;
        foreach (TextMarker marker in markers.FindOverlappingSegments(viewStart, viewEnd - viewStart)) {
            if (marker.BackgroundColor != null) {
                BackgroundGeometryBuilder geoBuilder = new BackgroundGeometryBuilder();
                geoBuilder.AlignToWholePixels = true;
                geoBuilder.CornerRadius = 3;
                geoBuilder.AddSegment(textView, marker);
                Geometry geometry = geoBuilder.CreateGeometry();
                if (geometry != null) {
                    Color color = marker.BackgroundColor.Value;
                    SolidColorBrush brush = new SolidColorBrush(color);
                    drawingContext.DrawGeometry(brush, null, geometry);
                }
            }
            var underlineMarkerTypes = TextMarkerTypes.SquigglyUnderline | TextMarkerTypes.NormalUnderline | TextMarkerTypes.DottedUnderline;
            if ((marker.MarkerTypes & underlineMarkerTypes) != 0) {
                foreach (Rect r in BackgroundGeometryBuilder.GetRectsForSegment(textView, marker)) {
                    Point startPoint = r.BottomLeft;
                    Point endPoint = r.BottomRight;
						
                    Brush usedBrush = new SolidColorBrush(marker.MarkerColor);
                    if ((marker.MarkerTypes & TextMarkerTypes.SquigglyUnderline) != 0) {
                        double offset = 2.5;
							
                        int count = Math.Max((int)((endPoint.X - startPoint.X) / offset) + 1, 4);
							
                        StreamGeometry geometry = new StreamGeometry();
							
                        using (StreamGeometryContext ctx = geometry.Open()) {
                            ctx.BeginFigure(startPoint, false);
                            //ctx.PolyLineTo(CreatePoints(startPoint, endPoint, offset, count).ToArray(), true, false);
                            Point[] points = CreatePoints(startPoint, endPoint, offset, count).ToArray();
                            for (int i = 0; i < points.Length; i++)
                            {
                                if (i + 1 < points.Length)
                                {
                                    ctx.QuadraticBezierTo(points[i], new Point((points[i].X + points[i + 1].X) / 2, points[i].Y + offset));
                                }
                                else
                                {
                                    ctx.QuadraticBezierTo(points[i], new Point((points[i].X + endPoint.X) / 2, points[i].Y + offset));
                                }
                            }
                        }

                        var fontSize = SkEditorAPI.Files.GetCurrentOpenedFile().Editor.FontSize;
                        var strokeThickness = (float)Math.Max(fontSize / 15, 1);

                        Pen usedPen = new(usedBrush, strokeThickness);
                        drawingContext.DrawGeometry(Brushes.Transparent, usedPen, geometry);
                    }
                    if ((marker.MarkerTypes & TextMarkerTypes.NormalUnderline) != 0) {
                        Pen usedPen = new Pen(usedBrush, 1);
                        drawingContext.DrawLine(usedPen, startPoint, endPoint);
                    }
                    if ((marker.MarkerTypes & TextMarkerTypes.DottedUnderline) != 0) {
                        Pen usedPen = new Pen(usedBrush, 1);
                        usedPen.DashStyle = DashStyle.Dot;
                        drawingContext.DrawLine(usedPen, startPoint, endPoint);
                    }
                }
            }
        }
    }
		
    IEnumerable<Point> CreatePoints(Point start, Point end, double offset, int count)
    {
        for (int i = 0; i < count; i++)
            yield return new Point(start.X + i * offset, start.Y - ((i + 1) % 2 == 0 ? offset : 0));
    }
    #endregion
}
	
public sealed class TextMarker : TextSegment, ITextMarker
{
    readonly TextMarkerService service;
		
    public TextMarker(TextMarkerService service, int startOffset, int length)
    {
        if (service == null)
            throw new ArgumentNullException("service");
        this.service = service;
        this.StartOffset = startOffset;
        this.Length = length;
        this.markerTypes = TextMarkerTypes.None;
    }
		
    public event EventHandler Deleted;
		
    public bool IsDeleted {
        get { return !this.IsConnectedToCollection; }
    }
		
    public void Delete()
    {
        service.Remove(this);
    }
		
    internal void OnDeleted()
    {
        if (Deleted != null)
            Deleted(this, EventArgs.Empty);
    }
		
    void Redraw()
    {
        service.Redraw(this);
    }
		
    Color? backgroundColor;
		
    public Color? BackgroundColor {
        get { return backgroundColor; }
        set {
            if (backgroundColor != value) {
                backgroundColor = value;
                Redraw();
            }
        }
    }
		
    Color? foregroundColor;
		
    public Color? ForegroundColor {
        get { return foregroundColor; }
        set {
            if (foregroundColor != value) {
                foregroundColor = value;
                Redraw();
            }
        }
    }
		
    FontWeight? fontWeight;
		
    public FontWeight? FontWeight {
        get { return fontWeight; }
        set {
            if (fontWeight != value) {
                fontWeight = value;
                Redraw();
            }
        }
    }
		
    FontStyle? fontStyle;
		
    public FontStyle? FontStyle {
        get { return fontStyle; }
        set {
            if (fontStyle != value) {
                fontStyle = value;
                Redraw();
            }
        }
    }
		
    public object Tag { get; set; }
		
    TextMarkerTypes markerTypes;
		
    public TextMarkerTypes MarkerTypes {
        get { return markerTypes; }
        set {
            if (markerTypes != value) {
                markerTypes = value;
                Redraw();
            }
        }
    }
		
    Color markerColor;
		
    public Color MarkerColor {
        get { return markerColor; }
        set {
            if (markerColor != value) {
                markerColor = value;
                Redraw();
            }
        }
    }
		
    public Func<Control> ToolTip { get; set; }
}