//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace RetroSchematic.Recognition;

/// <summary>
/// Finds the symbols on one scanned schematic page: chips and gates with their position codes
/// and chip type text, passive parts, circled pins, net-label diamonds and wire dots.
/// <para>
/// This is a port of the offline prototype (Python, <c>draft.py</c> run with the detector only:
/// <c>DETMODE=replace FIX6 FIX7 FIX10 FIX11</c>). The prototype's rule-based boxes
/// (<c>classic.py</c>) and its wire tracing are not part of it: wires are connected by a person.
/// Each step names the prototype step it follows.
/// </para>
/// </summary>
public sealed class PageAnalyser : IDisposable
{
    /// <summary>Letter size (pixels, the most common long side of a letter) the detector learned at.</summary>
    public const double TargetLetterSize = 20.0;

    private const double CodeFloor = -2.995732273553991; // log(0.05): weaker code readings are dropped
    private const int Bar = 60;                           // long-line bar length, classic.py L

    private static readonly string[] Look = { "O0", "I1", "S5", "Z2", "B8", "G6", "Q0", "D0" };

    private readonly LetterNet letters;
    private readonly SymbolDetector handDetector;
    private readonly SymbolDetector cadDetector;

    /// <summary>Loads the three networks from a folder.</summary>
    /// <param name="modelsDir">The folder with <c>det-L3.onnx</c>, <c>det-A.onnx</c> and <c>letters-A.onnx</c>.</param>
    public PageAnalyser(string modelsDir)
    {
        letters = new LetterNet(Path.Combine(modelsDir, "letters-A.onnx"));
        handDetector = new SymbolDetector(Path.Combine(modelsDir, "det-L3.onnx"));
        cadDetector = new SymbolDetector(Path.Combine(modelsDir, "det-A.onnx"));
    }

    /// <summary>Gets the models folder beside the running program (<c>models/</c>).</summary>
    public static string DefaultModelsDir => Path.Combine(AppContext.BaseDirectory, "models");

    /// <summary>Gets or sets the lowest detector heat kept (prototype: 0.3).</summary>
    public double Threshold { get; set; } = 0.3;

    /// <summary>Gets or sets a value indicating whether the page is turned upright first (prototype <c>rotate.py</c>).</summary>
    public bool FindTurn { get; set; } = true;

    /// <summary>Gets the letter network (for tests).</summary>
    internal LetterNet Letters => letters;

    /// <summary>Gets the hand-drawn detector (for tests).</summary>
    internal SymbolDetector HandDetector => handDetector;

    /// <summary>Gets the CAD detector (for tests).</summary>
    internal SymbolDetector CadDetector => cadDetector;

    /// <summary>Analyses one page.</summary>
    /// <param name="source">The page as stored (any scale; it is brought to the detector's scale).</param>
    /// <param name="board">The board grid codes are read against.</param>
    /// <param name="chipIds">Catalogue chip ids (<c>74157</c>, ...) for the type text; may be empty.</param>
    /// <param name="style">The page style, or null to let the page-style rule decide.</param>
    /// <returns>The finds, in source page pixels.</returns>
    public PageAnalysis Analyse(GreyImage source, BoardGrid board, IReadOnlyCollection<string> chipIds, PageStyle? style = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(chipIds);
        Stopwatch sw = Stopwatch.StartNew();
        PageAnalysis result = new PageAnalysis();

        // pageio.render: contrast stretch of a light scan; pageio.normalised: letters to the trained size
        GreyImage g = ImageOps.StretchContrast(source);
        double size = LetterSize(g);
        result.LetterSize = size;
        double f = 1.0;
        if (size > 0)
        {
            f = Math.Clamp(TargetLetterSize / size, 0.33, 3.0);

            // within a tenth of the trained size the page is read as it is: a resize then costs
            // more (softening) than the size difference
            if (Math.Abs(f - 1.0) <= 0.1)
            {
                f = 1.0;
            }
        }

        if (f != 1.0)
        {
            g = ImageOps.Scale(g, f);
        }

        result.Scale = f;
        long t0 = sw.ElapsedMilliseconds;
        int turn = FindTurn ? Turn(g) : 0;
        result.TurnMs = sw.ElapsedMilliseconds - t0;
        result.TurnCw = turn;
        GreyImage page = g.Rotate(turn);

        t0 = sw.ElapsedMilliseconds;
        PageWork w = new PageWork(page, letters, board);
        result.PiecesMs = sw.ElapsedMilliseconds - t0;
        PageStyle st = style ?? StyleOf(w);
        result.Style = st;
        SymbolDetector det = st == PageStyle.Cad ? cadDetector : handDetector;
        result.Detector = st == PageStyle.Cad ? "det-A" : "det-L3";
        t0 = sw.ElapsedMilliseconds;
        List<Detection> dets = det.Detect(page, Threshold);
        result.DetectMs = sw.ElapsedMilliseconds - t0;
        List<PageFind> finds = w.Symbols(dets, chipIds);
        result.Pieces = w.PieceCount;

        // back to source pixels: undo the turn, then the scale
        for (int i = 0; i < finds.Count; i++)
        {
            PageFind p = finds[i];
            Unturn(p, turn, g.Width, g.Height);
            if (f != 1.0)
            {
                int x0 = (int)Math.Round(p.X / f);
                int y0 = (int)Math.Round(p.Y / f);
                int x1 = (int)Math.Round((p.X + p.Width) / f);
                int y1 = (int)Math.Round((p.Y + p.Height) / f);
                p.X = x0;
                p.Y = y0;
                p.Width = x1 - x0;
                p.Height = y1 - y0;
            }

            p.Id = i + 1;
            result.Finds.Add(p);
        }

        result.Milliseconds = sw.ElapsedMilliseconds;
        return result;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        letters.Dispose();
        handDetector.Dispose();
        cadDetector.Dispose();
    }

    /// <summary>
    /// <c>pageio.letter_size</c>: the most common long side of the letter-like ink pieces, any
    /// direction, after smoothing the counts with [1, 2, 1]. 0 when fewer than 30 pieces qualify.
    /// </summary>
    /// <param name="g">The page.</param>
    /// <returns>The size, or 0.</returns>
    internal static double LetterSize(GreyImage g)
    {
        byte[] ink = ImageOps.OtsuInk(g, out _);
        List<Blob> blobs = ImageOps.Components(ink, g.Width, g.Height, out _);
        Span<int> hist = stackalloc int[81];
        hist.Clear();
        int count = 0;
        for (int i = 0; i < blobs.Count; i++)
        {
            Blob b = blobs[i];
            int big = Math.Max(b.W, b.H);
            int small = Math.Min(b.W, b.H);
            double fill = b.Area / (double)Math.Max(1, b.W * b.H);
            if (big >= 6 && big <= 80 && small >= 0.25 * big && fill > 0.12 && fill < 0.75)
            {
                hist[big]++;
                count++;
            }
        }

        if (count < 30)
        {
            return 0;
        }

        int best = 0;
        int bestV = int.MinValue;
        for (int i = 0; i <= 80; i++)
        {
            int v = (2 * hist[i]) + (i > 0 ? hist[i - 1] : 0) + (i < 80 ? hist[i + 1] : 0);
            if (v > bestV)
            {
                bestV = v;
                best = i;
            }
        }

        return best;
    }

    /// <summary>
    /// The page-style rule (prototype <c>pagestyle2.py</c>): spread of the letter heights and the
    /// median thickness of the long horizontal lines, one fitted straight-line rule.
    /// </summary>
    /// <param name="w">The page work (pieces and line masks).</param>
    /// <returns>The style.</returns>
    internal static PageStyle StyleOf(PageWork w)
    {
        (double spread, double thick) = w.StyleMeasures();
        double z = -5.834973685476028
            + (-6.459994666628151 * (spread - 0.13464971670995984) / 0.040134987347884725)
            + (3.300906536078031 * (thick - 2.8776717101421547) / 1.0921438756781727);
        return z > 0 ? PageStyle.Cad : PageStyle.Hand;
    }

    /// <summary>
    /// <c>rotate.py</c>: the turn (clockwise) that makes the text read upright. The text direction
    /// (letter pieces side by side or stacked) picks the axis; the letter network's mean top
    /// probability picks between the two turns on it. Where the prototype took a random sample
    /// of pieces (more than 3000 for the direction, 800 per turn), this takes every n-th piece.
    /// </summary>
    /// <param name="g">The page at the detector's scale.</param>
    /// <returns>0, 90, 180 or 270.</returns>
    internal int Turn(GreyImage g)
    {
        List<Blob> pcs0 = PageWork.LetterPieces(g);
        (int hp, int vp) = PairDirection(Every(pcs0, 3000));
        bool across = hp >= vp;
        int a = across ? 0 : 90;
        int b = across ? 180 : 270;
        // turn 0 is the page as it is, so its pieces are the ones just cut
        double ca = TurnConfidence(g.Rotate(a), a == 0 ? pcs0 : null);
        double cb = TurnConfidence(g.Rotate(b), null);
        return cb > ca ? b : a;
    }

    private static List<Blob> Every(List<Blob> all, int max)
    {
        if (all.Count <= max)
        {
            return all;
        }

        List<Blob> o = new List<Blob>(max);
        for (int i = 0; i < max; i++)
        {
            o.Add(all[(int)((long)i * all.Count / max)]);
        }

        return o;
    }

    private static (int Side, int Stacked) PairDirection(List<Blob> pcs)
    {
        if (pcs.Count < 10)
        {
            return (0, 0);
        }

        int hp = 0;
        int vp = 0;
        for (int i = 0; i < pcs.Count; i++)
        {
            Blob p = pcs[i];
            double cx = p.X + (p.W / 2.0);
            double cy = p.Y + (p.H / 2.0);
            double s = Math.Max(p.W, p.H);
            for (int j = 0; j < pcs.Count; j++)
            {
                Blob q = pcs[j];
                double dx = Math.Abs(q.X + (q.W / 2.0) - cx);
                double dy = Math.Abs(q.Y + (q.H / 2.0) - cy);
                if (dx > 0.3 * s && dx < 1.3 * s && dy < 0.25 * s)
                {
                    hp++;
                }

                if (dy > 0.3 * s && dy < 1.3 * s && dx < 0.25 * s)
                {
                    vp++;
                }
            }
        }

        return (hp, vp);
    }

    private double TurnConfidence(GreyImage g, List<Blob>? cut)
    {
        List<Blob> pcs = Every(cut ?? PageWork.LetterPieces(g), 800);
        if (pcs.Count == 0)
        {
            return 0;
        }

        List<(int X, int Y)> cs = new List<(int X, int Y)>(pcs.Count);
        for (int i = 0; i < pcs.Count; i++)
        {
            cs.Add((pcs[i].X + (pcs[i].W / 2), pcs[i].Y + (pcs[i].H / 2)));
        }

        float[] p = letters.Probabilities(g, cs);
        int nc = LetterNet.Classes.Length;
        double sum = 0;
        for (int i = 0; i < pcs.Count; i++)
        {
            float m = 0;
            for (int c = 0; c < nc; c++)
            {
                if (p[(i * nc) + c] > m)
                {
                    m = p[(i * nc) + c];
                }
            }

            sum += m;
        }

        return sum / pcs.Count;
    }

    private static void Unturn(PageFind p, int turn, int w, int h)
    {
        // corners of the box in the turned picture, back to the upright one (continuous pixels)
        double x0 = p.X;
        double y0 = p.Y;
        double x1 = p.X + p.Width;
        double y1 = p.Y + p.Height;
        double ax;
        double ay;
        double bx;
        double by;
        switch (turn)
        {
            case 90:
                // turned (x', y') = (h - y, x)  =>  x = y', y = h - x'
                ax = y0;
                bx = y1;
                ay = h - x1;
                by = h - x0;
                break;
            case 180:
                ax = w - x1;
                bx = w - x0;
                ay = h - y1;
                by = h - y0;
                break;
            case 270:
                // turned (x', y') = (y, w - x)  =>  x = w - y', y = x'
                ax = w - y1;
                bx = w - y0;
                ay = x0;
                by = x1;
                break;
            default:
                return;
        }

        p.X = (int)Math.Round(ax);
        p.Y = (int)Math.Round(ay);
        p.Width = (int)Math.Round(bx - ax);
        p.Height = (int)Math.Round(by - ay);
    }

    /// <summary>
    /// <c>draft.py</c>'s <c>lev</c> with its look-alike rule: an edit distance where a character
    /// and its look-alike (O/0, I/1, S/5, Z/2, B/8, G/6, Q/0, D/0) cost nothing.
    /// </summary>
    /// <param name="a">First text.</param>
    /// <param name="b">Second text.</param>
    /// <returns>The distance.</returns>
    internal static int Lev(string a, string b)
    {
        Span<int> p = stackalloc int[b.Length + 1];
        Span<int> q = stackalloc int[b.Length + 1];
        for (int j = 0; j <= b.Length; j++)
        {
            p[j] = j;
        }

        for (int i = 1; i <= a.Length; i++)
        {
            q[0] = i;
            char ca = a[i - 1];
            for (int j = 1; j <= b.Length; j++)
            {
                char cb = b[j - 1];
                int sub = ca == cb || LookAlike(ca) == LookAlike(cb) ? 0 : 1;
                q[j] = Math.Min(Math.Min(p[j] + 1, q[j - 1] + 1), p[j - 1] + sub);
            }

            q.CopyTo(p);
        }

        return p[b.Length];
    }

    /// <summary>
    /// <c>draft.py</c>'s <c>snap</c>: the one pool entry nearest to the text, when it is near enough
    /// (1 edit below 6 characters, else 2) and no other entry is as near. Otherwise the text.
    /// </summary>
    /// <param name="t">The text read.</param>
    /// <param name="pool">The names it may be.</param>
    /// <returns>The snapped text.</returns>
    internal static string Snap(string t, IReadOnlyCollection<string> pool)
    {
        if (t.Length < 2)
        {
            return t;
        }

        // sorted((lev, d)): the smallest distance, ties by name; taken only when no other entry
        // has the same distance
        int best = int.MaxValue;
        int atBest = 0;
        string? bestName = null;
        foreach (string d in pool)
        {
            if (d == t)
            {
                return t;
            }

            if (Math.Abs(d.Length - t.Length) > 1)
            {
                continue;
            }

            int l = Lev(t, d);
            if (l < best)
            {
                best = l;
                atBest = 1;
                bestName = d;
            }
            else if (l == best)
            {
                atBest++;
                if (string.CompareOrdinal(d, bestName) < 0)
                {
                    bestName = d;
                }
            }
        }

        if (bestName != null && best <= (t.Length < 6 ? 1 : 2) && atBest == 1)
        {
            return bestName;
        }

        return t;
    }

    private static char LookAlike(char c)
    {
        for (int i = 0; i < Look.Length; i++)
        {
            if (Look[i][0] == c)
            {
                return Look[i][1];
            }
        }

        return c;
    }

    /// <summary>The working state of one page: pieces, their letter probabilities, words.</summary>
    internal sealed class PageWork
    {
        private readonly GreyImage page;
        private readonly LetterNet net;
        private readonly int[] gridN;
        private readonly string gridL;
        private readonly byte[] ink;
        private readonly byte[] txt;
        private readonly byte[] hl;
        private readonly List<Piece> pieces = new List<Piece>();
        private readonly List<float> prob = new List<float>();
        private readonly List<int> bigp = new List<int>();
        private List<Word>? words;

        public PageWork(GreyImage page, LetterNet net, BoardGrid board)
        {
            this.page = page;
            this.net = net;

            // FIX9: the board grid of this board type
            if (board == BoardGrid.Nd100)
            {
                gridN = Range(1, 29);
                gridL = "ABCDEFGHJ";
            }
            else
            {
                gridN = Range(1, 19);
                gridL = "ABCDE";
            }

            int w = page.Width;
            int h = page.Height;
            ink = ImageOps.OtsuInk(page, out _);
            hl = ImageOps.OpenBar(ink, w, h, Bar, true);
            byte[] vl = ImageOps.OpenBar(ink, w, h, Bar, false);
            byte[] lines = new byte[ink.Length];
            for (int i = 0; i < lines.Length; i++)
            {
                lines[i] = (byte)(hl[i] | vl[i]);
            }

            byte[] grown = ImageOps.Dilate3(lines, w, h);
            txt = new byte[ink.Length];
            for (int i = 0; i < txt.Length; i++)
            {
                txt[i] = grown[i] == 0 ? ink[i] : (byte)0;
            }

            CutPieces();
        }

        public int PieceCount => pieces.Count;

        /// <summary>
        /// <c>pageio.pieces_of</c>: letter-sized pieces of ink (not long lines), without the
        /// splitting of joined letters. Used by the turn finder.
        /// </summary>
        public static List<Blob> LetterPieces(GreyImage g)
        {
            int w = g.Width;
            int h = g.Height;
            byte[] ink = ImageOps.OtsuInk(g, out _);
            byte[] hl = ImageOps.OpenBar(ink, w, h, Bar, true);
            byte[] vl = ImageOps.OpenBar(ink, w, h, Bar, false);
            for (int i = 0; i < hl.Length; i++)
            {
                hl[i] = (byte)(hl[i] | vl[i]);
            }

            byte[] grown = ImageOps.Dilate3(hl, w, h);
            for (int i = 0; i < ink.Length; i++)
            {
                if (grown[i] != 0)
                {
                    ink[i] = 0;
                }
            }

            List<Blob> all = ImageOps.Components(ink, w, h, out _);
            List<Blob> o = new List<Blob>();
            for (int i = 0; i < all.Count; i++)
            {
                Blob b = all[i];
                if (b.H >= 18 && b.H <= 38 && b.W >= 4 && b.W <= 40 && b.Area >= 40)
                {
                    o.Add(b);
                }
            }

            return o;
        }

        /// <summary>The two page-style measures (<c>pagefeat.py</c> features 0 and 3).</summary>
        public (double Spread, double Thickness) StyleMeasures()
        {
            List<Blob> all = ImageOps.Components(txt, page.Width, page.Height, out _);
            double sum = 0;
            double sq = 0;
            int n = 0;
            for (int i = 0; i < all.Count; i++)
            {
                Blob b = all[i];
                if (b.H >= 18 && b.H <= 38 && b.W >= 4 && b.W <= 40 && b.Area >= 40)
                {
                    sum += b.H;
                    sq += (double)b.H * b.H;
                    n++;
                }
            }

            double spread = 0;
            if (n > 0)
            {
                double mean = sum / n;
                spread = Math.Sqrt(Math.Max(0, (sq / n) - (mean * mean))) / mean;
            }

            List<Blob> lines = ImageOps.Components(hl, page.Width, page.Height, out _);
            List<double> t = new List<double>();
            for (int i = 0; i < lines.Count; i++)
            {
                if (lines[i].W >= 100)
                {
                    t.Add(lines[i].Area / (double)lines[i].W);
                }
            }

            double thick = 0;
            if (t.Count > 0)
            {
                t.Sort();
                int m = t.Count / 2;
                thick = t.Count % 2 == 1 ? t[m] : (t[m - 1] + t[m]) / 2;
            }

            return (spread, thick);
        }

        /// <summary>
        /// The detector part of <c>draft.py</c>: FIX6 (extra pieces in code boxes), the code reading
        /// (<c>det_code</c> with FIX10's sliding window), the chip type (<c>det_type</c>), one box per
        /// symbol (<c>_one</c>), FIX7 (pin-number cells are not gates) and FIX11 (a column's code).
        /// </summary>
        public List<PageFind> Symbols(List<Detection> dets, IReadOnlyCollection<string> chipIds)
        {
            List<double[]> codeBoxes = new List<double[]>();
            for (int i = 0; i < dets.Count; i++)
            {
                if (SymbolDetector.Classes[dets[i].Class] == "poscode")
                {
                    Detection d = dets[i];
                    codeBoxes.Add(new double[] { d.X, d.Y, d.W, d.H });
                }
            }

            AddCodeBoxPieces(codeBoxes);
            words = BuildWords();

            List<PageFind> chips = new List<PageFind>();
            List<PageFind> gates = new List<PageFind>();
            List<PageFind> rest = new List<PageFind>();
            for (int i = 0; i < dets.Count; i++)
            {
                Detection d = dets[i];
                string cls = SymbolDetector.Classes[d.Class];
                PageFind p = new PageFind { Kind = cls, X = d.X, Y = d.Y, Width = d.W, Height = d.H, Score = d.Score };
                if (cls == "chip")
                {
                    (string raw, string? typ) = TypeOf(p, chipIds);
                    p.TypeText = raw;
                    p.TypeId = typ;
                    SetCode(p, codeBoxes);
                    chips.Add(p);
                }
                else if (cls == "gate")
                {
                    string k = SymbolDetector.GateKinds[d.Kind];
                    p.GateKind = k == "aoi" ? "and-or-invert" : k;
                    SetCode(p, codeBoxes);
                    gates.Add(p);
                }
                else if (cls != "poscode" && cls != "pinnum" && cls != "label")
                {
                    rest.Add(p);
                }
            }

            chips = OnePerSymbol(chips);
            gates = OnePerSymbol(gates);

            // FIX7 (b): a code-less gate glued to the side of a big chip box, or inside it, is the
            // chip's pin-number column
            List<PageFind> bigc = new List<PageFind>();
            for (int i = 0; i < chips.Count; i++)
            {
                if (chips[i].Width >= 50 && chips[i].Height >= 90)
                {
                    bigc.Add(chips[i]);
                }
            }

            List<PageFind> keptGates = new List<PageFind>();
            for (int i = 0; i < gates.Count; i++)
            {
                if (gates[i].Code != null || !IsCell(gates[i], bigc))
                {
                    keptGates.Add(gates[i]);
                }
            }

            ColumnCodes(keptGates);
            List<PageFind> all = new List<PageFind>(chips.Count + keptGates.Count + rest.Count);
            all.AddRange(chips);
            all.AddRange(keptGates);
            all.AddRange(rest);
            return all;
        }

        private static bool IsCell(PageFind b, List<PageFind> bigc)
        {
            for (int i = 0; i < bigc.Count; i++)
            {
                PageFind o = bigc[i];
                int vo = Math.Min(b.Y + b.Height, o.Y + o.Height) - Math.Max(b.Y, o.Y);
                if (vo < 0.5 * b.Height)
                {
                    continue;
                }

                if (Math.Abs(b.X + b.Width - o.X) < 10 || Math.Abs(b.X - (o.X + o.Width)) < 10)
                {
                    return true;
                }

                if (o.X - 3 <= b.X && b.X + b.Width <= o.X + o.Width + 3)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// FIX11: a code-less gate in a column with a coded gate of the same kind (left edges within
        /// 12 px, widths within 30 %, gap under 2.5 gate heights) takes that code while the package
        /// has a free gate (4 per package, 6 for inverters and buffers). Repeated until nothing changes.
        /// </summary>
        private static void ColumnCodes(List<PageFind> gates)
        {
            Dictionary<string, int> count = new Dictionary<string, int>(StringComparer.Ordinal);
            for (int i = 0; i < gates.Count; i++)
            {
                string? r = gates[i].Code;
                if (r != null)
                {
                    count[r] = count.GetValueOrDefault(r) + 1;
                }
            }

            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int i = 0; i < gates.Count; i++)
                {
                    PageFind q = gates[i];
                    if (q.Code != null)
                    {
                        continue;
                    }

                    int slots = q.GateKind == "not" || q.GateKind == "buffer" ? 6 : 4;
                    double bestGap = double.MaxValue;
                    string? bestRef = null;
                    for (int j = 0; j < gates.Count; j++)
                    {
                        PageFind o = gates[j];
                        string? r = o.Code;
                        if (r == null || o.GateKind != q.GateKind || count.GetValueOrDefault(r) >= slots)
                        {
                            continue;
                        }

                        if (Math.Abs(o.X - q.X) > 12 || Math.Abs(o.Width - q.Width) > 0.3 * Math.Max(o.Width, q.Width))
                        {
                            continue;
                        }

                        int gap = Math.Max(o.Y - (q.Y + q.Height), q.Y - (o.Y + o.Height));
                        if (gap < 2.5 * Math.Max(q.Height, o.Height) && gap < bestGap)
                        {
                            bestGap = gap;
                            bestRef = r;
                        }
                    }

                    if (bestRef != null)
                    {
                        q.Code = bestRef;
                        q.CodeFromColumn = true;
                        count[bestRef] = count.GetValueOrDefault(bestRef) + 1;
                        changed = true;
                    }
                }
            }
        }

        /// <summary><c>_one</c>: biggest first; a box whose centre lies in a kept box is the same symbol.</summary>
        private static List<PageFind> OnePerSymbol(List<PageFind> l)
        {
            List<PageFind> sorted = new List<PageFind>(l);

            // a stable sort by area, biggest first (List.Sort is not stable, so the index breaks ties)
            int[] idx = new int[sorted.Count];
            for (int i = 0; i < idx.Length; i++)
            {
                idx[i] = i;
            }

            Array.Sort(idx, (a, b) =>
            {
                long aa = (long)l[a].Width * l[a].Height;
                long bb = (long)l[b].Width * l[b].Height;
                int c = bb.CompareTo(aa);
                return c != 0 ? c : a.CompareTo(b);
            });
            List<PageFind> kept = new List<PageFind>();
            for (int n = 0; n < idx.Length; n++)
            {
                PageFind q = l[idx[n]];
                double cx = q.X + (q.Width / 2.0);
                double cy = q.Y + (q.Height / 2.0);
                bool inside = false;
                for (int k = 0; k < kept.Count; k++)
                {
                    if (InBox(cx, cy, kept[k].X, kept[k].Y, kept[k].Width, kept[k].Height, 0))
                    {
                        inside = true;
                        break;
                    }
                }

                if (!inside)
                {
                    kept.Add(q);
                }
            }

            return kept;
        }

        private static bool InBox(double px, double py, double bx, double by, double bw, double bh, double m)
        {
            return bx - m <= px && px <= bx + bw + m && by - m <= py && py <= by + bh + m;
        }

        private static int[] Range(int a, int b)
        {
            int[] r = new int[b - a + 1];
            for (int i = 0; i < r.Length; i++)
            {
                r[i] = a + i;
            }

            return r;
        }

        // ------------------------------------------------------------------ letter pieces

        /// <summary>
        /// <c>draft.py</c> step 2: letter-sized ink pieces that are not long lines, joined letters
        /// split by <c>cut</c>, and small pieces (subscripts, dots); then the letter network on all.
        /// </summary>
        private void CutPieces()
        {
            int w = page.Width;
            List<Blob> blobs = ImageOps.Components(txt, w, page.Height, out int[] lab);
            for (int i = 0; i < blobs.Count; i++)
            {
                Blob b = blobs[i];
                if (b.H >= 18 && b.H <= 38 && b.W >= 4 && b.W <= 90 && b.Area >= 40)
                {
                    byte[] m = new byte[b.W * b.H];
                    for (int y = 0; y < b.H; y++)
                    {
                        int row = (b.Y + y) * w;
                        for (int x = 0; x < b.W; x++)
                        {
                            m[(y * b.W) + x] = lab[row + b.X + x] == i + 1 ? (byte)1 : (byte)0;
                        }
                    }

                    List<(int Ox, byte[] M, int Mw)> parts = b.W > 30 ? Cut(m, b.W, b.H, 0) : new List<(int, byte[], int)> { (0, m, b.W) };
                    AddParts(parts, b.X, b.Y, b.H);
                }
                else if (b.H >= 7 && b.H <= 16 && b.W >= 6 && b.W <= 16 && b.Area >= 25)
                {
                    pieces.Add(new Piece(b.X, b.Y, b.W, b.H, true));
                }
            }

            Classify(0);
        }

        private void AddParts(List<(int Ox, byte[] M, int Mw)> parts, int bx, int by, int h)
        {
            for (int p = 0; p < parts.Count; p++)
            {
                (int ox, byte[] m, int mw) = parts[p];
                int minX = int.MaxValue;
                int maxX = -1;
                int minY = int.MaxValue;
                int maxY = -1;
                for (int y = 0; y < h; y++)
                {
                    for (int x = 0; x < mw; x++)
                    {
                        if (m[(y * mw) + x] != 0)
                        {
                            minX = Math.Min(minX, x);
                            maxX = Math.Max(maxX, x);
                            minY = Math.Min(minY, y);
                            maxY = Math.Max(maxY, y);
                        }
                    }
                }

                if (maxX < 0)
                {
                    continue;
                }

                pieces.Add(new Piece(bx + ox + minX, by + minY, maxX - minX + 1, maxY - minY + 1, false));
            }
        }

        /// <summary>
        /// <c>cut</c> (split.py): a piece wider than 0.8 x its height is cut at the emptiest column of
        /// its middle half, the columns taken along a 0.2 slant (handwriting leans); only when that
        /// column holds at most 2 ink pixels and both sides have 15 or more. Up to 3 levels deep.
        /// </summary>
        private static List<(int Ox, byte[] M, int Mw)> Cut(byte[] m, int w, int h, int depth)
        {
            List<(int, byte[], int)> whole = new List<(int, byte[], int)> { (0, m, w) };
            if (w <= 0.8 * h || depth > 3)
            {
                return whole;
            }

            // xs, ys of the ink in row order (numpy nonzero)
            List<int> xs = new List<int>();
            List<int> ys = new List<int>();
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    if (m[(y * w) + x] != 0)
                    {
                        xs.Add(x);
                        ys.Add(y);
                    }
                }
            }

            if (xs.Count == 0)
            {
                return whole;
            }

            int[] xs2 = new int[xs.Count];
            int min = int.MaxValue;
            for (int i = 0; i < xs.Count; i++)
            {
                xs2[i] = (int)Math.Round(xs[i] + (0.2 * (ys[i] - (h / 2.0))), MidpointRounding.ToEven);
                min = Math.Min(min, xs2[i]);
            }

            int max = 0;
            for (int i = 0; i < xs2.Length; i++)
            {
                xs2[i] -= min;
                max = Math.Max(max, xs2[i]);
            }

            int[] prof = new int[max + 1];
            for (int i = 0; i < xs2.Length; i++)
            {
                prof[xs2[i]]++;
            }

            int lo = (int)(prof.Length * 0.25);
            int hi = (int)(prof.Length * 0.75);
            if (hi <= lo)
            {
                return whole;
            }

            int c = lo;
            for (int i = lo + 1; i < hi; i++)
            {
                if (prof[i] < prof[c])
                {
                    c = i;
                }
            }

            if (prof[c] > 2)
            {
                return whole;
            }

            List<(int, byte[], int)> outp = new List<(int, byte[], int)>();
            for (int side = 0; side < 2; side++)
            {
                int n = 0;
                int xmin = int.MaxValue;
                int xmax = -1;
                for (int i = 0; i < xs2.Length; i++)
                {
                    bool sel = side == 0 ? xs2[i] < c : xs2[i] > c;
                    if (sel)
                    {
                        n++;
                        xmin = Math.Min(xmin, xs[i]);
                        xmax = Math.Max(xmax, xs[i]);
                    }
                }

                if (n < 15)
                {
                    return whole;
                }

                int pw = xmax - xmin + 1;
                byte[] p = new byte[pw * h];
                for (int i = 0; i < xs2.Length; i++)
                {
                    bool sel = side == 0 ? xs2[i] < c : xs2[i] > c;
                    if (sel)
                    {
                        p[(ys[i] * pw) + xs[i] - xmin] = 1;
                    }
                }

                List<(int Ox, byte[] M, int Mw)> sub = Cut(p, pw, h, depth + 1);
                for (int k = 0; k < sub.Count; k++)
                {
                    outp.Add((xmin + sub[k].Ox, sub[k].M, sub[k].Mw));
                }
            }

            return outp;
        }

        /// <summary>Runs the letter network on the pieces from <paramref name="from"/> on.</summary>
        private void Classify(int from)
        {
            List<(int X, int Y)> cs = new List<(int X, int Y)>(pieces.Count - from);
            for (int k = from; k < pieces.Count; k++)
            {
                Piece p = pieces[k];
                cs.Add((p.X + (p.W / 2), p.Y + (p.H / 2)));
                if (!p.Small)
                {
                    bigp.Add(k);
                }
            }

            float[] pr = net.Probabilities(page, cs);
            prob.AddRange(pr);
        }

        /// <summary>
        /// FIX6: a code written on a box edge loses the strokes that touch the line and falls under
        /// the letter size limit. In each code box with no full-size piece, the ink pieces are taken
        /// again with a lower limit (12 px) and classified.
        /// </summary>
        private void AddCodeBoxPieces(List<double[]> boxes)
        {
            int w = page.Width;
            int h = page.Height;
            int before = pieces.Count;
            List<Piece> add = new List<Piece>();
            for (int i = 0; i < boxes.Count; i++)
            {
                double[] b = boxes[i];
                double x0 = b[0] - 4;
                double y0 = b[1] - 4;
                double x1 = b[0] + b[2] + 4;
                double y1 = b[1] + b[3] + 4;
                bool has = false;
                for (int n = 0; n < bigp.Count && !has; n++)
                {
                    Piece p = pieces[bigp[n]];
                    double cx = p.X + (p.W / 2.0);
                    double cy = p.Y + (p.H / 2.0);
                    has = x0 <= cx && cx <= x1 && y0 <= cy && cy <= y1;
                }

                if (has)
                {
                    continue;
                }

                // numpy slice txt[ya:int(y1), xa:int(x1)], clipped to the page
                int xa = Math.Max(0, (int)x0);
                int ya = Math.Max(0, (int)y0);
                int xb = Math.Min(w, (int)x1);
                int yb = Math.Min(h, (int)y1);
                int sw = xb - xa;
                int sh = yb - ya;
                if (sw <= 0 || sh <= 0)
                {
                    continue;
                }

                byte[] sub = new byte[sw * sh];
                for (int y = 0; y < sh; y++)
                {
                    Array.Copy(txt, ((ya + y) * w) + xa, sub, y * sw, sw);
                }

                List<Blob> comps = ImageOps.Components(sub, sw, sh, out int[] lab);
                for (int j = 0; j < comps.Count; j++)
                {
                    Blob c = comps[j];
                    if (c.H < 12 || c.H > 38 || c.W < 4 || c.W > 60 || c.Area < 25)
                    {
                        continue;
                    }

                    byte[] m = new byte[c.W * c.H];
                    for (int y = 0; y < c.H; y++)
                    {
                        for (int x = 0; x < c.W; x++)
                        {
                            m[(y * c.W) + x] = lab[((c.Y + y) * sw) + c.X + x] == j + 1 ? (byte)1 : (byte)0;
                        }
                    }

                    List<(int Ox, byte[] M, int Mw)> parts = c.W > 0.8 * c.H ? Cut(m, c.W, c.H, 0) : new List<(int, byte[], int)> { (0, m, c.W) };
                    int k0 = pieces.Count;
                    AddParts(parts, xa + c.X, ya + c.Y, c.H);
                    for (int k = k0; k < pieces.Count; k++)
                    {
                        add.Add(pieces[k]);
                    }

                    pieces.RemoveRange(k0, pieces.Count - k0);
                }
            }

            pieces.AddRange(add);
            if (pieces.Count > before)
            {
                Classify(before);
            }
        }

        // ------------------------------------------------------------------ words and chip type

        private int Argmax(int k)
        {
            int nc = LetterNet.Classes.Length;
            int best = 0;
            for (int c = 1; c < nc; c++)
            {
                if (prob[(k * nc) + c] > prob[(k * nc) + best])
                {
                    best = c;
                }
            }

            return best;
        }

        private double LogP(int k, int c) => Math.Log(prob[(k * LetterNet.Classes.Length) + c] + 1e-6);

        /// <summary>
        /// <c>draft.py</c>'s word grouping: full-size letter pieces (not read as a subscript or
        /// not-a-letter), each joined to the nearest piece to its right on the same row with a gap
        /// of at most half a letter. Subscripts are not attached here (the chip type does not use them).
        /// </summary>
        private List<Word> BuildWords()
        {
            List<int> big = new List<int>();
            for (int k = 0; k < pieces.Count; k++)
            {
                if (pieces[k].Small)
                {
                    continue;
                }

                string c = LetterNet.Classes[Argmax(k)];
                if (c != "sub0" && c != "sub1" && c != "x")
                {
                    big.Add(k);
                }
            }

            int[] order = big.ToArray();
            int[] keys = new int[order.Length];
            for (int i = 0; i < order.Length; i++)
            {
                keys[i] = pieces[order[i]].X;
            }

            StableSortByKey(order, keys);
            Dictionary<int, int> next = new Dictionary<int, int>();
            HashSet<int> taken = new HashSet<int>();
            for (int ia = 0; ia < order.Length; ia++)
            {
                int a = order[ia];
                Piece pa = pieces[a];
                int bestGap = int.MaxValue;
                int bestB = -1;
                for (int ib = 0; ib < order.Length; ib++)
                {
                    int b = order[ib];
                    Piece pb = pieces[b];
                    int gap = pb.X - (pa.X + pa.W);
                    if (b == a || taken.Contains(b) || gap < -4 || gap > 0.5 * Math.Max(pa.H, pb.H))
                    {
                        continue;
                    }

                    int ov = Math.Min(pa.Y + pa.H, pb.Y + pb.H) - Math.Max(pa.Y, pb.Y);
                    if (ov < 0.6 * Math.Min(pa.H, pb.H))
                    {
                        continue;
                    }

                    if (bestB < 0 || gap < bestGap)
                    {
                        bestGap = gap;
                        bestB = b;
                    }
                }

                if (bestB >= 0)
                {
                    next[a] = bestB;
                    taken.Add(bestB);
                }
            }

            List<Word> ws = new List<Word>();
            for (int ia = 0; ia < order.Length; ia++)
            {
                int a = order[ia];
                if (taken.Contains(a))
                {
                    continue;
                }

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                int k = a;
                int x0 = int.MaxValue;
                int y0 = int.MaxValue;
                int x1 = int.MinValue;
                int y1 = int.MinValue;
                int guard = 0;
                while (true)
                {
                    Piece p = pieces[k];
                    sb.Append(LetterNet.Classes[Argmax(k)]);
                    x0 = Math.Min(x0, p.X);
                    y0 = Math.Min(y0, p.Y);
                    x1 = Math.Max(x1, p.X + p.W);
                    y1 = Math.Max(y1, p.Y + p.H);
                    if (!next.TryGetValue(k, out int nk) || ++guard > 200)
                    {
                        break;
                    }

                    k = nk;
                }

                ws.Add(new Word(sb.ToString(), x0, y0, x1 - x0, y1 - y0));
            }

            return ws;
        }

        private static void StableSortByKey(int[] items, int[] keys)
        {
            int[] idx = new int[items.Length];
            for (int i = 0; i < idx.Length; i++)
            {
                idx[i] = i;
            }

            Array.Sort(idx, (a, b) =>
            {
                int c = keys[a].CompareTo(keys[b]);
                return c != 0 ? c : a.CompareTo(b);
            });
            int[] copy = (int[])items.Clone();
            for (int i = 0; i < idx.Length; i++)
            {
                items[i] = copy[idx[i]];
            }
        }

        /// <summary>
        /// <c>det_type</c>: the chip type from the words inside the box (lowest first) or just above
        /// it (CAD), matched to the catalogue: as read, without a maker's prefix (DM, DH, SN), or with
        /// 74 put in front of a 2-3 digit number.
        /// </summary>
        private (string Raw, string? Id) TypeOf(PageFind sb, IReadOnlyCollection<string> chipIds)
        {
            if (words == null || chipIds.Count == 0)
            {
                return (string.Empty, null);
            }

            List<Word> near = new List<Word>();
            for (int i = 0; i < words.Count; i++)
            {
                Word wd = words[i];
                double cx = wd.X + (wd.W / 2.0);
                double cy = wd.Y + (wd.H / 2.0);
                bool inside = InBox(cx, cy, sb.X, sb.Y, sb.Width, sb.Height, -2);
                bool above = sb.X - 10 <= wd.X && wd.X <= sb.X + sb.Width && sb.Y - 45 <= cy && cy < sb.Y;
                if (inside || above)
                {
                    near.Add(wd);
                }
            }

            // sorted by -y, stable
            Word[] arr = near.ToArray();
            int[] idx = new int[arr.Length];
            for (int i = 0; i < idx.Length; i++)
            {
                idx[i] = i;
            }

            Array.Sort(idx, (a, b) =>
            {
                int c = arr[b].Y.CompareTo(arr[a].Y);
                return c != 0 ? c : a.CompareTo(b);
            });
            HashSet<string> ids = chipIds as HashSet<string> ?? new HashSet<string>(chipIds, StringComparer.Ordinal);
            // two passes: first only words of 3 or more characters, then the shorter ones. A pin
            // number is never more than 2 digits, and ND writes pin numbers in cells inside the
            // chip box, so a short word low in the box can be a pin number: on the 2A50 page, pin
            // "11" of 15B lies below the printed "74157" (read "157") and was taken as a 7411 once
            // a 7411 chip file existed (BUGS.md B6). A 2-digit type (read "00" for 7400) is still
            // found in the second pass when no longer word names a chip.
            for (int pass = 0; pass < 2; pass++)
            {
                for (int n = 0; n < idx.Length; n++)
                {
                    string t = arr[idx[n]].Text;
                    if ((pass == 0) != (t.Length >= 3))
                    {
                        continue;
                    }

                    string?[] cands =
                    {
                    t,
                    t.Length >= 2 && (t.StartsWith("DM", StringComparison.Ordinal) || t.StartsWith("DH", StringComparison.Ordinal) || t.StartsWith("SN", StringComparison.Ordinal) || t.StartsWith("0M", StringComparison.Ordinal)) ? t.Substring(2) : null,
                    t.Length >= 2 && t.Length <= 3 && IsDigits(t) ? "74" + t : null,
                };
                    for (int c = 0; c < cands.Length; c++)
                    {
                        string? cand = cands[c];
                        if (string.IsNullOrEmpty(cand))
                        {
                            continue;
                        }

                        string s = Snap(cand, ids);
                        if (ids.Contains(s))
                        {
                            return (t, s);
                        }
                    }
                }
            }

            return (string.Empty, null);
        }

        private static bool IsDigits(string t)
        {
            for (int i = 0; i < t.Length; i++)
            {
                if (t[i] < '0' || t[i] > '9')
                {
                    return false;
                }
            }

            return t.Length > 0;
        }

        // ------------------------------------------------------------------ position codes

        /// <summary>
        /// <c>det_code</c>: the code for a symbol from the code boxes inside it, or within 60 px above
        /// or below it (and up to 25 px to each side); also the union of the boxes inside (number
        /// above letter). The best reading wins when its score is above log(0.05).
        /// </summary>
        private void SetCode(PageFind sb, List<double[]> boxes)
        {
            double x = sb.X;
            double y = sb.Y;
            double w = sb.Width;
            double h = sb.Height;
            List<double[]> ins = new List<double[]>();
            List<double[]> outs = new List<double[]>();
            for (int i = 0; i < boxes.Count; i++)
            {
                double[] b = boxes[i];
                double cx = b[0] + (b[2] / 2);
                double cy = b[1] + (b[3] / 2);
                if (InBox(cx, cy, x, y, w, h, 2))
                {
                    ins.Add(b);
                }

                if (x - 25 <= cx && cx <= x + w + 25 && ((y - 60 <= cy && cy < y) || (y + h < cy && cy <= y + h + 60)))
                {
                    outs.Add(b);
                }
            }

            string? best = null;
            double bestSc = double.NegativeInfinity;
            void Consider((string? Code, double Score) r)
            {
                if (r.Code != null && (best == null || r.Score > bestSc))
                {
                    best = r.Code;
                    bestSc = r.Score;
                }
            }

            for (int i = 0; i < ins.Count; i++)
            {
                Consider(Read(ins[i]));
            }

            for (int i = 0; i < outs.Count; i++)
            {
                Consider(Read(outs[i]));
            }

            if (ins.Count >= 2)
            {
                double ux0 = double.MaxValue;
                double uy0 = double.MaxValue;
                double ux1 = double.MinValue;
                double uy1 = double.MinValue;
                for (int i = 0; i < ins.Count; i++)
                {
                    ux0 = Math.Min(ux0, ins[i][0]);
                    uy0 = Math.Min(uy0, ins[i][1]);
                    ux1 = Math.Max(ux1, ins[i][0] + ins[i][2]);
                    uy1 = Math.Max(uy1, ins[i][1] + ins[i][3]);
                }

                Consider(Read(new[] { ux0, uy0, ux1 - ux0, uy1 - uy0 }));
            }

            if (best != null && bestSc > CodeFloor)
            {
                sb.Code = best;
                sb.CodeScore = Math.Round(bestSc, 3);
            }
        }

        /// <summary><c>_read</c>: a code box read as a run, as a stack, and with the sliding window (FIX10).</summary>
        private (string? Code, double Score) Read(double[] b)
        {
            double[] reg = { b[0] - 4, b[1] - 4, b[0] + b[2] + 4, b[1] + b[3] + 4 };
            (string? Code, double Score) r1 = CodeIn(reg, false);
            (string? Code, double Score) r2 = CodeIn(reg, true);
            (string? Code, double Score) r = r2.Score > r1.Score ? r2 : r1;
            (string? Code, double Score) s = Slide(b);
            if (s.Code != null && (r.Code == null || s.Score > r.Score))
            {
                r = s;
            }

            return r;
        }

        private bool SameLine(Piece a, Piece b) => Math.Abs(a.Y + (a.H / 2.0) - (b.Y + (b.H / 2.0))) < 0.5 * Math.Max(a.H, b.H);

        private int RightOf(List<int> ks, int a, double maxGap)
        {
            Piece pa = pieces[a];
            int best = -1;
            int bestGap = int.MaxValue;
            for (int i = 0; i < ks.Count; i++)
            {
                int b = ks[i];
                Piece pb = pieces[b];
                int gap = pb.X - (pa.X + pa.W);
                if (b != a && gap >= -4 && gap <= maxGap * Math.Max(pa.H, pb.H) && SameLine(pa, pb) && (best < 0 || gap < bestGap))
                {
                    best = b;
                    bestGap = gap;
                }
            }

            return best;
        }

        /// <summary>
        /// <c>code_in</c>: the best position code among the letter pieces centred in a region, scored
        /// from the letter network over the board grid only. Run mode: maximal runs on one line
        /// (N+letter, N-N+L-L, NN-NN+letter). Stack mode: number above, letter below.
        /// </summary>
        private (string? Code, double Score) CodeIn(double[] region, bool stack)
        {
            List<int> ks = new List<int>();
            for (int i = 0; i < bigp.Count; i++)
            {
                Piece p = pieces[bigp[i]];
                double cx = p.X + (p.W / 2.0);
                double cy = p.Y + (p.H / 2.0);
                if (region[0] <= cx && cx <= region[2] && region[1] <= cy && cy <= region[3])
                {
                    ks.Add(bigp[i]);
                }
            }

            int[] arr = ks.ToArray();
            int[] keys = new int[arr.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                keys[i] = pieces[arr[i]].X;
            }

            StableSortByKey(arr, keys);
            ks = new List<int>(arr);
            string? bestCode = null;
            double bestSc = 0;
            void Add(double sc, string code)
            {
                if (bestCode == null || sc > bestSc)
                {
                    bestCode = code;
                    bestSc = sc;
                }
            }

            if (!stack)
            {
                HashSet<int> hasLeft = new HashSet<int>();
                for (int i = 0; i < ks.Count; i++)
                {
                    hasLeft.Add(RightOf(ks, ks[i], 0.9));
                }

                List<int> run = new List<int>(6);
                for (int i = 0; i < ks.Count; i++)
                {
                    int a = ks[i];
                    if (hasLeft.Contains(a))
                    {
                        continue;
                    }

                    run.Clear();
                    run.Add(a);
                    while (run.Count < 6)
                    {
                        int nx = RightOf(ks, run[^1], 0.9);
                        if (nx < 0)
                        {
                            break;
                        }

                        run.Add(nx);
                    }

                    if (run.Count == 2 || run.Count == 3)
                    {
                        (double sc, string? code) = BestCode(run.GetRange(0, run.Count - 1), run[^1]);
                        if (code != null)
                        {
                            Add(sc / run.Count, code);
                        }
                    }

                    // N-N+L-L: dashes are too small to be pieces, so wider gaps
                    run.Clear();
                    run.Add(a);
                    while (run.Count < 4)
                    {
                        int nx = RightOf(ks, run[^1], 2.2);
                        if (nx < 0)
                        {
                            break;
                        }

                        run.Add(nx);
                    }

                    if (run.Count == 4)
                    {
                        (double sc, string? code) = BestSpan(run);
                        if (code != null)
                        {
                            Add(sc / 4, code);
                        }
                    }

                    // NN-NN+letter (18-19E)
                    run.Clear();
                    run.Add(a);
                    while (run.Count < 5)
                    {
                        int nx = RightOf(ks, run[^1], 2.2);
                        if (nx < 0)
                        {
                            break;
                        }

                        run.Add(nx);
                    }

                    if (run.Count == 5)
                    {
                        double bb = double.NegativeInfinity;
                        string? bc = null;
                        int last = gridN[^1];
                        for (int n = 10; n < last; n++)
                        {
                            string sa = n.ToString(System.Globalization.CultureInfo.InvariantCulture);
                            string sb2 = (n + 1).ToString(System.Globalization.CultureInfo.InvariantCulture);
                            for (int li = 0; li < gridL.Length; li++)
                            {
                                char l = gridL[li];
                                double sc = LogP(run[0], LetterNet.IndexOf(sa[0])) + LogP(run[1], LetterNet.IndexOf(sa[1]))
                                    + LogP(run[2], LetterNet.IndexOf(sb2[0])) + LogP(run[3], LetterNet.IndexOf(sb2[1]))
                                    + LogP(run[4], LetterNet.IndexOf(l));
                                if (bc == null || sc > bb)
                                {
                                    bb = sc;
                                    bc = sa + "-" + sb2 + l;
                                }
                            }
                        }

                        if (bc != null)
                        {
                            Add(bb / 5, bc);
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < ks.Count; i++)
                {
                    int a = ks[i];
                    Piece pa = pieces[a];
                    int nx = RightOf(ks, a, 0.9);
                    for (int t = 0; t < 2; t++)
                    {
                        if (t == 1 && nx < 0)
                        {
                            continue;
                        }

                        List<int> top = t == 0 ? new List<int> { a } : new List<int> { a, nx };
                        Piece last = pieces[top[^1]];
                        double xl = pa.X;
                        double xr = last.X + last.W;
                        for (int j = 0; j < ks.Count; j++)
                        {
                            int b = ks[j];
                            if (top.Contains(b))
                            {
                                continue;
                            }

                            Piece pb = pieces[b];
                            double dy = pb.Y - pa.Y;
                            double bcx = pb.X + (pb.W / 2.0);
                            if (0.6 * pa.H < dy && dy < 2.2 * pa.H && xl - 12 <= bcx && bcx <= xr + 12)
                            {
                                (double sc, string? code) = BestCode(top, b);
                                if (code != null)
                                {
                                    Add(sc / (top.Count + 1), code);
                                }
                            }
                        }
                    }
                }
            }

            return bestCode == null ? (null, -99) : (bestCode, bestSc);
        }

        /// <summary><c>best_code</c>: number pieces + one letter piece, every grid code of that length.</summary>
        private (double Score, string? Code) BestCode(List<int> digits, int letter)
        {
            double best = 0;
            string? code = null;
            for (int i = 0; i < gridN.Length; i++)
            {
                string ds = gridN[i].ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (ds.Length != digits.Count)
                {
                    continue;
                }

                double dsum = 0;
                for (int k = 0; k < ds.Length; k++)
                {
                    dsum += LogP(digits[k], LetterNet.IndexOf(ds[k]));
                }

                for (int li = 0; li < gridL.Length; li++)
                {
                    double sc = dsum + LogP(letter, LetterNet.IndexOf(gridL[li]));
                    if (code == null || sc > best)
                    {
                        best = sc;
                        code = ds + gridL[li];
                    }
                }
            }

            return (best, code);
        }

        /// <summary><c>best_span</c>: 4 pieces d d L L, numbers and letters next to each other (7-8C-D).</summary>
        private (double Score, string? Code) BestSpan(List<int> s)
        {
            double best = 0;
            string? code = null;
            for (int n = 1; n < 9; n++)
            {
                for (int li = 0; li < gridL.Length - 1; li++)
                {
                    char l1 = gridL[li];
                    char l2 = gridL[li + 1];
                    double sc = LogP(s[0], LetterNet.IndexOf((char)('0' + n))) + LogP(s[1], LetterNet.IndexOf((char)('0' + n + 1)))
                        + LogP(s[2], LetterNet.IndexOf(l1)) + LogP(s[3], LetterNet.IndexOf(l2));
                    if (code == null || sc > best)
                    {
                        best = sc;
                        code = n.ToString(System.Globalization.CultureInfo.InvariantCulture) + "-" + (n + 1).ToString(System.Globalization.CultureInfo.InvariantCulture) + l1 + "-" + l2;
                    }
                }
            }

            return (best, code);
        }

        /// <summary>
        /// FIX10: a code whose characters touch is one ink piece (or none). The letter network is run
        /// on windows every 2 px along the box's middle line, and the code is the best split into 2 or
        /// 3 characters (each 0.25 - 1.1 box heights wide) over the board grid; score = mean log
        /// probability per character. The letter is the grid letter the last window likes best.
        /// </summary>
        private (string? Code, double Score) Slide(double[] bd)
        {
            int x = (int)bd[0];
            int y = (int)bd[1];
            int w = (int)bd[2];
            int h = (int)bd[3];
            if (w < 12 || h < 10)
            {
                return (null, -99);
            }

            int cy = y + (h / 2);
            List<(int X, int Y)> cs = new List<(int X, int Y)>();
            for (int cx = x; cx <= x + w; cx += 2)
            {
                cs.Add((cx, cy));
            }

            float[] pr = net.Probabilities(page, cs);
            int nc = LetterNet.Classes.Length;
            double[] lp = new double[pr.Length];
            for (int i = 0; i < pr.Length; i++)
            {
                lp[i] = Math.Log(pr[i] + 1e-6);
            }

            double mn = 0.25 * h;
            double mx = 1.1 * h;
            int At(double xc)
            {
                int i = (int)Math.Round((xc - x) / 2.0, MidpointRounding.ToEven);
                return Math.Min(cs.Count - 1, Math.Max(0, i)) * nc;
            }

            double best = -99;
            string? bestCode = null;
            List<int> cuts = new List<int>();
            for (int c = x; c <= x + w; c += 2)
            {
                cuts.Add(c);
            }

            List<(int A, int B)> seg = new List<(int A, int B)>(3);
            for (int n = 2; n <= 3; n++)
            {
                Segs(x, n);
            }

            return (bestCode, best);

            void Segs(int start, int left)
            {
                if (left == 1)
                {
                    int width = x + w - start;
                    if (mn <= width && width <= mx)
                    {
                        seg.Add((start, x + w));
                        Score();
                        seg.RemoveAt(seg.Count - 1);
                    }

                    return;
                }

                for (int i = 0; i < cuts.Count; i++)
                {
                    int c = cuts[i];
                    int width = c - start;
                    if (mn <= width && width <= mx)
                    {
                        seg.Add((start, c));
                        Segs(c, left - 1);
                        seg.RemoveAt(seg.Count - 1);
                    }
                }
            }

            void Score()
            {
                int n = seg.Count;
                int lt = At((seg[n - 1].A + seg[n - 1].B) / 2.0);
                char lb = gridL[0];
                double lbv = double.NegativeInfinity;
                for (int li = 0; li < gridL.Length; li++)
                {
                    double v = lp[lt + LetterNet.IndexOf(gridL[li])];
                    if (v > lbv)
                    {
                        lbv = v;
                        lb = gridL[li];
                    }
                }

                for (int i = 0; i < gridN.Length; i++)
                {
                    string st = gridN[i].ToString(System.Globalization.CultureInfo.InvariantCulture);
                    if (st.Length != n - 1)
                    {
                        continue;
                    }

                    double sc = lbv;
                    for (int k = 0; k < st.Length; k++)
                    {
                        sc += lp[At((seg[k].A + seg[k].B) / 2.0) + LetterNet.IndexOf(st[k])];
                    }

                    sc /= n;
                    if (sc > best)
                    {
                        best = sc;
                        bestCode = st + lb;
                    }
                }
            }
        }

        private readonly record struct Piece(int X, int Y, int W, int H, bool Small);

        private sealed record Word(string Text, int X, int Y, int W, int H);
    }
}
