//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using RetroSchematic.Model;

namespace RetroSchematic.Render
{
    /// <summary>
    /// Picking several chips and instances at once (Ctrl-click or a dragged box), for making a
    /// block from them or moving them into an instance; and going in and out of instances.
    /// </summary>
    public sealed partial class SheetSession
    {
        private readonly List<string> _picked = new();

        /// <summary>
        /// The chips and instances picked together ("part:11D", "inst:U1"), in the order picked.
        /// </summary>
        public IReadOnlyList<string> Picked => _picked;

        /// <summary>
        /// Adds a chip or instance to the group picked, or takes it out when it is already in.
        /// A gate or port stands for its chip or instance; anything else is ignored.
        /// </summary>
        /// <param name="owner">
        /// What was clicked (an owner name from the drawing).
        /// </param>
        public void TogglePick(string owner)
        {
            string whole = WholeOf(owner);
            if (whole.Length == 0)
                return;
            if (!_picked.Remove(whole))
                _picked.Add(whole);
            SelectionChanged?.Invoke(this, Selected);
        }

        /// <summary>
        /// Picks every chip and instance whose box lies wholly inside a rectangle (a dragged box),
        /// in place of what was picked before.
        /// </summary>
        /// <param name="x1">
        /// One corner, X, in drawing units.
        /// </param>
        /// <param name="y1">
        /// One corner, Y.
        /// </param>
        /// <param name="x2">
        /// The other corner, X.
        /// </param>
        /// <param name="y2">
        /// The other corner, Y.
        /// </param>
        public void PickInBox(double x1, double y1, double x2, double y2)
        {
            double l = Math.Min(x1, x2), r = Math.Max(x1, x2), t = Math.Min(y1, y2), b = Math.Max(y1, y2);
            _picked.Clear();
            foreach (KeyValuePair<string, PartFrame> kv in Drawing.Frames)
            {
                if (Inside(kv.Value, l, t, r, b))
                    _picked.Add("part:" + kv.Key);
            }
            foreach (KeyValuePair<string, PartFrame> kv in Drawing.InstanceFrames)
            {
                if (Inside(kv.Value, l, t, r, b))
                    _picked.Add("inst:" + kv.Key);
            }
            SelectionChanged?.Invoke(this, Selected);
        }

        /// <summary>
        /// Empties the group picked.
        /// </summary>
        public void ClearPicked()
        {
            if (_picked.Count == 0)
                return;
            _picked.Clear();
            SelectionChanged?.Invoke(this, Selected);
        }

        /// <summary>
        /// The group picked as the editor wants it for a block: part references, and instances
        /// as inst:NAME. When nothing is picked together, the one picked chip or instance.
        /// </summary>
        /// <returns>
        /// The members, possibly empty.
        /// </returns>
        public List<string> PickedMembers()
        {
            List<string> list = new();
            if (_picked.Count == 0)
            {
                string one = WholeOf(Selected);
                if (one.Length > 0)
                    list.Add(one.StartsWith("part:", StringComparison.Ordinal) ? one.Substring(5) : one);
                return list;
            }
            for (int i = 0; i < _picked.Count; i++)
                list.Add(_picked[i].StartsWith("part:", StringComparison.Ordinal) ? _picked[i].Substring(5) : _picked[i]);
            return list;
        }

        /// <summary>
        /// Goes inside an instance (see <see cref="SheetEditor.EnterInstance"/>); the group picked
        /// and the trace are cleared because they named things on the level left.
        /// </summary>
        /// <param name="instance">
        /// The instance name.
        /// </param>
        public void OpenInstance(string instance)
        {
            Editor.EnterInstance(instance);
            AfterLevelChange();
        }

        /// <summary>
        /// Goes back up one level.
        /// </summary>
        public void LeaveInstance()
        {
            Editor.LeaveInstance();
            AfterLevelChange();
        }

        /// <summary>
        /// The levels from the top down to the one shown, for a breadcrumb bar: "Top", then
        /// "U2 (read-multiplexers)" for each instance gone into.
        /// </summary>
        /// <returns>
        /// The names.
        /// </returns>
        public List<string> LevelNames()
        {
            List<string> names = new() { "Top" };
            List<BlockInstance>? instances = Editor.Root.Instances;
            for (int i = 0; i < Editor.LevelPath.Count; i++)
            {
                BlockInstance? inst = Hierarchy.FindInstance(instances, Editor.LevelPath[i]);
                BlockDefinition? b = inst == null ? null : Hierarchy.FindBlock(Editor.Root, inst.Block);
                names.Add(Editor.LevelPath[i] + (b == null ? "" : " (" + b.Name + ")"));
                instances = b?.Instances;
            }
            return names;
        }

        /// <summary>
        /// The instance a chip, gate, instance or port belongs to, as "inst:NAME", or the chip
        /// as "part:REF"; empty for anything else.
        /// </summary>
        /// <param name="owner">
        /// An owner name from the drawing.
        /// </param>
        /// <returns>
        /// The whole thing it is part of.
        /// </returns>
        public static string WholeOf(string owner)
        {
            if (owner.StartsWith("part:", StringComparison.Ordinal) || owner.StartsWith("inst:", StringComparison.Ordinal))
                return owner;
            if (owner.StartsWith("gate:", StringComparison.Ordinal))
            {
                int slash = owner.IndexOf('/', 5);
                return "part:" + (slash < 0 ? owner.Substring(5) : owner.Substring(5, slash - 5));
            }
            if (owner.StartsWith("port:", StringComparison.Ordinal))
            {
                int colon = owner.IndexOf(':', 5);
                return colon < 0 ? "" : "inst:" + owner.Substring(5, colon - 5);
            }
            return "";
        }

        private void AfterLevelChange()
        {
            _picked.Clear();
            Trace("");
            Select("");
        }

        /// <summary>
        /// True when an item belongs to a chip or instance in the group picked.
        /// </summary>
        private bool IsPicked(DrawItem item)
        {
            if (_picked.Count == 0 || item.Owner.Length == 0)
                return false;
            string whole = WholeOf(item.Owner);
            return whole.Length > 0 && _picked.Contains(whole);
        }

        private static bool Inside(PartFrame f, double l, double t, double r, double b)
            => f.X >= l && f.Y >= t && f.X + f.Width <= r && f.Y + f.Height <= b;
    }
}
