//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace RetroSchematic.Model
{
    /// <summary>
    /// Renders one page of a PDF as a PNG picture.
    /// </summary>
    /// <param name="pdf">
    /// The PDF file's bytes.
    /// </param>
    /// <param name="page">
    /// The page, 1 = first.
    /// </param>
    /// <param name="dpi">
    /// The resolution, dots per inch.
    /// </param>
    /// <returns>
    /// The PNG file's bytes.
    /// </returns>
    public delegate byte[] PdfPageRenderer(byte[] pdf, int page, int dpi);

    /// <summary>
    /// The files a sheet points at: its source pages and board photos (docs/FORMAT.md, "Board,
    /// sources, photos, switches"). A plain-text sheet (.rsc) links them by a path relative to
    /// the sheet file. A zipped sheet (.rsx) stores them inside the zip, beside "sheet.json", as
    /// "sources/..." and "photos/...", so one file carries everything.
    /// <para>
    /// The store holds the bytes of every file it has been given (read from a .rsx, or imported
    /// in the window) and reads any other name from disk, relative to <see cref="Folder"/>.
    /// It is not part of the undo history: undo changes the links, and a file no link names is
    /// simply not written.
    /// </para>
    /// </summary>
    public sealed class SheetAttachments
    {
        /// <summary>
        /// The resolution a PDF page is rendered at when it is packed into a .rsx.
        /// </summary>
        public const int PackDpi = 300;

        private readonly Dictionary<string, byte[]> _held = new(StringComparer.Ordinal);

        /// <summary>
        /// The folder of the sheet file, which relative links start from; null for a sheet not
        /// saved yet.
        /// </summary>
        public string? Folder { get; set; }

        /// <summary>
        /// The store for a sheet file: the zip entries of a .rsx are read into memory, the links
        /// of a .rsc are read from disk when asked for.
        /// </summary>
        /// <param name="sheetPath">
        /// The sheet file.
        /// </param>
        /// <returns>
        /// The store.
        /// </returns>
        public static SheetAttachments Load(string sheetPath)
        {
            SheetAttachments a = new() { Folder = Path.GetDirectoryName(Path.GetFullPath(sheetPath)) };
            byte[] bytes = File.ReadAllBytes(sheetPath);
            if (!SheetFile.IsZip(bytes))
                return a;
            using MemoryStream ms = new(bytes, writable: false);
            using ZipArchive zip = new(ms, ZipArchiveMode.Read);
            for (int i = 0; i < zip.Entries.Count; i++)
            {
                ZipArchiveEntry e = zip.Entries[i];
                if (e.FullName == SheetFile.ZipEntryName || e.FullName.EndsWith('/'))
                    continue;
                using Stream s = e.Open();
                using MemoryStream copy = new();
                s.CopyTo(copy);
                a._held[e.FullName] = copy.ToArray();
            }
            return a;
        }

        /// <summary>
        /// The names held in memory (the entries of a .rsx and every imported file).
        /// </summary>
        public IReadOnlyCollection<string> HeldNames => _held.Keys;

        /// <summary>
        /// Puts a file's bytes into the store under a name (replacing one of that name).
        /// </summary>
        /// <param name="name">
        /// The link name, with forward slashes, such as "sources/2A50-p64.png".
        /// </param>
        /// <param name="bytes">
        /// The file's bytes.
        /// </param>
        public void Put(string name, byte[] bytes) => _held[name] = bytes;

        /// <summary>
        /// True when the store holds the bytes itself (not just a link to disk).
        /// </summary>
        /// <param name="name">
        /// The link name.
        /// </param>
        /// <returns>
        /// True when held in memory.
        /// </returns>
        public bool Holds(string name) => _held.ContainsKey(name);

        /// <summary>
        /// The full path a link names on disk, or null when there is no folder.
        /// </summary>
        /// <param name="name">
        /// The link, relative to the sheet.
        /// </param>
        /// <returns>
        /// The full path, or null.
        /// </returns>
        public string? PathOf(string name)
            => Folder == null ? (Path.IsPathRooted(name) ? name : null) : Path.GetFullPath(Path.Combine(Folder, name.Replace('/', Path.DirectorySeparatorChar)));

        /// <summary>
        /// The bytes of a linked file: held ones first, else read from disk; null when neither
        /// has it.
        /// </summary>
        /// <param name="name">
        /// The link name.
        /// </param>
        /// <returns>
        /// The bytes, or null.
        /// </returns>
        public byte[]? Read(string name)
        {
            if (_held.TryGetValue(name, out byte[]? b))
                return b;
            string? path = PathOf(name);
            return path != null && File.Exists(path) ? File.ReadAllBytes(path) : null;
        }

        /// <summary>
        /// True when a link names a PDF (by its ending).
        /// </summary>
        /// <param name="name">
        /// The link name.
        /// </param>
        /// <returns>
        /// True for a PDF.
        /// </returns>
        public static bool IsPdf(string name) => name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Writes a sheet with its files.
        /// <list type="bullet">
        ///   <item><description>.rsx: one zip holding "sheet.json" and every source page and photo
        ///   as "sources/NAME" and "photos/NAME"; the links in the written sheet name those
        ///   entries. A PDF source stores only its page, rendered as PNG at
        ///   <see cref="PackDpi"/> dpi ("sources/NAME-pN.png"), and keeps the PDF's name and page
        ///   in <c>original</c>.</description></item>
        ///   <item><description>.rsc: the JSON as text. Links stay links, made relative to the new
        ///   sheet's folder; a file the store only holds in memory is written beside the sheet in
        ///   "SHEETNAME-files/sources/" or ".../photos/".</description></item>
        /// </list>
        /// The netlist given is not changed; the one written is returned, and this store is set
        /// up for it (folder, held files), so the session can carry on with it.
        /// </summary>
        /// <param name="path">
        /// The sheet file to write.
        /// </param>
        /// <param name="netlist">
        /// The sheet.
        /// </param>
        /// <param name="toJson">
        /// Turns a netlist into the checked JSON text to write.
        /// </param>
        /// <param name="pdf">
        /// Renders a PDF page; needed only when a .rsx is written with a PDF source.
        /// </param>
        /// <returns>
        /// The netlist as written, with its links.
        /// </returns>
        /// <exception cref="IOException">
        /// A linked file cannot be found, or a PDF source cannot be rendered.
        /// </exception>
        public Netlist Save(string path, Netlist netlist, Func<Netlist, string> toJson, PdfPageRenderer? pdf)
        {
            Netlist copy = NetlistReader.Clone(netlist);
            string folder = Path.GetDirectoryName(Path.GetFullPath(path))!;
            if (SheetFile.IsZipPath(path))
            {
                Dictionary<string, byte[]> entries = new(StringComparer.Ordinal);
                for (int i = 0; copy.Sources != null && i < copy.Sources.Count; i++)
                {
                    SourcePage s = copy.Sources[i];
                    byte[] data = Need(s.File);
                    if (IsPdf(s.File))
                    {
                        if (s.PdfPage == null)
                            throw new IOException("Source " + s.File + " is a PDF without a page number.");
                        if (pdf == null)
                            throw new IOException("Source " + s.File + " is a PDF, and no PDF renderer was given to store its page as a picture.");
                        int page = s.PdfPage.Value;
                        byte[] png = pdf(data, page, PackDpi);
                        string name = Unique(entries, "sources/" + Path.GetFileNameWithoutExtension(s.File) + "-p" + page.ToString(System.Globalization.CultureInfo.InvariantCulture) + ".png");
                        entries[name] = png;
                        s.Original = new SourceOrigin { File = s.File, PdfPage = page, Dpi = PackDpi };
                        s.PdfPage = null;
                        s.File = name;
                    }
                    else
                    {
                        string name = Unique(entries, "sources/" + FileName(s.File));
                        entries[name] = data;
                        s.File = name;
                    }
                }
                for (int i = 0; copy.Photos != null && i < copy.Photos.Count; i++)
                {
                    BoardPhoto p = copy.Photos[i];
                    byte[] data = Need(p.File);
                    string name = Unique(entries, "photos/" + FileName(p.File));
                    entries[name] = data;
                    p.File = name;
                }
                string json = toJson(copy);
                using (FileStream fs = new(path, FileMode.Create, FileAccess.Write))
                using (ZipArchive zip = new(fs, ZipArchiveMode.Create))
                {
                    WriteEntry(zip, SheetFile.ZipEntryName, new UTF8Encoding(false).GetBytes(json), CompressionLevel.Optimal);
                    foreach (KeyValuePair<string, byte[]> e in entries)
                    {
                        // pictures are compressed already; deflating them again gains nothing
                        WriteEntry(zip, e.Key, e.Value, CompressionLevel.NoCompression);
                    }
                }
                _held.Clear();
                foreach (KeyValuePair<string, byte[]> e in entries)
                    _held[e.Key] = e.Value;
                Folder = folder;
                return copy;
            }

            // plain text: links, relative to the new folder
            string sideFolder = Path.GetFileNameWithoutExtension(path) + "-files";
            for (int i = 0; copy.Sources != null && i < copy.Sources.Count; i++)
                copy.Sources[i].File = Relink(copy.Sources[i].File, folder, sideFolder + "/sources/");
            for (int i = 0; copy.Photos != null && i < copy.Photos.Count; i++)
                copy.Photos[i].File = Relink(copy.Photos[i].File, folder, sideFolder + "/photos/");
            SheetFile.WriteText(path, toJson(copy));
            _held.Clear();
            Folder = folder;
            return copy;
        }

        // a link for a sheet in another folder: a held file is written out beside the sheet, a
        // file on disk keeps its place and gets a new relative path
        private string Relink(string name, string folder, string sidePrefix)
        {
            if (_held.TryGetValue(name, out byte[]? bytes))
            {
                string link = sidePrefix + FileName(name);
                string full = Path.Combine(folder, link.Replace('/', Path.DirectorySeparatorChar));
                Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                File.WriteAllBytes(full, bytes);
                return link;
            }
            string? old = PathOf(name) ?? throw new IOException("Linked file " + name + " has no folder to be found in.");
            if (!File.Exists(old))
                throw new IOException("Linked file " + name + " was not found at " + old + ".");
            return Path.GetRelativePath(folder, old).Replace('\\', '/');
        }

        private byte[] Need(string name) => Read(name) ?? throw new IOException("Linked file " + name + " was not found" + (PathOf(name) is string p ? " at " + p : "") + ".");

        private static string FileName(string name)
        {
            int slash = Math.Max(name.LastIndexOf('/'), name.LastIndexOf('\\'));
            return slash < 0 ? name : name.Substring(slash + 1);
        }

        // two files of the same name get -2, -3 ... before the ending
        private static string Unique(Dictionary<string, byte[]> taken, string name)
        {
            if (!taken.ContainsKey(name))
                return name;
            string stem = name.Substring(0, name.Length - Path.GetExtension(name).Length);
            string ext = Path.GetExtension(name);
            for (int n = 2; ; n++)
            {
                string candidate = stem + "-" + n.ToString(System.Globalization.CultureInfo.InvariantCulture) + ext;
                if (!taken.ContainsKey(candidate))
                    return candidate;
            }
        }

        private static void WriteEntry(ZipArchive zip, string name, byte[] data, CompressionLevel level)
        {
            ZipArchiveEntry entry = zip.CreateEntry(name, level);
            using Stream s = entry.Open();
            s.Write(data, 0, data.Length);
        }
    }
}
