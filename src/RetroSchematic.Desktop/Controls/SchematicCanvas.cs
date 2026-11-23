//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
// RetroSchematic - viewer for Norsk Data circuit diagrams
//

using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using RetroSchematic.Desktop.Themes;
using RetroSchematic.Model;
using PathSegment = RetroSchematic.Render.PathSegment;
using Point = Avalonia.Point;
using RetroSchematic.Render;

namespace RetroSchematic.Desktop.Controls
{
    /// <summary>
    /// Draws a <see cref="SheetSession"/>'s drawing - the same drawing steps the SVG output
    /// writes - with the colours of the current theme. Mouse wheel zooms round the pointer, the
    /// left button goes to the <see cref="EditTool"/> (which picks in read-only mode, and places,
    /// wires and moves in edit mode), dragging with the right or middle button moves the view.
    /// It holds no sheet logic: picking, editing, the selection and the descriptions are the
    /// library's; this control only turns screen points into drawing units and paints.
    /// </summary>
    public sealed class SchematicCanvas : Control
    {
        private SheetSession? _session;
        private EditTool? _tool;
        private double _zoom = 1;
        private Vector _offset;
        private Point? _dragFrom;

        private const double MinZoom = 0.1;
        private const double MaxZoom = 20;

        private static readonly Typeface s_mono = new("Consolas, Courier New, monospace");
        private static readonly Typeface s_monoBold = new(new FontFamily("Consolas, Courier New, monospace"), FontStyle.Normal, FontWeight.Bold);

        /// <summary>
        /// Called after the edit tool did something or refused to; the window shows the message.
        /// </summary>
        /// <param name="message">
        /// What happened, in plain words.
        /// </param>
        public delegate void ToolMessageHandler(string message);

        /// <summary>
        /// The edit tool has a message for the status bar.
        /// </summary>
        public event ToolMessageHandler? ToolMessage;

        /// <summary>
        /// Creates the canvas; it repaints when the theme changes.
        /// </summary>
        public SchematicCanvas()
        {
            ClipToBounds = true;
            Focusable = true;
            ActualThemeVariantChanged += OnThemeChanged;
        }

        /// <summary>
        /// The zoom factor (1 = one drawing unit per screen pixel).
        /// </summary>
        public double Zoom => _zoom;

        /// <summary>
        /// The mouse and key handling for the session shown, or null when none is shown.
        /// </summary>
        public EditTool? Tool => _tool;

        /// <summary>
        /// The session shown, or null for an empty canvas.
        /// </summary>
        public SheetSession? Session
        {
            get => _session;
            set
            {
                if (_session != null)
                {
                    _session.DrawingChanged -= OnDrawingChanged;
                    _session.SelectionChanged -= OnSelectionChanged;
                }
                _session = value;
                _tool = value == null ? null : new EditTool(value);
                if (_session != null)
                {
                    _session.DrawingChanged += OnDrawingChanged;
                    _session.SelectionChanged += OnSelectionChanged;
                }
                FitToWindow();
            }
        }

        /// <summary>
        /// Scales and centres the whole sheet in the control.
        /// </summary>
        public void FitToWindow()
        {
            if (_session == null || Bounds.Width <= 0 || Bounds.Height <= 0)
            {
                InvalidateVisual();
                return;
            }
            SchematicDrawing d = _session.Drawing;
            _zoom = Math.Clamp(Math.Min(Bounds.Width / d.Width, Bounds.Height / d.Height), MinZoom, MaxZoom);
            _offset = new Vector((Bounds.Width - (d.Width * _zoom)) / 2, (Bounds.Height - (d.Height * _zoom)) / 2);
            InvalidateVisual();
        }

        /// <summary>
        /// Zooms by a factor, keeping the drawing point under <paramref name="screen"/> still.
        /// </summary>
        /// <param name="screen">
        /// The point in the control that stays still, usually the mouse pointer.
        /// </param>
        /// <param name="factor">
        /// How much to zoom: above 1 zooms in, below 1 zooms out.
        /// </param>
        public void ZoomAt(Point screen, double factor)
        {
            double z = Math.Clamp(_zoom * factor, MinZoom, MaxZoom);
            // drawing point under the pointer before, placed under the pointer after
            double dx = (screen.X - _offset.X) / _zoom;
            double dy = (screen.Y - _offset.Y) / _zoom;
            _zoom = z;
            _offset = new Vector(screen.X - (dx * z), screen.Y - (dy * z));
            InvalidateVisual();
        }

        /// <summary>
        /// Writes the whole sheet, in the current theme's colours, as a PNG file.
        /// </summary>
        /// <param name="path">
        /// The file to write.
        /// </param>
        /// <param name="scale">
        /// Pixels per drawing unit; 2 gives a sharp print of an A3 sheet.
        /// </param>
        public void ExportPng(string path, double scale)
        {
            if (_session == null)
                return;
            SchematicDrawing d = _session.Drawing;
            PixelSize size = new((int)Math.Ceiling(d.Width * scale), (int)Math.Ceiling(d.Height * scale));
            using RenderTargetBitmap bitmap = new(size, new Vector(96, 96));
            using (DrawingContext context = bitmap.CreateDrawingContext())
            {
                context.FillRectangle(Brush(ThemeManager.SchematicPaperKey), new Rect(0, 0, size.Width, size.Height));
                using (context.PushTransform(Matrix.CreateScale(scale, scale)))
                    DrawSheet(context, false);
            }
            bitmap.Save(path);
        }

        /// <inheritdoc/>
        public override void Render(DrawingContext context)
        {
            IBrush paper = Brush(ThemeManager.SchematicPaperKey);
            context.FillRectangle(paper, new Rect(Bounds.Size));
            if (_session == null)
                return;
            using (context.PushTransform(Matrix.CreateScale(_zoom, _zoom) * Matrix.CreateTranslation(_offset.X, _offset.Y)))
                DrawSheet(context, true);
        }

        /// <summary>
        /// The drawing steps, then (on screen only) the line of a wire being drawn or the outline
        /// of a chip being moved.
        /// </summary>
        /// <param name="context">
        /// Where to draw, already scaled to drawing units.
        /// </param>
        /// <param name="withGesture">
        /// True to draw the edit tool's drag preview.
        /// </param>
        private void DrawSheet(DrawingContext context, bool withGesture)
        {
            SheetSession session = _session!;

            IBrush ink = Brush(ThemeManager.SchematicInkKey);
            IBrush frame = Brush(ThemeManager.SchematicFrameKey);
            IBrush inferred = Brush(ThemeManager.SchematicInferredKey);
            IBrush note = Brush(ThemeManager.SchematicNoteKey);
            IBrush lit = Brush(ThemeManager.SchematicSelectionKey);
            IBrush open = Brush(ThemeManager.SchematicOpenKey);

            SchematicDrawing d = session.Drawing;
            for (int i = 0; i < d.Items.Count; i++)
            {
                DrawItem it = d.Items[i];
                bool isLit = session.IsLit(it);
                IBrush b = isLit ? lit : Pick(it, ink, frame, inferred, open, note);
                DrawOne(context, it, b, isLit);
            }

            if (!withGesture || _tool == null)
                return;
            Pen preview = new(lit, 1.5, new DashStyle(new double[] { 3, 3 }, 0));
            if (_tool.Gesture == EditGesture.Wire)
                context.DrawLine(preview, new Point(_tool.Start.X, _tool.Start.Y), new Point(_tool.Current.X, _tool.Current.Y));
            else if (_tool.Gesture == EditGesture.Move && session.Selected.Length > 0)
            {
                string reference = session.Selected.Substring(session.Selected.IndexOf(':') + 1);
                int slash = reference.IndexOf('/');
                if (slash >= 0)
                    reference = reference.Substring(0, slash);
                if (d.Frames.TryGetValue(reference, out PartFrame f))
                {
                    double dx = _tool.Current.X - _tool.Start.X, dy = _tool.Current.Y - _tool.Start.Y;
                    context.DrawRectangle(null, preview, new Rect(f.X + dx, f.Y + dy, f.Width, f.Height));
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);
            if (e.PreviousSize.Width <= 0 || e.PreviousSize.Height <= 0)
                FitToWindow();
        }

        /// <inheritdoc/>
        protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
        {
            base.OnPointerWheelChanged(e);
            ZoomAt(e.GetPosition(this), e.Delta.Y > 0 ? 1.2 : 1 / 1.2);
            e.Handled = true;
        }

        /// <inheritdoc/>
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            base.OnPointerPressed(e);
            Focus();
            PointerPoint p = e.GetCurrentPoint(this);
            if (p.Properties.IsLeftButtonPressed && _tool != null)
            {
                _tool.PointerDown(ToDrawingX(p.Position.X), ToDrawingY(p.Position.Y));
                if (_tool.Gesture != EditGesture.None)
                    e.Pointer.Capture(this);
                Report();
                InvalidateVisual();
                e.Handled = true;
            }
            else if (p.Properties.IsRightButtonPressed || p.Properties.IsMiddleButtonPressed)
            {
                _dragFrom = p.Position;
                e.Pointer.Capture(this);
                e.Handled = true;
            }
        }

        /// <inheritdoc/>
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            if (_tool != null && _tool.Gesture != EditGesture.None)
            {
                Point at = e.GetPosition(this);
                _tool.PointerMove(ToDrawingX(at.X), ToDrawingY(at.Y));
                InvalidateVisual();
                return;
            }
            if (_dragFrom is not Point from)
                return;
            Point now = e.GetPosition(this);
            _offset += now - from;
            _dragFrom = now;
            InvalidateVisual();
        }

        /// <inheritdoc/>
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            if (_tool != null && _tool.Gesture != EditGesture.None)
            {
                Point at = e.GetPosition(this);
                _tool.PointerUp(ToDrawingX(at.X), ToDrawingY(at.Y));
                Report();
                InvalidateVisual();
            }
            _dragFrom = null;
            e.Pointer.Capture(null);
        }

        /// <inheritdoc/>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (_tool == null)
                return;
            if (e.Key == Key.Delete)
            {
                _tool.DeleteSelected();
                Report();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                _tool.Cancel();
                InvalidateVisual();
                e.Handled = true;
            }
        }

        private double ToDrawingX(double screenX) => (screenX - _offset.X) / _zoom;

        private double ToDrawingY(double screenY) => (screenY - _offset.Y) / _zoom;

        private void Report()
        {
            if (_tool != null && _tool.Message.Length > 0)
                ToolMessage?.Invoke(_tool.Message);
        }

        /// <summary>
        /// The colour of an item: what it is and whether its reading is proved.
        /// </summary>
        /// <param name="it">
        /// The drawing step to colour.
        /// </param>
        /// <param name="ink">
        /// Colour for what was read off the drawing.
        /// </param>
        /// <param name="frame">
        /// Colour for the frame round a chip.
        /// </param>
        /// <param name="inferred">
        /// Colour for what was inferred, not read.
        /// </param>
        /// <param name="open">
        /// Colour for what is OPEN, not known.
        /// </param>
        /// <param name="note">
        /// Colour for secondary text.
        /// </param>
        /// <returns>
        /// The brush to draw the step with.
        /// </returns>
        private static IBrush Pick(DrawItem it, IBrush ink, IBrush frame, IBrush inferred, IBrush open, IBrush note)
        {
            if (it.Role == DrawRole.PartOutline || it.Role == DrawRole.PinDot)
                return frame;
            if (it.Role == DrawRole.Note)
                return note;
            if (it.Confidence == Confidence.Open)
                return open;
            if (it.Confidence != Confidence.Read)
                return inferred;
            return ink;
        }

        private static void DrawOne(DrawingContext context, DrawItem it, IBrush brush, bool lit)
        {
            // the same widths as the SVG; a lit item one unit thicker
            Pen pen = new(brush, LineWidths.Of(it) + (lit ? 1 : 0), it.Dashed ? new DashStyle(new double[] { 4, 3 }, 0) : null);
            switch (it.Kind)
            {
                case DrawKind.Line:
                    context.DrawLine(pen, new Point(it.X1, it.Y1), new Point(it.X2, it.Y2));
                    break;
                case DrawKind.Rect:
                    context.DrawRectangle(null, pen, new Rect(it.X1, it.Y1, it.X2, it.Y2));
                    break;
                case DrawKind.Circle:
                    context.DrawEllipse(null, pen, new Point(it.X1, it.Y1), it.X2, it.X2);
                    break;
                case DrawKind.Path:
                    context.DrawGeometry(null, pen, BuildPath(it.Path!));
                    break;
                case DrawKind.Text:
                    DrawText(context, it, brush);
                    break;
            }
        }

        private static StreamGeometry BuildPath(PathSegment[] p)
        {
            StreamGeometry g = new();
            using StreamGeometryContext c = g.Open();
            bool open = false;
            for (int i = 0; i < p.Length; i++)
            {
                PathSegment s = p[i];
                switch (s.Op)
                {
                    case PathOp.MoveTo:
                        if (open)
                            c.EndFigure(false);
                        c.BeginFigure(new Point(s.X, s.Y), false);
                        open = true;
                        break;
                    case PathOp.LineTo:
                        c.LineTo(new Point(s.X, s.Y));
                        break;
                    case PathOp.QuadTo:
                        c.QuadraticBezierTo(new Point(s.CX, s.CY), new Point(s.X, s.Y));
                        break;
                    case PathOp.ArcClockwiseTo:
                        c.ArcTo(new Point(s.X, s.Y), new Size(s.CX, s.CX), 0, false, SweepDirection.Clockwise);
                        break;
                    case PathOp.Close:
                        c.EndFigure(true);
                        open = false;
                        break;
                }
            }
            if (open)
                c.EndFigure(false);
            return g;
        }

        /// <summary>
        /// Text with its baseline at (X1,Y1) like SVG, and ND's subscript lowered and smaller
        /// after it, as the SVG output writes it (baseline-shift sub, size 7).
        /// </summary>
        /// <param name="context">
        /// Where to draw.
        /// </param>
        /// <param name="it">
        /// The text step: text, subscript, size, anchor and baseline point.
        /// </param>
        /// <param name="brush">
        /// The colour.
        /// </param>
        private static void DrawText(DrawingContext context, DrawItem it, IBrush brush)
        {
            FormattedText main = new(it.Text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                it.Bold ? s_monoBold : s_mono, it.FontSize, brush);
            FormattedText? sub = it.Subscript.Length == 0 ? null
                : new FormattedText(it.Subscript, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, s_mono, 7, brush);
            double width = main.WidthIncludingTrailingWhitespace + (sub?.WidthIncludingTrailingWhitespace ?? 0);
            double left = it.Anchor == TextAnchor.Start ? it.X1 : it.Anchor == TextAnchor.Middle ? it.X1 - (width / 2) : it.X1 - width;
            context.DrawText(main, new Point(left, it.Y1 - main.Baseline));
            if (sub != null)
                context.DrawText(sub, new Point(left + main.WidthIncludingTrailingWhitespace, it.Y1 - sub.Baseline + (0.3 * it.FontSize)));
        }

        /// <summary>
        /// A theme brush looked up NOW, so it follows a theme change. A key no theme supplies is a
        /// defect in ThemeManager; it is drawn magenta so it is seen at once (MainWindowTests.EveryTheme_GivesEveryBrush also
        /// checks every key in every theme).
        /// </summary>
        /// <param name="key">
        /// One of the ThemeManager brush keys.
        /// </param>
        /// <returns>
        /// The current theme's brush, or magenta when the key is missing.
        /// </returns>
        private IBrush Brush(string key)
        {
            if (this.TryFindResource(key, ActualThemeVariant, out object? value) && value is IBrush b)
                return b;
            return Brushes.Magenta;
        }

        private void OnThemeChanged(object? sender, EventArgs e) => InvalidateVisual();

        private void OnDrawingChanged(SheetSession session) => InvalidateVisual();

        private void OnSelectionChanged(SheetSession session, string owner) => InvalidateVisual();
    }
}
