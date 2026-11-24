//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using RetroSchematic.Model;

namespace RetroSchematic.Render
{
    /// <summary>
    /// Back / Forward: the place shown now, and going back to a place.
    /// </summary>
    public sealed partial class SheetSession
    {
        /// <summary>
        /// The place shown now (level, pick, trace), without the view; the window adds that.
        /// </summary>
        /// <returns>
        /// The place.
        /// </returns>
        public NavPlace PlaceNow()
        {
            string[] level = new string[Editor.LevelPath.Count];
            for (int i = 0; i < level.Length; i++)
                level[i] = Editor.LevelPath[i];
            string page = CurrentPage?.Id ?? "";
            string label = LabelOf(Selected, level);
            if (page.Length > 0 && PageCount > 1)
                label += " (page " + page + ")";
            return new NavPlace { Level = level, Page = page, Selected = Selected, Traced = Traced, Label = label };
        }

        /// <summary>
        /// True when the traced signal (if any) belongs to what is picked. A trace started just
        /// before a new pick is not yet the place's trace: the pick that follows is.
        /// </summary>
        public bool TraceBelongsToPick => Traced.Length == 0 || SignalAt(Selected) == Traced;

        /// <summary>
        /// Shows a place again: its level, its pick and its trace.
        /// </summary>
        /// <param name="place">
        /// The place.
        /// </param>
        public void GoTo(NavPlace place)
        {
            bool same = place.Level.Length == Editor.LevelPath.Count;
            for (int i = 0; same && i < place.Level.Length; i++)
                same = place.Level[i] == Editor.LevelPath[i];
            if (!same)
            {
                while (Editor.LevelPath.Count > 0)
                    Editor.LeaveInstance();
                for (int i = 0; i < place.Level.Length; i++)
                    Editor.EnterInstance(place.Level[i]);
                AfterLevelChange();
            }
            // the page first, so the pick and the trace are shown on it
            if (place.Page.Length > 0)
                SetPage(place.Page);
            Select(place.Selected);
            Trace(place.Traced);
        }

        /// <summary>
        /// A short name for a pick, for a Back or Forward tooltip: "11D 7402", "/LEV12",
        /// "U1 (SYNC)", or the level's name when nothing is picked.
        /// </summary>
        /// <param name="owner">
        /// The pick.
        /// </param>
        /// <param name="level">
        /// The level it is on.
        /// </param>
        /// <returns>
        /// The name.
        /// </returns>
        public string LabelOf(string owner, string[] level)
        {
            if (owner.Length == 0)
                return level.Length == 0 ? "the top level" : "level " + level[level.Length - 1];
            if (owner.StartsWith("part:", StringComparison.Ordinal))
            {
                Part? p = Editor.FindPart(owner.Substring(5));
                return p == null ? owner.Substring(5) : p.Ref + " " + p.Type;
            }
            if (owner.StartsWith("conn:", StringComparison.Ordinal))
                return ConnectorPins.Name(owner.Substring(5));
            if (owner.StartsWith("bus:", StringComparison.Ordinal))
                return "bus " + owner.Substring(4);
            if (owner.StartsWith("inst:", StringComparison.Ordinal))
                return owner.Substring(5);
            string key = SignalAt(owner);
            SignalInfo? info = key.Length == 0 ? null : SignalInfoOf(key);
            if (info != null && owner.StartsWith("net:", StringComparison.Ordinal))
                return info.Name;
            return owner.Substring(owner.IndexOf(':') + 1);
        }
    }
}
