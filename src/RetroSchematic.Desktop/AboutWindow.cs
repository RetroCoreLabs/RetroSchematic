//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using RetroSchematic.Model;

namespace RetroSchematic.Desktop
{
    /// <summary>
    /// The About box (Help > About): the program's name and which build is running - version,
    /// commit, branch, "(uncommitted changes)" when the tree was not clean, and the build date and
    /// time. Every value comes from the build stamp (<see cref="BuildIdentity"/>), never from a
    /// typed-in string.
    /// </summary>
    public sealed class AboutWindow : Window
    {
        /// <summary>
        /// Builds the window.
        /// </summary>
        public AboutWindow()
        {
            Title = "About RetroSchematic";
            Icon = new WindowIcon(Avalonia.Platform.AssetLoader.Open(new Uri("avares://RetroSchematic/Assets/RetroSchematic.ico")));
            Width = 460;
            SizeToContent = SizeToContent.Height;
            CanResize = false;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            this.Bind(BackgroundProperty, this.GetResourceObservable("FluentBackgroundPrimaryBrush"));

            StackPanel root = new() { Spacing = 6, Margin = new Thickness(20) };
            root.Children.Add(new TextBlock { Text = "RetroSchematic", FontSize = 22, FontWeight = FontWeight.SemiBold });
            root.Children.Add(new TextBlock { Text = "Norsk Data card schematics, read from the drawings", TextWrapping = TextWrapping.Wrap });

            Grid facts = new() { ColumnDefinitions = new ColumnDefinitions("Auto,*"), Margin = new Thickness(0, 10, 0, 0) };
            VersionText = AddRow(facts, 0, "Version", BuildIdentity.Version);
            CommitText = AddRow(facts, 1, "Build", BuildIdentity.CommitLine());
            BuiltText = AddRow(facts, 2, "Built", BuildIdentity.BuildDate + " " + BuildIdentity.BuildTime);
            root.Children.Add(facts);

            root.Children.Add(new TextBlock { Text = BuildIdentity.Copyright, Margin = new Thickness(0, 10, 0, 0), TextWrapping = TextWrapping.Wrap });
            root.Children.Add(new TextBlock { Text = "RetroCore Labs - https://github.com/RetroCoreLabs", TextWrapping = TextWrapping.Wrap });

            Button close = new() { Content = "Close", HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 12, 0, 0), IsDefault = true, IsCancel = true };
            close.Click += (_, _) => Close();
            root.Children.Add(close);

            Content = root;
        }

        /// <summary>
        /// Gets the version text shown, for tests.
        /// </summary>
        public TextBlock VersionText { get; }

        /// <summary>
        /// Gets the commit line shown (commit, branch, uncommitted changes), for tests.
        /// </summary>
        public TextBlock CommitText { get; }

        /// <summary>
        /// Gets the build date and time shown, for tests.
        /// </summary>
        public TextBlock BuiltText { get; }

        /// <summary>
        /// Adds one label and value row to the facts grid.
        /// </summary>
        /// <param name="grid">
        /// The grid.
        /// </param>
        /// <param name="row">
        /// The row number.
        /// </param>
        /// <param name="label">
        /// The label.
        /// </param>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <returns>
        /// The value's text block.
        /// </returns>
        private static TextBlock AddRow(Grid grid, int row, string label, string value)
        {
            grid.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            TextBlock l = new() { Text = label, Opacity = 0.7, Margin = new Thickness(0, 2, 14, 2) };
            TextBlock v = new() { Text = value, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2) };
            Grid.SetRow(l, row);
            Grid.SetRow(v, row);
            Grid.SetColumn(v, 1);
            grid.Children.Add(l);
            grid.Children.Add(v);
            return v;
        }
    }
}
