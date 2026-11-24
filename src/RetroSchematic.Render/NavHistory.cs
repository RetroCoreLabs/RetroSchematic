//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.Collections.Generic;

namespace RetroSchematic.Render
{
    /// <summary>
    /// One place in the Back / Forward history: the block level, what was picked and traced,
    /// and where the view was.
    /// </summary>
    public sealed class NavPlace
    {
        /// <summary>
        /// The instance names from the top down to the level shown; empty for the top.
        /// </summary>
        public string[] Level { get; init; } = System.Array.Empty<string>();

        /// <summary>
        /// What was picked (part:13D, net:LEV12_0 ...), or empty.
        /// </summary>
        public string Selected { get; init; } = "";

        /// <summary>
        /// The drawn page shown (format version 3), or empty for a level without pages.
        /// </summary>
        public string Page { get; init; } = "";

        /// <summary>
        /// The traced signal's key, or empty.
        /// </summary>
        public string Traced { get; set; } = "";

        /// <summary>
        /// The text for a button's tooltip: "11D 7402", "/LEV12", "U1 (SYNC)".
        /// </summary>
        public string Label { get; init; } = "";

        /// <summary>
        /// True once the view below has been filled in.
        /// </summary>
        public bool HasView { get; set; }

        /// <summary>
        /// The drawing point in the middle of the view.
        /// </summary>
        public double CentreX { get; set; }

        /// <summary>
        /// The drawing point in the middle of the view.
        /// </summary>
        public double CentreY { get; set; }

        /// <summary>
        /// The zoom factor.
        /// </summary>
        public double Zoom { get; set; } = 1;

        /// <summary>
        /// True when the two places are on the same level with the same pick.
        /// </summary>
        /// <param name="other">
        /// The other place.
        /// </param>
        /// <returns>
        /// True when they are the same place.
        /// </returns>
        public bool SameAs(NavPlace other)
        {
            if (Selected != other.Selected || Page != other.Page || Level.Length != other.Level.Length)
                return false;
            for (int i = 0; i < Level.Length; i++)
            {
                if (Level[i] != other.Level[i])
                    return false;
            }
            return true;
        }

        /// <summary>
        /// True when the two places are on the same block level and the same drawn page (a
        /// page change is a place of its own, even with nothing picked).
        /// </summary>
        /// <param name="other">
        /// The other place.
        /// </param>
        /// <returns>
        /// True when the levels and pages match.
        /// </returns>
        public bool SameLevel(NavPlace other)
        {
            if (Page != other.Page || Level.Length != other.Level.Length)
                return false;
            for (int i = 0; i < Level.Length; i++)
            {
                if (Level[i] != other.Level[i])
                    return false;
            }
            return true;
        }
    }

    /// <summary>
    /// The Back / Forward history, as in a web browser: every new place is added after the one
    /// shown and drops any places that were ahead of it; Back and Forward move along the list.
    /// </summary>
    public sealed class NavHistory
    {
        private readonly List<NavPlace> _places = new();
        private int _at = -1;

        /// <summary>
        /// Makes a history that keeps at most <paramref name="limit"/> places.
        /// </summary>
        /// <param name="limit">
        /// The most places kept; the oldest go first.
        /// </param>
        public NavHistory(int limit = 200)
        {
            Limit = limit < 2 ? 2 : limit;
        }

        /// <summary>
        /// The most places kept.
        /// </summary>
        public int Limit { get; }

        /// <summary>
        /// How many places are kept now.
        /// </summary>
        public int Count => _places.Count;

        /// <summary>
        /// The place shown now, or null before the first one.
        /// </summary>
        public NavPlace? Current => _at < 0 ? null : _places[_at];

        /// <summary>
        /// True when there is a place to go back to.
        /// </summary>
        public bool CanBack => _at > 0;

        /// <summary>
        /// True when there is a place to go forward to.
        /// </summary>
        public bool CanForward => _at >= 0 && _at < _places.Count - 1;

        /// <summary>
        /// The place Back goes to, or null.
        /// </summary>
        public NavPlace? BackPlace => CanBack ? _places[_at - 1] : null;

        /// <summary>
        /// The place Forward goes to, or null.
        /// </summary>
        public NavPlace? ForwardPlace => CanForward ? _places[_at + 1] : null;

        /// <summary>
        /// Notes a new place. The same place as the current one only updates its trace. An empty
        /// pick on the same level is not a place. Anything ahead of the current place is dropped.
        /// </summary>
        /// <param name="place">
        /// The new place.
        /// </param>
        /// <returns>
        /// True when it was added as a new entry.
        /// </returns>
        public bool Push(NavPlace place)
        {
            NavPlace? cur = Current;
            if (cur != null && cur.SameAs(place))
            {
                cur.Traced = place.Traced;
                return false;
            }
            if (cur != null && place.Selected.Length == 0 && cur.SameLevel(place))
                return false;
            if (_at < _places.Count - 1)
                _places.RemoveRange(_at + 1, _places.Count - _at - 1);
            _places.Add(place);
            if (_places.Count > Limit)
                _places.RemoveAt(0);
            _at = _places.Count - 1;
            return true;
        }

        /// <summary>
        /// Goes one place back.
        /// </summary>
        /// <returns>
        /// The place to show, or null when there is none.
        /// </returns>
        public NavPlace? Back()
        {
            if (!CanBack)
                return null;
            _at--;
            return _places[_at];
        }

        /// <summary>
        /// Goes one place forward.
        /// </summary>
        /// <returns>
        /// The place to show, or null when there is none.
        /// </returns>
        public NavPlace? Forward()
        {
            if (!CanForward)
                return null;
            _at++;
            return _places[_at];
        }

        /// <summary>
        /// Forgets every place (a new sheet was opened).
        /// </summary>
        public void Clear()
        {
            _places.Clear();
            _at = -1;
        }
    }
}
