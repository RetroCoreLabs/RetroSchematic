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
using Avalonia.Threading;
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
        /// Called with the owner name of what was under the pointer (empty for paper).
        /// </summary>
        /// <param name="owner">
        /// The owner name, such as inst:U1.
        /// </param>
        public delegate void OwnerHandler(string owner);

        /// <summary>
        /// An instance box was double-clicked: the window opens it.
        /// </summary>
        public event OwnerHandler? InstanceOpened;

        /// <summary>
        /// The right button was clicked without dragging the view: the window shows its menu.
        /// </summary>
        public event OwnerHandler? MenuRequested;

        private bool _panned;

        // the trace pulse: while a signal is traced, each drawn frame asks for the next one
        // (RequestAnimationFrame), so the pulse runs at the screen's pace and never asks for
        // more frames than get drawn. A fixed 40 ms timer at Render priority did that before:
        // on a big sheet a frame takes longer than 40 ms, the queue never emptied, and input
        // waited behind it (docs/BUGS.md B1). The level itself (how bright, at what moment) is
        // SheetSession.PulseLevel in the library.
        private bool _framePending;
        private bool _tracePulse = true;

        // the hover text shows after the pointer rests on one item for about 400 ms
        private readonly DispatcherTimer _hover;
        private string _hoverOwner = "";
        private readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();

        /// <summary>
        /// Creates the canvas; it repaints when the theme changes.
        /// </summary>
        public SchematicCanvas()
        {
            ClipToBounds = true;
            Focusable = true;
            _hover = new DispatcherTimer(TimeSpan.FromMilliseconds(400), DispatcherPriority.Normal, (s, e) =>
            {
                ((DispatcherTimer)s!).Stop();
                ShowHover(_hoverOwner);
            });
            _hover.Stop();
            ActualThemeVariantChanged += OnThemeChanged;
        }

        /// <summary>
        /// The hover text shown now, or null. The text itself is built in the library
        /// (<see cref="SheetSession.TooltipAt"/>).
        /// </summary>
        public string? HoverText { get; private set; }

        /// <summary>
        /// Shows the hover text for an item at once (the pointer calls this after resting about
        /// 400 ms on it). Public so the window tests can use it without waiting.
        /// </summary>
        /// <param name="owner">
        /// The owner of the item under the pointer.
        /// </param>
        public void ShowHover(string owner)
        {
            HoverText = _session?.TooltipAt(owner);
            ToolTip.SetTip(this, HoverText);
            ToolTip.SetIsOpen(this, HoverText != null);
        }

        private void HoverAt(Point at)
        {
            if (_session == null)
                return;
            string owner = _session.Drawing.HitTest(ToDrawingX(at.X), ToDrawingY(at.Y));
            if (owner == _hoverOwner)
                return;
            _hoverOwner = owner;
            _hover.Stop();
            if (HoverText != null)
                ShowHover("");
            if (owner.Length > 0)
                _hover.Start();
        }

        /// <summary>
        /// The zoom factor (1 = one drawing unit per screen pixel).
        /// </summary>
        public double Zoom => _zoom;

        /// <summary>
        /// True to make a traced signal pulse; false shows it still (a setting).
        /// </summary>
        public bool TracePulse
        {
            get => _tracePulse;
            set
            {
                _tracePulse = value;
                InvalidateVisual();
            }
        }

        /// <inheritdoc/>
        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
        }

        /// <inheritdoc/>
        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _framePending = false;
        }

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
                    _session.TraceChanged -= OnDrawingChanged;
                }
                _session = value;
                _tool = value == null ? null : new EditTool(value);
                if (_session != null)
                {
                    _session.DrawingChanged += OnDrawingChanged;
                    _session.SelectionChanged += OnSelectionChanged;
                    _session.TraceChanged += OnDrawingChanged;
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
        /// Brings an area into view: the whole of it with a margin when that is still readable,
        /// else the view is centred on <paramref name="first"/> at a readable zoom.
        /// </summary>
        /// <param name="area">
        /// X, Y, width, height in drawing units.
        /// </param>
        /// <param name="first">
        /// The place to centre on when the whole area does not fit readably.
        /// </param>
        public void ShowArea(double[] area, DrawPoint first)
        {
            if (Bounds.Width <= 0 || Bounds.Height <= 0)
                return;
            const double Margin = 40;
            // smallest zoom at which text can still be read
            const double Readable = 0.6;
            double fit = Math.Min(Bounds.Width / (area[2] + (2 * Margin)), Bounds.Height / (area[3] + (2 * Margin)));
            if (fit >= Readable)
            {
                // never zoom in further than needed: keep the zoom when the area already fits
                bool fits = area[2] * _zoom <= Bounds.Width - (2 * Margin) && area[3] * _zoom <= Bounds.Height - (2 * Margin);
                if (!fits || _zoom < Readable)
                    _zoom = Math.Clamp(Math.Min(fit, 2), MinZoom, MaxZoom);
                CentreOn(area[0] + (area[2] / 2), area[1] + (area[3] / 2));
                return;
            }
            _zoom = Math.Clamp(Math.Max(_zoom, Readable), MinZoom, MaxZoom);
            CentreOn(first.X, first.Y);
        }

        /// <summary>
        /// True when a drawing point is inside the visible part of the control.
        /// </summary>
        /// <param name="x">
        /// X in drawing units.
        /// </param>
        /// <param name="y">
        /// Y in drawing units.
        /// </param>
        /// <returns>
        /// True when on screen.
        /// </returns>
        public bool IsOnScreen(double x, double y)
        {
            double sx = (x * _zoom) + _offset.X, sy = (y * _zoom) + _offset.Y;
            return sx >= 0 && sy >= 0 && sx <= Bounds.Width && sy <= Bounds.Height;
        }

        /// <summary>
        /// Moves the view so a drawing point is in the middle of the control, keeping the zoom.
        /// </summary>
        /// <param name="x">
        /// Point X in drawing units.
        /// </param>
        /// <param name="y">
        /// Point Y in drawing units.
        /// </param>
        public void CentreOn(double x, double y)
        {
            _offset = new Vector((Bounds.Width / 2) - (x * _zoom), (Bounds.Height / 2) - (y * _zoom));
            InvalidateVisual();
        }

        /// <summary>
        /// The drawing point in the middle of the control now (for Back / Forward).
        /// </summary>
        public Point ViewCentre => new(((Bounds.Width / 2) - _offset.X) / _zoom, ((Bounds.Height / 2) - _offset.Y) / _zoom);

        /// <summary>
        /// Puts the view back where it was: a zoom and the drawing point in the middle.
        /// </summary>
        /// <param name="x">
        /// Point X in drawing units.
        /// </param>
        /// <param name="y">
        /// Point Y in drawing units.
        /// </param>
        /// <param name="zoom">
        /// The zoom factor.
        /// </param>
        public void SetView(double x, double y, double zoom)
        {
            _zoom = Math.Clamp(zoom, MinZoom, MaxZoom);
            CentreOn(x, y);
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
            NextPulseFrame();
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

            // one brush per schematic colour, looked up now so a theme change is followed; what
            // colour, fill and line style a step gets is SchematicStyle's answer, the same one the
            // SVG export uses
            SchematicColour[] names = Enum.GetValues<SchematicColour>();
            IBrush[] colours = new IBrush[names.Length];
            for (int c = 0; c < names.Length; c++)
                colours[c] = Brush(SchematicTheme.BrushKey(names[c]));
            IBrush lit = Brush(ThemeManager.SchematicSelectionKey);
            IBrush open = Brush(ThemeManager.SchematicOpenKey);
            IBrush trace = Brush(ThemeManager.SchematicTraceKey);

            SchematicDrawing d = session.Drawing;
            // on screen the trace pulses once a second; an exported picture shows it still
            double level = withGesture && TracePulse ? SheetSession.PulseLevel(_clock.Elapsed.TotalSeconds) : 0.6;
            for (int i = 0; i < d.Items.Count; i++)
            {
                DrawItem it = d.Items[i];
                bool isLit = session.IsLit(it);
                ItemStyle st = SchematicStyle.Of(it);
                IBrush b = isLit ? lit : colours[(int)st.Stroke];
                IBrush? fill = st.Fill == null ? null : colours[(int)st.Fill.Value];
                // a simulated value colours the signal's wires, labels and names (none today)
                string? valueKey = isLit ? null : ThemeManager.ValueKey(session.ValueOf(it));
                if (valueKey != null)
                    b = Brush(valueKey);
                // the trace tool: the signal thick in the trace colour, the chips it joins outlined
                // in it, everything else faint
                TraceRole role = session.TraceOf(it);
                if (role == TraceRole.Signal)
                {
                    // a soft glow that swells and fades, then the line itself on top
                    using (context.PushOpacity(0.15 + (0.35 * level)))
                        DrawOne(context, it, trace, null, 3 + (4 * level));
                    DrawOne(context, it, trace, fill, 1.5 + level);
                    continue;
                }
                if (role == TraceRole.Joined)
                {
                    DrawOne(context, it, trace, fill, 1);
                    continue;
                }
                if (role == TraceRole.Dimmed)
                {
                    using (context.PushOpacity(0.25))
                        DrawOne(context, it, b, fill, 0);
                    continue;
                }
                DrawOne(context, it, b, fill, isLit ? 1 : 0);
            }

            // where the traced signal goes inside a block: a pulsing ring on the box edge
            System.Collections.Generic.List<TraceMark> marks = session.TracedPortMarks();
            for (int i = 0; i < marks.Count; i++)
            {
                Point c = new(marks[i].At.X, marks[i].At.Y);
                using (context.PushOpacity(0.3 + (0.4 * level)))
                    context.DrawEllipse(trace, null, c, 4 + (3 * level), 4 + (3 * level));
                context.DrawEllipse(null, new Pen(trace, 1.5), c, 8, 8);
            }

            if (!withGesture || _tool == null)
                return;
            Pen preview = new(lit, 1.5, new DashStyle(new double[] { 3, 3 }, 0));
            if (_tool.Gesture == EditGesture.BusPath)
            {
                // the bus path clicked so far, and a line on to the pointer
                System.Collections.Generic.IReadOnlyList<DrawPoint> pts = _tool.BusPoints;
                Pen bus = new(lit, LineWidths.Bus, new DashStyle(new double[] { 3, 3 }, 0));
                for (int i = 1; i < pts.Count; i++)
                    context.DrawLine(bus, new Point(pts[i - 1].X, pts[i - 1].Y), new Point(pts[i].X, pts[i].Y));
                if (pts.Count > 0)
                    context.DrawLine(preview, new Point(pts[^1].X, pts[^1].Y), new Point(_tool.Current.X, _tool.Current.Y));
            }
            else if (_tool.Gesture == EditGesture.Wire)
                context.DrawLine(preview, new Point(_tool.Start.X, _tool.Start.Y), new Point(_tool.Current.X, _tool.Current.Y));
            else if (_tool.Gesture == EditGesture.Band)
            {
                // the box being dragged to pick chips and instances
                double l = Math.Min(_tool.Start.X, _tool.Current.X), t = Math.Min(_tool.Start.Y, _tool.Current.Y);
                context.DrawRectangle(null, preview, new Rect(l, t, Math.Abs(_tool.Current.X - _tool.Start.X), Math.Abs(_tool.Current.Y - _tool.Start.Y)));
            }
            else if (_tool.Gesture == EditGesture.MoveInstance && session.Selected.StartsWith("inst:", StringComparison.Ordinal)
                && d.InstanceFrames.TryGetValue(session.Selected.Substring(5), out PartFrame box))
            {
                double dx = _tool.Current.X - _tool.Start.X, dy = _tool.Current.Y - _tool.Start.Y;
                context.DrawRectangle(null, preview, new Rect(box.X + dx, box.Y + dy, box.Width, box.Height));
            }
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
            if (p.Properties.IsLeftButtonPressed && e.ClickCount == 2 && Session != null)
            {
                // a double-click on a port the traced signal goes through follows it inside
                string clicked = Session.Drawing.HitTest(ToDrawingX(p.Position.X), ToDrawingY(p.Position.Y));
                string traced = Session.Traced;
                if (Session.FollowAt(clicked))
                {
                    _tool?.Cancel();
                    ToolMessage?.Invoke("Followed " + traced + " inside: now tracing " + Session.Traced + ".");
                    InvalidateVisual();
                    e.Handled = true;
                    return;
                }
                // a double-click on an instance box opens it
                string owner = SheetSession.WholeOf(clicked);
                if (owner.StartsWith("inst:", StringComparison.Ordinal))
                {
                    _tool?.Cancel();
                    InstanceOpened?.Invoke(owner);
                    e.Handled = true;
                    return;
                }
            }
            if (p.Properties.IsLeftButtonPressed && _tool != null)
            {
                _tool.PointerDown(ToDrawingX(p.Position.X), ToDrawingY(p.Position.Y), (e.KeyModifiers & KeyModifiers.Control) != 0);
                if (_tool.Gesture != EditGesture.None)
                    e.Pointer.Capture(this);
                Report();
                InvalidateVisual();
                e.Handled = true;
            }
            else if (p.Properties.IsRightButtonPressed || p.Properties.IsMiddleButtonPressed)
            {
                _dragFrom = p.Position;
                _panned = false;
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
            {
                HoverAt(e.GetPosition(this));
                return;
            }
            Point now = e.GetPosition(this);
            // a few pixels of wobble on a right-click is not a drag of the view
            if (Math.Abs(now.X - from.X) + Math.Abs(now.Y - from.Y) > 3 || _panned)
                _panned = true;
            else
                return;
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
            if (_dragFrom != null && !_panned && e.InitialPressMouseButton == MouseButton.Right && Session != null)
            {
                Point at = e.GetPosition(this);
                MenuRequested?.Invoke(Session.Drawing.HitTest(ToDrawingX(at.X), ToDrawingY(at.Y)));
            }
            _dragFrom = null;
            e.Pointer.Capture(null);
        }

        /// <inheritdoc/>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            // Tab / Shift-Tab: centre the view on each place the traced signal goes, in turn
            if (e.Key == Key.Tab && _session != null && _session.Traced.Length > 0)
            {
                if (_session.NextTracePlace((e.KeyModifiers & KeyModifiers.Shift) != 0 ? -1 : 1, out DrawPoint at, out string what))
                {
                    CentreOn(at.X, at.Y);
                    ToolMessage?.Invoke("At " + what + " (" + _session.Traced + ").");
                }
                e.Handled = true;
                return;
            }
            if (_tool == null)
                return;
            if (e.Key == Key.Delete)
            {
                _tool.DeleteSelected();
                Report();
                e.Handled = true;
            }
            else if (e.Key == Key.Enter && _tool.BusPathFor != null)
            {
                _tool.FinishBusPath();
                Report();
                InvalidateVisual();
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
        /// One drawing step.
        /// </summary>
        /// <param name="context">
        /// Where to draw.
        /// </param>
        /// <param name="it">
        /// The step.
        /// </param>
        /// <param name="brush">
        /// Its colour.
        /// </param>
        /// <param name="fill">
        /// The fill of a closed shape, or null for none.
        /// </param>
        /// <param name="extra">
        /// Added to its usual line width: 1 for the picked item, more for a traced signal.
        /// </param>
        private static void DrawOne(DrawingContext context, DrawItem it, IBrush brush, IBrush? fill, double extra)
        {
            // width, dashes and line ends from the shared style, plus the extra for a picked or
            // traced item
            ItemStyle st = SchematicStyle.Of(it);
            double width = st.Width + extra;
            double[] dash = ItemStyle.Dashes(st.Line, st.Width);
            bool round = st.Round || st.Line == LineStyle.Dotted;
            Pen pen = new(brush, width, dash.Length == 0 ? null : new DashStyle(dash, 0),
                round ? PenLineCap.Round : PenLineCap.Flat, round ? PenLineJoin.Round : PenLineJoin.Miter);
            switch (it.Kind)
            {
                case DrawKind.Line:
                    context.DrawLine(pen, new Point(it.X1, it.Y1), new Point(it.X2, it.Y2));
                    break;
                case DrawKind.Rect:
                    context.DrawRectangle(fill, pen, new Rect(it.X1, it.Y1, it.X2, it.Y2));
                    break;
                case DrawKind.Circle:
                    context.DrawEllipse(fill, pen, new Point(it.X1, it.Y1), it.X2, it.X2);
                    break;
                case DrawKind.Path:
                    context.DrawGeometry(fill, pen, BuildPath(it.Path!, fill != null));
                    break;
                case DrawKind.Text:
                    DrawText(context, it, brush, st.Bold);
                    break;
            }
        }

        private static StreamGeometry BuildPath(PathSegment[] p, bool filled)
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
                        c.BeginFigure(new Point(s.X, s.Y), filled);
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
        /// after it, as the SVG output writes it (baseline-shift sub, at least size 8).
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
        /// <param name="bold">
        /// True when the style asks for bold text.
        /// </param>
        private static void DrawText(DrawingContext context, DrawItem it, IBrush brush, bool bold)
        {
            FormattedText main = new(it.Text, CultureInfo.InvariantCulture, FlowDirection.LeftToRight,
                it.Bold || bold ? s_monoBold : s_mono, it.FontSize, brush);
            // the 0/1 level subscript: big enough to read, the same size as in the SVG
            FormattedText? sub = it.Subscript.Length == 0 ? null
                : new FormattedText(it.Subscript, CultureInfo.InvariantCulture, FlowDirection.LeftToRight, it.Bold || bold ? s_monoBold : s_mono,
                    Math.Max(8, it.FontSize * 0.8), brush);
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

        // after a frame is drawn: while a signal pulses, ask for one more frame
        private void NextPulseFrame()
        {
            if (_framePending || !_tracePulse || _session == null || _session.Traced.Length == 0)
                return;
            TopLevel? top = TopLevel.GetTopLevel(this);
            if (top == null)
                return;
            _framePending = true;
            top.RequestAnimationFrame(_ =>
            {
                _framePending = false;
                InvalidateVisual();
            });
        }

        private void OnSelectionChanged(SheetSession session, string owner) => InvalidateVisual();
    }
}
