//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using RetroSchematic.Desktop.Controls;
using RetroSchematic.Model;
using RetroSchematic.Recognition;
using RetroSchematic.Render;

namespace RetroSchematic.Desktop
{
    /// <summary>
    /// Assisted reading on the Original view: "Analyse page" runs the page analyser (offline) and
    /// draws its finds, solid when sure and dashed when not, with the code read. Clicking a find
    /// shows it in the find bar: accept it as a part (code and type filled in from the reading and
    /// the catalogue, both editable) or link it to an existing part. The accepted box is stored as
    /// the part's symbol box on the page. Wire assist: click a wire end next to a part's box, pick
    /// the pin from the chip's pinout, and the pin goes on the signal named in the pin bar (another
    /// pin makes or joins that pin's signal). Every change is one undo step.
    /// </summary>
    public partial class MainWindow
    {
        /// <summary>The dpi a PDF source page is rendered at for the analyser (boxes carry it).</summary>
        private const int ReadDpi = 300;

        private const string NewPart = "(new part)";

        private static PageAnalyser? s_analyser;

        private PageAnalysis? _analysis;
        private int _analysedSource;
        private int? _analysedDpi;
        private PageFind? _pickedFind;
        private string? _wirePart;
        private bool _readingWired;

        /// <summary>
        /// Gets the last analysis of the page shown, or null.
        /// </summary>
        public PageAnalysis? Analysis => _analysis;

        /// <summary>
        /// Gets the find picked in the Original view, or null.
        /// </summary>
        public PageFind? PickedFind => _pickedFind;

        /// <summary>
        /// Gets the part wire assist picked (the pin bar is for it), or null.
        /// </summary>
        public string? WirePart => _wirePart;

        /// <summary>
        /// Gets the source page (1 = first) the Original view shows, or 0.
        /// </summary>
        public int ShownSource => PagePicker.SelectedIndex + 1;

        /// <summary>
        /// Runs the page analyser on the page the Original view shows and draws the finds. Waits for
        /// the result (the button runs it in the background).
        /// </summary>
        public void AnalyseShownPage()
        {
            (GreyImage picture, BoardGrid grid, HashSet<string> ids, int source, int? dpi)? job = AnalysisJob();
            if (job == null)
                return;
            s_analyser ??= new PageAnalyser(PageAnalyser.DefaultModelsDir);
            PageAnalysis a = s_analyser.Analyse(job.Value.picture, job.Value.grid, job.Value.ids);
            TakeAnalysis(a, job.Value.source, job.Value.dpi);
        }

        /// <summary>
        /// Picks a find to accept, as a click on it does.
        /// </summary>
        /// <param name="id">
        /// The find's number.
        /// </param>
        public void PickFind(int id)
        {
            for (int i = 0; _analysis != null && i < _analysis.Finds.Count; i++)
            {
                if (_analysis.Finds[i].Id == id)
                {
                    ShowFind(_analysis.Finds[i]);
                    return;
                }
            }
        }

        /// <summary>
        /// Accepts the picked find with the code, type and link in the find bar.
        /// </summary>
        /// <returns>
        /// The part the box is tied to, or null (nothing picked, refused, or a box without a part).
        /// </returns>
        public string? AcceptPickedFind()
        {
            if (_session == null || _pickedFind == null)
                return null;
            PageFind f = _pickedFind;
            string? link = FindLink.SelectedItem as string;
            SymbolBox box = new()
            {
                Source = _analysedSource, X = f.X, Y = f.Y, Width = f.Width, Height = f.Height, Dpi = _analysedDpi,
                Kind = f.Kind, GateKind = f.GateKind, Score = f.Score, By = "tool",
            };
            try
            {
                string? part = _session.Editor.AcceptFind(box, Blank(FindCode.Text), Blank(FindType.Text), link == null || link == NewPart ? null : link);
                ReadStatus.Text = part == null ? "Stored the " + f.Kind + " as a symbol box." : "Accepted as the box of " + part + ".";
                _pickedFind = null;
                FindBar.IsVisible = false;
                ShowReading();
                return part;
            }
            catch (SheetEditException ex)
            {
                ReadStatus.Text = ex.Message;
                return null;
            }
        }

        /// <summary>
        /// Wire assist: a click at a picture point (the shown picture's pixels) picks the part whose
        /// symbol box holds it or is near it and offers its pins.
        /// </summary>
        /// <param name="x">
        /// Picture x.
        /// </param>
        /// <param name="y">
        /// Picture y.
        /// </param>
        /// <returns>
        /// The part, or null when no box is there.
        /// </returns>
        public string? PickWireEnd(double x, double y)
        {
            if (_session == null)
                return null;
            int source = ShownSource;
            double k = ViewScale(source, ReadDpiOfBoxes(source));
            SymbolBox? b = SymbolBoxes.PartBoxAt(_session.Editor.Root, source, x / k, y / k, SheetEditor.WireEndMargin);
            if (b == null)
            {
                ReadStatus.Text = "No accepted part's box is near that point; accept the part first.";
                PinBar.IsVisible = false;
                _wirePart = null;
                return null;
            }
            _wirePart = b.Ref;
            List<ChipPin> pins = new(_session.Editor.PinoutOf(b.Ref!));
            pins.Sort((p, q) => p.Number.CompareTo(q.Number));
            List<string> items = new(pins.Count);
            for (int i = 0; i < pins.Count; i++)
                items.Add(pins[i].Number.ToString(CultureInfo.InvariantCulture) + "  " + (pins[i].Inverted ? "/" : "") + pins[i].Name);
            PinPicker.ItemsSource = items;
            PinPicker.SelectedIndex = items.Count > 0 ? 0 : -1;
            PinText.Text = "Pin of " + b.Ref;
            PinBar.IsVisible = true;
            ReadStatus.Text = items.Count == 0 ? b.Ref + " has no pin table in the catalogue." : "Pick the pin the wire ends on.";
            return b.Ref;
        }

        /// <summary>
        /// Wire assist: puts the pin picked in the pin bar on the signal (or pin) named there. The
        /// signal it ends on is kept in the bar, so the next pin joins the same signal.
        /// </summary>
        /// <returns>
        /// The signal, or null when refused.
        /// </returns>
        public string? ConnectPickedPin()
        {
            if (_session == null || _wirePart == null || PinPicker.SelectedItem is not string item)
                return null;
            string pin = _wirePart + "." + item.Substring(0, item.IndexOf(' '));
            try
            {
                string target = PinTarget.Text?.Trim() ?? "";
                string signal = target.Length == 0 ? throw new SheetEditException("Name the signal (or a pin) the wire is on.") : _session.Editor.ConnectTo(pin, target);
                PinTarget.Text = signal;
                ReadStatus.Text = "Pin " + pin + " is on signal " + signal + ".";
                return signal;
            }
            catch (SheetEditException ex)
            {
                ReadStatus.Text = ex.Message;
                return null;
            }
        }

        private async void OnAnalysePage(object? sender, RoutedEventArgs e)
        {
            (GreyImage picture, BoardGrid grid, HashSet<string> ids, int source, int? dpi)? job = AnalysisJob();
            if (job == null)
                return;
            AnalyseButton.IsEnabled = false;
            ReadStatus.Text = "Analysing the page...";
            try
            {
                PageAnalysis a = await Task.Run(() =>
                {
                    s_analyser ??= new PageAnalyser(PageAnalyser.DefaultModelsDir);
                    return s_analyser.Analyse(job.Value.picture, job.Value.grid, job.Value.ids);
                });
                if (ShownSource == job.Value.source)
                    TakeAnalysis(a, job.Value.source, job.Value.dpi);
            }
            catch (Exception ex) when (ex is InvalidOperationException || ex is System.IO.IOException || ex is Microsoft.ML.OnnxRuntime.OnnxRuntimeException)
            {
                ReadStatus.Text = "The page could not be analysed: " + ex.Message;
            }
            finally
            {
                AnalyseButton.IsEnabled = true;
            }
        }

        private void OnAcceptFind(object? sender, RoutedEventArgs e) => AcceptPickedFind();

        private void OnConnectPin(object? sender, RoutedEventArgs e) => ConnectPickedPin();

        private void OnWireAssist(object? sender, RoutedEventArgs e)
        {
            FindBar.IsVisible = false;
            _pickedFind = null;
            if (WireAssist.IsChecked != true)
                PinBar.IsVisible = false;
            ReadStatus.Text = WireAssist.IsChecked == true ? "Click a wire end next to an accepted part." : "";
            ShowReading();
        }

        // the picture, board grid and catalogue names for one run, or null with the reason shown
        private (GreyImage, BoardGrid, HashSet<string>, int, int?)? AnalysisJob()
        {
            if (_session == null)
                return null;
            int source = ShownSource;
            byte[] bytes;
            int? dpi;
            try
            {
                bytes = _session.Editor.SourcePicture(source, ReadDpi, out dpi);
            }
            catch (SheetEditException ex)
            {
                ReadStatus.Text = ex.Message;
                return null;
            }
            HashSet<string> ids = new(StringComparer.Ordinal);
            if (_session.Editor.Catalog != null)
                ids.UnionWith(_session.Editor.Catalog.Types());
            BoardGrid grid = _session.Editor.Root.Sheet.Bus == MachineBus.Nd100 ? BoardGrid.Nd100 : BoardGrid.Nord10;
            return (GreyImage.FromEncoded(bytes), grid, ids, source, dpi);
        }

        private void TakeAnalysis(PageAnalysis a, int source, int? dpi)
        {
            _analysis = a;
            _analysedSource = source;
            _analysedDpi = dpi;
            _pickedFind = null;
            FindBar.IsVisible = false;
            int chips = 0, gates = 0, sure = 0;
            for (int i = 0; i < a.Finds.Count; i++)
            {
                chips += a.Finds[i].Kind == "chip" ? 1 : 0;
                gates += a.Finds[i].Kind == "gate" ? 1 : 0;
                sure += a.Finds[i].IsSure ? 1 : 0;
            }
            ReadStatus.Text = string.Create(CultureInfo.InvariantCulture,
                $"{a.Finds.Count} finds: {chips} chips, {gates} gates, {sure} sure (solid), the rest unsure (dashed). Guesses until accepted; click one. {a.Milliseconds / 1000.0:0.0} s.");
            ShowReading();
        }

        private void WireReading()
        {
            if (_readingWired)
                return;
            _readingWired = true;
            OriginalView.PictureClicked += p =>
            {
                if (WireAssist.IsChecked == true)
                    PickWireEnd(p.X, p.Y);
                else
                    ClickFind(p.X, p.Y);
            };
        }

        // a click on the picture: the smallest open find under it
        private void ClickFind(double x, double y)
        {
            if (_analysis == null)
                return;
            double k = ViewScale(_analysedSource, _analysedDpi);
            PageFind? best = null;
            for (int i = 0; i < _analysis.Finds.Count; i++)
            {
                PageFind f = _analysis.Finds[i];
                if (x < f.X * k || x > (f.X + f.Width) * k || y < f.Y * k || y > (f.Y + f.Height) * k || Accepted(f))
                    continue;
                if (best == null || (double)f.Width * f.Height < (double)best.Width * best.Height)
                    best = f;
            }
            if (best != null)
                ShowFind(best);
        }

        private void ShowFind(PageFind f)
        {
            if (_session == null)
                return;
            _pickedFind = f;
            FindText.Text = string.Create(CultureInfo.InvariantCulture,
                $"Find {f.Id}: {f.Kind}{(f.GateKind != null ? " " + f.GateKind : "")}, score {f.Score:0.00}{(f.IsSure ? ", sure" : ", unsure")}{(f.CodeFromColumn ? ", code from the column" : "")}{(f.TypeText.Length > 0 ? ", type read \"" + f.TypeText + "\"" : "")}.");
            FindCode.Text = f.Code ?? "";
            Part? existing = f.Code == null ? null : _session.Editor.FindPart(f.Code);
            FindType.Text = existing?.Type ?? f.TypeId ?? "";
            List<string> parts = new() { NewPart };
            List<string> refs = new();
            for (int i = 0; i < _session.Editor.Root.Parts.Count; i++)
                refs.Add(_session.Editor.Root.Parts[i].Ref);
            refs.Sort(StringComparer.Ordinal);
            parts.AddRange(refs);
            FindLink.ItemsSource = parts;
            FindLink.SelectedIndex = 0;
            FindBar.IsVisible = true;
            ReadStatus.Text = _session.Editor.ReadOnly ? "Switch on edit mode to accept a find." : "Check the code and type, then Accept.";
            ShowReading();
        }

        // true when an accepted symbol box on the page covers the find
        private bool Accepted(PageFind f)
        {
            List<SymbolBox>? boxes = _session?.Editor.Root.SymbolBoxes;
            for (int i = 0; boxes != null && i < boxes.Count; i++)
            {
                SymbolBox b = boxes[i];
                if (b.Source != _analysedSource)
                    continue;
                double ix = Math.Min(b.X + b.Width, f.X + f.Width) - Math.Max(b.X, f.X);
                double iy = Math.Min(b.Y + b.Height, f.Y + f.Height) - Math.Max(b.Y, f.Y);
                if (ix > 0 && iy > 0 && ix * iy > 0.5 * Math.Min(b.Width * b.Height, (double)f.Width * f.Height))
                    return true;
            }
            return false;
        }

        // shown picture pixels per box pixel: a PDF page is shown at ShowDpi, boxes are at their dpi
        private static double ViewScale(int source, int? boxDpi) => boxDpi == null ? 1.0 : ShowDpi / (double)boxDpi.Value;

        // the dpi of boxes on a source: a PDF source is read at ReadDpi, a picture at its own pixels
        private int? ReadDpiOfBoxes(int source)
        {
            List<SourcePage>? s = _session?.Editor.Root.Sources;
            return s != null && source >= 1 && source <= s.Count && SheetAttachments.IsPdf(s[source - 1].File) ? ReadDpi : null;
        }

        /// <summary>
        /// Draws the accepted symbol boxes of the shown page and the open finds of its analysis,
        /// and shows the reading bar in the Original view.
        /// </summary>
        private void ShowReading()
        {
            WireReading();
            ReadBar.IsVisible = _session != null && _view == ViewOriginal;
            int source = ShownSource;
            List<PcbBox> boxes = new();
            List<SymbolBox>? stored = _session?.Editor.Root.SymbolBoxes;
            for (int i = 0; stored != null && i < stored.Count; i++)
            {
                SymbolBox b = stored[i];
                if (b.Source != source)
                    continue;
                double k = ViewScale(source, b.Dpi);
                string name = b.Ref ?? b.Text ?? b.Kind;
                if (b.Gate != null)
                    name += "/" + b.Gate.Value.ToString(CultureInfo.InvariantCulture);
                boxes.Add(new PcbBox
                {
                    Ref = name,
                    Owner = b.Ref == null ? "" : "part:" + b.Ref,
                    ByHand = b.By == "hand",
                    Corners = new[]
                    {
                        new GridPoint(b.X * k, b.Y * k), new GridPoint((b.X + b.Width) * k, b.Y * k),
                        new GridPoint((b.X + b.Width) * k, (b.Y + b.Height) * k), new GridPoint(b.X * k, (b.Y + b.Height) * k),
                    },
                });
            }
            OriginalView.Boxes = boxes;
            OriginalView.ShowBoxes = true;
            List<FindMark> marks = new();
            if (_analysis != null && _analysedSource == source)
            {
                double k = ViewScale(source, _analysedDpi);
                for (int i = 0; i < _analysis.Finds.Count; i++)
                {
                    PageFind f = _analysis.Finds[i];
                    if (Accepted(f))
                        continue;
                    marks.Add(new FindMark
                    {
                        Id = f.Id, X = f.X * k, Y = f.Y * k, Width = f.Width * k, Height = f.Height * k,
                        Label = f.Code ?? "", Sure = f.IsSure, Picked = f == _pickedFind,
                    });
                }
            }
            OriginalView.Finds = marks;
            OriginalView.InvalidateVisual();
        }

        private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }
}
