//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
// RetroSchematic - viewer for Norsk Data circuit diagrams
//

using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Desktop
{
    /// <summary>
    /// The New bus dialog: a name, a checkable list of the sheet's signals with a filter box, and
    /// a pattern box that ticks every signal it matches. OK makes the bus through the library
    /// (<see cref="SheetEditor.CreateBus"/>); the window then lets the user click out its path.
    /// The list comes from <see cref="SheetSession.SignalKeys"/> and the pattern from
    /// <see cref="SheetEditor.MatchSignals"/>; only the ticks are kept here.
    /// </summary>
    public partial class NewBusWindow : Window
    {
        private readonly SheetSession? _session;
        private readonly HashSet<string> _ticked = new(System.StringComparer.Ordinal);

        /// <summary>
        /// For the designer only.
        /// </summary>
        public NewBusWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// The dialog for one open sheet.
        /// </summary>
        /// <param name="session">
        /// The sheet the bus goes on.
        /// </param>
        public NewBusWindow(SheetSession session)
        {
            InitializeComponent();
            _session = session;
            FillList();
        }

        /// <summary>
        /// The name of the bus made, or null when the dialog was cancelled.
        /// </summary>
        public string? Created { get; private set; }

        /// <summary>
        /// The signals ticked now, as keys (BD15_0).
        /// </summary>
        public IReadOnlyCollection<string> Ticked => _ticked;

        /// <summary>
        /// Ticks every signal a pattern matches (as the Tick button does).
        /// </summary>
        /// <param name="pattern">
        /// The pattern, such as BD*_0.
        /// </param>
        public void TickPattern(string pattern)
        {
            if (_session == null)
                return;
            List<Net> found = _session.Editor.MatchSignals(pattern);
            for (int i = 0; i < found.Count; i++)
                _ticked.Add(SheetLayout.NetKey(found[i]));
            ProblemText.Text = found.Count == 0 ? "No signal matches \"" + pattern + "\"." : "";
            FillList();
        }

        /// <summary>
        /// Makes the bus (as OK does). On a refusal the reason is shown and the dialog stays open.
        /// </summary>
        /// <returns>
        /// True when the bus was made.
        /// </returns>
        public bool TryCreate()
        {
            if (_session == null)
                return false;
            try
            {
                _session.Editor.CreateBus((NameBox.Text ?? "").Trim(), new List<string>(_ticked), null);
                Created = (NameBox.Text ?? "").Trim();
                return true;
            }
            catch (SheetEditException e)
            {
                ProblemText.Text = e.Message;
                return false;
            }
        }

        private void FillList()
        {
            if (_session == null)
                return;
            List<string> keys = _session.SignalKeys(FilterBox.Text ?? "");
            List<CheckBox> boxes = new();
            for (int i = 0; i < keys.Count; i++)
            {
                CheckBox box = new() { Content = keys[i], Tag = keys[i], IsChecked = _ticked.Contains(keys[i]) };
                box.IsCheckedChanged += OnTick;
                boxes.Add(box);
            }
            SignalList.ItemsSource = boxes;
            CountText.Text = _ticked.Count + " ticked";
        }

        private void OnTick(object? sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox box || box.Tag is not string key)
                return;
            if (box.IsChecked == true)
                _ticked.Add(key);
            else
                _ticked.Remove(key);
            CountText.Text = _ticked.Count + " ticked";
        }

        private void OnFilter(object? sender, TextChangedEventArgs e) => FillList();

        private void OnPattern(object? sender, RoutedEventArgs e) => TickPattern(PatternBox.Text ?? "");

        private void OnOk(object? sender, RoutedEventArgs e)
        {
            if (TryCreate())
                Close();
        }

        private void OnCancel(object? sender, RoutedEventArgs e) => Close();
    }
}
