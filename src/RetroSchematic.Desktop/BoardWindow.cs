//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Desktop
{
    /// <summary>
    /// The Board window (File > Board info...): the board facts (machine, board number, print
    /// and ECO versions, size, layers), the card's connectors, and the original schematic pages
    /// and board photos with thumbnails - import, replace and remove. Every change goes through
    /// <see cref="SheetEditor"/>, so it needs edit mode and can be undone. A .rsc links an
    /// imported file where it is; a .rsx (or a sheet not saved yet) keeps it inside.
    /// </summary>
    public sealed class BoardWindow : Window
    {
        private const int ThumbWidth = 160;

        private readonly SheetSession _session;
        private readonly StackPanel _root = new() { Spacing = 10, Margin = new Avalonia.Thickness(14) };

        private ComboBox _system = new();
        private TextBox _number = new(), _print = new(), _eco = new(), _width = new(), _height = new(), _layers = new(), _note = new();
        private readonly List<ConnectorRow> _connectors = new();

        /// <summary>
        /// Opens the window on a sheet.
        /// </summary>
        /// <param name="session">
        /// The open sheet.
        /// </param>
        public BoardWindow(SheetSession session)
        {
            _session = session;
            Title = "Board - " + session.Editor.Root.Sheet.Id;
            Icon = new WindowIcon(Avalonia.Platform.AssetLoader.Open(new Uri("avares://RetroSchematic/Assets/RetroSchematic.ico")));
            Width = 760;
            Height = 820;
            Content = new ScrollViewer { Content = _root };
            this.Bind(BackgroundProperty, this.GetResourceObservable("FluentBackgroundPrimaryBrush"));
            Refresh();
        }

        /// <summary>
        /// The last message: what was done, or why a change was refused.
        /// </summary>
        public string Message { get; private set; } = "";

        /// <summary>
        /// The thumbnails shown, one per source page then one per photo (null where the file
        /// cannot be read).
        /// </summary>
        public List<Bitmap?> Thumbnails { get; } = new();

        /// <summary>
        /// The board-fact fields, for tests: machine, number, print, ECO, width, height, layers.
        /// </summary>
        public (ComboBox System, TextBox Number, TextBox Print, TextBox Eco, TextBox Width, TextBox Height, TextBox Layers) Fields
            => (_system, _number, _print, _eco, _width, _height, _layers);

        /// <summary>
        /// Builds the window's content again from the sheet.
        /// </summary>
        public void Refresh()
        {
            _root.Children.Clear();
            _connectors.Clear();
            Thumbnails.Clear();
            Netlist n = _session.Editor.Root;
            bool editing = !_session.Editor.ReadOnly;
            if (!editing)
                _root.Children.Add(Note("Read-only: turn on edit mode (Ctrl+E) to change the board."));
            if (Message.Length > 0)
                _root.Children.Add(Note(Message));

            // the board facts
            _root.Children.Add(Header("Board"));
            BoardSection b = n.Board ?? new BoardSection();
            _system = new ComboBox { ItemsSource = new[] { "not known", "NORD-10", "ND-100" }, SelectedItem = b.NdSystem ?? "not known", MinWidth = 160 };
            _number = Field(b.BoardNumber, "e.g. 322049");
            _print = Field(b.PrintVersion, "e.g. I");
            _eco = Field(b.EcoVersion, "e.g. 3");
            _width = Field(b.Size?.WidthMm.ToString(CultureInfo.InvariantCulture), "mm");
            _height = Field(b.Size?.HeightMm.ToString(CultureInfo.InvariantCulture), "mm");
            _layers = Field(b.Layers?.ToString(CultureInfo.InvariantCulture), "e.g. 2");
            _note = Field(b.Note, "where the values were read");
            Grid facts = Form();
            AddField(facts, "ND machine", _system);
            AddField(facts, "Board number", _number);
            AddField(facts, "Print version", _print);
            AddField(facts, "ECO version", _eco);
            AddField(facts, "Width (mm)", _width);
            AddField(facts, "Height (mm)", _height);
            AddField(facts, "Layers", _layers);
            AddField(facts, "Note", _note);
            _root.Children.Add(facts);
            _root.Children.Add(Action("Save board facts", editing, (s, e) => SaveBoard()));

            // the connectors
            _root.Children.Add(Header("Connectors"));
            _root.Children.Add(Note(n.Sheet.Bus == MachineBus.Nd100
                ? "ND-100: plugs A, B and C, each its own entry (P.A, P.B, P.C), rows a/b/c x 32 pins (DIN 41612). C is the ND-100 bus; A and B are card specific or not used."
                : "NORD-10: one backplane connector (P, pins 0-99, its letter not known) and a front connector (F) if the card has one."));
            for (int i = 0; n.Connectors != null && i < n.Connectors.Count; i++)
                AddConnectorRow(n.Connectors[i], editing);
            _root.Children.Add(Action("Add a connector", editing, (s, e) => AddConnectorRow(new ConnectorUse { Ref = "" }, true, at: _root.Children.IndexOf((Control)s!))));

            // the original pages
            _root.Children.Add(Header("Original pages"));
            for (int i = 0; n.Sources != null && i < n.Sources.Count; i++)
                _root.Children.Add(SourceRow(n.Sources[i], i, editing));
            if (n.Sources == null)
                _root.Children.Add(Note("No original page yet."));
            _root.Children.Add(Action("Import page...", editing, async (s, e) => await PickAndImportSource(-1)));

            // the photos
            _root.Children.Add(Header("Photos of the board"));
            for (int i = 0; n.Photos != null && i < n.Photos.Count; i++)
                _root.Children.Add(PhotoRow(n.Photos[i], i, editing));
            if (n.Photos == null)
                _root.Children.Add(Note("No photo yet."));
            StackPanel add = new() { Orientation = Orientation.Horizontal, Spacing = 6 };
            add.Children.Add(Action("Import component side...", editing, async (s, e) => await PickAndImportPhoto(BoardSide.Component, -1)));
            add.Children.Add(Action("Import solder side...", editing, async (s, e) => await PickAndImportPhoto(BoardSide.Solder, -1)));
            _root.Children.Add(add);
        }

        /// <summary>
        /// Saves the board-fact fields into the sheet.
        /// </summary>
        /// <returns>
        /// True when saved.
        /// </returns>
        public bool SaveBoard()
        {
            BoardSection b = new()
            {
                NdSystem = _system.SelectedItem as string == "not known" ? null : _system.SelectedItem as string,
                BoardNumber = Clean(_number.Text),
                PrintVersion = Clean(_print.Text),
                EcoVersion = Clean(_eco.Text),
                Note = Clean(_note.Text),
            };
            if (!Number(_width.Text, "width", out double? w) || !Number(_height.Text, "height", out double? h))
                return false;
            if (w != null || h != null)
            {
                if (w == null || h == null)
                    return Fail("Give both width and height, or neither.");
                b.Size = new BoardSize { WidthMm = w.Value, HeightMm = h.Value };
            }
            if (Clean(_layers.Text) is string layers)
            {
                if (!int.TryParse(layers, NumberStyles.Integer, CultureInfo.InvariantCulture, out int l) || l < 1)
                    return Fail("Layers is a whole number, 1 or more.");
                b.Layers = l;
            }
            bool empty = b.NdSystem == null && b.BoardNumber == null && b.PrintVersion == null && b.EcoVersion == null && b.Size == null && b.Layers == null && b.Note == null;
            return Change(() => _session.Editor.SetBoard(empty ? null : b), "Saved the board facts.");
        }

        /// <summary>
        /// Imports original pages from a file: a picture (one page), or the given pages of a PDF
        /// (one entry each).
        /// </summary>
        /// <param name="path">
        /// The file.
        /// </param>
        /// <param name="pdfPages">
        /// For a PDF: the pages, 1 = first. Ignored for a picture.
        /// </param>
        /// <param name="replace">
        /// The entry to replace (only with one page), or -1 to add.
        /// </param>
        /// <returns>
        /// True when imported.
        /// </returns>
        public bool ImportSource(string path, IReadOnlyList<int> pdfPages, int replace = -1)
        {
            bool pdf = SheetAttachments.IsPdf(path);
            if (pdf && pdfPages.Count == 0)
                return Fail("Say which pages of the PDF to use.");
            if (replace >= 0 && pdf && pdfPages.Count != 1)
                return Fail("Replace one page with one page.");
            return Change(() =>
            {
                string link = _session.Editor.ImportFile(path, "sources", Hold());
                int count = pdf ? pdfPages.Count : 1;
                for (int i = 0; i < count; i++)
                {
                    string title = Path.GetFileNameWithoutExtension(path) + (pdf ? " page " + pdfPages[i].ToString(CultureInfo.InvariantCulture) : "");
                    _session.Editor.SetSource(new SourcePage { File = link, PdfPage = pdf ? pdfPages[i] : null, Title = title }, replace);
                }
            }, "Imported " + Path.GetFileName(path) + ".");
        }

        /// <summary>
        /// Imports a photo of one side of the board.
        /// </summary>
        /// <param name="path">
        /// The picture.
        /// </param>
        /// <param name="side">
        /// Which side it shows.
        /// </param>
        /// <param name="replace">
        /// The entry to replace, or -1 to add.
        /// </param>
        /// <returns>
        /// True when imported.
        /// </returns>
        public bool ImportPhoto(string path, BoardSide side, int replace = -1)
            => Change(() =>
            {
                string link = _session.Editor.ImportFile(path, "photos", Hold());
                _session.Editor.SetPhoto(new BoardPhoto { File = link, Side = side }, replace);
            }, "Imported " + Path.GetFileName(path) + ".");

        /// <summary>
        /// Removes an original page.
        /// </summary>
        /// <param name="index">
        /// 0 first.
        /// </param>
        /// <returns>
        /// True when removed.
        /// </returns>
        public bool RemoveSource(int index) => Change(() => _session.Editor.RemoveSource(index), "Removed the page.");

        /// <summary>
        /// Removes a photo.
        /// </summary>
        /// <param name="index">
        /// 0 first.
        /// </param>
        /// <returns>
        /// True when removed.
        /// </returns>
        public bool RemovePhoto(int index) => Change(() => _session.Editor.RemovePhoto(index), "Removed the photo.");

        /// <summary>
        /// Reads a page list such as "64", "63-65" or "1, 3, 5".
        /// </summary>
        /// <param name="text">
        /// The text.
        /// </param>
        /// <param name="count">
        /// The PDF's page count.
        /// </param>
        /// <param name="pages">
        /// The pages, 1 = first.
        /// </param>
        /// <returns>
        /// True when every page is a number within the PDF.
        /// </returns>
        public static bool ParsePages(string text, int count, out List<int> pages)
        {
            pages = new List<int>();
            string[] parts = text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                string[] range = parts[i].Split('-', StringSplitOptions.TrimEntries);
                if (range.Length > 2 || !int.TryParse(range[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int a))
                    return false;
                int b = a;
                if (range.Length == 2 && !int.TryParse(range[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out b))
                    return false;
                if (a < 1 || b < a || b > count)
                    return false;
                for (int p = a; p <= b; p++)
                    pages.Add(p);
            }
            return pages.Count > 0;
        }

        // a .rsc links an imported file where it is; a .rsx or an unsaved sheet keeps it inside
        private bool Hold() => _session.SheetPath == null || SheetFile.IsZipPath(_session.SheetPath);

        private bool Change(Action change, string done)
        {
            try
            {
                change();
                Message = done;
            }
            catch (Exception ex) when (ex is SheetEditException || ex is IOException || ex is UnauthorizedAccessException)
            {
                Message = ex.Message;
                Refresh();
                return false;
            }
            Refresh();
            return true;
        }

        private bool Fail(string why)
        {
            Message = why;
            Refresh();
            return false;
        }

        private bool Number(string? text, string what, out double? value)
        {
            value = null;
            if (Clean(text) is not string t)
                return true;
            if (double.TryParse(t, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) && v > 0)
            {
                value = v;
                return true;
            }
            Fail("The " + what + " is a number of millimetres, more than 0.");
            return false;
        }

        private async Task PickAndImportSource(int replace)
        {
            string? path = await PickFile("Original page", new[] { "*.png", "*.jpg", "*.jpeg", "*.pdf" });
            if (path == null)
                return;
            List<int> pages = new();
            if (SheetAttachments.IsPdf(path))
            {
                int count;
                try
                {
                    count = PdfPages.Count(File.ReadAllBytes(path));
                }
                catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is InvalidOperationException)
                {
                    Fail("The PDF cannot be read: " + ex.Message);
                    return;
                }
                string? text = await AskPages(Path.GetFileName(path), count);
                if (text == null)
                    return;
                if (!ParsePages(text, count, out pages))
                {
                    Fail("\"" + text + "\" is not a page list within 1-" + count.ToString(CultureInfo.InvariantCulture) + " (such as 64, 63-65 or 1, 3).");
                    return;
                }
            }
            ImportSource(path, pages, replace);
        }

        private async Task PickAndImportPhoto(BoardSide side, int replace)
        {
            string? path = await PickFile(side == BoardSide.Component ? "Photo of the component side" : "Photo of the solder side", new[] { "*.png", "*.jpg", "*.jpeg" });
            if (path != null)
                ImportPhoto(path, side, replace);
        }

        private async Task<string?> PickFile(string title, string[] patterns)
        {
            IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = new[] { new FilePickerFileType(title) { Patterns = patterns } },
            });
            return files.Count == 0 ? null : files[0].TryGetLocalPath();
        }

        // a small dialog: which pages of the PDF
        private async Task<string?> AskPages(string file, int count)
        {
            TextBox box = Field("", "e.g. 64, or 63-65, or 1, 3");
            Window dialog = new() { Title = "Which pages?", Width = 380, SizeToContent = SizeToContent.Height };
            dialog.Bind(BackgroundProperty, dialog.GetResourceObservable("FluentBackgroundPrimaryBrush"));
            string? answer = null;
            StackPanel p = new() { Margin = new Avalonia.Thickness(14), Spacing = 8 };
            p.Children.Add(new TextBlock { Text = file + " has " + count.ToString(CultureInfo.InvariantCulture) + " pages. Which are the original pages of this sheet?", TextWrapping = TextWrapping.Wrap });
            p.Children.Add(box);
            StackPanel buttons = new() { Orientation = Orientation.Horizontal, Spacing = 6, HorizontalAlignment = HorizontalAlignment.Right };
            Button ok = new() { Content = "Import", IsDefault = true };
            ok.Classes.Add("fluent-button");
            ok.Click += (s, e) => { answer = box.Text; dialog.Close(); };
            Button cancel = new() { Content = "Cancel", IsCancel = true };
            cancel.Classes.Add("fluent-button");
            cancel.Click += (s, e) => dialog.Close();
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);
            p.Children.Add(buttons);
            dialog.Content = p;
            await dialog.ShowDialog(this);
            return answer;
        }

        private Control SourceRow(SourcePage s, int index, bool editing)
        {
            StackPanel text = new() { Spacing = 2 };
            text.Children.Add(new TextBlock { Text = MainWindow.PageTitle(s, index), FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap });
            text.Children.Add(Muted(s.File + (_session.Editor.Files.Holds(s.File) ? "  (inside the sheet)" : "  (linked)")));
            if (s.Original != null)
                text.Children.Add(Muted("from " + s.Original.File + " page " + s.Original.PdfPage?.ToString(CultureInfo.InvariantCulture) + " at " + s.Original.Dpi?.ToString(CultureInfo.InvariantCulture) + " dpi"));
            if (s.Note != null)
                text.Children.Add(Muted(s.Note));
            StackPanel buttons = new() { Orientation = Orientation.Horizontal, Spacing = 6 };
            buttons.Children.Add(Action("Replace...", editing, async (o, e) => await PickAndImportSource(index)));
            buttons.Children.Add(Action("Remove", editing, (o, e) => RemoveSource(index)));
            text.Children.Add(buttons);
            return ThumbRow(Thumb(s.File, s.PdfPage), text);
        }

        private Control PhotoRow(BoardPhoto p, int index, bool editing)
        {
            StackPanel text = new() { Spacing = 2 };
            text.Children.Add(new TextBlock { Text = (index + 1).ToString(CultureInfo.InvariantCulture) + ". " + (p.Side == BoardSide.Component ? "Component side" : "Solder side"), FontWeight = FontWeight.SemiBold });
            text.Children.Add(Muted(p.File + (_session.Editor.Files.Holds(p.File) ? "  (inside the sheet)" : "  (linked)")));
            if (p.Note != null)
                text.Children.Add(Muted(p.Note));
            StackPanel buttons = new() { Orientation = Orientation.Horizontal, Spacing = 6 };
            buttons.Children.Add(Action("Replace...", editing, async (o, e) => await PickAndImportPhoto(p.Side, index)));
            buttons.Children.Add(Action("Remove", editing, (o, e) => RemovePhoto(index)));
            text.Children.Add(buttons);
            return ThumbRow(Thumb(p.File, null), text);
        }

        // a small picture of a page or photo; a PDF page is rendered at a low resolution
        private Control Thumb(string file, int? pdfPage)
        {
            Bitmap? bmp = null;
            try
            {
                byte[]? bytes = _session.Editor.Files.Read(file);
                if (bytes != null && SheetAttachments.IsPdf(file))
                    bytes = pdfPage != null && SheetEditor.Pdf != null ? SheetEditor.Pdf(bytes, pdfPage.Value, 24) : null;
                if (bytes != null)
                {
                    using MemoryStream ms = new(bytes, writable: false);
                    bmp = Bitmap.DecodeToWidth(ms, ThumbWidth);
                }
            }
            catch (Exception ex) when (ex is IOException || ex is ArgumentException || ex is InvalidOperationException)
            {
                bmp = null;
            }
            Thumbnails.Add(bmp);
            if (bmp == null)
                return new Border { Width = ThumbWidth, Height = 90, Child = Muted("cannot be read") };
            return new Image { Source = bmp, Width = ThumbWidth, Stretch = Stretch.Uniform };
        }

        private static Control ThumbRow(Control thumb, Control text)
        {
            Grid g = new() { ColumnSpacing = 10 };
            g.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(ThumbWidth)));
            g.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
            Grid.SetColumn(text, 1);
            thumb.VerticalAlignment = VerticalAlignment.Top;
            g.Children.Add(thumb);
            g.Children.Add(text);
            return g;
        }

        private void AddConnectorRow(ConnectorUse c, bool editing, int at = -1)
        {
            ConnectorRow r = new(c);
            Grid form = Form();
            AddField(form, "Connector", r.Ref);
            AddField(form, "Catalogue", r.Catalog);
            AddField(form, "Used for", r.Use);
            AddField(form, "", r.Used);
            AddField(form, "Standard", r.Standard);
            AddField(form, "Pins", r.Pins);
            AddField(form, "Rows", r.Rows);
            AddField(form, "Pins per row", r.PinsPerRow);
            AddField(form, "Note", r.Note);
            StackPanel buttons = new() { Orientation = Orientation.Horizontal, Spacing = 6 };
            buttons.Children.Add(Action("Save connector", editing, (s, e) => SaveConnector(r)));
            if (c.Ref.Length > 0)
                buttons.Children.Add(Action("Remove", editing, (s, e) => Change(() => _session.Editor.RemoveConnector(c.Ref), "Removed connector " + c.Ref + ".")));
            AddField(form, "", buttons);
            Border box = new() { Child = form, Padding = new Avalonia.Thickness(8), CornerRadius = new Avalonia.CornerRadius(4), BorderThickness = new Avalonia.Thickness(1) };
            box.Bind(Border.BorderBrushProperty, box.GetResourceObservable("FluentBorderLightBrush"));
            if (at >= 0)
                _root.Children.Insert(at, box);
            else
                _root.Children.Add(box);
            _connectors.Add(r);
        }

        /// <summary>
        /// The connector entries shown, in order (a new, unsaved one last).
        /// </summary>
        internal List<ConnectorRow> ConnectorRows => _connectors;

        /// <summary>
        /// Saves one connector row into the sheet.
        /// </summary>
        /// <param name="r">
        /// The row.
        /// </param>
        /// <returns>
        /// True when saved.
        /// </returns>
        internal bool SaveConnector(ConnectorRow r)
        {
            ConnectorUse c = new()
            {
                Ref = (r.Ref.Text ?? "").Trim(),
                Catalog = Clean(r.Catalog.Text),
                Use = Clean(r.Use.Text),
                Used = r.Used.IsChecked == true ? null : false,
                Standard = Clean(r.Standard.Text),
                Pins = Clean(r.Pins.Text),
                Note = Clean(r.Note.Text),
            };
            if (Clean(r.Rows.Text) is string rows)
            {
                c.Rows = new List<string>();
                string[] parts = rows.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                for (int i = 0; i < parts.Length; i++)
                    c.Rows.Add(parts[i]);
            }
            if (Clean(r.PinsPerRow.Text) is string ppr)
            {
                if (!int.TryParse(ppr, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) || v < 1)
                    return Fail("Pins per row is a whole number, 1 or more.");
                c.PinsPerRow = v;
            }
            return Change(() => _session.Editor.SetConnector(c), "Saved connector " + c.Ref + ".");
        }

        private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

        private static TextBlock Header(string text) => new() { Text = text, FontSize = 16, FontWeight = FontWeight.SemiBold, Margin = new Avalonia.Thickness(0, 8, 0, 0) };

        private static TextBlock Note(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap };

        private static TextBlock Muted(string text)
        {
            TextBlock t = new() { Text = text, TextWrapping = TextWrapping.Wrap };
            t.Bind(TextBlock.ForegroundProperty, t.GetResourceObservable("FluentTextSecondaryBrush"));
            return t;
        }

        private static TextBox Field(string? text, string watermark)
        {
            TextBox t = new() { Text = text ?? "", Watermark = watermark };
            t.Classes.Add("fluent-textbox");
            return t;
        }

        private static Button Action(string text, bool enabled, EventHandler<RoutedEventArgs> click)
        {
            Button b = new() { Content = text, IsEnabled = enabled, HorizontalAlignment = HorizontalAlignment.Left };
            b.Classes.Add("fluent-button");
            b.Click += click;
            return b;
        }

        private static Grid Form()
        {
            Grid g = new() { ColumnSpacing = 8, RowSpacing = 4 };
            g.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(110)));
            g.ColumnDefinitions.Add(new ColumnDefinition(1, GridUnitType.Star));
            return g;
        }

        private static void AddField(Grid g, string label, Control c)
        {
            int row = g.RowDefinitions.Count;
            g.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            TextBlock l = new() { Text = label, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(l, row);
            Grid.SetRow(c, row);
            Grid.SetColumn(c, 1);
            g.Children.Add(l);
            g.Children.Add(c);
        }

        /// <summary>
        /// The fields of one connector entry.
        /// </summary>
        internal sealed class ConnectorRow
        {
            /// <summary>
            /// Fills the fields from an entry.
            /// </summary>
            /// <param name="c">
            /// The entry.
            /// </param>
            public ConnectorRow(ConnectorUse c)
            {
                Ref = Field(c.Ref, "P, F, P.A, P.B, P.C");
                Catalog = Field(c.Catalog, "e.g. nord10-io-card-backplane, nd100-c-connector");
                Use = Field(c.Use, "what it carries on this card, or not used");
                Used = new CheckBox { Content = "used on this card", IsChecked = c.Used != false };
                Standard = Field(c.Standard, "e.g. DIN 41612");
                Pins = Field(c.Pins, "e.g. 0-99");
                Rows = Field(c.Rows == null ? null : string.Join(", ", c.Rows), "e.g. a, b, c");
                PinsPerRow = Field(c.PinsPerRow?.ToString(CultureInfo.InvariantCulture), "e.g. 32");
                Note = Field(c.Note, "");
            }

            /// <summary>The connector reference.</summary>
            public TextBox Ref { get; }

            /// <summary>The catalogue id.</summary>
            public TextBox Catalog { get; }

            /// <summary>What it carries.</summary>
            public TextBox Use { get; }

            /// <summary>Used on this card.</summary>
            public CheckBox Used { get; }

            /// <summary>The standard.</summary>
            public TextBox Standard { get; }

            /// <summary>The pin numbers in words.</summary>
            public TextBox Pins { get; }

            /// <summary>The row letters.</summary>
            public TextBox Rows { get; }

            /// <summary>Pins per row.</summary>
            public TextBox PinsPerRow { get; }

            /// <summary>Free text.</summary>
            public TextBox Note { get; }
        }
    }
}
