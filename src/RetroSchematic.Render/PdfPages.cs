//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.IO;
using PDFtoImage;

namespace RetroSchematic.Render
{
    /// <summary>
    /// PDF pages as pictures, for source pages: the Original view shows a PDF page, and packing
    /// a .rsx stores a PDF source as the PNG of its one page at 300 dpi. Uses PDFium through the
    /// PDFtoImage package (MIT).
    /// </summary>
    public static class PdfPages
    {
        /// <summary>
        /// Renders one page of a PDF as PNG.
        /// </summary>
        /// <param name="pdf">
        /// The PDF file's bytes.
        /// </param>
        /// <param name="page">
        /// The page, 1 = first.
        /// </param>
        /// <param name="dpi">
        /// Dots per inch.
        /// </param>
        /// <returns>
        /// The PNG bytes.
        /// </returns>
        /// <exception cref="IOException">
        /// The page does not exist or the PDF cannot be read.
        /// </exception>
        public static byte[] Render(byte[] pdf, int page, int dpi)
        {
            int count = Count(pdf);
            if (page < 1 || page > count)
                throw new IOException("The PDF has " + count + " pages; page " + page + " does not exist.");
            using MemoryStream png = new();
#pragma warning disable CA1416 // PDFium runs on Windows, Linux and macOS, the platforms this program is built for
            Conversion.SavePng(png, pdf, page - 1, options: new RenderOptions(Dpi: dpi));
#pragma warning restore CA1416
            return png.ToArray();
        }

        /// <summary>
        /// The number of pages in a PDF.
        /// </summary>
        /// <param name="pdf">
        /// The PDF file's bytes.
        /// </param>
        /// <returns>
        /// The page count.
        /// </returns>
        public static int Count(byte[] pdf)
        {
#pragma warning disable CA1416
            return Conversion.GetPageCount(pdf);
#pragma warning restore CA1416
        }
    }
}
