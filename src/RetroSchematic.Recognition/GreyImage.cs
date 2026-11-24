//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using SkiaSharp;

namespace RetroSchematic.Recognition;

/// <summary>
/// An 8-bit grey picture, one byte per pixel, rows top to bottom, 0 = black, 255 = white.
/// Every image step of the page analyser works on this form.
/// </summary>
public sealed class GreyImage
{
    /// <summary>Creates a picture over the given pixels (not copied).</summary>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="pixels">The pixels, <paramref name="width"/> x <paramref name="height"/> bytes.</param>
    public GreyImage(int width, int height, byte[] pixels)
    {
        ArgumentNullException.ThrowIfNull(pixels);
        if (width <= 0 || height <= 0 || pixels.Length < width * height)
        {
            throw new ArgumentException("The pixel buffer does not match the size.", nameof(pixels));
        }

        Width = width;
        Height = height;
        Pixels = pixels;
    }

    /// <summary>Gets the width in pixels.</summary>
    public int Width { get; }

    /// <summary>Gets the height in pixels.</summary>
    public int Height { get; }

    /// <summary>Gets the pixels, row by row.</summary>
    public byte[] Pixels { get; }

    /// <summary>Reads a PNG or JPEG file as grey.</summary>
    /// <param name="path">The picture file.</param>
    /// <returns>The grey picture.</returns>
    public static GreyImage FromFile(string path)
    {
        byte[] bytes = System.IO.File.ReadAllBytes(path);
        return FromEncoded(bytes);
    }

    /// <summary>Decodes a PNG or JPEG held in memory as grey.</summary>
    /// <param name="encoded">The file's bytes.</param>
    /// <returns>The grey picture.</returns>
    public static GreyImage FromEncoded(byte[] encoded)
    {
        using SKBitmap? decoded = SKBitmap.Decode(encoded);
        if (decoded is null)
        {
            throw new InvalidOperationException("The picture could not be decoded.");
        }

        return FromBitmap(decoded);
    }

    /// <summary>Turns a Skia bitmap into grey.</summary>
    /// <param name="bitmap">Any bitmap.</param>
    /// <returns>The grey picture.</returns>
    public static GreyImage FromBitmap(SKBitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        int w = bitmap.Width;
        int h = bitmap.Height;
        byte[] grey = new byte[w * h];
        using SKBitmap rgba = bitmap.ColorType == SKColorType.Rgba8888 ? bitmap.Copy() : bitmap.Copy(SKColorType.Rgba8888);
        ReadOnlySpan<byte> src = rgba.GetPixelSpan();
        int stride = rgba.RowBytes;
        for (int y = 0; y < h; y++)
        {
            int s = y * stride;
            int d = y * w;
            for (int x = 0; x < w; x++)
            {
                int o = s + (x * 4);

                // OpenCV's colour-to-grey rule (BT.601 weights in 14-bit fixed point), so a colour
                // scan reads to the same grey the prototype read with cv2.imread(..., 0).
                // A transparent pixel counts as white paper.
                int r = src[o];
                int g = src[o + 1];
                int b = src[o + 2];
                int a = src[o + 3];
                if (a < 255)
                {
                    r = ((r * a) + (255 * (255 - a))) / 255;
                    g = ((g * a) + (255 * (255 - a))) / 255;
                    b = ((b * a) + (255 * (255 - a))) / 255;
                }

                grey[d + x] = (byte)(((r * 4899) + (g * 9617) + (b * 1868) + 8192) >> 14);
            }
        }

        return new GreyImage(w, h, grey);
    }

    /// <summary>Returns the picture turned clockwise by 0, 90, 180 or 270 degrees.</summary>
    /// <param name="turnCw">Degrees clockwise.</param>
    /// <returns>A new picture (or this one for 0).</returns>
    public GreyImage Rotate(int turnCw)
    {
        int t = ((turnCw % 360) + 360) % 360;
        if (t == 0)
        {
            return this;
        }

        int w = Width;
        int h = Height;
        byte[] src = Pixels;
        if (t == 180)
        {
            byte[] o = new byte[w * h];
            int n = w * h;
            for (int i = 0; i < n; i++)
            {
                o[n - 1 - i] = src[i];
            }

            return new GreyImage(w, h, o);
        }

        // 90 clockwise: new size h x w; new (x', y') = (h - 1 - y, x).
        // 270 clockwise: new (x', y') = (y, w - 1 - x).
        byte[] r = new byte[w * h];
        int nw = h;
        for (int y = 0; y < h; y++)
        {
            int row = y * w;
            for (int x = 0; x < w; x++)
            {
                int nx = t == 90 ? h - 1 - y : y;
                int ny = t == 90 ? x : w - 1 - x;
                r[(ny * nw) + nx] = src[row + x];
            }
        }

        return new GreyImage(h, w, r);
    }
}
