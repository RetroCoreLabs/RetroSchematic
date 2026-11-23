//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;

namespace RetroSchematic.Desktop.Controls
{
    /// <summary>
    /// One row of a list in the left panel (Parts, Signals, Found): an optional kind icon, the
    /// main text, an optional badge and a muted text at the end. The list only shows what the
    /// main window puts in; it works nothing out itself.
    /// </summary>
    public sealed class SideRow
    {
        /// <summary>
        /// Key of the icon in Styles/Icons.axaml (IconSignal), or null for no icon.
        /// </summary>
        public string? Icon { get; init; }

        /// <summary>
        /// The main text: a board position (13D) or a signal name in the chosen name style.
        /// </summary>
        public string Main { get; init; } = "";

        /// <summary>
        /// The second column, in normal ink (a part type such as 74153), or null.
        /// </summary>
        public string? Second { get; init; }

        /// <summary>
        /// A small badge after the main text (the active level: "low", "high"), or null.
        /// </summary>
        public string? Badge { get; init; }

        /// <summary>
        /// The muted text at the end (what a part does, how many pins a signal has, where a
        /// found item is), or null.
        /// </summary>
        public string? Muted { get; init; }

        /// <summary>
        /// What the row stands for in the session's pick form (part:13D, net:BD3_0, pin:5A.8).
        /// </summary>
        public string Owner { get; init; } = "";

        /// <summary>
        /// The main text, so a row reads as its name where a string is wanted.
        /// </summary>
        /// <returns>
        /// The main text.
        /// </returns>
        public override string ToString() => Main;
    }

    /// <summary>
    /// Builds the row look for the left panel's lists: aligned columns in a grid. Every colour
    /// comes from the theme through the styles in Styles/SideList.axaml.
    /// </summary>
    public static class SideList
    {
        /// <summary>
        /// The template for a list of parts: board position | type | function (muted).
        /// </summary>
        public static IDataTemplate Parts { get; } = new FuncDataTemplate<SideRow>((r, _) => Build(r, "56,64,*"), true);

        /// <summary>
        /// The template for a list of signals: name | active-level badge | pin count (muted).
        /// </summary>
        public static IDataTemplate Signals { get; } = new FuncDataTemplate<SideRow>((r, _) => Build(r, "*,Auto,44"), true);

        /// <summary>
        /// The template for found items: kind icon | name | where (muted).
        /// </summary>
        public static IDataTemplate Found { get; } = new FuncDataTemplate<SideRow>((r, _) => Build(r, "20,Auto,*"), true);

        // one row as a grid with the given columns; which cells are filled depends on the row
        private static Control Build(SideRow? r, string columns)
        {
            Grid g = new() { ColumnDefinitions = new ColumnDefinitions(columns) };
            g.Classes.Add("side-row");
            if (r == null)
                return g;
            int col = 0;
            if (columns.StartsWith("20,", System.StringComparison.Ordinal))
            {
                // the found list: the kind icon in the first column
                Path icon = new() { Data = Geometry(r.Icon) };
                icon.Classes.Add("side-icon");
                g.Children.Add(icon);
                col = 1;
            }
            TextBlock main = new() { Text = r.Main };
            main.Classes.Add("side-main");
            Grid.SetColumn(main, col);
            g.Children.Add(main);
            col++;
            if (r.Second != null)
            {
                TextBlock second = new() { Text = r.Second };
                second.Classes.Add("side-second");
                Grid.SetColumn(second, col);
                g.Children.Add(second);
            }
            else if (r.Badge != null)
            {
                Border badge = new() { Child = new TextBlock { Text = r.Badge } };
                badge.Classes.Add("side-badge");
                Grid.SetColumn(badge, col);
                g.Children.Add(badge);
            }
            col++;
            if (r.Muted != null)
            {
                TextBlock muted = new() { Text = r.Muted };
                muted.Classes.Add("side-muted");
                // the pin count is a number: right-aligned so the counts line up
                if (columns.EndsWith(",44", System.StringComparison.Ordinal))
                    muted.HorizontalAlignment = HorizontalAlignment.Right;
                Grid.SetColumn(muted, col);
                g.Children.Add(muted);
            }
            ToolTip.SetTip(g, r.Muted == null ? r.Main : r.Main + "  " + r.Muted);
            return g;
        }

        private static Geometry? Geometry(string? key)
        {
            if (key == null || Avalonia.Application.Current == null)
                return null;
            return Avalonia.Application.Current.TryGetResource(key, null, out object? g) ? g as Geometry : null;
        }
    }
}
