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
    /// How a signal's active level is shown next to its name. Display only: the names in the
    /// sheet file never change.
    /// </summary>
    public enum SignalNameStyle
    {
        /// <summary>
        /// "/NAME" for an active-low signal, plain "NAME" for an active-high one.
        /// </summary>
        Slash,

        /// <summary>
        /// "~NAME" for an active-low signal, plain "NAME" for an active-high one.
        /// </summary>
        Tilde,

        /// <summary>
        /// ND's own way: NAME with a small 0 (active low) or 1 (active high) below the line.
        /// </summary>
        Subscript,
    }

    /// <summary>
    /// Writes signal names in the chosen <see cref="SignalNameStyle"/>, and reads a name typed
    /// in any of the styles back. A signal with no active level is plain in every style.
    /// </summary>
    public static class SignalNames
    {
        /// <summary>
        /// The style used when nothing else is chosen: slash.
        /// </summary>
        public const SignalNameStyle Default = SignalNameStyle.Slash;

        /// <summary>
        /// The text and the subscript to draw for a signal.
        /// </summary>
        /// <param name="name">
        /// The signal's name as stored (no level mark).
        /// </param>
        /// <param name="active">
        /// Its active level.
        /// </param>
        /// <param name="style">
        /// The style.
        /// </param>
        /// <param name="subscript">
        /// The subscript to draw after the text ("0", "1" or empty).
        /// </param>
        /// <returns>
        /// The text to draw.
        /// </returns>
        public static string Format(string name, ActiveLevel active, SignalNameStyle style, out string subscript)
        {
            subscript = "";
            if (active == ActiveLevel.None)
                return name;
            if (style == SignalNameStyle.Subscript)
            {
                subscript = active == ActiveLevel.High ? "1" : "0";
                return name;
            }
            if (active == ActiveLevel.High)
                return name;
            return (style == SignalNameStyle.Tilde ? "~" : "/") + name;
        }

        /// <summary>
        /// A signal written on one line, as in a list or the status bar: subscript style writes
        /// NAME_0 / NAME_1 there, since a list has no subscripts.
        /// </summary>
        /// <param name="net">
        /// The signal.
        /// </param>
        /// <param name="style">
        /// The style.
        /// </param>
        /// <returns>
        /// The name for one line of text.
        /// </returns>
        public static string Line(Net net, SignalNameStyle style)
        {
            string text = Format(net.Name, net.Active, style, out string sub);
            return sub.Length == 0 ? text : text + "_" + sub;
        }

        /// <summary>
        /// Reads a typed name in any style: "/NAME" or "~NAME" is NAME active low, "NAME_0" and
        /// "NAME_1" are NAME with that level, anything else is the plain name with no level said.
        /// </summary>
        /// <param name="typed">
        /// What was typed.
        /// </param>
        /// <param name="name">
        /// The name without the level mark.
        /// </param>
        /// <returns>
        /// The level the text asks for, or null when it says none.
        /// </returns>
        public static ActiveLevel? Parse(string typed, out string name)
        {
            string t = typed.Trim();
            if (t.Length > 1 && (t[0] == '/' || t[0] == '~'))
            {
                name = t.Substring(1);
                return ActiveLevel.Low;
            }
            if (t.Length > 2 && t[^2] == '_' && (t[^1] == '0' || t[^1] == '1'))
            {
                name = t.Substring(0, t.Length - 2);
                return t[^1] == '1' ? ActiveLevel.High : ActiveLevel.Low;
            }
            name = t;
            return null;
        }

        /// <summary>
        /// Reads a style's name as written in the settings or on the command line (slash, tilde,
        /// subscript; case does not matter).
        /// </summary>
        /// <param name="text">
        /// The text.
        /// </param>
        /// <param name="style">
        /// The style read.
        /// </param>
        /// <returns>
        /// False when the text names no style.
        /// </returns>
        public static bool TryParseStyle(string? text, out SignalNameStyle style)
            => Enum.TryParse(text, true, out style) && Enum.IsDefined(style);
    }
}
