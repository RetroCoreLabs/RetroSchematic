//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;

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
                context.DrawImage(_bitmap, new Rect(s), new Rect(_offset.X, _offset.Y, s.Width * _zoom, s.Height * _zoom));
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
            e.Pointer.Capture(this);
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

        // a theme brush, looked up each time so a theme change shows at once
        private IBrush Brush(string key, IBrush fallback)
            => this.TryFindResource(key, ActualThemeVariant, out object? o) && o is IBrush b ? b : fallback;
    }
}
