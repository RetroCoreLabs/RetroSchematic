//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;

namespace RetroSchematic.Render
{
    /// <summary>
    /// What a mouse drag on the sheet is doing right now.
    /// </summary>
    public enum EditGesture
    {
        /// <summary>
        /// Nothing.
        /// </summary>
        None,

        /// <summary>
        /// Drawing a wire from a pin; let go on another pin to connect them.
        /// </summary>
        Wire,

        /// <summary>
        /// Moving a chip.
        /// </summary>
        Move,
    }

    /// <summary>
    /// Turns mouse presses, drags and keys on the drawing into calls on a session's
    /// <see cref="SheetEditor"/>. The window forwards its pointer and key events here (in drawing
    /// units) and draws <see cref="Gesture"/>'s preview line; it decides nothing itself.
    /// <list type="bullet">
    /// <item><description>Read-only: a press only picks.</description></item>
    /// <item><description>Edit mode with a chip type chosen (<see cref="PlaceType"/>): a press
    /// places that chip there.</description></item>
    /// <item><description>Edit mode, press on a pin dot: drag to another pin dot and let go to
    /// connect the two.</description></item>
    /// <item><description>Edit mode, press on a chip or gate: drag and let go to move the
    /// chip.</description></item>
    /// <item><description>Delete key: deletes the picked chip or signal, or takes the picked pin
    /// off its signal.</description></item>
    /// </list>
    /// A refused change does not throw out of here: its reason is put in <see cref="Message"/>.
    /// </summary>
    public sealed class EditTool
    {
        private readonly SheetSession _session;
        private string _from = "";

        /// <summary>
        /// A drag shorter than this (drawing units) is a click, not a move.
        /// </summary>
        public const double MoveThreshold = 3;

        /// <summary>
        /// Works on one session.
        /// </summary>
        /// <param name="session">
        /// The session to edit.
        /// </param>
        public EditTool(SheetSession session)
        {
            _session = session;
        }

        /// <summary>
        /// The chip type the next press places, or null to place nothing. Kept after a place, so
        /// several of one type can be placed; set it to null (Escape) to stop.
        /// </summary>
        public string? PlaceType { get; set; }

        /// <summary>
        /// True: the trace tool is chosen. A press traces the signal under it (in read-only and in
        /// edit mode alike) and changes nothing.
        /// </summary>
        public bool TraceMode { get; set; }

        /// <summary>
        /// What the current drag is doing.
        /// </summary>
        public EditGesture Gesture { get; private set; }

        /// <summary>
        /// Where the drag started (drawing units): the pin end for a wire, the press point for a move.
        /// </summary>
        public DrawPoint Start { get; private set; }

        /// <summary>
        /// Where the pointer is now during a drag (drawing units).
        /// </summary>
        public DrawPoint Current { get; private set; }

        /// <summary>
        /// What the last action did or why it was refused, for the status bar. Empty for nothing.
        /// </summary>
        public string Message { get; private set; } = "";

        /// <summary>
        /// The mouse button went down at a point.
        /// </summary>
        /// <param name="x">
        /// X in drawing units.
        /// </param>
        /// <param name="y">
        /// Y in drawing units.
        /// </param>
        public void PointerDown(double x, double y)
        {
            Message = "";
            Gesture = EditGesture.None;
            if (TraceMode)
            {
                _session.TraceAt(x, y);
                Message = _session.Traced.Length == 0 ? "" : "Tracing " + _session.Traced + ".";
                return;
            }
            string hit = _session.Drawing.HitTest(x, y);
            if (_session.Editor.ReadOnly)
            {
                _session.Select(hit);
                return;
            }
            if (PlaceType != null)
            {
                Try(() =>
                {
                    string reference = _session.Editor.PlacePart(PlaceType, SheetLayout.ToMm(x), SheetLayout.ToMm(y));
                    _session.Select("part:" + reference);
                    Message = "Placed " + PlaceType + " as " + reference + ".";
                });
                return;
            }
            _session.Select(hit);
            Start = new DrawPoint(x, y);
            Current = Start;
            if (hit.StartsWith("pin:", StringComparison.Ordinal))
            {
                _from = hit.Substring(4);
                if (_session.Drawing.Pins.TryGetValue(_from, out DrawPoint p))
                    Start = p;
                Gesture = EditGesture.Wire;
            }
            else if (hit.StartsWith("part:", StringComparison.Ordinal) || hit.StartsWith("gate:", StringComparison.Ordinal))
            {
                _from = PartOf(hit);
                Gesture = EditGesture.Move;
            }
        }

        /// <summary>
        /// The pointer moved; only matters during a drag.
        /// </summary>
        /// <param name="x">
        /// X in drawing units.
        /// </param>
        /// <param name="y">
        /// Y in drawing units.
        /// </param>
        public void PointerMove(double x, double y)
        {
            if (Gesture != EditGesture.None)
                Current = new DrawPoint(x, y);
        }

        /// <summary>
        /// The mouse button came up: finishes a wire or a move.
        /// </summary>
        /// <param name="x">
        /// X in drawing units.
        /// </param>
        /// <param name="y">
        /// Y in drawing units.
        /// </param>
        public void PointerUp(double x, double y)
        {
            EditGesture g = Gesture;
            Gesture = EditGesture.None;
            if (g == EditGesture.Wire)
            {
                string hit = _session.Drawing.HitTest(x, y);
                if (!hit.StartsWith("pin:", StringComparison.Ordinal) || hit.Substring(4) == _from)
                    return;
                string to = hit.Substring(4);
                Try(() =>
                {
                    string net = _session.Editor.Connect(_from, to);
                    _session.Select("net:" + net);
                    Message = "Connected " + _from + " to " + to + " on " + net + ".";
                });
            }
            else if (g == EditGesture.Move)
            {
                double dx = x - Start.X, dy = y - Start.Y;
                if (Math.Abs(dx) < MoveThreshold && Math.Abs(dy) < MoveThreshold)
                    return;
                if (!_session.Drawing.Frames.TryGetValue(_from, out PartFrame f))
                    return;
                Try(() =>
                {
                    _session.Editor.MovePart(_from, SheetLayout.ToMm(f.X + dx), SheetLayout.ToMm(f.Y + dy));
                    Message = "Moved " + _from + ".";
                });
            }
        }

        /// <summary>
        /// The Delete key: removes the picked chip or signal, or takes the picked pin off its signal.
        /// A picked gate is not deleted on its own (a gate is part of its chip).
        /// </summary>
        public void DeleteSelected()
        {
            string s = _session.Selected;
            Message = "";
            Try(() =>
            {
                if (s.StartsWith("part:", StringComparison.Ordinal))
                    _session.Editor.DeletePart(s.Substring(5));
                else if (s.StartsWith("net:", StringComparison.Ordinal))
                    _session.Editor.DeleteNet(s.Substring(4));
                else if (s.StartsWith("pin:", StringComparison.Ordinal))
                    _session.Editor.Disconnect(s.Substring(4));
                else if (s.StartsWith("gate:", StringComparison.Ordinal))
                    throw new SheetEditException("A gate is part of its chip; pick the chip to delete it.");
                else
                    return;
                Message = "Deleted " + s.Substring(s.IndexOf(':') + 1) + ".";
            });
        }

        /// <summary>
        /// Undo, with a refusal put in <see cref="Message"/>.
        /// </summary>
        public void Undo() => Try(() => { _session.Editor.Undo(); Message = "Undone."; });

        /// <summary>
        /// Redo, with a refusal put in <see cref="Message"/>.
        /// </summary>
        public void Redo() => Try(() => { _session.Editor.Redo(); Message = "Done again."; });

        /// <summary>
        /// Escape: stops placing, drops a drag and clears the trace.
        /// </summary>
        public void Cancel()
        {
            PlaceType = null;
            _session.Trace("");
            Gesture = EditGesture.None;
            Message = "";
        }

        private static string PartOf(string owner)
        {
            string name = owner.Substring(owner.IndexOf(':') + 1);
            int slash = name.IndexOf('/');
            return slash < 0 ? name : name.Substring(0, slash);
        }

        private void Try(Action action)
        {
            try
            {
                action();
            }
            catch (SheetEditException e)
            {
                Message = e.Message;
            }
        }
    }
}
