//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;

namespace RetroSchematic.Recognition;

/// <summary>One connected piece of ink: its bounding box and pixel count.</summary>
/// <param name="X">Left edge.</param>
/// <param name="Y">Top edge.</param>
/// <param name="W">Width.</param>
/// <param name="H">Height.</param>
/// <param name="Area">Number of ink pixels.</param>
internal readonly record struct Blob(int X, int Y, int W, int H, int Area);

/// <summary>
/// The plain image steps of the prototype, written to give the same answers as the OpenCV calls it
/// made (each method names the call). Masks are byte arrays, 255 = set, 0 = clear.
/// </summary>
internal static class ImageOps
{
    /// <summary>
    /// <c>cv2.threshold(g, 0, 255, THRESH_BINARY_INV + THRESH_OTSU)</c>: ink mask, 255 where the
    /// pixel is at or below Otsu's threshold.
    /// </summary>
    /// <param name="g">The grey page.</param>
    /// <param name="threshold">The threshold Otsu's rule chose.</param>
    /// <returns>The ink mask.</returns>
    public static byte[] OtsuInk(GreyImage g, out int threshold)
    {
        byte[] px = g.Pixels;
        int n = g.Width * g.Height;
        Span<int> hist = stackalloc int[256];
        hist.Clear();
        for (int i = 0; i < n; i++)
        {
            hist[px[i]]++;
        }

        // OpenCV getThreshVal_Otsu_8u, step for step (including that mu1 is left multiplied by
        // q1 on a skipped level, which only matters while q1 is still zero).
        double scale = 1.0 / n;
        double mu = 0;
        for (int i = 0; i < 256; i++)
        {
            mu += i * (double)hist[i];
        }

        mu *= scale;
        double mu1 = 0;
        double q1 = 0;
        double maxSigma = 0;
        int maxVal = 0;
        const double eps = 1.1920928955078125e-07; // FLT_EPSILON
        for (int i = 0; i < 256; i++)
        {
            double pi = hist[i] * scale;
            mu1 *= q1;
            q1 += pi;
            double q2 = 1.0 - q1;
            if (Math.Min(q1, q2) < eps || Math.Max(q1, q2) > 1.0 - eps)
            {
                continue;
            }

            mu1 = (mu1 + (i * pi)) / q1;
            double mu2 = (mu - (q1 * mu1)) / q2;
            double sigma = q1 * q2 * (mu1 - mu2) * (mu1 - mu2);
            if (sigma > maxSigma)
            {
                maxSigma = sigma;
                maxVal = i;
            }
        }

        threshold = maxVal;
        byte[] ink = new byte[n];
        for (int i = 0; i < n; i++)
        {
            ink[i] = px[i] > maxVal ? (byte)0 : (byte)255;
        }

        return ink;
    }

    /// <summary>
    /// <c>cv2.morphologyEx(m, MORPH_OPEN, MORPH_RECT (len, 1))</c> (horizontal) or <c>(1, len)</c>
    /// (vertical): keeps only straight runs at least <paramref name="len"/> long. OpenCV puts the
    /// anchor of an even bar at len/2, so both the erosion and the dilation look at offsets
    /// -len/2 .. len/2 - 1, and the kept run comes out one pixel later than the ink (measured with
    /// OpenCV 4: a run 5..69 opens to 6..70). Outside the picture counts as ink for the erosion and
    /// as paper for the dilation, as in OpenCV.
    /// </summary>
    /// <param name="m">The mask.</param>
    /// <param name="w">Width.</param>
    /// <param name="h">Height.</param>
    /// <param name="len">Bar length.</param>
    /// <param name="horizontal">True for a horizontal bar.</param>
    /// <returns>The opened mask.</returns>
    public static byte[] OpenBar(byte[] m, int w, int h, int len, bool horizontal)
    {
        int lo = -(len / 2);
        int hi = len - 1 - (len / 2);
        int lineLen = horizontal ? w : h;
        int lines = horizontal ? h : w;
        int step = horizontal ? 1 : w;
        int lineStep = horizontal ? w : 1;
        byte[] eroded = new byte[m.Length];
        byte[] outp = new byte[m.Length];
        int[] runEnd = new int[lineLen];
        for (int l = 0; l < lines; l++)
        {
            int b = l * lineStep;

            // runEnd[i]: last index of the ink run that contains i (or -1 when i is paper);
            // the erosion at x is set when x+lo .. x+hi is all ink (outside = ink).
            int end = -1;
            for (int i = lineLen - 1; i >= 0; i--)
            {
                if (m[b + (i * step)] != 0)
                {
                    if (end < i)
                    {
                        end = i;
                    }

                    runEnd[i] = end;
                }
                else
                {
                    runEnd[i] = -1;
                    end = -1;
                }
            }

            for (int x = 0; x < lineLen; x++)
            {
                int a = x + lo;
                int z = x + hi;
                int a0 = a < 0 ? 0 : a;
                int z0 = z >= lineLen ? lineLen - 1 : z;
                bool all = a0 > z0 || (runEnd[a0] >= z0);
                eroded[b + (x * step)] = all ? (byte)255 : (byte)0;
            }

            // dilation: x is set when any of x+lo .. x+hi is set in the erosion (outside = paper)
            int[] nextSet = runEnd; // reuse: nearest set index at or after i
            int nxt = int.MaxValue / 2;
            for (int i = lineLen - 1; i >= 0; i--)
            {
                if (eroded[b + (i * step)] != 0)
                {
                    nxt = i;
                }

                nextSet[i] = nxt;
            }

            for (int x = 0; x < lineLen; x++)
            {
                int a = x + lo;
                int a0 = a < 0 ? 0 : a;
                int z = x + hi;
                bool any = false;
                if (a0 < lineLen)
                {
                    int ns = nextSet[a0];
                    any = ns <= z && ns < lineLen;
                }

                outp[b + (x * step)] = any ? (byte)255 : (byte)0;
            }
        }

        return outp;
    }

    /// <summary><c>cv2.dilate(m, ones 3x3)</c>: outside counts as paper.</summary>
    /// <param name="m">The mask.</param>
    /// <param name="w">Width.</param>
    /// <param name="h">Height.</param>
    /// <returns>The grown mask.</returns>
    public static byte[] Dilate3(byte[] m, int w, int h)
    {
        byte[] row = new byte[m.Length];
        for (int y = 0; y < h; y++)
        {
            int b = y * w;
            for (int x = 0; x < w; x++)
            {
                byte v = m[b + x];
                if (x > 0 && m[b + x - 1] > v)
                {
                    v = m[b + x - 1];
                }

                if (x < w - 1 && m[b + x + 1] > v)
                {
                    v = m[b + x + 1];
                }

                row[b + x] = v;
            }
        }

        byte[] o = new byte[m.Length];
        for (int y = 0; y < h; y++)
        {
            int b = y * w;
            for (int x = 0; x < w; x++)
            {
                byte v = row[b + x];
                if (y > 0 && row[b - w + x] > v)
                {
                    v = row[b - w + x];
                }

                if (y < h - 1 && row[b + w + x] > v)
                {
                    v = row[b + w + x];
                }

                o[b + x] = v;
            }
        }

        return o;
    }

    /// <summary>
    /// <c>cv2.connectedComponentsWithStats(m, 8)</c>: the 8-connected pieces of a mask, in the order
    /// of their first pixel in a top-to-bottom, left-to-right scan (OpenCV's numbering), and a
    /// label picture (0 = paper, piece k = k + 1).
    /// </summary>
    /// <param name="m">The mask.</param>
    /// <param name="w">Width.</param>
    /// <param name="h">Height.</param>
    /// <param name="labels">Receives the label picture.</param>
    /// <returns>The pieces.</returns>
    public static List<Blob> Components(byte[] m, int w, int h, out int[] labels)
    {
        int[] lab = new int[w * h];
        List<int> parent = new List<int>(4096) { 0 };
        for (int y = 0; y < h; y++)
        {
            int b = y * w;
            for (int x = 0; x < w; x++)
            {
                if (m[b + x] == 0)
                {
                    continue;
                }

                int best = 0;

                // neighbours already visited: left, up-left, up, up-right
                if (x > 0)
                {
                    best = Join(parent, best, lab[b + x - 1]);
                }

                if (y > 0)
                {
                    int u = b - w + x;
                    if (x > 0)
                    {
                        best = Join(parent, best, lab[u - 1]);
                    }

                    best = Join(parent, best, lab[u]);
                    if (x < w - 1)
                    {
                        best = Join(parent, best, lab[u + 1]);
                    }
                }

                if (best == 0)
                {
                    best = parent.Count;
                    parent.Add(best);
                }

                lab[b + x] = best;
            }
        }

        // final numbering by first appearance in the scan
        int[] final = new int[parent.Count];
        List<Blob> blobs = new List<Blob>();
        List<int> x0 = new List<int>();
        List<int> y0 = new List<int>();
        List<int> x1 = new List<int>();
        List<int> y1 = new List<int>();
        List<int> area = new List<int>();
        for (int i = 0; i < lab.Length; i++)
        {
            int l = lab[i];
            if (l == 0)
            {
                continue;
            }

            int r = Find(parent, l);
            int f = final[r];
            if (f == 0)
            {
                f = x0.Count + 1;
                final[r] = f;
                x0.Add(int.MaxValue);
                y0.Add(int.MaxValue);
                x1.Add(-1);
                y1.Add(-1);
                area.Add(0);
            }

            lab[i] = f;
            int px = i % w;
            int py = i / w;
            int k = f - 1;
            if (px < x0[k])
            {
                x0[k] = px;
            }

            if (px > x1[k])
            {
                x1[k] = px;
            }

            if (py < y0[k])
            {
                y0[k] = py;
            }

            if (py > y1[k])
            {
                y1[k] = py;
            }

            area[k]++;
        }

        for (int k = 0; k < x0.Count; k++)
        {
            blobs.Add(new Blob(x0[k], y0[k], x1[k] - x0[k] + 1, y1[k] - y0[k] + 1, area[k]));
        }

        labels = lab;
        return blobs;
    }

    private static int Find(List<int> parent, int a)
    {
        while (parent[a] != a)
        {
            parent[a] = parent[parent[a]];
            a = parent[a];
        }

        return a;
    }

    private static int Join(List<int> parent, int cur, int other)
    {
        if (other == 0)
        {
            return cur;
        }

        int ro = Find(parent, other);
        if (cur == 0)
        {
            return ro;
        }

        int rc = Find(parent, cur);
        if (rc == ro)
        {
            return rc;
        }

        int lo = rc < ro ? rc : ro;
        int hi = rc < ro ? ro : rc;
        parent[hi] = lo;
        return lo;
    }

    /// <summary>
    /// <c>cv2.resize(src, (dw, dh), interpolation=INTER_AREA)</c> for shrinking: each output pixel is
    /// the area-weighted mean of the source pixels it covers, rounded to the nearest byte.
    /// </summary>
    /// <param name="src">Source pixels.</param>
    /// <param name="sw">Source width.</param>
    /// <param name="sh">Source height.</param>
    /// <param name="dw">Output width.</param>
    /// <param name="dh">Output height.</param>
    /// <param name="dst">Receives dw x dh bytes.</param>
    public static void ResizeArea(ReadOnlySpan<byte> src, int sw, int sh, int dw, int dh, Span<byte> dst)
    {
        double fx = (double)sw / dw;
        double fy = (double)sh / dh;
        Span<double> rowAcc = dw <= 1024 ? stackalloc double[dw] : new double[dw];
        for (int dy = 0; dy < dh; dy++)
        {
            double sy0 = dy * fy;
            double sy1 = sy0 + fy;
            rowAcc.Clear();
            int ya = (int)Math.Floor(sy0);
            for (int sy = ya; sy < sy1 && sy < sh; sy++)
            {
                double wy = Math.Min(sy1, sy + 1) - Math.Max(sy0, sy);
                if (wy <= 0)
                {
                    continue;
                }

                int srow = sy * sw;
                for (int dx = 0; dx < dw; dx++)
                {
                    double sx0 = dx * fx;
                    double sx1 = sx0 + fx;
                    double acc = 0;
                    int xa = (int)Math.Floor(sx0);
                    for (int sx = xa; sx < sx1 && sx < sw; sx++)
                    {
                        double wx = Math.Min(sx1, sx + 1) - Math.Max(sx0, sx);
                        if (wx > 0)
                        {
                            acc += wx * src[srow + sx];
                        }
                    }

                    rowAcc[dx] += acc * wy;
                }
            }

            double norm = 1.0 / (fx * fy);
            for (int dx = 0; dx < dw; dx++)
            {
                double v = Math.Round(rowAcc[dx] * norm, MidpointRounding.ToEven);
                dst[(dy * dw) + dx] = (byte)(v < 0 ? 0 : v > 255 ? 255 : v);
            }
        }
    }

    /// <summary>
    /// Scales a whole page by factor <paramref name="f"/>: area averaging when shrinking (as the
    /// prototype's INTER_AREA). When growing, the prototype used OpenCV's cubic filter; this uses
    /// straight-line (bilinear) filtering instead, so a grown page is a little softer than the
    /// prototype's. Not used for pages already at the detector's scale (sheet 2A50).
    /// </summary>
    /// <param name="g">The page.</param>
    /// <param name="f">Scale factor.</param>
    /// <returns>The scaled page.</returns>
    public static GreyImage Scale(GreyImage g, double f)
    {
        int dw = Math.Max(1, (int)Math.Round(g.Width * f));
        int dh = Math.Max(1, (int)Math.Round(g.Height * f));
        byte[] o = new byte[dw * dh];
        if (f < 1)
        {
            ResizeArea(g.Pixels, g.Width, g.Height, dw, dh, o);
            return new GreyImage(dw, dh, o);
        }

        byte[] s = g.Pixels;
        int sw = g.Width;
        int sh = g.Height;
        for (int y = 0; y < dh; y++)
        {
            double sy = ((y + 0.5) / f) - 0.5;
            int y0 = Math.Clamp((int)Math.Floor(sy), 0, sh - 1);
            int y1 = Math.Min(sh - 1, y0 + 1);
            double ty = Math.Clamp(sy - y0, 0, 1);
            for (int x = 0; x < dw; x++)
            {
                double sx = ((x + 0.5) / f) - 0.5;
                int x0 = Math.Clamp((int)Math.Floor(sx), 0, sw - 1);
                int x1 = Math.Min(sw - 1, x0 + 1);
                double tx = Math.Clamp(sx - x0, 0, 1);
                double top = (s[(y0 * sw) + x0] * (1 - tx)) + (s[(y0 * sw) + x1] * tx);
                double bot = (s[(y1 * sw) + x0] * (1 - tx)) + (s[(y1 * sw) + x1] * tx);
                o[(y * dw) + x] = (byte)Math.Round((top * (1 - ty)) + (bot * ty));
            }
        }

        return new GreyImage(dw, dh, o);
    }

    /// <summary>
    /// The prototype's contrast stretch (<c>pageio.render</c>): when the darkest 0.5 % of the page
    /// is lighter than 5, that level is taken as full black. Black pages are left alone.
    /// </summary>
    /// <param name="g">The page.</param>
    /// <returns>The page, stretched or unchanged.</returns>
    public static GreyImage StretchContrast(GreyImage g)
    {
        int n = g.Width * g.Height;
        Span<int> hist = stackalloc int[256];
        hist.Clear();
        byte[] px = g.Pixels;
        for (int i = 0; i < n; i++)
        {
            hist[px[i]]++;
        }

        // numpy percentile (linear): the value at rank 0.005 * (n - 1)
        double rank = 0.005 * (n - 1);
        int lowRank = (int)Math.Floor(rank);
        int vLow = ValueAtRank(hist, lowRank);
        int vHigh = ValueAtRank(hist, lowRank + 1);
        double lo = vLow + ((vHigh - vLow) * (rank - lowRank));
        if (lo <= 5)
        {
            return g;
        }

        byte[] o = new byte[n];
        double k = 255.0 / (255.0 - lo);
        for (int i = 0; i < n; i++)
        {
            double v = (px[i] - lo) * k;
            o[i] = (byte)(v < 0 ? 0 : v > 255 ? 255 : (int)v);
        }

        return new GreyImage(g.Width, g.Height, o);
    }

    private static int ValueAtRank(ReadOnlySpan<int> hist, int rank)
    {
        int c = 0;
        for (int v = 0; v < 256; v++)
        {
            c += hist[v];
            if (c > rank)
            {
                return v;
            }
        }

        return 255;
    }
}
