//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using RetroSchematic.Model;
using Point = Avalonia.Point;
using RetroSchematic.Render;

namespace RetroSchematic.Desktop.Controls
{
    /// <summary>
    /// Shows one picture - an original schematic page or a photo of the board - and zooms and
    /// pans it the way the drawing does: the wheel zooms round the mouse, a drag moves it, and
    /// <see cref="Fit"/> fits it to the window. Each view keeps its own zoom and place, so going
    /// back to a view finds it as it was left. The picture is drawn on the theme's paper colour;
    /// its own colours are never changed (a photo is a photo).
    /// </summary>
    public sealed class ImageView : Control
    {
        private const double MinZoom = 0.02;
        private const double MaxZoom = 16;

        private Bitmap? _bitmap;
        private double _zoom = 1;
        private Vector _offset;
        private bool _fitted;
        private Point? _dragFrom;
        private Point _pressAt;
        private bool _editing;
        private bool _moved;

        // picture pixels per picture unit (a JPEG that says 72 or 300 dpi has a Size in
        // device-independent units that differs from its pixel count)
        private double _pxPerUnit = 1;

        /// <summary>
        /// The size of a corner handle on screen, pixels.
        /// </summary>
        public const double HandleSize = 12;

        /// <summary>
        /// The chip boxes on the photo (picture pixels), or null.
        /// </summary>
        public IReadOnlyList<PcbBox>? Boxes { get; set; }

        /// <summary>
        /// The page analyser's finds drawn on the picture (the Original view), or null: a sure
        /// find has a solid edge in the success colour, an unsure one a dashed edge in the warning
        /// colour, the picked one the selection colour. Each shows its label (the code read).
        /// </summary>
        public IReadOnlyList<FindMark>? Finds { get; set; }

        /// <summary>
        /// True to draw <see cref="Boxes"/>: see-through boxes with their position names.
        /// </summary>
        public bool ShowBoxes { get; set; }

        /// <summary>
        /// The box under the mouse (drawn stronger, even when the boxes are off), or null.
        /// </summary>
        public string? HotRef { get; set; }

        /// <summary>
        /// The picked part's box (drawn in the pick colour), or null.
        /// </summary>
        public string? PickedRef { get; set; }

        /// <summary>
        /// The board's outline in the picture (four corners, pixels), drawn with handles when
        /// <see cref="ShowOutline"/> is on.
        /// </summary>
        public IReadOnlyList<GridPoint>? Outline { get; set; }

        /// <summary>
        /// True to draw the board outline and its corner handles.
        /// </summary>
        public bool ShowOutline { get; set; }

        /// <summary>
        /// The mouse moved over the picture (picture pixels).
        /// </summary>
        public event Action<GridPoint>? PictureMoved;

        /// <summary>
        /// A click (press and release without a drag) on the picture (picture pixels).
        /// </summary>
        public event Action<GridPoint>? PictureClicked;

        /// <summary>
        /// Asked when a drag starts at a picture point: true takes the drag for editing (a
        /// box or a corner), false lets it move the view. The second value says Shift is held.
        /// </summary>
        public Func<GridPoint, bool, bool>? EditStart { get; set; }

        /// <summary>
        /// The editing drag moved to a picture point.
        /// </summary>
        public Action<GridPoint>? EditMove { get; set; }

        /// <summary>
        /// The editing drag ended.
        /// </summary>
        public Action? EditEnd { get; set; }

        /// <summary>
        /// Screen pixels per picture pixel, for distances given on screen.
        /// </summary>
        public double ScreenPerPixel => _zoom / _pxPerUnit;

        /// <summary>
        /// A screen point as a picture point in pixels.
        /// </summary>
        /// <param name="screen">
        /// The point on the control.
        /// </param>
        /// <returns>
        /// The picture point.
        /// </returns>
        public GridPoint ToPicture(Point screen)
            => new((screen.X - _offset.X) / _zoom * _pxPerUnit, (screen.Y - _offset.Y) / _zoom * _pxPerUnit);

        /// <summary>
        /// A picture point in pixels as a screen point.
        /// </summary>
        /// <param name="p">
        /// The picture point.
        /// </param>
        /// <returns>
        /// The point on the control.
        /// </returns>
        public Point ToScreen(GridPoint p) => new((p.X / _pxPerUnit * _zoom) + _offset.X, (p.Y / _pxPerUnit * _zoom) + _offset.Y);

        /// <summary>
        /// Simulates a click at a picture point (tests).
        /// </summary>
        /// <param name="p">
        /// The picture point.
        /// </param>
        public void ClickAt(GridPoint p) => PictureClicked?.Invoke(p);

        /// <summary>
        /// Simulates the mouse moving to a picture point (tests).
        /// </summary>
        /// <param name="p">
        /// The picture point.
        /// </param>
        public void HoverAt(GridPoint p) => PictureMoved?.Invoke(p);

        /// <summary>
        /// The text shown when there is no picture, such as "No photo of the solder side".
        /// </summary>
        public string EmptyText { get; set; } = "";

        /// <summary>
        /// The picture shown, or null.
        /// </summary>
        public Bitmap? Bitmap => _bitmap;

        /// <summary>
        /// The zoom: screen pixels per picture pixel.
        /// </summary>
        public double Zoom => _zoom;

        /// <summary>
        /// Shows a picture from its file bytes, or nothing. A new picture is fitted to the window
        /// the first time it is drawn.
        /// </summary>
        /// <param name="bytes">
        /// A PNG or JPEG file's bytes, or null.
        /// </param>
        /// <param name="emptyText">
        /// What to say when there is no picture.
        /// </param>
        public void SetPicture(byte[]? bytes, string emptyText)
        {
            _bitmap?.Dispose();
            _bitmap = null;
            EmptyText = emptyText;
            if (bytes != null)
            {
                using MemoryStream ms = new(bytes, writable: false);
                _bitmap = new Bitmap(ms);
                _pxPerUnit = _bitmap.Size.Width > 0 ? _bitmap.PixelSize.Width / _bitmap.Size.Width : 1;
            }
            _fitted = false;
            InvalidateVisual();
        }

        /// <summary>
        /// Fits the whole picture into the window, centred.
        /// </summary>
        public void Fit()
        {
            if (FitValues())
                InvalidateVisual();
        }

        // the zoom and place that fit the picture; no redraw is asked for, so the drawing pass
        // itself can use it (asking for a redraw while drawing is refused by Avalonia)
        private bool FitValues()
        {
            if (_bitmap == null || Bounds.Width <= 0 || Bounds.Height <= 0)
                return false;
            Size s = _bitmap.Size;
            _zoom = Math.Clamp(Math.Min(Bounds.Width / s.Width, Bounds.Height / s.Height), MinZoom, MaxZoom);
            _offset = new Vector((Bounds.Width - (s.Width * _zoom)) / 2, (Bounds.Height - (s.Height * _zoom)) / 2);
            _fitted = true;
            return true;
        }

        /// <summary>
        /// Zooms by a factor, keeping the picture point under <paramref name="screen"/> still.
        /// </summary>
        /// <param name="screen">
        /// The point on the control.
        /// </param>
        /// <param name="factor">
        /// Above 1 zooms in.
        /// </param>
        public void ZoomAt(Point screen, double factor)
        {
            double z = Math.Clamp(_zoom * factor, MinZoom, MaxZoom);
            double px = (screen.X - _offset.X) / _zoom;
            double py = (screen.Y - _offset.Y) / _zoom;
            _zoom = z;
            _offset = new Vector(screen.X - (px * z), screen.Y - (py * z));
            InvalidateVisual();
        }

        /// <summary>
        /// Zooms round the middle of the control (the toolbar's zoom buttons).
        /// </summary>
        /// <param name="factor">
        /// Above 1 zooms in.
        /// </param>
        public void ZoomCentre(double factor) => ZoomAt(new Point(Bounds.Width / 2, Bounds.Height / 2), factor);

        /// <inheritdoc/>
        public override void Render(DrawingContext context)
        {
            IBrush paper = Brush("FluentBackgroundSecondaryBrush", Brushes.White);
            context.FillRectangle(paper, new Rect(Bounds.Size));
            if (_bitmap == null)
            {
                FormattedText t = new(EmptyText, System.Globalization.CultureInfo.CurrentCulture, FlowDirection.LeftToRight,
                    Typeface.Default, 14, Brush("FluentTextSecondaryBrush", Brushes.Gray));
                context.DrawText(t, new Point(Math.Max(8, (Bounds.Width - t.Width) / 2), Math.Max(8, (Bounds.Height - t.Height) / 2)));
                return;
            }
            if (!_fitted)
                FitValues();
            Size s = _bitmap.Size;
            using (context.PushClip(new Rect(Bounds.Size)))
            {
                context.DrawImage(_bitmap, new Rect(s), new Rect(_offset.X, _offset.Y, s.Width * _zoom, s.Height * _zoom));
                DrawOverlay(context);
            }
        }

        // the chip boxes (see-through, theme coloured, named) and the board outline; a box
        // from the grid is dashed, a box set by hand is solid
        private void DrawOverlay(DrawingContext context)
        {
            IBrush accent = Brush("FluentAccentBlueBrush", Brushes.SteelBlue);
            IBrush pick = Brush("FluentSelectionHighlightBrush", Brushes.Orange);
            Color ac = accent is ISolidColorBrush sa ? sa.Color : Colors.SteelBlue;
            Color pc = pick is ISolidColorBrush sp ? sp.Color : Colors.Orange;
            IBrush paper = Brush("FluentBackgroundPrimaryBrush", Brushes.White);
            IBrush ink = Brush("FluentTextPrimaryBrush", Brushes.Black);
            for (int i = 0; Boxes != null && i < Boxes.Count; i++)
            {
                PcbBox b = Boxes[i];
                bool hot = b.Ref == HotRef, picked = b.Ref == PickedRef;
                if (!ShowBoxes && !hot && !picked)
                    continue;
                Color c = picked ? pc : ac;
                StreamGeometry g = new();
                using (StreamGeometryContext gc = g.Open())
                {
                    gc.BeginFigure(ToScreen(b.Corners[0]), true);
                    for (int k = 1; k < b.Corners.Length; k++)
                        gc.LineTo(ToScreen(b.Corners[k]));
                    gc.EndFigure(true);
                }
                byte alpha = (byte)(hot || picked ? 0x70 : 0x38);
                Pen edge = new(new SolidColorBrush(c), hot || picked ? 2.5 : 1.5, b.ByHand ? null : new DashStyle(new double[] { 4, 2 }, 0));
                context.DrawGeometry(new SolidColorBrush(Color.FromArgb(alpha, c.R, c.G, c.B)), edge, g);
                // the position name on a small paper tag at the box's top left
                FormattedText t = new(b.Ref, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 12, ink);
                Point at = ToScreen(TopLeft(b.Corners));
                Rect tag = new(at.X + 2, at.Y + 2, t.Width + 6, t.Height + 2);
                context.FillRectangle(paper, tag, 3);
                context.DrawText(t, new Point(tag.X + 3, tag.Y + 1));
            }
            DrawFinds(context, paper, ink, pc);
            if (ShowOutline && Outline != null && Outline.Count == 4)
            {
                Pen line = new(pick, 2, new DashStyle(new double[] { 6, 3 }, 0));
                for (int i = 0; i < 4; i++)
                    context.DrawLine(line, ToScreen(Outline[i]), ToScreen(Outline[(i + 1) % 4]));
                for (int i = 0; i < 4; i++)
                {
                    Point p = ToScreen(Outline[i]);
                    context.DrawRectangle(paper, new Pen(pick, 2), new Rect(p.X - (HandleSize / 2), p.Y - (HandleSize / 2), HandleSize, HandleSize));
                }
            }
        }

        // the analyser's finds: an accepted one is drawn by its symbol box instead (Boxes), so
        // only the open ones come here
        private void DrawFinds(DrawingContext context, IBrush paper, IBrush ink, Color pickColour)
        {
            if (Finds == null)
                return;
            Color sure = Brush("FluentSuccessBorderBrush", Brushes.SeaGreen) is ISolidColorBrush s1 ? s1.Color : Colors.SeaGreen;
            Color unsure = Brush("FluentWarningBorderBrush", Brushes.DarkOrange) is ISolidColorBrush s2 ? s2.Color : Colors.DarkOrange;
            for (int i = 0; i < Finds.Count; i++)
            {
                FindMark f = Finds[i];
                Color c = f.Picked ? pickColour : f.Sure ? sure : unsure;
                Point a = ToScreen(new GridPoint(f.X, f.Y));
                Point b = ToScreen(new GridPoint(f.X + f.Width, f.Y + f.Height));
                Rect r = new(a, b);
                Pen edge = new(new SolidColorBrush(c), f.Picked ? 2.5 : 1.5, f.Sure || f.Picked ? null : new DashStyle(new double[] { 3, 2 }, 0));
                context.DrawRectangle(new SolidColorBrush(Color.FromArgb((byte)(f.Picked ? 0x50 : 0x20), c.R, c.G, c.B)), edge, r);
                if (f.Label.Length == 0 || r.Width < 14)
                    continue;
                FormattedText t = new(f.Label, System.Globalization.CultureInfo.InvariantCulture, FlowDirection.LeftToRight, Typeface.Default, 11, ink);
                Rect tag = new(r.X, r.Y - t.Height - 2, t.Width + 6, t.Height + 2);
                context.FillRectangle(paper, tag, 3);
                context.DrawRectangle(null, new Pen(new SolidColorBrush(c), 1), tag, 3);
                context.DrawText(t, new Point(tag.X + 3, tag.Y + 1));
            }
        }

        // the corner nearest the top left of the screen
        private static GridPoint TopLeft(GridPoint[] c)
        {
            GridPoint best = c[0];
            for (int i = 1; i < c.Length; i++)
            {
                if (c[i].X + c[i].Y < best.X + best.Y)
                    best = c[i];
            }
            return best;
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
            _dragFrom = e.GetPosition(this);
            _pressAt = _dragFrom.Value;
            _moved = false;
            // an edit (a box or a board corner) takes the drag; otherwise it moves the view
            _editing = _bitmap != null && EditStart != null && EditStart(ToPicture(_pressAt), (e.KeyModifiers & KeyModifiers.Shift) != 0);
            e.Pointer.Capture(this);
        }

        /// <inheritdoc/>
        protected override void OnPointerMoved(PointerEventArgs e)
        {
            base.OnPointerMoved(e);
            Point now = e.GetPosition(this);
            if (_dragFrom is not Point from)
            {
                if (_bitmap != null)
                    PictureMoved?.Invoke(ToPicture(now));
                return;
            }
            if (Math.Abs(now.X - _pressAt.X) + Math.Abs(now.Y - _pressAt.Y) > 4)
                _moved = true;
            if (_editing)
            {
                EditMove?.Invoke(ToPicture(now));
                InvalidateVisual();
                return;
            }
            _offset += now - from;
            _dragFrom = now;
            InvalidateVisual();
        }

        /// <inheritdoc/>
        protected override void OnPointerReleased(PointerReleasedEventArgs e)
        {
            base.OnPointerReleased(e);
            bool click = !_moved && _dragFrom != null && _bitmap != null;
            if (_editing)
                EditEnd?.Invoke();
            _editing = false;
            _dragFrom = null;
            e.Pointer.Capture(null);
            if (click)
                PictureClicked?.Invoke(ToPicture(e.GetPosition(this)));
        }

        // a theme brush, looked up each time so a theme change shows at once
        private IBrush Brush(string key, IBrush fallback)
            => this.TryFindResource(key, ActualThemeVariant, out object? o) && o is IBrush b ? b : fallback;
    }
}
