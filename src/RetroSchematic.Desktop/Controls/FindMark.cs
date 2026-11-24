//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

namespace RetroSchematic.Desktop.Controls
{
    /// <summary>
    /// One find of the page analyser as the Original view draws it, in the shown picture's pixels.
    /// </summary>
    public sealed class FindMark
    {
        /// <summary>
        /// The find's number in the analysis.
        /// </summary>
        public int Id { get; init; }

        /// <summary>
        /// Left edge, picture pixels.
        /// </summary>
        public double X { get; init; }

        /// <summary>
        /// Top edge, picture pixels.
        /// </summary>
        public double Y { get; init; }

        /// <summary>
        /// Width, picture pixels.
        /// </summary>
        public double Width { get; init; }

        /// <summary>
        /// Height, picture pixels.
        /// </summary>
        public double Height { get; init; }

        /// <summary>
        /// The text on its tag: the code read, or empty.
        /// </summary>
        public string Label { get; init; } = "";

        /// <summary>
        /// True when the analyser is sure of it.
        /// </summary>
        public bool Sure { get; init; }

        /// <summary>
        /// True for the find picked to accept.
        /// </summary>
        public bool Picked { get; init; }
    }
}
