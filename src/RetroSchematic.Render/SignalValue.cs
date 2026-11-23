//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

namespace RetroSchematic.Render
{
    /// <summary>
    /// The value a signal has while a simulation runs, coloured as Logisim does: 1 bright green,
    /// 0 dark green, not driven blue, driven both ways red, no value yet grey. Nothing sets these
    /// yet (the simulator is not written); a signal with <see cref="NotSimulated"/> keeps the
    /// ink and reading colours.
    /// </summary>
    public enum SignalValue
    {
        /// <summary>
        /// No simulation: drawn in the ink and reading colours as always.
        /// </summary>
        NotSimulated,

        /// <summary>
        /// The simulation runs but has not given this signal a value yet (grey).
        /// </summary>
        Unknown,

        /// <summary>
        /// Low, 0 (dark green).
        /// </summary>
        Zero,

        /// <summary>
        /// High, 1 (bright green).
        /// </summary>
        One,

        /// <summary>
        /// Not driven: every driver is a switched-off three-state output, or there is none (blue).
        /// </summary>
        NotDriven,

        /// <summary>
        /// Driven high and low at once by two outputs (red).
        /// </summary>
        Conflict,
    }
}
