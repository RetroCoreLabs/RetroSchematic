//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
// RetroSchematic - viewer for Norsk Data circuit diagrams
//

using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using RetroSchematic.Render;

namespace RetroSchematic.Desktop
{
    /// <summary>
    /// The Make block dialog: the block's name, and the ports the selection will get with
    /// their names editable. The window holds no logic: the ports come from
    /// <see cref="SheetEditor.PlanBlock"/> and the block is made by
    /// <see cref="SheetEditor.MakeBlock"/>.
    /// </summary>
    public partial class MakeBlockWindow : Window
    {
        private readonly SheetSession? _session;
        private readonly List<string> _members = new();
        private readonly List<PlannedPort> _ports = new();
        private readonly List<TextBox> _names = new();

        /// <summary>
        /// For the designer only.
        /// </summary>
        public MakeBlockWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Creates the dialog for the chips and instances picked in a session.
        /// </summary>
        /// <param name="session">
        /// The open sheet.
        /// </param>
        /// <param name="members">
        /// Part references and inst:NAME entries (see <see cref="SheetSession.PickedMembers"/>).
        /// </param>
        public MakeBlockWindow(SheetSession session, List<string> members)
        {
            InitializeComponent();
            _session = session;
            _members.AddRange(members);
            MembersText.Text = members.Count + " picked: " + string.Join(", ", members);
            _ports.AddRange(session.Editor.PlanBlock(members));
            for (int i = 0; i < _ports.Count; i++)
            {
                TextBox name = new() { Text = _ports[i].Name, Width = 150, Classes = { "fluent-textbox" } };
                string what = _ports[i].ToString();
                what = what.Substring(_ports[i].Name.Length).Trim();
                DockPanel row = new();
                DockPanel.SetDock(name, Dock.Left);
                row.Children.Add(name);
                row.Children.Add(new TextBlock { Text = what, Margin = new Avalonia.Thickness(8, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center, Classes = { "fluent-text" } });
                PortList.Children.Add(row);
                _names.Add(name);
            }
        }

        /// <summary>
        /// The new instance's name once the block is made; null otherwise.
        /// </summary>
        public string? Created { get; private set; }

        /// <summary>
        /// The name boxes of the ports, in port order (for tests).
        /// </summary>
        public IReadOnlyList<TextBox> PortNames => _names;

        /// <summary>
        /// Makes the block with the name and port names typed. On a refusal the reason is shown
        /// and nothing changes.
        /// </summary>
        /// <returns>
        /// True when the block was made.
        /// </returns>
        public bool TryCreate()
        {
            if (_session == null)
                return false;
            for (int i = 0; i < _ports.Count; i++)
                _ports[i].Name = (_names[i].Text ?? "").Trim();
            try
            {
                Created = _session.Editor.MakeBlock((NameBox.Text ?? "").Trim(), null, _members, _ports);
                _session.ClearPicked();
                _session.Select("inst:" + Created);
                return true;
            }
            catch (SheetEditException e)
            {
                ProblemText.Text = e.Message;
                return false;
            }
        }

        private void OnOk(object? sender, RoutedEventArgs e)
        {
            if (TryCreate())
                Close();
        }

        private void OnCancel(object? sender, RoutedEventArgs e) => Close();
    }
}
