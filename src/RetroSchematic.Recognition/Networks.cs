//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace RetroSchematic.Recognition;

/// <summary>
/// The letter network (<c>models/letters-A.onnx</c>): one character per 32 x 32 crop. Crops are
/// made the way the prototype made them (<c>pageio.crops</c>, <c>draft.py</c>): a 48 x 48 window
/// centred on the piece, paper outside the page, shrunk by area averaging, ink = 1.
/// </summary>
internal sealed class LetterNet : IDisposable
{
    /// <summary>The class names in the network's output order.</summary>
    public static readonly string[] Classes = BuildClasses();

    /// <summary>Window size around a piece, page pixels.</summary>
    public const int Win = 48;

    /// <summary>Network input size.</summary>
    public const int Size = 32;

    private readonly InferenceSession session;

    /// <summary>Loads the network.</summary>
    /// <param name="path">The ONNX file.</param>
    public LetterNet(string path)
    {
        SessionOptions o = new SessionOptions();
        o.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
        session = new InferenceSession(path, o);
    }

    /// <summary>Gets the index of a class name.</summary>
    /// <param name="name">Such as <c>"7"</c>, <c>"B"</c>, <c>"sub0"</c>.</param>
    /// <returns>The index.</returns>
    public static int IndexOf(string name)
    {
        for (int i = 0; i < Classes.Length; i++)
        {
            if (Classes[i] == name)
            {
                return i;
            }
        }

        throw new ArgumentException("Not a letter class: " + name, nameof(name));
    }

    /// <summary>Gets the index of a one-character class.</summary>
    /// <param name="c">A digit or capital letter.</param>
    /// <returns>The index.</returns>
    public static int IndexOf(char c)
    {
        if (c >= '0' && c <= '9')
        {
            return c - '0';
        }

        if (c >= 'A' && c <= 'Z')
        {
            return 10 + (c - 'A');
        }

        throw new ArgumentException("Not a letter class: " + c, nameof(c));
    }

    /// <summary>
    /// Runs the network on windows centred at the given points of a page and returns the softmax
    /// probabilities, one row of <see cref="Classes"/> per point.
    /// </summary>
    /// <param name="page">The page (at the detector's scale).</param>
    /// <param name="centres">Window centres (x, y), already whole pixels.</param>
    /// <returns>Probabilities, <c>centres.Count</c> x 39.</returns>
    public float[] Probabilities(GreyImage page, IReadOnlyList<(int X, int Y)> centres)
    {
        int n = centres.Count;
        int nc = Classes.Length;
        float[] result = new float[n * nc];
        if (n == 0)
        {
            return result;
        }

        const int chunk = 1024;
        byte[] win = new byte[Win * Win];
        byte[] small = new byte[Size * Size];
        for (int start = 0; start < n; start += chunk)
        {
            int m = Math.Min(chunk, n - start);
            DenseTensor<float> x = new DenseTensor<float>(new[] { m, 1, Size, Size });
            Span<float> xs = x.Buffer.Span;
            for (int k = 0; k < m; k++)
            {
                (int cx, int cy) = centres[start + k];
                CutWindow(page, cx, cy, win);
                ImageOps.ResizeArea(win, Win, Win, Size, Size, small);
                int o = k * Size * Size;
                for (int i = 0; i < small.Length; i++)
                {
                    xs[o + i] = (255 - small[i]) / 255f;
                }
            }

            List<NamedOnnxValue> inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("x", x) };
            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outs = session.Run(inputs);
            Tensor<float> logits = outs[0].AsTensor<float>();
            for (int k = 0; k < m; k++)
            {
                float mx = float.MinValue;
                for (int c = 0; c < nc; c++)
                {
                    float v = logits[k, c];
                    if (v > mx)
                    {
                        mx = v;
                    }
                }

                double sum = 0;
                int ro = (start + k) * nc;
                for (int c = 0; c < nc; c++)
                {
                    double e = Math.Exp(logits[k, c] - mx);
                    result[ro + c] = (float)e;
                    sum += e;
                }

                for (int c = 0; c < nc; c++)
                {
                    result[ro + c] = (float)(result[ro + c] / sum);
                }
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public void Dispose() => session.Dispose();

    /// <summary>
    /// The prototype's window: the page padded with 48 px of paper, then rows
    /// <c>cy + 48 - 24 .. cy + 48 + 24</c> and the same for x, so page pixels cy-24 .. cy+23.
    /// </summary>
    private static void CutWindow(GreyImage page, int cx, int cy, byte[] win)
    {
        int w = page.Width;
        int h = page.Height;
        byte[] px = page.Pixels;
        int half = Win / 2;
        for (int yy = 0; yy < Win; yy++)
        {
            int sy = cy - half + yy;
            int o = yy * Win;
            if (sy < 0 || sy >= h)
            {
                win.AsSpan(o, Win).Fill(255);
                continue;
            }

            int row = sy * w;
            for (int xx = 0; xx < Win; xx++)
            {
                int sx = cx - half + xx;
                win[o + xx] = sx < 0 || sx >= w ? (byte)255 : px[row + sx];
            }
        }
    }

    private static string[] BuildClasses()
    {
        string[] c = new string[39];
        for (int i = 0; i < 10; i++)
        {
            c[i] = ((char)('0' + i)).ToString();
        }

        for (int i = 0; i < 26; i++)
        {
            c[10 + i] = ((char)('A' + i)).ToString();
        }

        c[36] = "sub0";
        c[37] = "sub1";
        c[38] = "x";
        return c;
    }
}

/// <summary>One raw detector answer, in the coordinates of the picture it was run on.</summary>
/// <param name="Class">Index into <see cref="SymbolDetector.Classes"/>.</param>
/// <param name="Score">Centre heat, 0..1.</param>
/// <param name="X">Box left.</param>
/// <param name="Y">Box top.</param>
/// <param name="W">Box width.</param>
/// <param name="H">Box height.</param>
/// <param name="Kind">Gate kind (index into <see cref="SymbolDetector.GateKinds"/>), or -1.</param>
internal readonly record struct Detection(int Class, double Score, int X, int Y, int W, int H, int Kind);

/// <summary>
/// One of the two symbol detectors (<c>det-L3.onnx</c> for hand-drawn pages, <c>det-A.onnx</c> for
/// CAD pages). Pre- and post-processing are <c>detio.detect</c> of the prototype.
/// </summary>
internal sealed class SymbolDetector : IDisposable
{
    /// <summary>The detector's classes in output order.</summary>
    public static readonly string[] Classes =
    {
        "chip", "gate", "resistor", "capacitor", "switch", "strap", "ground", "supply", "circle",
        "offpage", "dot", "poscode", "pinnum", "label",
    };

    /// <summary>The gate kinds in output order.</summary>
    public static readonly string[] GateKinds = { "nand", "nor", "not", "and", "or", "aoi", "buffer", "other" };

    /// <summary>Output cell size in page pixels.</summary>
    private const int Stride = 4;

    private readonly InferenceSession session;

    /// <summary>Loads a detector.</summary>
    /// <param name="path">The ONNX file.</param>
    public SymbolDetector(string path)
    {
        SessionOptions o = new SessionOptions();
        o.GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL;
        session = new InferenceSession(path, o);
    }

    /// <summary>Gets the size of the last output, for tests: classes, height, width.</summary>
    public (int Classes, int H4, int W4) LastShape { get; private set; }

    /// <summary>
    /// Runs the detector on a whole page. The page is padded with paper to a multiple of 32 on
    /// the right and bottom, fed as grey / 255, and every class's heat map is searched for cells
    /// at least <paramref name="threshold"/> that no 3 x 3 neighbour beats.
    /// </summary>
    /// <param name="page">The page at the detector's scale.</param>
    /// <param name="threshold">Lowest heat kept (the prototype used 0.3).</param>
    /// <returns>Every find, class by class, each class in scan order.</returns>
    public List<Detection> Detect(GreyImage page, double threshold)
    {
        int h = page.Height;
        int w = page.Width;
        int hp = (h + 31) / 32 * 32;
        int wp = (w + 31) / 32 * 32;
        DenseTensor<float> x = new DenseTensor<float>(new[] { 1, 1, hp, wp });
        Span<float> xs = x.Buffer.Span;
        xs.Fill(1f);
        byte[] px = page.Pixels;
        for (int y = 0; y < h; y++)
        {
            int s = y * w;
            int d = y * wp;
            for (int xx = 0; xx < w; xx++)
            {
                xs[d + xx] = px[s + xx] / 255f;
            }
        }

        List<NamedOnnxValue> inputs = new List<NamedOnnxValue> { NamedOnnxValue.CreateFromTensor("page", x) };
        using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outs = session.Run(inputs);
        DenseTensor<float> hm = (DenseTensor<float>)outs[0].AsTensor<float>();
        DenseTensor<float> reg = (DenseTensor<float>)outs[1].AsTensor<float>();
        DenseTensor<float> kind = (DenseTensor<float>)outs[2].AsTensor<float>();
        int nc = hm.Dimensions[1];
        int oh = hm.Dimensions[2];
        int ow = hm.Dimensions[3];
        LastShape = (nc, oh, ow);
        ReadOnlySpan<float> H = hm.Buffer.Span;
        ReadOnlySpan<float> R = reg.Buffer.Span;
        ReadOnlySpan<float> K = kind.Buffer.Span;
        int plane = oh * ow;
        int nk = kind.Dimensions[1];
        List<Detection> found = new List<Detection>();
        float thr = (float)threshold;
        for (int c = 0; c < nc; c++)
        {
            int cb = c * plane;
            for (int y = 0; y < oh; y++)
            {
                for (int xx = 0; xx < ow; xx++)
                {
                    float v = H[cb + (y * ow) + xx];
                    if (v < thr || !IsPeak(H, cb, ow, oh, xx, y, v))
                    {
                        continue;
                    }

                    int i = (y * ow) + xx;
                    double bw = Math.Exp(R[i]) * Stride;
                    double bh = Math.Exp(R[plane + i]) * Stride;
                    double cx = (xx + R[(2 * plane) + i]) * Stride;
                    double cy = (y + R[(3 * plane) + i]) * Stride;
                    int k = -1;
                    if (c == 1)
                    {
                        float best = float.MinValue;
                        for (int j = 0; j < nk; j++)
                        {
                            float kv = K[(j * plane) + i];
                            if (kv > best)
                            {
                                best = kv;
                                k = j;
                            }
                        }
                    }

                    // Python round(): halves go to the even neighbour
                    found.Add(new Detection(
                        c,
                        Math.Round(v, 3, MidpointRounding.ToEven),
                        (int)Math.Round(cx - (bw / 2), MidpointRounding.ToEven),
                        (int)Math.Round(cy - (bh / 2), MidpointRounding.ToEven),
                        (int)Math.Round(bw, MidpointRounding.ToEven),
                        (int)Math.Round(bh, MidpointRounding.ToEven),
                        k));
                }
            }
        }

        return found;
    }

    /// <inheritdoc/>
    public void Dispose() => session.Dispose();

    /// <summary><c>m == cv2.dilate(m, ones 3x3)</c>: no neighbour inside the map is higher.</summary>
    private static bool IsPeak(ReadOnlySpan<float> h, int cb, int ow, int oh, int x, int y, float v)
    {
        for (int dy = -1; dy <= 1; dy++)
        {
            int yy = y + dy;
            if (yy < 0 || yy >= oh)
            {
                continue;
            }

            for (int dx = -1; dx <= 1; dx++)
            {
                int xx = x + dx;
                if (xx < 0 || xx >= ow)
                {
                    continue;
                }

                if (h[cb + (yy * ow) + xx] > v)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
