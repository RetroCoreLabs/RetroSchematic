//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.Collections.Generic;

namespace RetroSchematic.Recognition;

/// <summary>The board grid position codes are read against.</summary>
public enum BoardGrid
{
    /// <summary>NORD-10 card: numbers 1-19, letters A-E.</summary>
    Nord10,

    /// <summary>ND-100 card: numbers 1-29, letters A-H and J (no I).</summary>
    Nd100,
}

/// <summary>How the page is drawn, which picks the detector.</summary>
public enum PageStyle
{
    /// <summary>Drawn by hand (detector <c>det-L3</c>).</summary>
    Hand,

    /// <summary>Drawn by a computer (detector <c>det-A</c>).</summary>
    Cad,
}

/// <summary>
/// One thing the analyser found on a scanned page. Every value is a guess (confidence
/// <c>inferred</c>) until a person accepts it.
/// </summary>
public sealed class PageFind
{
    /// <summary>Gets or sets a number unique within one analysis (1 first).</summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets what it is: <c>chip</c>, <c>gate</c>, <c>resistor</c>, <c>capacitor</c>,
    /// <c>switch</c>, <c>strap</c>, <c>ground</c>, <c>supply</c>, <c>circle</c> (a circled
    /// backplane pin), <c>offpage</c> (a net label's diamond) or <c>dot</c> (a wire joint).
    /// </summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>Gets or sets the gate kind for a gate (<c>nand</c>, <c>nor</c>, <c>not</c>, <c>and</c>, <c>or</c>, <c>and-or-invert</c>, <c>buffer</c>, <c>other</c>).</summary>
    public string? GateKind { get; set; }

    /// <summary>Gets or sets the box's left edge, source page pixels.</summary>
    public int X { get; set; }

    /// <summary>Gets or sets the box's top edge, source page pixels.</summary>
    public int Y { get; set; }

    /// <summary>Gets or sets the box's width, source page pixels.</summary>
    public int Width { get; set; }

    /// <summary>Gets or sets the box's height, source page pixels.</summary>
    public int Height { get; set; }

    /// <summary>Gets or sets how sure the detector is, 0..1.</summary>
    public double Score { get; set; }

    /// <summary>Gets or sets the position code read for it (<c>13D</c>, <c>7-8C-D</c>), or null.</summary>
    public string? Code { get; set; }

    /// <summary>Gets or sets the code reading's mean log probability per character (higher is surer).</summary>
    public double CodeScore { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the code was not read at this symbol but taken from
    /// the gate above or below it in the same column (ND writes a package's code once).
    /// </summary>
    public bool CodeFromColumn { get; set; }

    /// <summary>Gets or sets the chip type text as read (<c>157</c>), or empty.</summary>
    public string TypeText { get; set; } = string.Empty;

    /// <summary>Gets or sets the catalogue chip the type text matched (<c>74157</c>), or null.</summary>
    public string? TypeId { get; set; }

    /// <summary>
    /// Gets a value indicating whether the find is "sure": the detector at least 0.6, and for a
    /// chip or gate also a code read at the symbol itself. Shown in the window as a solid box.
    /// </summary>
    public bool IsSure => Score >= 0.6 && (Kind != "chip" && Kind != "gate" || (Code != null && !CodeFromColumn));
}

/// <summary>The result of analysing one page.</summary>
public sealed class PageAnalysis
{
    /// <summary>Gets the finds: chips and gates first, then the other kinds.</summary>
    public List<PageFind> Finds { get; } = new List<PageFind>();

    /// <summary>Gets or sets the page style the rule chose.</summary>
    public PageStyle Style { get; set; }

    /// <summary>Gets or sets the detector file used.</summary>
    public string Detector { get; set; } = string.Empty;

    /// <summary>Gets or sets how far the page was turned clockwise before reading (0, 90, 180, 270).</summary>
    public int TurnCw { get; set; }

    /// <summary>Gets or sets the scale the page was read at (1 = as given).</summary>
    public double Scale { get; set; } = 1.0;

    /// <summary>Gets or sets the measured letter size before scaling, or 0 when it could not be measured.</summary>
    public double LetterSize { get; set; }

    /// <summary>Gets or sets the number of letter pieces read.</summary>
    public int Pieces { get; set; }

    /// <summary>Gets or sets the time spent finding the turn, milliseconds.</summary>
    public long TurnMs { get; set; }

    /// <summary>Gets or sets the time spent cutting and reading the letter pieces, milliseconds.</summary>
    public long PiecesMs { get; set; }

    /// <summary>Gets or sets the time the detector took, milliseconds.</summary>
    public long DetectMs { get; set; }

    /// <summary>Gets or sets the time taken, milliseconds.</summary>
    public long Milliseconds { get; set; }
}
