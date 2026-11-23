//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using RetroSchematic.Desktop.Controls;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Desktop
{
    /// <summary>
    /// The chip grid on the PCB photos (docs/FORMAT.md, "pcb"): the board type's columns and
    /// the sheet's rows give each chip a cell, a hand-set box wins over the cell, and the board's
    /// four corners in the photo (saved, else found, else the whole picture) place it all on the
    /// photo; the solder side is mirrored. Hovering names the chip, a click picks it as on the
    /// drawing, and in edit mode the boxes and corners are dragged.
    /// </summary>
    public partial class MainWindow
    {
        // the board type and why there is none
        private BoardTemplate? _pcbTemplate;
        private string _pcbProblem = "";
        private bool _pcbWired;

        // per side: the boxes, the corners used and where they came from
        private List<PcbBox> _frontBoxes = new(), _backBoxes = new();
        private GridPoint[]? _frontCorners, _backCorners;
        private string _frontCornersFrom = "", _backCornersFrom = "";

        // corners found in a photo, by file, so they are looked for once
        private readonly Dictionary<string, GridPoint[]?> _foundCorners = new(StringComparer.Ordinal);

        // a drag in edit mode: "move", "size", "turn" a box, or "corner" of the outline
        private string _dragMode = "";
        private BoardSide _dragSide;
        private GridPoint _dragFrom;
        private Model.ChipBox? _dragOriginal, _dragBox;
        private int _dragCorner = -1;
        private GridPoint[]? _dragCorners;

        /// <summary>
        /// True when the chip boxes are drawn on the PCB photos.
        /// </summary>
        public bool ShowChipBoxes => TbChipBoxes.IsChecked == true;

        /// <summary>
        /// Why the photos have no chip grid ("" when they have one).
        /// </summary>
        public string PcbProblem => _pcbProblem;

        /// <summary>
        /// The chip boxes of the photo shown on a side, in its pixels.
        /// </summary>
        /// <param name="side">
        /// The side.
        /// </param>
        /// <returns>
        /// The boxes.
        /// </returns>
        public IReadOnlyList<PcbBox> ChipBoxes(BoardSide side) => side == BoardSide.Component ? _frontBoxes : _backBoxes;

        /// <summary>
        /// The board's corners used on a side's photo, or null.
        /// </summary>
        /// <param name="side">
        /// The side.
        /// </param>
        /// <returns>
        /// Top left, top right, bottom right, bottom left in pixels.
        /// </returns>
        public GridPoint[]? BoardCorners(BoardSide side) => side == BoardSide.Component ? _frontCorners : _backCorners;

        /// <summary>
        /// Where a side's corners came from: "saved" (set by hand), "found" (the board was found
        /// in the photo) or "whole picture".
        /// </summary>
        /// <param name="side">
        /// The side.
        /// </param>
        /// <returns>
        /// The words.
        /// </returns>
        public string CornersFrom(BoardSide side) => side == BoardSide.Component ? _frontCornersFrom : _backCornersFrom;

        /// <summary>
        /// Turns the chip boxes on or off.
        /// </summary>
        /// <param name="on">
        /// True to draw them.
        /// </param>
        public void SetChipBoxes(bool on)
        {
            TbChipBoxes.IsChecked = on;
            ChipBoxesItem.IsChecked = on;
            FrontView.ShowBoxes = on;
            BackView.ShowBoxes = on;
            FrontView.InvalidateVisual();
            BackView.InvalidateVisual();
        }

        /// <summary>
        /// What is under a photo point: the chip's label, which is also shown as the view's tip
        /// and in the status line; null when no chip is there.
        /// </summary>
        /// <param name="side">
        /// The side.
        /// </param>
        /// <param name="p">
        /// The point in the photo's pixels.
        /// </param>
        /// <returns>
        /// "13D  7442  register decoder", or null.
        /// </returns>
        public string? HoverPcb(BoardSide side, GridPoint p)
        {
            ImageView view = side == BoardSide.Component ? FrontView : BackView;
            PcbBox? b = PcbOverlay.At(side == BoardSide.Component ? _frontBoxes : _backBoxes, p);
            if (view.HotRef != b?.Ref)
            {
                view.HotRef = b?.Ref;
                ToolTip.SetTip(view, b?.Label);
                if (b != null)
                    StatusText.Text = b.Label;
                view.InvalidateVisual();
            }
            return b?.Label;
        }

        /// <summary>
        /// A click on a photo: picks the chip there as a click on the drawing does (Details,
        /// trace, Back and Forward), or a switch block as the parts list does.
        /// </summary>
        /// <param name="side">
        /// The side.
        /// </param>
        /// <param name="p">
        /// The point in the photo's pixels.
        /// </param>
        /// <returns>
        /// The reference picked, or null.
        /// </returns>
        public string? ClickPcb(BoardSide side, GridPoint p)
        {
            PcbBox? b = PcbOverlay.At(side == BoardSide.Component ? _frontBoxes : _backBoxes, p);
            if (b == null || _session == null)
                return null;
            if (b.Owner.StartsWith(SwitchOwner, StringComparison.Ordinal))
            {
                PickSwitch(b.Ref);
            }
            else
            {
                _pickedSwitch = "";
                _session.PickAndTrace(b.Owner);
            }
            FrontView.PickedRef = b.Ref;
            BackView.PickedRef = b.Ref;
            FrontView.InvalidateVisual();
            BackView.InvalidateVisual();
            return b.Ref;
        }

        private void OnToggleChipBoxes(object? sender, RoutedEventArgs e) => SetChipBoxes(TbChipBoxes.IsChecked == true);

        // the View menu items do the same as the toolbar buttons (the toolbar can be cut off
        // in a narrow window)
        private void OnChipBoxesItem(object? sender, RoutedEventArgs e)
        {
            if (_session != null)
                SetChipBoxes(TbChipBoxes.IsChecked != true);
        }

        private void OnBoardCornersItem(object? sender, RoutedEventArgs e)
        {
            if (_session == null)
                return;
            TbBoardCorners.IsChecked = TbBoardCorners.IsChecked != true;
            OnToggleBoardCorners(sender, e);
        }

        private void OnToggleBoardCorners(object? sender, RoutedEventArgs e)
        {
            BoardCornersItem.IsChecked = TbBoardCorners.IsChecked == true;
            FrontView.ShowOutline = TbBoardCorners.IsChecked == true;
            BackView.ShowOutline = TbBoardCorners.IsChecked == true;
            FrontView.InvalidateVisual();
            BackView.InvalidateVisual();
            if (TbBoardCorners.IsChecked == true)
                StatusText.Text = "Board outline: " + _frontCornersFrom + " on the front, " + _backCornersFrom + " on the back." + (_session?.Editor.ReadOnly == false ? " Drag a corner to move it." : "");
        }

        // builds the grid boxes again for both photos shown
        private void RefreshPcb()
        {
            WirePcbViews();
            bool open = _session != null;
            TbChipBoxes.IsEnabled = open;
            TbBoardCorners.IsEnabled = open;
            _frontBoxes = new List<PcbBox>();
            _backBoxes = new List<PcbBox>();
            _frontCorners = _backCorners = null;
            if (_session == null)
                return;
            Netlist n = _session.Editor.Root;
            _pcbTemplate = null;
            _pcbProblem = "";
            string? id = n.Pcb?.Template ?? (n.Board?.NdSystem == "NORD-10" ? "nord10-card" : n.Board?.NdSystem == "ND-100" ? "nd100-card" : null);
            try
            {
                string? catalog = ChipCatalog.Find(_session.SheetPath ?? AppContext.BaseDirectory) ?? ChipCatalog.Find(AppContext.BaseDirectory);
                _pcbTemplate = BoardTemplate.Find(catalog, id);
            }
            catch (Exception ex) when (ex is NetlistException || ex is System.IO.IOException)
            {
                _pcbProblem = ex.Message;
            }
            if (_pcbTemplate == null && _pcbProblem.Length == 0)
                _pcbProblem = id == null ? "The sheet names no board type (pcb.template, or the board's ND machine)." : "There is no board type file catalog/boards/" + id + ".json.";
            else if (_pcbTemplate != null && _pcbTemplate.Columns == null)
                _pcbProblem = "The board type " + _pcbTemplate.Id + " has no columns yet (open).";
            Side(BoardSide.Component);
            Side(BoardSide.Solder);
            string picked = _session.Selected.StartsWith("part:", StringComparison.Ordinal) ? _session.Selected.Substring(5) : _pickedSwitch.Length > 0 ? _pickedSwitch : "";
            FrontView.PickedRef = picked;
            BackView.PickedRef = picked;
            FrontView.InvalidateVisual();
            BackView.InvalidateVisual();
        }

        // one side: corners (saved, found, whole picture) and boxes
        private void Side(BoardSide side)
        {
            ImageView view = side == BoardSide.Component ? FrontView : BackView;
            Netlist n = _session!.Editor.Root;
            List<int> mine = PhotosOf(n, side);
            BoardPhoto? photo = mine.Count == 0 ? null : n.Photos![mine[Math.Clamp(PhotoShown(side), 0, mine.Count - 1)]];
            GridPoint[]? corners = null;
            string from = "";
            if (photo != null && view.Bitmap != null)
            {
                if (photo.Corners != null && photo.Corners.Count == 4)
                {
                    corners = new GridPoint[4];
                    for (int i = 0; i < 4; i++)
                        corners[i] = new GridPoint(photo.Corners[i].X, photo.Corners[i].Y);
                    from = "saved";
                }
                else if (_pcbTemplate != null && Found(photo.File, view.Bitmap, _pcbTemplate.Size) is GridPoint[] found)
                {
                    corners = found;
                    from = "found";
                }
                else
                {
                    PixelSize s = view.Bitmap.PixelSize;
                    corners = new[] { new GridPoint(0, 0), new GridPoint(s.Width, 0), new GridPoint(s.Width, s.Height), new GridPoint(0, s.Height) };
                    from = "whole picture";
                }
            }
            List<PcbBox> boxes = corners != null && _pcbTemplate?.Columns != null
                ? PcbOverlay.Build(n, _pcbTemplate, corners, side == BoardSide.Solder)
                : new List<PcbBox>();
            view.Boxes = boxes;
            view.Outline = corners;
            if (side == BoardSide.Component)
            {
                _frontBoxes = boxes;
                _frontCorners = corners;
                _frontCornersFrom = from;
            }
            else
            {
                _backBoxes = boxes;
                _backCorners = corners;
                _backCornersFrom = from;
            }
        }

        // the board found in a photo (on a small copy of it), or null; kept per file
        private GridPoint[]? Found(string file, Bitmap bitmap, BoardSize size)
        {
            if (_foundCorners.TryGetValue(file, out GridPoint[]? known))
                return known;
            GridPoint[]? result = null;
            PixelSize full = bitmap.PixelSize;
            int w = Math.Min(600, full.Width);
            int h = Math.Max(1, (int)Math.Round((double)full.Height * w / full.Width));
            using (Bitmap small = bitmap.CreateScaledBitmap(new PixelSize(w, h), BitmapInterpolationMode.LowQuality))
            {
                int stride = w * 4;
                byte[] pixels = new byte[stride * h];
                GCHandle pin = GCHandle.Alloc(pixels, GCHandleType.Pinned);
                try
                {
                    small.CopyPixels(new PixelRect(0, 0, w, h), pin.AddrOfPinnedObject(), pixels.Length, stride);
                }
                finally
                {
                    pin.Free();
                }
                GridPoint[]? c = PcbGrid.FindOutline(pixels, w, h, stride, size);
                if (c != null)
                {
                    double k = (double)full.Width / w;
                    result = new GridPoint[4];
                    for (int i = 0; i < 4; i++)
                        result[i] = new GridPoint(c[i].X * k, c[i].Y * k);
                }
            }
            _foundCorners[file] = result;
            return result;
        }

        private void WirePcbViews()
        {
            if (_pcbWired)
                return;
            _pcbWired = true;
            FrontView.PictureMoved += p => HoverPcb(BoardSide.Component, p);
            BackView.PictureMoved += p => HoverPcb(BoardSide.Solder, p);
            FrontView.PictureClicked += p => ClickPcb(BoardSide.Component, p);
            BackView.PictureClicked += p => ClickPcb(BoardSide.Solder, p);
            FrontView.EditStart = (p, shift) => StartPcbDrag(BoardSide.Component, p, shift);
            BackView.EditStart = (p, shift) => StartPcbDrag(BoardSide.Solder, p, shift);
            FrontView.EditMove = MovePcbDrag;
            BackView.EditMove = MovePcbDrag;
            FrontView.EditEnd = EndPcbDrag;
            BackView.EditEnd = EndPcbDrag;
        }

        /// <summary>
        /// Starts an edit drag at a photo point (edit mode): a board corner when the outline is
        /// shown, else a chip box when the boxes are shown - near a box corner it sizes the box
        /// (with Shift it turns it), inside it moves it.
        /// </summary>
        /// <param name="side">
        /// The side.
        /// </param>
        /// <param name="p">
        /// The point in the photo's pixels.
        /// </param>
        /// <param name="shift">
        /// True when Shift is held.
        /// </param>
        /// <returns>
        /// True when the drag edits something.
        /// </returns>
        public bool StartPcbDrag(BoardSide side, GridPoint p, bool shift)
        {
            _dragMode = "";
            if (_session == null || _session.Editor.ReadOnly || _pcbTemplate == null)
                return false;
            ImageView view = side == BoardSide.Component ? FrontView : BackView;
            double near = ImageView.HandleSize / Math.Max(1e-9, view.ScreenPerPixel);
            GridPoint[]? corners = BoardCorners(side);
            _dragSide = side;
            _dragFrom = p;
            if (TbBoardCorners.IsChecked == true && corners != null)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (Math.Abs(corners[i].X - p.X) <= near && Math.Abs(corners[i].Y - p.Y) <= near)
                    {
                        _dragMode = "corner";
                        _dragCorner = i;
                        _dragCorners = (GridPoint[])corners.Clone();
                        return true;
                    }
                }
            }
            if (!ShowChipBoxes)
                return false;
            PcbBox? b = PcbOverlay.At(side == BoardSide.Component ? _frontBoxes : _backBoxes, p);
            if (b == null)
            {
                // a press near a corner just outside the box still sizes it
                List<PcbBox> all = side == BoardSide.Component ? _frontBoxes : _backBoxes;
                for (int i = 0; i < all.Count && b == null; i++)
                {
                    for (int k = 0; k < all[i].Corners.Length; k++)
                    {
                        if (Math.Abs(all[i].Corners[k].X - p.X) <= near && Math.Abs(all[i].Corners[k].Y - p.Y) <= near)
                            b = all[i];
                    }
                }
            }
            if (b == null)
                return false;
            _dragOriginal = BoxOf(b);
            _dragBox = NetlistReader.Clone(_dragOriginal);
            _dragMode = "move";
            for (int k = 0; k < b.Corners.Length; k++)
            {
                if (Math.Abs(b.Corners[k].X - p.X) <= near && Math.Abs(b.Corners[k].Y - p.Y) <= near)
                {
                    _dragMode = shift ? "turn" : "size";
                    _dragCorner = k;
                }
            }
            return true;
        }

        /// <summary>
        /// The edit drag moved to a photo point: the box or corner follows (shown at once; saved
        /// when the drag ends).
        /// </summary>
        /// <param name="p">
        /// The point in the photo's pixels.
        /// </param>
        public void MovePcbDrag(GridPoint p)
        {
            if (_dragMode.Length == 0 || _pcbTemplate == null)
                return;
            ImageView view = _dragSide == BoardSide.Component ? FrontView : BackView;
            if (_dragMode == "corner")
            {
                _dragCorners![_dragCorner] = p;
                view.Outline = _dragCorners;
                view.InvalidateVisual();
                return;
            }
            GridPoint a = ToBoard(_dragFrom), b = ToBoard(p);
            Model.ChipBox o = _dragOriginal!, box = _dragBox!;
            double dx = b.X - a.X, dy = b.Y - a.Y;
            if (_dragMode == "move")
            {
                box.XMm = o.XMm + dx;
                box.YMm = o.YMm + dy;
            }
            else if (_dragMode == "size")
            {
                // the drag in the box's own turned axes; the far corner stays where it is
                GridPoint[] oc = PcbGrid.BoxCorners(o);
                GridPoint far = oc[(_dragCorner + 2) % 4];
                double rad = o.RotationDeg * Math.PI / 180, cos = Math.Cos(rad), sin = Math.Sin(rad);
                double vx = b.X - far.X, vy = b.Y - far.Y;
                double lw = Math.Abs((vx * cos) + (vy * sin)), lh = Math.Abs((-vx * sin) + (vy * cos));
                box.WidthMm = Math.Max(1, lw);
                box.HeightMm = Math.Max(1, lh);
                box.XMm = (far.X + b.X) / 2;
                box.YMm = (far.Y + b.Y) / 2;
            }
            else
            {
                double from = Math.Atan2(a.Y - o.YMm, a.X - o.XMm), to = Math.Atan2(b.Y - o.YMm, b.X - o.XMm);
                box.RotationDeg = Math.Round(o.RotationDeg + ((to - from) * 180 / Math.PI), 1);
            }
            // show the box where it is now, in place of the one in the list
            List<PcbBox> list = _dragSide == BoardSide.Component ? _frontBoxes : _backBoxes;
            GridPoint[]? corners = BoardCorners(_dragSide);
            for (int i = 0; corners != null && i < list.Count; i++)
            {
                if (list[i].Ref != box.Ref)
                    continue;
                GridPoint[] mm = PcbGrid.BoxCorners(box);
                GridPoint[] px = new GridPoint[4];
                for (int k = 0; k < 4; k++)
                    px[k] = PcbGrid.ToPhoto(corners, _pcbTemplate.Size, _dragSide == BoardSide.Solder, mm[k]);
                list[i] = new PcbBox { Ref = list[i].Ref, Owner = list[i].Owner, Type = list[i].Type, Function = list[i].Function, Corners = px, BoardCorners = mm, ByHand = true };
            }
            view.Boxes = list;
            view.InvalidateVisual();
        }

        /// <summary>
        /// Ends the edit drag: saves the box (it now wins over the grid cell) or the photo's
        /// corners into the sheet, one undo step.
        /// </summary>
        public void EndPcbDrag()
        {
            string mode = _dragMode;
            _dragMode = "";
            if (_session == null || mode.Length == 0)
                return;
            try
            {
                if (mode == "corner")
                {
                    Netlist n = _session.Editor.Root;
                    List<int> mine = PhotosOf(n, _dragSide);
                    int index = mine[Math.Clamp(PhotoShown(_dragSide), 0, mine.Count - 1)];
                    _session.Editor.SetPhotoCorners(index, _dragCorners);
                    StatusText.Text = "Saved the board corners of " + n.Photos![index].File + ".";
                }
                else if (_dragBox != null)
                {
                    _dragBox.XMm = Math.Round(_dragBox.XMm, 2);
                    _dragBox.YMm = Math.Round(_dragBox.YMm, 2);
                    _dragBox.WidthMm = Math.Round(_dragBox.WidthMm, 2);
                    _dragBox.HeightMm = Math.Round(_dragBox.HeightMm, 2);
                    _session.Editor.SetChipBox(_dragBox);
                    StatusText.Text = "Saved the box of " + _dragBox.Ref + " (set by hand; it now wins over the grid cell).";
                }
            }
            catch (SheetEditException ex)
            {
                StatusText.Text = ex.Message;
            }
            RefreshPcb();
            ShowEditState();
        }

        // a box as a hand-set box: the saved one, else the grid cell as an upright box
        private Model.ChipBox BoxOf(PcbBox b)
        {
            List<Model.ChipBox>? saved = _session!.Editor.Root.Pcb?.Boxes;
            for (int i = 0; saved != null && i < saved.Count; i++)
            {
                if (string.Equals(saved[i].Ref, b.Ref, StringComparison.OrdinalIgnoreCase))
                    return NetlistReader.Clone(saved[i]);
            }
            double x0 = double.MaxValue, y0 = double.MaxValue, x1 = double.MinValue, y1 = double.MinValue;
            for (int i = 0; i < b.BoardCorners.Length; i++)
            {
                x0 = Math.Min(x0, b.BoardCorners[i].X);
                y0 = Math.Min(y0, b.BoardCorners[i].Y);
                x1 = Math.Max(x1, b.BoardCorners[i].X);
                y1 = Math.Max(y1, b.BoardCorners[i].Y);
            }
            return new Model.ChipBox { Ref = b.Ref, XMm = (x0 + x1) / 2, YMm = (y0 + y1) / 2, WidthMm = x1 - x0, HeightMm = y1 - y0 };
        }

        // a photo point as a board point in mm, using the outline's sides as the scale (the
        // corners are close to a rectangle; a slanted photo is handled when drawing, and a
        // drag only needs the size of a step)
        private GridPoint ToBoard(GridPoint p)
        {
            GridPoint[] c = BoardCorners(_dragSide)!;
            BoardSize s = _pcbTemplate!.Size;
            double u = (p.X - c[0].X) / Math.Max(1e-9, c[1].X - c[0].X);
            double v = (p.Y - c[0].Y) / Math.Max(1e-9, c[3].Y - c[0].Y);
            if (_dragSide == BoardSide.Solder)
                u = 1 - u;
            return new GridPoint(u * s.WidthMm, v * s.HeightMm);
        }
    }
}
