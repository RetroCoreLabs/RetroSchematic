//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace RetroSchematic.Model
{
    /// <summary>
    /// The sheet file on disk (docs/FORMAT.md, "File endings"). A sheet is the nd-netlist JSON,
    /// stored either as plain text (.rsc) or zipped (.rsx: a zip holding one entry,
    /// <see cref="ZipEntryName"/>). Old sheets ending in .json are plain text and still read.
    /// Reading looks at the content, not the name: a file that starts with the zip header
    /// "PK\x03\x04" is unzipped, anything else is read as text.
    /// </summary>
    public static class SheetFile
    {
        /// <summary>
        /// The ending of a plain-text sheet.
        /// </summary>
        public const string TextEnding = ".rsc";

        /// <summary>
        /// The ending of a zipped sheet.
        /// </summary>
        public const string ZipEnding = ".rsx";

        /// <summary>
        /// The name of the one entry inside a .rsx zip.
        /// </summary>
        public const string ZipEntryName = "sheet.json";

        /// <summary>
        /// Reads a sheet file's JSON text, unzipping it when the file is a zip.
        /// </summary>
        /// <param name="path">
        /// The sheet file (.rsc, .rsx or an old .json).
        /// </param>
        /// <returns>
        /// The JSON text.
        /// </returns>
        /// <exception cref="InvalidDataException">
        /// The file is a zip without a <see cref="ZipEntryName"/> entry.
        /// </exception>
        public static string ReadText(string path)
        {
            byte[] bytes = File.ReadAllBytes(path);
            if (!IsZip(bytes))
                return Encoding.UTF8.GetString(StripBom(bytes));
            using MemoryStream ms = new(bytes, writable: false);
            using ZipArchive zip = new(ms, ZipArchiveMode.Read);
            ZipArchiveEntry? entry = zip.GetEntry(ZipEntryName)
                ?? throw new InvalidDataException("The zipped sheet has no \"" + ZipEntryName + "\" entry.");
            using StreamReader reader = new(entry.Open(), Encoding.UTF8);
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Writes a sheet's JSON text: zipped when the path ends in .rsx, else as plain text.
        /// </summary>
        /// <param name="path">
        /// The file to write.
        /// </param>
        /// <param name="json">
        /// The JSON text.
        /// </param>
        public static void WriteText(string path, string json)
        {
            if (!IsZipPath(path))
            {
                File.WriteAllText(path, json);
                return;
            }
            using FileStream fs = new(path, FileMode.Create, FileAccess.Write);
            using ZipArchive zip = new(fs, ZipArchiveMode.Create);
            ZipArchiveEntry entry = zip.CreateEntry(ZipEntryName, CompressionLevel.Optimal);
            using Stream s = entry.Open();
            byte[] data = new UTF8Encoding(false).GetBytes(json);
            s.Write(data, 0, data.Length);
        }

        /// <summary>
        /// True when the path ends in .rsx (any case).
        /// </summary>
        /// <param name="path">
        /// A file path.
        /// </param>
        /// <returns>
        /// True for a zipped sheet's name.
        /// </returns>
        public static bool IsZipPath(string path) => path.EndsWith(ZipEnding, StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// True when the bytes start with the zip local file header "PK\x03\x04".
        /// </summary>
        /// <param name="bytes">
        /// The file's bytes.
        /// </param>
        /// <returns>
        /// True for a zip.
        /// </returns>
        public static bool IsZip(ReadOnlySpan<byte> bytes)
            => bytes.Length >= 4 && bytes[0] == (byte)'P' && bytes[1] == (byte)'K' && bytes[2] == 3 && bytes[3] == 4;

        private static ReadOnlySpan<byte> StripBom(byte[] bytes)
            => bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? bytes.AsSpan(3) : bytes;
    }
}
