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
    /// left button picks, dragging with the right or middle button moves the sheet.
    /// It holds no sheet logic: picking, the selection and the descriptions are the session's.
    /// </summary>
    public sealed class SchematicCanvas : Control
    {
        private SheetSession? _session;
        private double _zoom = 1;
        private Vector _offset;
        private Point? _dragFrom;

        private const double MinZoom = 0.1;
        private const double MaxZoom = 20;

        private static readonly Typeface s_mono = new("Consolas, Courier New, monospace");
        private static readonly Typeface s_monoBold = new(new FontFamily("Consolas, Courier New, monospace"), FontStyle.Normal, FontWeight.Bold);

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

        /// <inheritdoc/>
        public override void Render(DrawingContext context)
        {
            IBrush paper = Brush(ThemeManager.SchematicPaperKey);
            context.FillRectangle(paper, new Rect(Bounds.Size));
            if (_session == null)
                return;

            IBrush ink = Brush(ThemeManager.SchematicInkKey);
            IBrush frame = Brush(ThemeManager.SchematicFrameKey);
            IBrush inferred = Brush(ThemeManager.SchematicInferredKey);
            IBrush note = Brush(ThemeManager.SchematicNoteKey);
            IBrush lit = Brush(ThemeManager.SchematicSelectionKey);
            IBrush open = Brush(ThemeManager.SchematicOpenKey);

            using (context.PushTransform(Matrix.CreateScale(_zoom, _zoom) * Matrix.CreateTranslation(_offset.X, _offset.Y)))
            {
                SchematicDrawing d = _session.Drawing;
                for (int i = 0; i < d.Items.Count; i++)
                {
                    DrawItem it = d.Items[i];
                    bool isLit = _session.IsLit(it);
                    IBrush b = isLit ? lit : Pick(it, ink, frame, inferred, open, note);
                    DrawOne(context, it, b, isLit);
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
            if (p.Properties.IsLeftButtonPressed && _session != null)
            {
                _session.PickAt((p.Position.X - _offset.X) / _zoom, (p.Position.Y - _offset.Y) / _zoom);
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
            _dragFrom = null;
            e.Pointer.Capture(null);
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
            Pen pen = new(brush, lit ? 2 : 1, it.Dashed ? new DashStyle(new double[] { 4, 3 }, 0) : null);
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
