//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.Text.Json;

namespace RetroSchematic.Model
{
    /// <summary>
    /// A pin and the level at which it acts (an async set or clear, one output-enable pin).
    /// </summary>
    public sealed class ActivePin
    {
        /// <summary>
        /// The pin name as the chip file's pin list writes it.
        /// </summary>
        public string Pin { get; init; } = "";

        /// <summary>
        /// "high" or "low": the level at which the pin acts.
        /// </summary>
        public string Active { get; init; } = "";
    }

    /// <summary>
    /// What makes a stored bit take its next value.
    /// </summary>
    public sealed class ClockRule
    {
        /// <summary>
        /// The clock pin, or empty when <see cref="When"/> is used instead.
        /// </summary>
        public string Pin { get; init; } = "";

        /// <summary>
        /// A formula expression (no "target =") whose edge or level is the clock, for parts
        /// whose trigger is a combination of pins (a one-shot's A, B and clear). Empty when
        /// <see cref="Pin"/> is used.
        /// </summary>
        public string When { get; init; } = "";

        /// <summary>
        /// "rising" (L->H), "falling" (H->L), "level-high" (follows while high) or "level-low"
        /// (follows while low).
        /// </summary>
        public string Edge { get; init; } = "";
    }

    /// <summary>
    /// A rule that forces a stored bit at once, without the clock, while a condition holds
    /// (a counter's asynchronous load, a reset that needs two pins high).
    /// </summary>
    public sealed class AsyncRule
    {
        /// <summary>
        /// The condition, a formula expression; the rule acts while it is 1.
        /// </summary>
        public string When { get; init; } = "";

        /// <summary>
        /// The value the bit takes, a formula expression (often a pin, 0 or 1).
        /// </summary>
        public string Value { get; init; } = "";
    }

    /// <summary>
    /// One stored bit: a flip-flop, a latch, a counter stage or a one-shot.
    /// </summary>
    public sealed class StoredBit
    {
        /// <summary>
        /// The bit's name. Output formulas may use it like a pin name (1Q = FF1).
        /// </summary>
        public string Name { get; init; } = "";

        /// <summary>
        /// "flip-flop", "latch", "counter" or "one-shot".
        /// </summary>
        public string Kind { get; init; } = "";

        /// <summary>
        /// The value it takes at the clock, as a formula with the bit's own name as target
        /// (FF1 = 1D, QB = QB XOR QA). Empty for a one-shot.
        /// </summary>
        public string Next { get; init; } = "";

        /// <summary>
        /// When it takes the value.
        /// </summary>
        public ClockRule? Clock { get; init; }

        /// <summary>
        /// The pin that forces it to 1 at once, without the clock, or null.
        /// </summary>
        public ActivePin? Set { get; init; }

        /// <summary>
        /// The pin that forces it to 0 at once, without the clock, or null.
        /// </summary>
        public ActivePin? Clear { get; init; }

        /// <summary>
        /// What happens when set and clear act together: "set wins", "clear wins", or the
        /// datasheet's words when neither wins (7474: both outputs high, not stable).
        /// </summary>
        public string WhenSetAndClear { get; init; } = "";

        /// <summary>
        /// Rules that force the bit at once while their condition holds, the first that acts
        /// wins. Used instead of <see cref="Set"/> and <see cref="Clear"/> when the condition
        /// is more than one pin.
        /// </summary>
        public List<AsyncRule> Async { get; } = new();

        /// <summary>
        /// For a one-shot: how long the pulse lasts, in the datasheet's words.
        /// </summary>
        public string PulseWidth { get; init; } = "";

        /// <summary>
        /// For a one-shot: true when a new trigger during the pulse starts it again.
        /// </summary>
        public bool Retriggerable { get; init; }

        /// <summary>
        /// Where in the datasheet this is stated.
        /// </summary>
        public string Source { get; init; } = "";
    }

    /// <summary>
    /// A named step between inputs and outputs (a carry inside an adder). Other formulas may use
    /// its name like a pin name.
    /// </summary>
    public sealed class InternalTerm
    {
        /// <summary>
        /// The formula, with the term's name as target.
        /// </summary>
        public string Formula { get; init; } = "";

        /// <summary>
        /// Where in the datasheet this is stated.
        /// </summary>
        public string Source { get; init; } = "";
    }

    /// <summary>
    /// A logic value the chip makes from analog pin levels (a line receiver's threshold, a
    /// differential input). Formulas may use its name like a pin name; the simulator gives it a
    /// value from the analog levels by the rule in <see cref="Meaning"/>.
    /// </summary>
    public sealed class AnalogCondition
    {
        /// <summary>
        /// The name formulas use.
        /// </summary>
        public string Name { get; init; } = "";

        /// <summary>
        /// When it is 1 and when it is 0, in the datasheet's words and figures.
        /// </summary>
        public string Meaning { get; init; } = "";

        /// <summary>
        /// Where in the datasheet this is stated.
        /// </summary>
        public string Source { get; init; } = "";
    }

    /// <summary>
    /// When an output drives and what it does when it does not.
    /// </summary>
    public sealed class OutputEnable
    {
        /// <summary>
        /// The pins that must all be at their active level for the output to drive.
        /// </summary>
        public List<ActivePin> Pins { get; } = new();

        /// <summary>
        /// A formula expression (no "target =") that must also be 1 for the output to drive, or
        /// empty. Used when the enable is not a pin but a stored bit or a mix of pins and stored
        /// bits: a RAM whose chip select is latched at the clock edge drives only while
        /// <c>CE AND NOT LCS</c>.
        /// </summary>
        public string When { get; init; } = "";

        /// <summary>
        /// "Z" (floats), "0" or "1": what the output does while not enabled.
        /// </summary>
        public string Disabled { get; init; } = "";
    }

    /// <summary>
    /// One output pin's behaviour.
    /// </summary>
    public sealed class BehaviourOutput
    {
        /// <summary>
        /// The output pin name.
        /// </summary>
        public string Pin { get; init; } = "";

        /// <summary>
        /// Its value while enabled, as a formula with the pin as target.
        /// </summary>
        public string Formula { get; init; } = "";

        /// <summary>
        /// "totem-pole", "open-collector" or "three-state" (and the chip file's other output
        /// types: "open-collector-high-voltage", "open-emitter").
        /// </summary>
        public string Type { get; init; } = "";

        /// <summary>
        /// When it drives, or null when it always drives.
        /// </summary>
        public OutputEnable? OutputEnable { get; init; }

        /// <summary>
        /// Where in the datasheet this is stated.
        /// </summary>
        public string Source { get; init; } = "";
    }

    /// <summary>
    /// A memory array: words of bits picked by address pins.
    /// </summary>
    public sealed class MemoryArray
    {
        /// <summary>
        /// The array's name. Output formulas read the addressed word's bit n as Name + n
        /// (MEM1 .. MEM4), counting from 1.
        /// </summary>
        public string Name { get; init; } = "";

        /// <summary>
        /// Number of words.
        /// </summary>
        public int Words { get; init; }

        /// <summary>
        /// Bits per word.
        /// </summary>
        public int Width { get; init; }

        /// <summary>
        /// The read address, least significant first: pins, or the names of stored bits when the
        /// chip latches its address (2107B at the rise of CE; MK4096 row and column at RAS and CAS).
        /// </summary>
        public List<string> Address { get; } = new();

        /// <summary>
        /// The write address pins, least significant first, when they differ from the read
        /// address (74170); empty when the same pins are used.
        /// </summary>
        public List<string> WriteAddress { get; } = new();

        /// <summary>
        /// The data input pins, bit 1 first.
        /// </summary>
        public List<string> DataIn { get; } = new();

        /// <summary>
        /// When a word is written: a formula expression and "level-high" (written while it is 1).
        /// </summary>
        public ClockRule? Write { get; init; }

        /// <summary>
        /// Where in the datasheet this is stated.
        /// </summary>
        public string Source { get; init; } = "";
    }

    /// <summary>
    /// The structured behaviour of a chip, the <c>behaviour</c> object of a chip file: what
    /// every output does, from which stored bits, clocked how. Every formula in it is read by
    /// <see cref="ChipFormula"/>; the names of stored bits, internal terms, analog conditions and
    /// memory bits may be used like pin names.
    /// </summary>
    public sealed class ChipBehaviour
    {
        /// <summary>
        /// "logic" when the behaviour is given, "none" when the datasheet gives none.
        /// </summary>
        public string Kind { get; init; } = "";

        /// <summary>
        /// Why there is none (only for "none").
        /// </summary>
        public string Reason { get; init; } = "";

        /// <summary>
        /// The datasheet pages the behaviour as a whole comes from.
        /// </summary>
        public string Source { get; init; } = "";

        /// <summary>
        /// The stored bits.
        /// </summary>
        public List<StoredBit> Stored { get; } = new();

        /// <summary>
        /// Named steps between inputs and outputs.
        /// </summary>
        public List<InternalTerm> Internal { get; } = new();

        /// <summary>
        /// Logic values made from analog levels.
        /// </summary>
        public List<AnalogCondition> Conditions { get; } = new();

        /// <summary>
        /// The memory array, or null.
        /// </summary>
        public MemoryArray? Memory { get; init; }

        /// <summary>
        /// Every output pin's behaviour.
        /// </summary>
        public List<BehaviourOutput> Outputs { get; } = new();

        /// <summary>
        /// Reads the <c>behaviour</c> object of a chip file.
        /// </summary>
        /// <param name="e">
        /// The object.
        /// </param>
        /// <returns>
        /// The behaviour.
        /// </returns>
        public static ChipBehaviour Parse(JsonElement e)
        {
            ChipBehaviour b = new()
            {
                Kind = Str(e, "kind"),
                Reason = Str(e, "reason"),
                Source = Str(e, "source"),
                Memory = e.TryGetProperty("memory", out JsonElement m) && m.ValueKind == JsonValueKind.Object ? ParseMemory(m) : null,
            };
            if (e.TryGetProperty("stored", out JsonElement st) && st.ValueKind == JsonValueKind.Array)
            {
                for (int i = 0; i < st.GetArrayLength(); i++)
                {
                    JsonElement s = st[i];
                    StoredBit bit = new()
                    {
                        Name = Str(s, "name"),
                        Kind = Str(s, "kind"),
                        Next = Str(s, "next"),
                        Clock = s.TryGetProperty("clock", out JsonElement c) && c.ValueKind == JsonValueKind.Object ? ParseClock(c) : null,
                        Set = s.TryGetProperty("set", out JsonElement se) && se.ValueKind == JsonValueKind.Object ? ParseActive(se) : null,
                        Clear = s.TryGetProperty("clear", out JsonElement cl) && cl.ValueKind == JsonValueKind.Object ? ParseActive(cl) : null,
                        WhenSetAndClear = Str(s, "whenSetAndClear"),
                        PulseWidth = Str(s, "pulseWidth"),
                        Retriggerable = s.TryGetProperty("retriggerable", out JsonElement r) && r.ValueKind == JsonValueKind.True,
                        Source = Str(s, "source"),
                    };
                    if (s.TryGetProperty("async", out JsonElement asy) && asy.ValueKind == JsonValueKind.Array)
                    {
                        for (int k = 0; k < asy.GetArrayLength(); k++)
                            bit.Async.Add(new AsyncRule { When = Str(asy[k], "when"), Value = Str(asy[k], "value") });
                    }
                    b.Stored.Add(bit);
                }
            }
            if (e.TryGetProperty("internal", out JsonElement it) && it.ValueKind == JsonValueKind.Array)
            {
                for (int i = 0; i < it.GetArrayLength(); i++)
                    b.Internal.Add(new InternalTerm { Formula = Str(it[i], "formula"), Source = Str(it[i], "source") });
            }
            if (e.TryGetProperty("conditions", out JsonElement co) && co.ValueKind == JsonValueKind.Array)
            {
                for (int i = 0; i < co.GetArrayLength(); i++)
                    b.Conditions.Add(new AnalogCondition { Name = Str(co[i], "name"), Meaning = Str(co[i], "meaning"), Source = Str(co[i], "source") });
            }
            if (e.TryGetProperty("outputs", out JsonElement os) && os.ValueKind == JsonValueKind.Array)
            {
                for (int i = 0; i < os.GetArrayLength(); i++)
                {
                    JsonElement o = os[i];
                    OutputEnable? oe = null;
                    if (o.TryGetProperty("outputEnable", out JsonElement en) && en.ValueKind == JsonValueKind.Object)
                    {
                        oe = new OutputEnable { Disabled = Str(en, "disabled"), When = Str(en, "when") };
                        if (en.TryGetProperty("pins", out JsonElement ps) && ps.ValueKind == JsonValueKind.Array)
                        {
                            for (int k = 0; k < ps.GetArrayLength(); k++)
                                oe.Pins.Add(ParseActive(ps[k]));
                        }
                    }
                    b.Outputs.Add(new BehaviourOutput
                    {
                        Pin = Str(o, "pin"),
                        Formula = Str(o, "formula"),
                        Type = Str(o, "type"),
                        OutputEnable = oe,
                        Source = Str(o, "source"),
                    });
                }
            }
            return b;
        }

        /// <summary>
        /// Every name a formula of this chip may use: the pins, then the stored bits, internal
        /// terms, analog conditions and memory bits.
        /// </summary>
        /// <param name="pinNames">
        /// The chip's pin names.
        /// </param>
        /// <returns>
        /// The names.
        /// </returns>
        public List<string> Names(IReadOnlyList<string> pinNames)
        {
            List<string> names = new(pinNames.Count + 16);
            for (int i = 0; i < pinNames.Count; i++)
                names.Add(pinNames[i]);
            for (int i = 0; i < Stored.Count; i++)
                names.Add(Stored[i].Name);
            for (int i = 0; i < Internal.Count; i++)
            {
                string f = Internal[i].Formula;
                int eq = f.IndexOf(" = ", StringComparison.Ordinal);
                if (eq > 0)
                    names.Add(f.Substring(0, eq).Trim());
            }
            for (int i = 0; i < Conditions.Count; i++)
                names.Add(Conditions[i].Name);
            if (Memory != null)
            {
                for (int i = 1; i <= Memory.Width; i++)
                    names.Add(Memory.Name + i);
            }
            return names;
        }

        /// <summary>
        /// Checks the behaviour against the chip's pins: every formula is read, every pin named
        /// exists, every edge and level is one of the allowed words, every output pin of the
        /// chip has an entry, and every fact has a source.
        /// </summary>
        /// <param name="pinNames">
        /// The chip's pin names.
        /// </param>
        /// <param name="outputPins">
        /// The names of the chip's pins whose role is "output".
        /// </param>
        /// <returns>
        /// One line per problem; empty when it is all right.
        /// </returns>
        public List<string> Check(IReadOnlyList<string> pinNames, IReadOnlyList<string> outputPins)
        {
            List<string> errors = new();
            if (Kind == "none")
            {
                if (Reason.Length == 0)
                    errors.Add("behaviour \"none\" without a reason");
                return errors;
            }
            if (Kind != "logic")
            {
                errors.Add("behaviour kind \"" + Kind + "\" is not \"logic\" or \"none\"");
                return errors;
            }
            if (Source.Length == 0)
                errors.Add("behaviour without a source");
            List<string> names = Names(pinNames);
            // a stored bit, step, condition or memory bit named like a pin would be read as the pin
            for (int i = pinNames.Count; i < names.Count; i++)
            {
                if (pinNames.IndexOf(names[i]) >= 0)
                    errors.Add("the name " + names[i] + " is also a pin name");
            }
            for (int i = 0; i < Internal.Count; i++)
                CheckFormula(Internal[i].Formula, names, null, errors);
            for (int i = 0; i < Stored.Count; i++)
            {
                StoredBit s = Stored[i];
                if (s.Kind is not ("flip-flop" or "latch" or "counter" or "one-shot"))
                    errors.Add(s.Name + ": kind \"" + s.Kind + "\" is not flip-flop, latch, counter or one-shot");
                if (s.Kind == "one-shot")
                {
                    if (s.PulseWidth.Length == 0)
                        errors.Add(s.Name + ": a one-shot without a pulse width");
                }
                else
                {
                    CheckFormula(s.Next, names, s.Name, errors);
                }
                if (s.Clock == null && s.Kind != "latch")
                    errors.Add(s.Name + ": no clock");
                if (s.Clock != null)
                    CheckClock(s.Name, s.Clock, pinNames, names, errors);
                CheckActive(s.Name + " set", s.Set, pinNames, errors);
                CheckActive(s.Name + " clear", s.Clear, pinNames, errors);
                for (int k = 0; k < s.Async.Count; k++)
                {
                    CheckExpression(s.Name + " async when", s.Async[k].When, names, errors);
                    CheckExpression(s.Name + " async value", s.Async[k].Value, names, errors);
                }
                if (s.Async.Count > 0 && (s.Set != null || s.Clear != null))
                    errors.Add(s.Name + ": both async rules and set/clear; use one of the two");
                if (s.Set != null && s.Clear != null && s.WhenSetAndClear.Length == 0)
                    errors.Add(s.Name + ": set and clear, but not what happens when both act");
            }
            if (Memory != null)
            {
                MemoryArray m = Memory;
                if (m.Words < 1 || m.Width < 1)
                    errors.Add("memory without words or width");
                if (m.Address.Count == 0 || (1 << m.Address.Count) != m.Words)
                    errors.Add("memory: " + m.Address.Count + " address pins do not give " + m.Words + " words");
                if (m.DataIn.Count != 0 && m.DataIn.Count != m.Width)
                    errors.Add("memory: " + m.DataIn.Count + " data inputs for " + m.Width + " bits");
                // an address latched on the chip (2107B: at the rise of CE; MK4096: row at RAS,
                // column at CAS) is written as the stored bits that hold it, so a stored bit
                // name is allowed here as well as a pin
                List<string> pinsAndStored = new(pinNames.Count + Stored.Count);
                for (int i = 0; i < pinNames.Count; i++)
                    pinsAndStored.Add(pinNames[i]);
                for (int i = 0; i < Stored.Count; i++)
                    pinsAndStored.Add(Stored[i].Name);
                CheckPins("memory address", m.Address, pinsAndStored, errors);
                CheckPins("memory write address", m.WriteAddress, pinsAndStored, errors);
                CheckPins("memory data in", m.DataIn, pinsAndStored, errors);
                if (m.Write != null)
                    CheckClock("memory write", m.Write, pinNames, names, errors);
                if (m.Source.Length == 0)
                    errors.Add("memory without a source");
            }
            for (int i = 0; i < Conditions.Count; i++)
            {
                if (Conditions[i].Meaning.Length == 0 || Conditions[i].Source.Length == 0)
                    errors.Add(Conditions[i].Name + ": an analog condition needs a meaning and a source");
            }
            for (int i = 0; i < Outputs.Count; i++)
            {
                BehaviourOutput o = Outputs[i];
                if (pinNames.IndexOf(o.Pin) < 0)
                    errors.Add("output " + o.Pin + " is not a pin of the chip");
                CheckFormula(o.Formula, names, o.Pin, errors);
                if (o.Type is not ("totem-pole" or "open-collector" or "open-collector-high-voltage" or "three-state" or "open-emitter"))
                    errors.Add(o.Pin + ": output type \"" + o.Type + "\" is not known");
                if (o.OutputEnable != null)
                {
                    if (o.OutputEnable.Disabled is not ("Z" or "0" or "1"))
                        errors.Add(o.Pin + ": disabled output \"" + o.OutputEnable.Disabled + "\" is not Z, 0 or 1");
                    if (o.OutputEnable.Pins.Count == 0 && o.OutputEnable.When.Length == 0)
                        errors.Add(o.Pin + ": output enable without a pin or an expression");
                    if (o.OutputEnable.When.Length > 0)
                        CheckExpression(o.Pin + " output enable", o.OutputEnable.When, names, errors);
                    for (int k = 0; k < o.OutputEnable.Pins.Count; k++)
                        CheckActive(o.Pin + " output enable", o.OutputEnable.Pins[k], pinNames, errors);
                }
                if (o.Type == "three-state" && o.OutputEnable == null)
                    errors.Add(o.Pin + ": three-state output without an output enable");
            }
            for (int i = 0; i < outputPins.Count; i++)
            {
                bool found = false;
                for (int k = 0; k < Outputs.Count; k++)
                    found |= Outputs[k].Pin == outputPins[i];
                if (!found)
                    errors.Add("output pin " + outputPins[i] + " has no behaviour");
            }
            return errors;
        }

        private static void CheckFormula(string formula, List<string> names, string? target, List<string> errors)
        {
            if (!ChipFormula.TryParse(formula, names, out ChipFormula? f, out string? error))
            {
                errors.Add("\"" + formula + "\": " + error);
                return;
            }
            if (target != null && f!.Target != target)
                errors.Add("\"" + formula + "\": the target is not " + target);
        }

        private static void CheckClock(string who, ClockRule c, IReadOnlyList<string> pinNames, List<string> names, List<string> errors)
        {
            if (c.Edge is not ("rising" or "falling" or "level-high" or "level-low"))
                errors.Add(who + ": clock edge \"" + c.Edge + "\" is not rising, falling, level-high or level-low");
            if (c.Pin.Length > 0)
            {
                if (pinNames.IndexOf(c.Pin) < 0)
                    errors.Add(who + ": clock pin " + c.Pin + " is not a pin of the chip");
            }
            else if (c.When.Length > 0)
            {
                CheckExpression(who + ": clock", c.When, names, errors);
            }
            else
            {
                errors.Add(who + ": clock with neither a pin nor an expression");
            }
        }

        /// <summary>
        /// Reads an expression without a target, with a made-up target that is surely not a pin
        /// name.
        /// </summary>
        private static void CheckExpression(string who, string expression, List<string> names, List<string> errors)
        {
            List<string> withTarget = new(names) { "EXPRESSION-TARGET" };
            if (!ChipFormula.TryParse("EXPRESSION-TARGET = " + expression, withTarget, out _, out string? error))
                errors.Add(who + " \"" + expression + "\": " + error);
        }

        private static void CheckActive(string who, ActivePin? a, IReadOnlyList<string> pinNames, List<string> errors)
        {
            if (a == null)
                return;
            if (pinNames.IndexOf(a.Pin) < 0)
                errors.Add(who + ": " + a.Pin + " is not a pin of the chip");
            if (a.Active is not ("high" or "low"))
                errors.Add(who + ": active level \"" + a.Active + "\" is not high or low");
        }

        private static void CheckPins(string who, List<string> pins, IReadOnlyList<string> pinNames, List<string> errors)
        {
            for (int i = 0; i < pins.Count; i++)
            {
                if (pinNames.IndexOf(pins[i]) < 0)
                    errors.Add(who + ": " + pins[i] + " is not a pin or stored bit of the chip");
            }
        }

        private static MemoryArray ParseMemory(JsonElement m)
        {
            MemoryArray a = new()
            {
                Name = Str(m, "name"),
                Words = m.TryGetProperty("words", out JsonElement w) && w.ValueKind == JsonValueKind.Number ? w.GetInt32() : 0,
                Width = m.TryGetProperty("width", out JsonElement b) && b.ValueKind == JsonValueKind.Number ? b.GetInt32() : 0,
                Write = m.TryGetProperty("write", out JsonElement wr) && wr.ValueKind == JsonValueKind.Object ? ParseClock(wr) : null,
                Source = Str(m, "source"),
            };
            StrList(m, "address", a.Address);
            StrList(m, "writeAddress", a.WriteAddress);
            StrList(m, "dataIn", a.DataIn);
            return a;
        }

        private static ClockRule ParseClock(JsonElement c) => new() { Pin = Str(c, "pin"), When = Str(c, "when"), Edge = Str(c, "edge") };

        private static ActivePin ParseActive(JsonElement a) => new() { Pin = Str(a, "pin"), Active = Str(a, "active") };

        private static string Str(JsonElement e, string name) =>
            e.TryGetProperty(name, out JsonElement v) && v.ValueKind == JsonValueKind.String ? v.GetString()! : "";

        private static void StrList(JsonElement e, string name, List<string> into)
        {
            if (!e.TryGetProperty(name, out JsonElement v) || v.ValueKind != JsonValueKind.Array)
                return;
            for (int i = 0; i < v.GetArrayLength(); i++)
                into.Add(v[i].GetString() ?? "");
        }
    }

    /// <summary>
    /// Small list helpers that avoid LINQ.
    /// </summary>
    internal static class ListExtensions
    {
        /// <summary>
        /// The place of a string in a read-only list, or -1.
        /// </summary>
        /// <param name="list">
        /// The list.
        /// </param>
        /// <param name="value">
        /// The string to find.
        /// </param>
        /// <returns>
        /// The index, or -1.
        /// </returns>
        public static int IndexOf(this IReadOnlyList<string> list, string value)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i], value, StringComparison.Ordinal))
                    return i;
            }
            return -1;
        }
    }
}
