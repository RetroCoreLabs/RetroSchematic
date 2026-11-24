//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
// RetroSchematic - viewer for Norsk Data circuit diagrams
//

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Controls.Primitives;     // ToggleButton
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using RetroSchematic.Desktop.Controls;
using RetroSchematic.Desktop.Themes;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Desktop
{
    /// <summary>
    /// The main window: search, parts and signals on the left, the drawing in the middle, details
    /// of the picked item on the right. It only connects controls to a <see cref="SheetSession"/>
    /// and its <see cref="SheetEditor"/>; what a click means, what is lit, what the details say
    /// and every change to the sheet all come from the library.
    /// </summary>
    public partial class MainWindow : Window
    {
        // Back / Forward history, and true while going back or forward (so that is not noted)
        private readonly NavHistory _history = new();
        private bool _navigating;

        private readonly List<string> _partOwners = new();
        private readonly List<string> _netOwners = new();
        private readonly List<string> _foundOwners = new();
        private SheetSession? _session;
        private bool _syncingLists;

        /// <summary>
        /// Creates the window and fills the theme menu.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            BuildThemeMenu();
            ShowStyleTicks(UiSettings.Instance.NdSymbols ? SymbolStyle.NorskData : SymbolStyle.Modern);
            Canvas.ToolMessage += OnToolMessage;
            Canvas.InstanceOpened += owner => OpenInstance(owner.Substring(5));
            Canvas.MenuRequested += owner => ShowDrawingMenu(owner);
            AddHandler(PointerPressedEvent, OnSideButtons, Avalonia.Interactivity.RoutingStrategies.Tunnel);
            Details.PinClicked += CentreOnPin;
            Details.SignalClicked += key => PickFromList("net:" + key);
            ApplyDisplay();
            TbSelect.IsChecked = true;
            OpenDemoItem.IsEnabled = SheetFiles.FindDemo(AppContext.BaseDirectory) != null;
            if (UiSettings.Instance.LoadProblem != null)
                StatusText.Text = UiSettings.Instance.LoadProblem;
        }

        /// <summary>
        /// The open sheet, or null.
        /// </summary>
        public SheetSession? Session => _session;

        /// <summary>
        /// Opens a sheet file, read-only. The catalogue is found by walking up from the sheet to
        /// the folder that holds <c>catalog/</c> (the repository's <c>src</c> folder).
        /// </summary>
        /// <param name="path">
        /// The sheet file (a netlist JSON file).
        /// </param>
        public void OpenSheet(string path)
        {
            SymbolStyle style = UiSettings.Instance.NdSymbols ? SymbolStyle.NorskData : SymbolStyle.Modern;
            Show(new SheetSession(path, style, ChipCatalog.Find(path)));
            StatusText.Text = path + "  -  " + _session!.Netlist.Parts.Count + " parts, " + _session.Netlist.Gates.Count + " gates, "
                + _session.Netlist.Nets.Count + " signals" + (_session.BusWarning == null ? "" : ".  " + _session.BusWarning);
            UiSettings.Instance.LastSheet = path;
            string? problem = UiSettings.Instance.Save();
            if (problem != null)
                StatusText.Text = problem;
        }

        /// <summary>
        /// Starts a new, empty design, already in edit mode. The catalogue is the one above the
        /// program's folder. The bus picks the bus-signal and connector catalogues; a sheet
        /// started without one shows a warning until it is set in Settings, This sheet.
        /// </summary>
        /// <param name="bus">
        /// Which machine bus the card is for.
        /// </param>
        public void NewSheet(MachineBus bus = MachineBus.Unknown)
        {
            SymbolStyle style = UiSettings.Instance.NdSymbols ? SymbolStyle.NorskData : SymbolStyle.Modern;
            Show(new SheetSession(SheetEditor.NewSheet("NEW", "New design", bus), null, style, ChipCatalog.Find(AppContext.BaseDirectory)));
            SetEditMode(true);
            StatusText.Text = (_session!.Editor.Catalog == null
                ? "New sheet - no chip catalogue found, so no chips can be placed."
                : "New sheet. Pick a chip to place.") + (_session.BusWarning == null ? "" : " " + _session.BusWarning);
        }

        /// <summary>
        /// Switches between read-only (every change refused) and edit mode.
        /// </summary>
        /// <param name="on">
        /// True for edit mode.
        /// </param>
        public void SetEditMode(bool on)
        {
            if (_session == null)
                return;
            _session.Editor.ReadOnly = !on;
            if (!on && Canvas.Tool != null)
                Canvas.Tool.Cancel();
            ShowEditState();
            ShowSignalEditor();
        }

        private void Show(SheetSession s)
        {
            if (_session != null)
            {
                _session.SelectionChanged -= OnSelectionChanged;
                _session.DrawingChanged -= OnDrawingChanged;
                _session.TraceChanged -= OnTraceChanged;
            }
            _session = s;
            _history.Clear();
            _session.SelectionChanged += OnSelectionChanged;
            _session.DrawingChanged += OnDrawingChanged;
            _session.TraceChanged += OnTraceChanged;
            Canvas.Session = s;
            // the app's defaults, with the sheet's own display values winning
            ApplyDisplay();
            FillLists();
            FillChipBox();
            _picturesFrom = "";
            LoadPictures();
            ShowView(ViewDrawing);
            ShowDetails(s, "");
            Record(s);
            SaveAsItem.IsEnabled = true;
            SaveSvgItem.IsEnabled = true;
            ExportPngItem.IsEnabled = true;
            EditModeItem.IsEnabled = true;
            TbExportPng.IsEnabled = true;
            TbExportSvg.IsEnabled = true;
            TbEditMode.IsEnabled = true;
            TbTrace.IsEnabled = true;
            TbZoomIn.IsEnabled = true;
            TbZoomOut.IsEnabled = true;
            TbFit.IsEnabled = true;
            if (TbTrace.IsChecked == true && Canvas.Tool != null)
                Canvas.Tool.TraceMode = true;
            OnTraceChanged(s);
            ShowEditState();
        }

        private void ShowEditState()
        {
            bool editing = _session != null && !_session.Editor.ReadOnly;
            EditModeItem.IsChecked = editing;
            ModeText.Text = editing ? "EDIT MODE" : "Read-only";
            ChipBox.IsEnabled = editing && _session!.Editor.Catalog != null;
            UndoItem.IsEnabled = editing && _session!.Editor.CanUndo;
            RedoItem.IsEnabled = editing && _session!.Editor.CanRedo;
            DeleteItem.IsEnabled = editing;
            SaveItem.IsEnabled = _session != null && _session.SheetPath != null;
            TbSave.IsEnabled = SaveItem.IsEnabled;
            bool netPicked = editing && _session!.Selected.StartsWith("net:", StringComparison.Ordinal);
            FixWireItem.IsEnabled = netPicked;
            FreeWireItem.IsEnabled = netPicked;
            TbUndo.IsEnabled = UndoItem.IsEnabled;
            TbRedo.IsEnabled = RedoItem.IsEnabled;
            TbEditMode.IsChecked = editing;
            TbPlace.IsEnabled = ChipBox.IsEnabled;
            TbWire.IsEnabled = editing;
            TbBus.IsEnabled = editing;
            NewBusItem.IsEnabled = editing;
            TbBus.IsChecked = Canvas.Tool?.BusPathFor != null;
            ShowBusPanel();
            bool canBlock = editing && _session!.PickedMembers().Count > 0;
            TbMakeBlock.IsEnabled = canBlock;
            MakeBlockItem.IsEnabled = canBlock;
            bool inside = _session != null && _session.Editor.LevelPath.Count > 0;
            TbUpLevel.IsEnabled = inside;
            UpLevelItem.IsEnabled = inside;
            ShowLevelBar();
            // leaving edit mode drops an editing tool back to Select (Trace stays: it works read-only)
            if (!editing && (TbPlace.IsChecked == true || TbWire.IsChecked == true))
                PickTool(TbSelect);
            if (_session != null)
                Title = "RetroSchematic - " + _session.Netlist.Sheet.Id + " " + _session.Netlist.Sheet.Title
                    + " (" + SheetSession.BusText(_session.Bus) + ")"
                    + (_session.Editor.Modified ? " (changed)" : "") + (editing ? " [edit]" : "");
        }

        private void FillLists()
        {
            _syncingLists = true;
            _partOwners.Clear();
            _netOwners.Clear();
            List<SideRow> parts = new();
            List<SideRow> nets = new();
            Netlist n = _session!.Netlist;
            for (int i = 0; i < n.Parts.Count; i++)
            {
                // board position | type | what it does here (muted)
                Part p = n.Parts[i];
                parts.Add(new SideRow { Main = p.Ref, Second = p.Type, Muted = p.Function, Owner = "part:" + p.Ref });
                _partOwners.Add("part:" + p.Ref);
            }
            // the switch blocks: not chips, so they are not on the drawing; picked here
            List<SwitchBlock>? switches = _session.Editor.Root.Switches;
            for (int i = 0; switches != null && i < switches.Count; i++)
            {
                SwitchBlock sw = switches[i];
                parts.Add(new SideRow { Main = sw.Ref, Second = "switch", Muted = sw.Function, Owner = SwitchOwner + sw.Ref });
                _partOwners.Add(SwitchOwner + sw.Ref);
            }
            for (int i = 0; i < n.Nets.Count; i++)
            {
                // in the chosen name style (/BD15 and BD15 differ, as BD15_0 and BD15_1 do)
                string key = SheetLayout.NetKey(n.Nets[i]);
                nets.Add(SignalRow(n.Nets[i], "net:" + key));
                _netOwners.Add("net:" + key);
            }
            PartsList.ItemTemplate = SideList.Parts;
            NetsList.ItemTemplate = SideList.Signals;
            PartsList.ItemsSource = parts;
            NetsList.ItemsSource = nets;
            _syncingLists = false;
            FillFound();
        }

        // a signal row: name in the chosen style | active level | pin count (muted)
        private SideRow SignalRow(Net net, string owner)
        {
            string? level = net.Active == ActiveLevel.Low ? "low" : net.Active == ActiveLevel.High ? "high" : null;
            int pins = net.Pins.Count;
            return new SideRow
            {
                Main = _session!.NameOf(net),
                Badge = level,
                Muted = pins.ToString(CultureInfo.InvariantCulture) + (pins == 1 ? " pin" : " pins"),
                Owner = owner,
            };
        }

        private void FillFound()
        {
            _syncingLists = true;
            _foundOwners.Clear();
            List<SideRow> shown = new();
            if (_session != null)
            {
                List<string> found = _session.Search(SearchBox.Text ?? "");
                for (int i = 0; i < found.Count; i++)
                {
                    _foundOwners.Add(found[i]);
                    shown.Add(FoundRow(found[i]));
                }
            }
            FoundList.ItemTemplate = SideList.Found;
            FoundList.ItemsSource = shown;
            SearchClear.IsVisible = !string.IsNullOrEmpty(SearchBox.Text);
            _syncingLists = false;
        }

        // a found item: kind icon | name | where it is (muted)
        private SideRow FoundRow(string owner)
        {
            Netlist n = _session!.Netlist;
            if (owner.StartsWith("part:", StringComparison.Ordinal))
            {
                string reference = owner.Substring(5);
                for (int i = 0; i < n.Parts.Count; i++)
                {
                    if (n.Parts[i].Ref == reference)
                        return new SideRow { Icon = "IconPlaceChip", Main = reference, Muted = n.Parts[i].Type + (n.Parts[i].Function == null ? "" : ", " + n.Parts[i].Function), Owner = owner };
                }
                return new SideRow { Icon = "IconPlaceChip", Main = reference, Muted = "part", Owner = owner };
            }
            if (owner.StartsWith("net:", StringComparison.Ordinal))
            {
                Net? net = _session.Editor.FindNetByKey(owner.Substring(4));
                if (net != null)
                    return new SideRow { Icon = "IconSignal", Main = _session.NameOf(net), Muted = "signal, " + net.Pins.Count.ToString(CultureInfo.InvariantCulture) + " pins", Owner = owner };
                return new SideRow { Icon = "IconSignal", Main = owner.Substring(4), Muted = "signal", Owner = owner };
            }
            if (owner.StartsWith("pin:", StringComparison.Ordinal))
            {
                // a pin: where it is is the signal on it
                string at = owner.Substring(4);
                for (int i = 0; i < n.Nets.Count; i++)
                {
                    List<PinReference> pins = n.Nets[i].Pins;
                    for (int j = 0; j < pins.Count; j++)
                    {
                        if (pins[j].At == at)
                            return new SideRow { Icon = "IconPinNumbers", Main = at, Muted = "pin on " + _session.NameOf(n.Nets[i]), Owner = owner };
                    }
                }
                return new SideRow { Icon = "IconPinNumbers", Main = at, Muted = "pin", Owner = owner };
            }
            return new SideRow { Main = owner, Owner = owner };
        }

        private void OnSearchClear(object? sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            SearchBox.Focus();
        }

        private void FillChipBox()
        {
            List<string> types = new() { "(none)" };
            if (_session?.Editor.Catalog != null)
                types.AddRange(_session.Editor.Catalog.Types());
            ChipBox.ItemsSource = types;
            ChipBox.SelectedIndex = 0;
        }

        private void BuildThemeMenu()
        {
            List<MenuItem> items = new();
            IReadOnlyList<ThemeDefinition> themes = ThemeManager.Instance.Themes;
            for (int i = 0; i < themes.Count; i++)
            {
                ThemeDefinition t = themes[i];
                MenuItem mi = new() { Header = t.DisplayName, Tag = t.Id, ToggleType = MenuItemToggleType.Radio };
                mi.IsChecked = ThemeManager.Instance.Current != null && ThemeManager.Instance.Current.Id == t.Id;
                mi.Click += OnThemePicked;
                items.Add(mi);
            }
            ThemeMenu.ItemsSource = items;
        }

        private void ShowStyleTicks(SymbolStyle style)
        {
            NdStyleItem.IsChecked = style == SymbolStyle.NorskData;
            ModernStyleItem.IsChecked = style == SymbolStyle.Modern;
        }

        private void SetStyle(SymbolStyle style)
        {
            UiSettings.Instance.NdSymbols = style == SymbolStyle.NorskData;
            SaveDefaults();
        }

        /// <summary>
        /// Gives the open sheet the app's defaults (its own display values still win) and makes
        /// every View tick, toolbar button, the theme and the trace pulse show what applies.
        /// </summary>
        public void ApplyDisplay()
        {
            DisplaySettings d = UiSettings.Instance.ToDefaults();
            if (_session != null)
            {
                _session.SetDefaults(d);
                d = _session.Display;
            }
            ShowStyleTicks(d.Symbols);
            PinNumbersItem.IsChecked = d.PinNumbers;
            TbPinNumbers.IsChecked = d.PinNumbers;
            BoardPositionsItem.IsChecked = d.BoardPositions;
            TbBoardPositions.IsChecked = d.BoardPositions;
            PortArrowsItem.IsChecked = d.PortArrows;
            TbPortArrows.IsChecked = d.PortArrows;
            ShowBusesItem.IsChecked = d.Buses;
            TbShowBuses.IsChecked = d.Buses;
            Canvas.TracePulse = d.TracePulse;
            // a sheet with a theme of its own shows in it; the saved choice is not changed
            if (ThemeManager.Instance.Current != null && !string.Equals(ThemeManager.Instance.Current.Id, d.Theme, StringComparison.OrdinalIgnoreCase))
                ThemeManager.Instance.Apply(d.Theme, persist: false);
            Canvas.InvalidateVisual();
        }

        /// <summary>
        /// A View switch: turns over the value in force for the open sheet. The sheet's own value
        /// when it has one (the sheet is then changed), else the default for all sheets.
        /// </summary>
        /// <param name="option">
        /// The option.
        /// </param>
        /// <param name="flipDefault">
        /// Turns over the default in the app settings.
        /// </param>
        private void ToggleOption(DisplayOption option, Action flipDefault)
        {
            if (_session != null && _session.ToggleSheetValue(option))
            {
                ApplyDisplay();
                ShowEditState();
                StatusText.Text = "Changed this sheet's own setting (save the sheet to keep it).";
                return;
            }
            flipDefault();
            SaveDefaults();
        }

        /// <summary>
        /// Saves the defaults after a View switch changed one, and applies them.
        /// </summary>
        private void SaveDefaults()
        {
            string? problem = UiSettings.Instance.Save();
            ApplyDisplay();
            if (problem != null)
                StatusText.Text = problem;
        }

        /// <summary>
        /// Turns drawing the buses on or off (this sheet's own value if it has one, else the default). Display only: the
        /// sheet keeps its buses. Public so the window tests can use it.
        /// </summary>
        public void ToggleShowBuses()
        {
            ToggleOption(DisplayOption.Buses, () => UiSettings.Instance.ShowBuses = !UiSettings.Instance.ShowBuses);
        }

        private void OnToggleShowBuses(object? sender, RoutedEventArgs e) => ToggleShowBuses();

        /// <summary>
        /// Opens the settings window. Public so the window tests can use it.
        /// </summary>
        /// <returns>
        /// The window, shown.
        /// </returns>
        public SettingsWindow OpenSettings()
        {
            SettingsWindow w = new(_session);
            w.Applied += () =>
            {
                ApplyDisplay();
                ShowEditState();
            };
            w.Show(this);
            return w;
        }

        private void OnSettings(object? sender, RoutedEventArgs e) => OpenSettings();

        /// <summary>
        /// Help > About: shows which build is running.
        /// </summary>
        /// <param name="sender">
        /// The menu item.
        /// </param>
        /// <param name="e">
        /// Not used.
        /// </param>
        private async void OnAbout(object? sender, RoutedEventArgs e) => await new AboutWindow().ShowDialog(this);

        /// <summary>
        /// The key of the signal the editing fields are for, or empty.
        /// </summary>
        private string _editedSignal = "";

        private static readonly string[] s_sourceKinds = { "the drawing", "a manual", "a reading document", "inferred" };
        private static readonly string[] s_confidences = { "read", "inferred", "open" };

        /// <summary>
        /// Shows the signal's description, source and notes for editing when the sheet is in edit
        /// mode and a signal (or anything of one) is picked; hides them otherwise.
        /// </summary>
        private void ShowSignalEditor()
        {
            string key = _session == null || _session.Editor.ReadOnly || _session.Selected.Length == 0 ? "" : _session.SignalAt(_session.Selected);
            Net? n = key.Length == 0 ? null : _session!.Editor.FindNetByKey(key);
            SignalEditPanel.IsVisible = n != null;
            _editedSignal = n == null ? "" : key;
            if (n == null)
                return;
            if (SourceKindBox.ItemsSource == null)
            {
                SourceKindBox.ItemsSource = s_sourceKinds;
                SourceConfidenceBox.ItemsSource = s_confidences;
            }
            SignalEditTitle.Text = "Describe " + _session!.NameOf(n);
            DescriptionBox.Text = n.Description ?? "";
            DescriptionSource? s = n.DescriptionSource;
            SourceKindBox.SelectedIndex = s == null ? 2 : (int)s.Kind;
            SourceDocumentBox.Text = s?.Document ?? "";
            SourcePlaceBox.Text = s?.Place ?? "";
            SourceConfidenceBox.SelectedIndex = s == null ? 0 : (int)s.Confidence;
            NotesBox.Text = n.Note ?? "";
        }

        /// <summary>
        /// Saves the editing fields into the signal (undoable; marks the sheet changed). Public so
        /// the window tests can use it.
        /// </summary>
        public void SaveSignalText()
        {
            if (_session == null || _editedSignal.Length == 0)
                return;
            DescriptionSource source = new()
            {
                Kind = (SourceKind)Math.Max(0, SourceKindBox.SelectedIndex),
                Document = string.IsNullOrWhiteSpace(SourceDocumentBox.Text) ? null : SourceDocumentBox.Text.Trim(),
                Place = string.IsNullOrWhiteSpace(SourcePlaceBox.Text) ? null : SourcePlaceBox.Text.Trim(),
                Confidence = (Confidence)Math.Max(0, SourceConfidenceBox.SelectedIndex),
            };
            try
            {
                _session.SetSignalInfo(_editedSignal, DescriptionBox.Text, source, NotesBox.Text);
                StatusText.Text = "Saved the text of the signal.";
            }
            catch (SheetEditException ex)
            {
                StatusText.Text = ex.Message;
            }
        }

        private void OnSignalTextSave(object? sender, RoutedEventArgs e) => SaveSignalText();

        /// <summary>
        /// Fills the Details panel for what is picked: a signal (or anything of one) and a part
        /// as cards built from the library's facts, anything else as text.
        /// </summary>
        /// <param name="session">
        /// The sheet.
        /// </param>
        /// <param name="owner">
        /// What is picked.
        /// </param>
        private void ShowDetails(SheetSession session, string owner)
        {
            SwitchPanel.IsVisible = false;
            if (owner.Length == 0)
            {
                // nothing picked: what the board is, with a way into the Board window
                Details.ShowBoard(session.Editor.Root, OpenBoardWindowFromDetails);
                return;
            }
            if (owner.StartsWith(SwitchOwner, StringComparison.Ordinal) && session.Editor.FindSwitch(owner.Substring(SwitchOwner.Length)) is SwitchBlock sw)
            {
                Details.ShowSwitch(sw);
                ShowSwitchEditor(sw);
                return;
            }
            if (owner.StartsWith("part:", StringComparison.Ordinal) && session.PartInfoOf(owner.Substring(5)) is PartInfo part)
            {
                Details.ShowPart(part);
                return;
            }
            if (SignalList.Of(session, owner) is SignalList list)
            {
                Details.ShowList(list);
                TracePanel.IsVisible = session.Traced.Length > 0;
                return;
            }
            string key = session.SignalAt(owner);
            SignalInfo? info = key.Length == 0 ? null : session.SignalInfoOf(key);
            if (info != null)
                Details.ShowSignal(info);
            else
                Details.ShowText("Details", session.Describe());
            // the separate list of traced pins is only needed when the panel shows something else
            TracePanel.IsVisible = session.Traced.Length > 0 && Details.SignalKey != session.Traced;
        }

        /// <summary>
        /// A pin row in the Details panel was clicked: centre the view on that pin.
        /// </summary>
        /// <param name="at">
        /// The pin.
        /// </param>
        public void CentreOnPin(string at)
        {
            if (_session != null && _session.Drawing.Pins.TryGetValue(at, out DrawPoint p))
                Canvas.CentreOn(p.X, p.Y);
        }

        /// <summary>
        /// Back / Forward through what was picked, as in a web browser.
        /// </summary>
        public NavHistory History => _history;

        /// <summary>
        /// Goes back to the place picked before, with its view.
        /// </summary>
        public void GoBack() => Navigate(back: true);

        /// <summary>
        /// Goes forward again after <see cref="GoBack"/>.
        /// </summary>
        public void GoForward() => Navigate(back: false);

        private void OnBack(object? sender, RoutedEventArgs e) => GoBack();

        private void OnForward(object? sender, RoutedEventArgs e) => GoForward();

        // the mouse's side buttons go back and forward, as in a browser
        private void OnSideButtons(object? sender, PointerPressedEventArgs e)
        {
            PointerPointProperties p = e.GetCurrentPoint(this).Properties;
            if (p.IsXButton1Pressed)
            {
                GoBack();
                e.Handled = true;
            }
            else if (p.IsXButton2Pressed)
            {
                GoForward();
                e.Handled = true;
            }
        }

        // keeps the view of the place being left, so going back to it puts the view back too
        private void KeepView()
        {
            NavPlace? cur = _history.Current;
            if (cur == null)
                return;
            Avalonia.Point c = Canvas.ViewCentre;
            cur.CentreX = c.X;
            cur.CentreY = c.Y;
            cur.Zoom = Canvas.Zoom;
            cur.HasView = true;
        }

        // notes the place shown now as a new history entry (unless going back or forward)
        private void Record(SheetSession session)
        {
            if (_navigating)
                return;
            NavPlace now = session.PlaceNow();
            NavPlace? cur = _history.Current;
            if (cur != null && !cur.SameAs(now))
                KeepView();
            _history.Push(now);
            ShowNav();
        }

        private void Navigate(bool back)
        {
            if (_session == null || (back ? !_history.CanBack : !_history.CanForward))
                return;
            KeepView();
            NavPlace p = back ? _history.Back()! : _history.Forward()!;
            _navigating = true;
            try
            {
                _session.GoTo(p);
            }
            finally
            {
                _navigating = false;
            }
            if (p.HasView)
                Canvas.SetView(p.CentreX, p.CentreY, p.Zoom);
            ShowNav();
        }

        // the buttons and menu items: on when there is somewhere to go, naming where
        private void ShowNav()
        {
            NavPlace? b = _history.BackPlace;
            NavPlace? f = _history.ForwardPlace;
            string bt = b == null ? "Back (Alt+Left)" : "Back to " + b.Label + " (Alt+Left)";
            string ft = f == null ? "Forward (Alt+Right)" : "Forward to " + f.Label + " (Alt+Right)";
            TbBack.IsEnabled = DetailsBack.IsEnabled = BackItem.IsEnabled = b != null;
            TbForward.IsEnabled = DetailsForward.IsEnabled = ForwardItem.IsEnabled = f != null;
            ToolTip.SetTip(TbBack, bt);
            ToolTip.SetTip(DetailsBack, bt);
            ToolTip.SetTip(TbForward, ft);
            ToolTip.SetTip(DetailsForward, ft);
        }

        private void OnSelectionChanged(SheetSession session, string owner)
        {
            _pickedSwitch = "";
            Record(session);
            ShowEditState();
            ShowSignalEditor();
            ShowDetails(session, owner);
            // keep the lists in step with a pick made on the drawing
            _syncingLists = true;
            PartsList.SelectedIndex = _partOwners.IndexOf(owner);
            NetsList.SelectedIndex = _netOwners.IndexOf(owner);
            _syncingLists = false;
        }

        private void OnDrawingChanged(SheetSession session)
        {
            FillLists();
            LoadPictures();
            if (session.Selected.Length == 0 && _pickedSwitch.Length == 0)
                ShowDetails(session, "");
            else if (_pickedSwitch.Length > 0)
                ShowDetails(session, SwitchOwner + _pickedSwitch);
            if (session.Selected.Length > 0)
                ShowDetails(session, session.Selected);
            ShowEditState();
        }

        private void OnToolMessage(string message)
        {
            StatusText.Text = message;
            ShowEditState();
        }

        /// <summary>
        /// A pick in the parts, signals or found list: exactly what a click on the drawing does
        /// (trace and pick, in the library), then the item is brought into view.
        /// </summary>
        /// <param name="owner">
        /// The owner name picked.
        /// </param>
        public void PickFromList(string owner)
        {
            if (_session == null)
                return;
            _session.PickAndTrace(owner);
            if (_session.Traced.Length > 0)
                StatusText.Text = "Tracing " + _session.TracedName + ": " + _session.TracedPins().Count + " pins. Tab jumps to each place.";
            if (_session.AreaOf(owner, out double[] area, out DrawPoint first))
                Canvas.ShowArea(area, first);
            Canvas.Focus();
        }

        private void OnPartPicked(object? sender, SelectionChangedEventArgs e)
        {
            if (!_syncingLists && _session != null && PartsList.SelectedIndex >= 0)
            {
                string owner = _partOwners[PartsList.SelectedIndex];
                if (owner.StartsWith(SwitchOwner, StringComparison.Ordinal))
                    PickSwitch(owner.Substring(SwitchOwner.Length));
                else
                    PickFromList(owner);
            }
        }

        private void OnNetPicked(object? sender, SelectionChangedEventArgs e)
        {
            if (!_syncingLists && _session != null && NetsList.SelectedIndex >= 0)
                PickFromList(_netOwners[NetsList.SelectedIndex]);
        }

        private void OnFoundPicked(object? sender, SelectionChangedEventArgs e)
        {
            if (!_syncingLists && _session != null && FoundList.SelectedIndex >= 0)
                PickFromList(_foundOwners[FoundList.SelectedIndex]);
        }

        private void OnSearchChanged(object? sender, TextChangedEventArgs e)
        {
            FillFound();
            if (!string.IsNullOrWhiteSpace(SearchBox.Text))
                ListTabs.SelectedItem = FoundTab;
        }

        private void OnChipPicked(object? sender, SelectionChangedEventArgs e)
        {
            if (Canvas.Tool == null)
                return;
            // choosing a chip type chooses the Place tool; "(none)" goes back to Select
            if (ChipBox.SelectedIndex > 0)
                PickTool(TbPlace);
            else if (TbPlace.IsChecked == true)
                PickTool(TbSelect);
        }

        private void OnThemePicked(object? sender, RoutedEventArgs e)
        {
            if (sender is MenuItem mi && mi.Tag is string id)
            {
                ThemeManager.Instance.Apply(id);
                BuildThemeMenu();
            }
        }

        private void OnNew(object? sender, RoutedEventArgs e) => NewSheet();

        private void OnNewForBus(object? sender, RoutedEventArgs e)
            => NewSheet((sender as MenuItem)?.Tag as string == "nd-100" ? MachineBus.Nd100 : MachineBus.Nord10);

        private async void OnOpen(object? sender, RoutedEventArgs e)
        {
            IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Open sheet",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("RetroSchematic (*.rsc;*.rsx)") { Patterns = new[] { "*.rsc", "*.rsx" } },
                    new FilePickerFileType("Old sheet (*.json)") { Patterns = new[] { "*.json" } },
                },
            });
            if (files.Count == 0)
                return;
            string? path = files[0].TryGetLocalPath();
            if (path != null)
                TryOpen(path);
        }

        private void OnOpenDemo(object? sender, RoutedEventArgs e)
        {
            string? path = SheetFiles.FindDemo(AppContext.BaseDirectory);
            if (path != null)
                TryOpen(path);
        }

        private void TryOpen(string path)
        {
            try
            {
                OpenSheet(path);
            }
            catch (Exception ex) when (ex is IOException || ex is FormatException || ex is System.Text.Json.JsonException || ex is NetlistException)
            {
                StatusText.Text = "Could not open " + path + ": " + ex.Message;
            }
        }

        private void OnSave(object? sender, RoutedEventArgs e)
        {
            if (_session?.SheetPath != null)
                SaveTo(_session.SheetPath);
        }

        private async void OnSaveAs(object? sender, RoutedEventArgs e)
        {
            if (_session == null)
                return;
            IStorageFile? file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save sheet as",
                // .rsc (plain JSON text) unless the zipped .rsx is picked
                DefaultExtension = "rsc",
                SuggestedFileName = _session.Netlist.Sheet.Id + ".rsc",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("RetroSchematic sheet (*.rsc)") { Patterns = new[] { "*.rsc" } },
                    new FilePickerFileType("RetroSchematic sheet, zipped (*.rsx)") { Patterns = new[] { "*.rsx" } },
                },
            });
            string? path = file?.TryGetLocalPath();
            if (path != null)
                SaveTo(path);
        }

        private void SaveTo(string path)
        {
            try
            {
                _session!.SaveAs(path);
                StatusText.Text = "Saved " + path;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException || ex is SheetEditException)
            {
                StatusText.Text = "Could not save " + path + ": " + ex.Message;
            }
            ShowEditState();
        }

        private async void OnSaveSvg(object? sender, RoutedEventArgs e)
        {
            if (_session == null)
                return;
            IStorageFile? file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save as SVG",
                DefaultExtension = "svg",
                SuggestedFileName = _session.Netlist.Sheet.Id + (_session.Style == SymbolStyle.NorskData ? "-nd" : "-modern") + ".svg",
            });
            string? path = file?.TryGetLocalPath();
            if (path == null)
                return;
            File.WriteAllText(path, SvgRenderer.Write(_session.Drawing, SchematicTheme.Find(ThemeManager.Instance.Current?.Id) ?? SchematicTheme.Default));
            StatusText.Text = "Saved " + path;
        }

        private async void OnExportPng(object? sender, RoutedEventArgs e)
        {
            if (_session == null)
                return;
            IStorageFile? file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export PNG",
                DefaultExtension = "png",
                SuggestedFileName = _session.Netlist.Sheet.Id + ".png",
            });
            string? path = file?.TryGetLocalPath();
            if (path == null)
                return;
            Canvas.ExportPng(path, 2);
            StatusText.Text = "Exported " + path;
        }

        private void OnToggleEditMode(object? sender, RoutedEventArgs e) => SetEditMode(_session != null && _session.Editor.ReadOnly);

        private void OnUndo(object? sender, RoutedEventArgs e)
        {
            Canvas.Tool?.Undo();
            OnToolMessage(Canvas.Tool?.Message ?? "");
        }

        private void OnRedo(object? sender, RoutedEventArgs e)
        {
            Canvas.Tool?.Redo();
            OnToolMessage(Canvas.Tool?.Message ?? "");
        }

        private void OnDelete(object? sender, RoutedEventArgs e)
        {
            Canvas.Tool?.DeleteSelected();
            OnToolMessage(Canvas.Tool?.Message ?? "");
        }

        private void OnTogglePinNumbers(object? sender, RoutedEventArgs e)
        {
            ToggleOption(DisplayOption.PinNumbers, () => UiSettings.Instance.PinNumbers = !UiSettings.Instance.PinNumbers);
        }

        private void OnToggleBoardPositions(object? sender, RoutedEventArgs e)
        {
            ToggleOption(DisplayOption.BoardPositions, () => UiSettings.Instance.BoardPositions = !UiSettings.Instance.BoardPositions);
        }

        /// <summary>
        /// Turns the block port arrows on or off (this sheet's own value if it has one, else the default). Public so the
        /// window tests can use it.
        /// </summary>
        public void TogglePortArrows()
        {
            ToggleOption(DisplayOption.PortArrows, () => UiSettings.Instance.PortArrows = !UiSettings.Instance.PortArrows);
        }

        private void OnTogglePortArrows(object? sender, RoutedEventArgs e) => TogglePortArrows();

        private void OnFixWire(object? sender, RoutedEventArgs e) => WireCommand(true);

        private void OnFreeWire(object? sender, RoutedEventArgs e) => WireCommand(false);

        private void WireCommand(bool fix)
        {
            if (_session == null || !_session.Selected.StartsWith("net:", StringComparison.Ordinal))
                return;
            string key = _session.Selected.Substring(4);
            try
            {
                if (fix)
                {
                    _session.FixWire(key);
                    StatusText.Text = "The wire of " + key + " is fixed; it is saved with the sheet.";
                }
                else
                {
                    Net n = _session.Editor.FindNetByKey(key)!;
                    _session.Editor.ClearWirePath(n.Name, n.Active == ActiveLevel.None ? null : n.Active);
                    StatusText.Text = "The wire of " + key + " is routed automatically again.";
                }
            }
            catch (SheetEditException ex)
            {
                StatusText.Text = ex.Message;
            }
            ShowEditState();
        }

        /// <summary>
        /// Opens the New bus dialog; when a bus is made, the canvas starts clicking out its path.
        /// </summary>
        /// <returns>
        /// The dialog, already shown (the tests drive it directly), or null outside edit mode.
        /// </returns>
        public NewBusWindow? OpenNewBusDialog()
        {
            if (_session == null || _session.Editor.ReadOnly)
            {
                StatusText.Text = "Switch on edit mode to make a bus.";
                TbBus.IsChecked = false;
                return null;
            }
            NewBusWindow dialog = new(_session);
            dialog.Closed += (s, a) =>
            {
                if (dialog.Created != null && Canvas.Tool != null)
                {
                    Canvas.Tool.StartBusPath(dialog.Created);
                    OnToolMessage(Canvas.Tool.Message);
                }
                ShowEditState();
            };
            dialog.Show(this);
            return dialog;
        }

        private void OnNewBus(object? sender, RoutedEventArgs e) => OpenNewBusDialog();

        private void ShowBusPanel()
        {
            string bus = _session != null && _session.Selected.StartsWith("bus:", StringComparison.Ordinal) ? _session.Selected.Substring(4) : "";
            BusPanel.IsVisible = bus.Length > 0 && _session!.Editor.FindBus(bus) != null;
            if (!BusPanel.IsVisible)
                return;
            BusTitle.Text = "Bus " + bus + " members";
            BusMemberList.ItemsSource = _session!.BusMembers(bus);
        }

        private void BusCommand(Action<SheetEditor, string> change)
        {
            if (_session == null || !_session.Selected.StartsWith("bus:", StringComparison.Ordinal))
                return;
            try
            {
                change(_session.Editor, _session.Selected.Substring(4));
            }
            catch (SheetEditException ex)
            {
                StatusText.Text = ex.Message;
            }
            ShowEditState();
        }

        private void OnBusAdd(object? sender, RoutedEventArgs e) => BusCommand((ed, bus) => ed.AddBusMember(bus, (BusAddBox.Text ?? "").Trim()));

        private void OnBusRemove(object? sender, RoutedEventArgs e)
            => BusCommand((ed, bus) =>
            {
                if (BusMemberList.SelectedItem is string key)
                    ed.RemoveBusMember(bus, key);
            });

        private void OnBusDelete(object? sender, RoutedEventArgs e) => BusCommand((ed, bus) => ed.DeleteBus(bus));

        private void OnBusRedraw(object? sender, RoutedEventArgs e)
            => BusCommand((ed, bus) =>
            {
                Canvas.Tool?.StartBusPath(bus);
                StatusText.Text = Canvas.Tool?.Message ?? "";
            });

        private void OnExit(object? sender, RoutedEventArgs e) => Close();

        /// <summary>
        /// The registry the file-association menu items write to. Tests put a fake here so the
        /// real registry is never touched; null means the real HKEY_CURRENT_USER on Windows.
        /// </summary>
        internal IRegistryWriter? Registry { get; set; }

        private FileAssociation? Association()
        {
            IRegistryWriter? r = Registry ?? (OperatingSystem.IsWindows() ? new CurrentUserRegistry() : null);
            return r == null ? null : new FileAssociation(r);
        }

        internal void OnAssociate(object? sender, RoutedEventArgs e)
        {
            FileAssociation? a = Association();
            string? exe = Environment.ProcessPath;
            if (a == null || exe == null)
            {
                StatusText.Text = "File association works on Windows only.";
                return;
            }
            a.Register(exe);
            StatusText.Text = ".rsc and .rsx files now open with RetroSchematic (for this Windows user).";
        }

        internal void OnUnassociate(object? sender, RoutedEventArgs e)
        {
            FileAssociation? a = Association();
            if (a == null)
            {
                StatusText.Text = "File association works on Windows only.";
                return;
            }
            a.Unregister();
            StatusText.Text = ".rsc and .rsx files no longer open with RetroSchematic.";
        }

        private void OnNdStyle(object? sender, RoutedEventArgs e) => SetStyle(SymbolStyle.NorskData);

        private void OnModernStyle(object? sender, RoutedEventArgs e) => SetStyle(SymbolStyle.Modern);

        // zoom and fit act on the view shown: the drawing, or one of the pictures
        private void OnFit(object? sender, RoutedEventArgs e)
        {
            if (ShownPicture() is ImageView v)
                v.Fit();
            else
                Canvas.FitToWindow();
        }

        private void OnZoomIn(object? sender, RoutedEventArgs e)
        {
            if (ShownPicture() is ImageView v)
                v.ZoomCentre(1.25);
            else
                Canvas.ZoomAt(new Avalonia.Point(Canvas.Bounds.Width / 2, Canvas.Bounds.Height / 2), 1.25);
        }

        private void OnZoomOut(object? sender, RoutedEventArgs e)
        {
            if (ShownPicture() is ImageView v)
                v.ZoomCentre(1 / 1.25);
            else
                Canvas.ZoomAt(new Avalonia.Point(Canvas.Bounds.Width / 2, Canvas.Bounds.Height / 2), 1 / 1.25);
        }

        private void OnToolSelect(object? sender, RoutedEventArgs e) => PickTool(TbSelect);

        private void OnToolPlace(object? sender, RoutedEventArgs e) => PickTool(TbPlace);

        private void OnToolWire(object? sender, RoutedEventArgs e) => PickTool(TbWire);

        private void OnToolTrace(object? sender, RoutedEventArgs e) => PickTool(TbTrace);

        /// <summary>
        /// Chooses one tool button: only that one stays pressed, and the edit tool is told.
        /// Select, Place and Wire are all the same pointer handling in <see cref="EditTool"/>
        /// (what a drag does depends on what it starts on); Place also needs a chip type, and
        /// Trace switches the tool to tracing.
        /// </summary>
        /// <param name="button">
        /// The chosen tool's button.
        /// </param>
        public void PickTool(ToggleButton button)
        {
            TbSelect.IsChecked = button == TbSelect;
            TbPlace.IsChecked = button == TbPlace;
            TbWire.IsChecked = button == TbWire;
            TbTrace.IsChecked = button == TbTrace;
            if (Canvas.Tool == null)
                return;
            Canvas.Tool.TraceMode = button == TbTrace;
            if (button != TbTrace)
                _session?.Trace("");
            if (button == TbPlace)
            {
                Canvas.Tool.PlaceType = ChipBox.SelectedIndex > 0 ? ChipBox.SelectedItem as string : null;
                StatusText.Text = Canvas.Tool.PlaceType == null ? "Pick the chip type in the box, then click on the sheet." : "Click on the sheet to place a " + Canvas.Tool.PlaceType + ".";
                ChipBox.Focus();
            }
            else
            {
                Canvas.Tool.PlaceType = null;
                StatusText.Text = button == TbWire ? "Drag from one pin to another to wire them."
                    : button == TbTrace ? "Click a wire, label or pin to trace its signal. Esc clears." : "";
            }
        }

        private void OnTraceChanged(SheetSession session)
        {
            if (session.TraceBelongsToPick)
                Record(session);
            _syncingLists = true;
            // the Details panel's pin table already lists the traced signal's pins
            TracePanel.IsVisible = session.Traced.Length > 0 && Details.SignalKey != session.Traced;
            TraceTitle.Text = "Signal " + session.Traced;
            TraceList.ItemsSource = session.TracedPins();
            _syncingLists = false;
        }

        private void OnTracePinPicked(object? sender, SelectionChangedEventArgs e)
        {
            if (_syncingLists || _session == null || TraceList.SelectedItem is not string at)
                return;
            if (_session.PinPoint(at, out double x, out double y))
                Canvas.CentreOn(x, y);
            else
                StatusText.Text = at + " is not drawn on this sheet (a backplane or off-sheet pin).";
        }

        /// <summary>
        /// Opens the Make block dialog for the chips and instances picked; null (with a reason in
        /// the status bar) when not in edit mode or nothing is picked. Public so the window tests
        /// can use it.
        /// </summary>
        /// <returns>
        /// The dialog, or null.
        /// </returns>
        public MakeBlockWindow? OpenMakeBlockDialog()
        {
            if (_session == null || _session.Editor.ReadOnly)
            {
                StatusText.Text = "Switch on edit mode to make a block.";
                return null;
            }
            List<string> members = _session.PickedMembers();
            if (members.Count == 0)
            {
                StatusText.Text = "Pick the parts first: Ctrl-click them, or drag a box round them.";
                return null;
            }
            MakeBlockWindow dialog = new(_session, members);
            dialog.Closed += (s, a) =>
            {
                if (dialog.Created != null)
                    StatusText.Text = "Made instance " + dialog.Created + ". Double-click it to open it.";
                ShowEditState();
            };
            dialog.Show(this);
            return dialog;
        }

        /// <summary>
        /// Shows the inside of an instance on the level shown.
        /// </summary>
        /// <param name="instance">
        /// The instance name.
        /// </param>
        public void OpenInstance(string instance)
        {
            if (_session == null)
                return;
            try
            {
                _session.OpenInstance(instance);
                StatusText.Text = "Inside " + instance + ". Up one level (or the bar above the drawing) goes back.";
            }
            catch (SheetEditException e)
            {
                StatusText.Text = e.Message;
            }
            ShowEditState();
        }

        /// <summary>
        /// Puts the inside of an instance back on the level shown.
        /// </summary>
        /// <param name="instance">
        /// The instance name.
        /// </param>
        public void DissolveInstance(string instance)
        {
            if (_session == null)
                return;
            try
            {
                _session.Editor.DissolveInstance(instance);
                StatusText.Text = "Dissolved " + instance + " into this level.";
            }
            catch (SheetEditException e)
            {
                StatusText.Text = e.Message;
            }
            ShowEditState();
        }

        /// <summary>
        /// Goes back up one level.
        /// </summary>
        public void LeaveLevel()
        {
            if (_session == null)
                return;
            _session.LeaveInstance();
            StatusText.Text = _session.Editor.LevelPath.Count == 0 ? "At the top level." : "Inside " + string.Join("/", _session.Editor.LevelPath) + ".";
            ShowEditState();
        }

        /// <summary>
        /// The right-click menu for a point on the drawing: Make block when parts are picked,
        /// Open and Dissolve on an instance. Built here from what the session says is possible.
        /// </summary>
        /// <param name="owner">
        /// What was under the pointer.
        /// </param>
        /// <returns>
        /// The menu, or null when it would be empty (for tests).
        /// </returns>
        public ContextMenu? ShowDrawingMenu(string owner)
        {
            if (_session == null)
                return null;
            bool editing = !_session.Editor.ReadOnly;
            string whole = SheetSession.WholeOf(owner);
            ContextMenu menu = new();
            if (editing && _session.PickedMembers().Count > 0)
            {
                MenuItem make = new() { Header = "Make block..." };
                make.Click += (s, a) => OpenMakeBlockDialog();
                menu.Items.Add(make);
            }
            if (whole.StartsWith("inst:", StringComparison.Ordinal))
            {
                string name = whole.Substring(5);
                MenuItem open = new() { Header = "Open " + name };
                open.Click += (s, a) => OpenInstance(name);
                menu.Items.Add(open);
                MenuItem dissolve = new() { Header = "Dissolve " + name + " into this level", IsEnabled = editing };
                dissolve.Click += (s, a) => DissolveInstance(name);
                menu.Items.Add(dissolve);
            }
            // a port pin: which edge of the box it sits on, and its place along that edge
            if (editing && owner.StartsWith("port:", StringComparison.Ordinal))
            {
                int colon = owner.IndexOf(':', 5);
                BlockInstance? inst = colon < 0 ? null : Hierarchy.FindInstance(_session.Netlist.Instances, owner.Substring(5, colon - 5));
                string port = colon < 0 ? "" : owner.Substring(colon + 1);
                string[] edges = { "left", "right", "top", "bottom" };
                for (int i = 0; inst != null && i < edges.Length; i++)
                {
                    PortSide side = (PortSide)i;
                    MenuItem edge = new() { Header = "Put port " + port + " on the " + edges[i] + " edge" };
                    edge.Click += (s, a) => PortCommand(() => _session.Editor.SetPortPosition(inst.Block, port, side, null));
                    menu.Items.Add(edge);
                }
                if (inst != null)
                {
                    MenuItem earlier = new() { Header = "Move port " + port + " one place earlier" };
                    earlier.Click += (s, a) => PortCommand(() => _session.Editor.MovePortAlong(inst.Block, port, -1));
                    menu.Items.Add(earlier);
                    MenuItem later = new() { Header = "Move port " + port + " one place later" };
                    later.Click += (s, a) => PortCommand(() => _session.Editor.MovePortAlong(inst.Block, port, 1));
                    menu.Items.Add(later);
                }
            }
            if (_session.Editor.LevelPath.Count > 0)
            {
                MenuItem up = new() { Header = "Up one level" };
                up.Click += (s, a) => LeaveLevel();
                menu.Items.Add(up);
            }
            if (menu.Items.Count == 0)
                return null;
            menu.Open(Canvas);
            return menu;
        }

        private void ShowLevelBar()
        {
            LevelBar.Children.Clear();
            if (_session == null || _session.Editor.LevelPath.Count == 0)
            {
                LevelBar.IsVisible = false;
                return;
            }
            LevelBar.IsVisible = true;
            List<string> names = _session.LevelNames();
            for (int i = 0; i < names.Count; i++)
            {
                if (i > 0)
                    LevelBar.Children.Add(new TextBlock { Text = ">", VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center, Classes = { "fluent-text" } });
                int up = names.Count - 1 - i;
                Button level = new() { Content = names[i], IsEnabled = up > 0, Classes = { "fluent-button" } };
                level.Click += (s, a) =>
                {
                    for (int k = 0; k < up; k++)
                        _session.LeaveInstance();
                    ShowEditState();
                };
                LevelBar.Children.Add(level);
            }
        }

        private void PortCommand(Action change)
        {
            try
            {
                change();
                StatusText.Text = "Port moved.";
            }
            catch (SheetEditException e)
            {
                StatusText.Text = e.Message;
            }
            ShowEditState();
        }

        private void OnMakeBlock(object? sender, RoutedEventArgs e) => OpenMakeBlockDialog();

        private void OnUpLevel(object? sender, RoutedEventArgs e) => LeaveLevel();
    }
}
