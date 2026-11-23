//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;

namespace RetroSchematic.Model
{
    /// <summary>
    /// What one step of a formula is.
    /// </summary>
    public enum FormulaKind
    {
        /// <summary>
        /// The level of a named pin.
        /// </summary>
        Pin,

        /// <summary>
        /// A fixed 0 or 1.
        /// </summary>
        Constant,

        /// <summary>
        /// The opposite of the one child.
        /// </summary>
        Not,

        /// <summary>
        /// 1 when every child is 1.
        /// </summary>
        And,

        /// <summary>
        /// 1 when any child is 1.
        /// </summary>
        Or,

        /// <summary>
        /// 1 when an odd number of children are 1.
        /// </summary>
        Xor,
    }

    /// <summary>
    /// One step of a read formula: a pin, a constant, or NOT / AND / OR / XOR of its children.
    /// </summary>
    public sealed class FormulaNode
    {
        /// <summary>
        /// What this step is.
        /// </summary>
        public FormulaKind Kind { get; init; }

        /// <summary>
        /// The pin name for <see cref="FormulaKind.Pin"/>, as the chip file's pin list writes it.
        /// </summary>
        public string Pin { get; init; } = "";

        /// <summary>
        /// The value for <see cref="FormulaKind.Constant"/>.
        /// </summary>
        public bool Value { get; init; }

        /// <summary>
        /// The parts it combines (one for NOT).
        /// </summary>
        public List<FormulaNode> Children { get; } = new();

        /// <summary>
        /// Works the step out from the level of each pin.
        /// </summary>
        /// <param name="levels">
        /// True for a high pin, by pin name. A pin missing from it is an error, never a quiet 0.
        /// </param>
        /// <returns>
        /// True for 1.
        /// </returns>
        public bool Evaluate(IReadOnlyDictionary<string, bool> levels)
        {
            switch (Kind)
            {
                case FormulaKind.Pin:
                    if (!levels.TryGetValue(Pin, out bool v))
                        throw new KeyNotFoundException("No level given for pin " + Pin + ".");
                    return v;
                case FormulaKind.Constant:
                    return Value;
                case FormulaKind.Not:
                    return !Children[0].Evaluate(levels);
                case FormulaKind.And:
                    for (int i = 0; i < Children.Count; i++)
                    {
                        if (!Children[i].Evaluate(levels))
                            return false;
                    }
                    return true;
                case FormulaKind.Or:
                    for (int i = 0; i < Children.Count; i++)
                    {
                        if (Children[i].Evaluate(levels))
                            return true;
                    }
                    return false;
                default:
                    bool odd = false;
                    for (int i = 0; i < Children.Count; i++)
                        odd ^= Children[i].Evaluate(levels);
                    return odd;
            }
        }

        /// <summary>
        /// Adds every pin name the step reads to a list, each once.
        /// </summary>
        /// <param name="into">
        /// The list to add to.
        /// </param>
        public void CollectPins(List<string> into)
        {
            if (Kind == FormulaKind.Pin)
            {
                if (!into.Contains(Pin))
                    into.Add(Pin);
                return;
            }
            for (int i = 0; i < Children.Count; i++)
                Children[i].CollectPins(into);
        }

        /// <summary>
        /// The step written out in full words, every NOT as NOT(...): 1Y = NOT(1A AND 1B).
        /// </summary>
        /// <returns>
        /// The text.
        /// </returns>
        public override string ToString()
        {
            switch (Kind)
            {
                case FormulaKind.Pin:
                    return Pin;
                case FormulaKind.Constant:
                    return Value ? "1" : "0";
                case FormulaKind.Not:
                    return "NOT(" + Children[0] + ")";
                default:
                    string op = Kind == FormulaKind.And ? " AND " : Kind == FormulaKind.Or ? " OR " : " XOR ";
                    string[] parts = new string[Children.Count];
                    for (int i = 0; i < parts.Length; i++)
                        parts[i] = Children[i].Kind is FormulaKind.Pin or FormulaKind.Constant or FormulaKind.Not ? Children[i].ToString() : "(" + Children[i] + ")";
                    return string.Join(op, parts);
            }
        }
    }

    /// <summary>
    /// A chip file formula read into steps: <c>target = expression</c>. It knows the chip's pin
    /// list, because a ' after a name means two different things: it is part of a pin name
    /// ("1Q'", "CLR'", "RIPPLE CLOCK'") and it also means NOT in a formula ("B'A'" is NOT B AND
    /// NOT A). The reader always takes the longest pin name that fits at each place, so "1Q'" is
    /// the pin 1Q' when the chip has such a pin, and NOT 1Q when it has only 1Q.
    /// </summary>
    /// <remarks>
    /// The grammar, loosest first:
    /// <list type="bullet">
    ///   <item><description><c>expr = term { OR term }</c></description></item>
    ///   <item><description><c>term = xorTerm { XOR xorTerm }</c></description></item>
    ///   <item><description><c>xorTerm = factor { [AND] factor }</c> - two factors side by side
    ///   (B'A) are ANDed, as in the datasheets' function lines</description></item>
    ///   <item><description><c>factor = NOT factor | ( expr ) | pin { ' } | 0 | 1</c> - each
    ///   trailing ' after a pin name is a NOT</description></item>
    /// </list>
    /// The target before " = " must be a pin name. Anything else (free text such as "1Q = FF1",
    /// "Y = MEM[address] when ...") is refused with the place and the reason; the reader never
    /// guesses.
    /// </remarks>
    public sealed class ChipFormula
    {
        private readonly string _text;
        private readonly string[] _pins;
        private int _at;

        private ChipFormula(string text, string[] pins)
        {
            _text = text;
            _pins = pins;
        }

        /// <summary>
        /// The output pin the formula is for.
        /// </summary>
        public string Target { get; private set; } = "";

        /// <summary>
        /// The expression.
        /// </summary>
        public FormulaNode Expression { get; private set; } = null!;

        /// <summary>
        /// Reads a formula against a chip's pin names.
        /// </summary>
        /// <param name="formula">
        /// The formula, such as <c>2Y1 = NOT(2G' AND 2C' AND B'A)</c>.
        /// </param>
        /// <param name="pinNames">
        /// Every pin name of the chip, as its file writes them.
        /// </param>
        /// <param name="result">
        /// The formula read, or null.
        /// </param>
        /// <param name="error">
        /// Why it could not be read (with the character position), or null.
        /// </param>
        /// <returns>
        /// True when the whole formula was read.
        /// </returns>
        public static bool TryParse(string formula, IReadOnlyList<string> pinNames, out ChipFormula? result, out string? error)
        {
            string[] pins = new string[pinNames.Count];
            for (int i = 0; i < pins.Length; i++)
                pins[i] = pinNames[i];
            // longest first, so the longest name that fits wins
            Array.Sort(pins, static (a, b) => b.Length.CompareTo(a.Length));
            ChipFormula f = new(formula, pins);
            result = null;
            error = null;
            int eq = formula.IndexOf(" = ", StringComparison.Ordinal);
            if (eq < 0)
            {
                error = "no \" = \" between the output and the expression";
                return false;
            }
            string target = formula.Substring(0, eq).Trim();
            if (Array.IndexOf(pins, target) < 0)
            {
                error = "the output \"" + target + "\" is not a pin name of the chip";
                return false;
            }
            f.Target = target;
            f._at = eq + 3;
            try
            {
                f.Expression = f.ReadOr();
                f.SkipSpaces();
                if (f._at < f._text.Length)
                    throw f.Fail("text left over");
            }
            catch (FormatException e)
            {
                error = e.Message;
                return false;
            }
            result = f;
            return true;
        }

        /// <summary>
        /// The formula written out in full words: <c>target = expression</c>.
        /// </summary>
        /// <returns>
        /// The text.
        /// </returns>
        public override string ToString() => Target + " = " + Expression;

        private FormulaNode ReadOr()
        {
            FormulaNode first = ReadXor();
            if (!PeekWord("OR"))
                return first;
            FormulaNode or = new() { Kind = FormulaKind.Or };
            or.Children.Add(first);
            while (TakeWord("OR"))
                or.Children.Add(ReadXor());
            return or;
        }

        private FormulaNode ReadXor()
        {
            FormulaNode first = ReadAnd();
            if (!PeekWord("XOR"))
                return first;
            FormulaNode xor = new() { Kind = FormulaKind.Xor };
            xor.Children.Add(first);
            while (TakeWord("XOR"))
                xor.Children.Add(ReadAnd());
            return xor;
        }

        private FormulaNode ReadAnd()
        {
            FormulaNode first = ReadFactor();
            FormulaNode? and = null;
            while (true)
            {
                bool word = TakeWord("AND");
                SkipSpaces();
                // side by side: another factor follows directly (B'A, RBRA')
                bool next = _at < _text.Length && !PeekWord("OR") && !PeekWord("XOR") && _text[_at] != ')';
                if (!word && !next)
                    break;
                if (and == null)
                {
                    and = new FormulaNode { Kind = FormulaKind.And };
                    and.Children.Add(first);
                }
                and.Children.Add(ReadFactor());
            }
            return and ?? first;
        }

        private FormulaNode ReadFactor()
        {
            SkipSpaces();
            if (_at >= _text.Length)
                throw Fail("the formula ends where a pin or ( was expected");
            if (TakeWord("NOT"))
            {
                FormulaNode not = new() { Kind = FormulaKind.Not };
                not.Children.Add(ReadFactor());
                return not;
            }
            if (_text[_at] == '(')
            {
                _at++;
                FormulaNode inner = ReadOr();
                SkipSpaces();
                if (_at >= _text.Length || _text[_at] != ')')
                    throw Fail("a ( is not closed");
                _at++;
                return Primes(inner);
            }
            string? pin = PinAt();
            if (pin != null)
            {
                _at += pin.Length;
                return Primes(new FormulaNode { Kind = FormulaKind.Pin, Pin = pin });
            }
            if (_text[_at] == '0' || _text[_at] == '1')
            {
                bool one = _text[_at] == '1';
                _at++;
                return Primes(new FormulaNode { Kind = FormulaKind.Constant, Value = one });
            }
            throw Fail("not a pin name of the chip");
        }

        /// <summary>
        /// Each ' right after a factor is a NOT round it.
        /// </summary>
        private FormulaNode Primes(FormulaNode node)
        {
            while (_at < _text.Length && _text[_at] == '\'')
            {
                _at++;
                FormulaNode not = new() { Kind = FormulaKind.Not };
                not.Children.Add(node);
                node = not;
            }
            return node;
        }

        /// <summary>
        /// The longest pin name that starts here and ends where a name may end (space, bracket, ',
        /// the end, or the start of another pin name written side by side). Null when none fits.
        /// </summary>
        private string? PinAt()
        {
            for (int i = 0; i < _pins.Length; i++)
            {
                string p = _pins[i];
                if (p.Length == 0 || string.CompareOrdinal(_text, _at, p, 0, p.Length) != 0)
                    continue;
                int end = _at + p.Length;
                if (end == _text.Length || _text[end] == ' ' || _text[end] == '(' || _text[end] == ')' || _text[end] == '\'')
                    return p;
                if (StartsPin(end))
                    return p;
            }
            return null;
        }

        /// <summary>
        /// True when some pin name starts at a place (a name written right after another).
        /// </summary>
        private bool StartsPin(int at)
        {
            for (int i = 0; i < _pins.Length; i++)
            {
                string p = _pins[i];
                if (p.Length > 0 && string.CompareOrdinal(_text, at, p, 0, p.Length) == 0)
                    return true;
            }
            return false;
        }

        private void SkipSpaces()
        {
            while (_at < _text.Length && _text[_at] == ' ')
                _at++;
        }

        /// <summary>
        /// True when the next word is the given keyword (a whole word, not the start of a pin
        /// name such as ORFT).
        /// </summary>
        private bool PeekWord(string word)
        {
            SkipSpaces();
            if (string.CompareOrdinal(_text, _at, word, 0, word.Length) != 0)
                return false;
            int end = _at + word.Length;
            if (end < _text.Length && _text[end] != ' ' && _text[end] != '(')
                return false;
            // a pin really called OR or NOT would win; none of the catalogue's chips has one
            return Array.IndexOf(_pins, word) < 0;
        }

        private bool TakeWord(string word)
        {
            if (!PeekWord(word))
                return false;
            _at += word.Length;
            return true;
        }

        private FormatException Fail(string why) => new(why + " at character " + (_at + 1) + " (\"" + _text.Substring(Math.Min(_at, _text.Length)) + "\")");
    }
}
