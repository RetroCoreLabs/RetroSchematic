//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace RetroSchematic.Model
{
    /// <summary>Which way the OUTPUT of a timing path moves. Datasheets give a delay for each.</summary>
    public enum OutputEdge
    {
        /// <summary>tPLH: the output goes from low to high.</summary>
        LowToHigh,

        /// <summary>tPHL: the output goes from high to low.</summary>
        HighToLow,

        /// <summary>tPZH: a three-state output starts driving, from floating (Z) to high.</summary>
        FloatToHigh,

        /// <summary>tPZL: a three-state output starts driving, from floating (Z) to low.</summary>
        FloatToLow,

        /// <summary>tPHZ: a three-state output stops driving, from high to floating (Z).</summary>
        HighToFloat,

        /// <summary>tPLZ: a three-state output stops driving, from low to floating (Z).</summary>
        LowToFloat,

        /// <summary>Output enable time, not split by level (datasheet gives one figure).</summary>
        FloatToDriven,

        /// <summary>Output disable time, not split by level.</summary>
        DrivenToFloat,

        /// <summary>Memory access time: from address or chip select until the data out is valid.</summary>
        Access,

        /// <summary>Memory recovery time after a write before a read is valid.</summary>
        Recovery,

        /// <summary>How long the output's rising edge itself takes.</summary>
        Rise,

        /// <summary>How long the output's falling edge itself takes.</summary>
        Fall,

        /// <summary>Opto-coupler turn-on time to the level the datasheet states.</summary>
        TurnOn,

        /// <summary>Opto-coupler turn-off time to the level the datasheet states.</summary>
        TurnOff,

        /// <summary>Line receiver response time.</summary>
        Response,
    }

    /// <summary>
    /// One documented delay figure in nanoseconds. Any of the three may be missing from a
    /// datasheet; a missing value is null, never filled in.
    /// </summary>
    public readonly struct DelayNs
    {
        /// <summary>Shortest delay the datasheet promises, or null if it gives none.</summary>
        public readonly double? Min;

        /// <summary>Typical delay, or null.</summary>
        public readonly double? Typ;

        /// <summary>Longest delay the datasheet promises, or null.</summary>
        public readonly double? Max;

        /// <summary>Creates a delay figure.</summary>
        public DelayNs(double? min, double? typ, double? max)
        {
            Min = min;
            Typ = typ;
            Max = max;
        }
    }

    /// <summary>
    /// One timing path of a chip as its datasheet lists it: from an input pin (or the clock, for
    /// clock-to-output) to an output pin, for one direction of the output edge.
    /// </summary>
    public sealed class TimingArc
    {
        /// <summary>Creates a timing path.</summary>
        public TimingArc(string from, string to, OutputEdge edge, DelayNs delay, string conditions)
        {
            From = from;
            To = to;
            Edge = edge;
            Delay = delay;
            Conditions = conditions;
        }

        /// <summary>Input pin name (or the clock pin).</summary>
        public string From { get; }

        /// <summary>Output pin name.</summary>
        public string To { get; }

        /// <summary>Which way the output moves.</summary>
        public OutputEdge Edge { get; }

        /// <summary>The delay figures.</summary>
        public DelayNs Delay { get; }

        /// <summary>The datasheet's test conditions (load, supply, temperature).</summary>
        public string Conditions { get; }
    }

    /// <summary>
    /// One part on a signal path: the delay when its output rises, the delay when it falls, and
    /// whether it turns the edge over (an inverting gate turns a rising input into a falling output).
    /// </summary>
    public readonly struct PathStage
    {
        /// <summary>Name for the report (for example "IC12 7400 A-&gt;Y").</summary>
        public readonly string Name;

        /// <summary>Delay when this stage's output goes low to high.</summary>
        public readonly DelayNs Rising;

        /// <summary>Delay when this stage's output goes high to low.</summary>
        public readonly DelayNs Falling;

        /// <summary>True if the output edge is the opposite of the input edge.</summary>
        public readonly bool Inverting;

        /// <summary>Creates a stage.</summary>
        public PathStage(string name, DelayNs rising, DelayNs falling, bool inverting)
        {
            Name = name;
            Rising = rising;
            Falling = falling;
            Inverting = inverting;
        }
    }

    /// <summary>
    /// The total delay of a path, as a range. When a datasheet gives no minimum for some stage,
    /// that stage counts as 0 ns in the minimum and <see cref="MinIsLowerBoundOnly"/> is set: the
    /// true minimum can only be larger. When any stage has no maximum, the total maximum cannot be
    /// known and <see cref="MaxNs"/> is null - it is never estimated from the typical figure.
    /// </summary>
    public readonly struct PathDelayResult
    {
        /// <summary>Shortest total delay, ns.</summary>
        public readonly double MinNs;

        /// <summary>Longest total delay, ns, or null if some stage has no documented maximum.</summary>
        public readonly double? MaxNs;

        /// <summary>Sum of the typical figures, or null if some stage has none.</summary>
        public readonly double? TypNs;

        /// <summary>True if one or more stages had no documented minimum (counted as 0 ns).</summary>
        public readonly bool MinIsLowerBoundOnly;

        /// <summary>Direction of the edge at the end of the path.</summary>
        public readonly OutputEdge FinalEdge;

        /// <summary>Creates a result.</summary>
        public PathDelayResult(double minNs, double? typNs, double? maxNs, bool minIsLowerBoundOnly, OutputEdge finalEdge)
        {
            MinNs = minNs;
            TypNs = typNs;
            MaxNs = maxNs;
            MinIsLowerBoundOnly = minIsLowerBoundOnly;
            FinalEdge = finalEdge;
        }
    }

    /// <summary>Adds up the documented delays along a chain of parts.</summary>
    public static class PathDelay
    {
        /// <summary>
        /// Total delay from the first stage's input to the last stage's output, for an input edge
        /// in the given direction. Each stage uses the figure for the edge its OUTPUT makes, which
        /// flips at every inverting stage - so tPLH and tPHL alternate through a chain of
        /// inverters, as they do on the board.
        /// </summary>
        public static PathDelayResult Calculate(ReadOnlySpan<PathStage> stages, OutputEdge inputEdge)
        {
            OutputEdge edge = inputEdge;
            double min = 0;
            double typ = 0;
            double max = 0;
            bool typKnown = true;
            bool maxKnown = true;
            bool minPartial = false;
            for (int i = 0; i < stages.Length; i++)
            {
                if (stages[i].Inverting)
                    edge = edge == OutputEdge.LowToHigh ? OutputEdge.HighToLow : OutputEdge.LowToHigh;
                DelayNs d = edge == OutputEdge.LowToHigh ? stages[i].Rising : stages[i].Falling;
                if (d.Min.HasValue)
                    min += d.Min.Value;
                else
                    minPartial = true;
                if (d.Typ.HasValue)
                    typ += d.Typ.Value;
                else
                    typKnown = false;
                if (d.Max.HasValue)
                    max += d.Max.Value;
                else
                    maxKnown = false;
            }
            return new PathDelayResult(min, typKnown ? typ : null, maxKnown ? max : null, minPartial, edge);
        }

        /// <summary>
        /// Both directions at once: the fastest and slowest the path can be for either input edge.
        /// </summary>
        public static (PathDelayResult InputRising, PathDelayResult InputFalling) CalculateBoth(ReadOnlySpan<PathStage> stages)
        {
            return (Calculate(stages, OutputEdge.LowToHigh), Calculate(stages, OutputEdge.HighToLow));
        }
    }

    /// <summary>Reads the "timing" list of a chip file (catalog/chips/&lt;id&gt;.json).</summary>
    public static class ChipTimingReader
    {
        /// <summary>Every timing path in the chip file.</summary>
        public static List<TimingArc> Read(string chipFilePath)
        {
            List<TimingArc> result = new();
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(chipFilePath));
            if (!doc.RootElement.TryGetProperty("timing", out JsonElement timing) || timing.ValueKind != JsonValueKind.Array)
                return result;
            for (int i = 0; i < timing.GetArrayLength(); i++)
            {
                JsonElement t = timing[i];
                string edgeText = Text(t, "edge");
                OutputEdge edge = edgeText switch
                {
                    "PLH" => OutputEdge.LowToHigh,
                    "PHL" => OutputEdge.HighToLow,
                    "PZH" => OutputEdge.FloatToHigh,
                    "PZL" => OutputEdge.FloatToLow,
                    "PHZ" => OutputEdge.HighToFloat,
                    "PLZ" => OutputEdge.LowToFloat,
                    "PZX" => OutputEdge.FloatToDriven,
                    "PXZ" => OutputEdge.DrivenToFloat,
                    "access" => OutputEdge.Access,
                    "recovery" => OutputEdge.Recovery,
                    "rise" => OutputEdge.Rise,
                    "fall" => OutputEdge.Fall,
                    "turnOn" => OutputEdge.TurnOn,
                    "turnOff" => OutputEdge.TurnOff,
                    "response" => OutputEdge.Response,
                    _ => throw new InvalidDataException($"{chipFilePath}: timing[{i}] edge is not one of the list in catalog/chips/FORMAT.md: '{edgeText}'"),
                };
                result.Add(new TimingArc(Text(t, "from"), Text(t, "to"), edge,
                    new DelayNs(Number(t, "minNs"), Number(t, "typNs"), Number(t, "maxNs")), Text(t, "conditions")));
            }
            return result;
        }

        /// <summary>
        /// A path stage for one input-to-output path of a chip, from its two timing entries
        /// (tPLH and tPHL). Throws if the chip file does not list that path.
        /// </summary>
        public static PathStage Stage(List<TimingArc> arcs, string name, string from, string to, bool inverting)
        {
            DelayNs? rising = null;
            DelayNs? falling = null;
            for (int i = 0; i < arcs.Count; i++)
            {
                if (arcs[i].From != from || arcs[i].To != to)
                    continue;
                // only the two data edges make a path stage; the three-state enable and disable
                // times (PZH, PZL, PHZ, PLZ) are a different kind of path
                if (arcs[i].Edge == OutputEdge.LowToHigh)
                    rising = arcs[i].Delay;
                else if (arcs[i].Edge == OutputEdge.HighToLow)
                    falling = arcs[i].Delay;
            }
            if (!rising.HasValue || !falling.HasValue)
                throw new InvalidDataException($"{name}: no tPLH and tPHL listed for {from} -> {to}");
            return new PathStage(name, rising.Value, falling.Value, inverting);
        }

        private static string Text(JsonElement e, string name)
        {
            return e.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : "";
        }

        private static double? Number(JsonElement e, string name)
        {
            return e.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : null;
        }
    }
}
