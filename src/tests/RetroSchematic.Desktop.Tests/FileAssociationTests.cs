//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using Avalonia.Headless.NUnit;
using NUnit.Framework;

namespace RetroSchematic.Desktop.Tests
{
    /// <summary>
    /// The per-user file association, against a fake registry: the real one is never touched.
    /// </summary>
    [TestFixture]
    public class FileAssociationTests
    {
        // a registry in a dictionary: key path -> default value
        private sealed class FakeRegistry : IRegistryWriter
        {
            public readonly Dictionary<string, string> Keys = new(StringComparer.OrdinalIgnoreCase);

            public string? GetDefault(string subKey) => Keys.TryGetValue(subKey, out string? v) ? v : null;

            public void SetDefault(string subKey, string value) => Keys[subKey] = value;

            public void DeleteTree(string subKey)
            {
                List<string> gone = new();
                foreach (string k in Keys.Keys)
                {
                    if (k.Equals(subKey, StringComparison.OrdinalIgnoreCase) || k.StartsWith(subKey + @"\", StringComparison.OrdinalIgnoreCase))
                        gone.Add(k);
                }
                for (int i = 0; i < gone.Count; i++)
                    Keys.Remove(gone[i]);
            }
        }

        [Test]
        public void Register_WritesOnlyCurrentUserClasses_AndUnregisterTakesItAway()
        {
            FakeRegistry r = new();
            FileAssociation a = new(r);
            a.Register(@"X:\Apps\RetroSchematic.exe");
            Assert.That(a.IsRegistered(), Is.True);
            Assert.That(r.Keys[@"Software\Classes\.rsc"], Is.EqualTo(FileAssociation.ProgId));
            Assert.That(r.Keys[@"Software\Classes\.rsx"], Is.EqualTo(FileAssociation.ProgId));
            Assert.That(r.Keys[@"Software\Classes\RetroSchematic.Sheet\shell\open\command"], Is.EqualTo("\"X:\\Apps\\RetroSchematic.exe\" \"%1\""));
            foreach (string k in r.Keys.Keys)
                Assert.That(k, Does.StartWith(@"Software\Classes\"), "nothing outside the user's own classes");
            a.Unregister();
            Assert.That(r.Keys, Is.Empty);
            Assert.That(a.IsRegistered(), Is.False);
        }

        [Test]
        public void Unregister_LeavesAnEndingAnotherProgramTookOver()
        {
            FakeRegistry r = new();
            FileAssociation a = new(r);
            a.Register(@"X:\RetroSchematic.exe");
            r.Keys[@"Software\Classes\.rsx"] = "SomeOther.File";
            a.Unregister();
            Assert.That(r.Keys[@"Software\Classes\.rsx"], Is.EqualTo("SomeOther.File"));
            Assert.That(r.Keys.ContainsKey(@"Software\Classes\.rsc"), Is.False);
        }

        [AvaloniaTest]
        public void TheMenuItems_WriteThroughTheWindowsRegistryWriter()
        {
            FakeRegistry r = new();
            MainWindow w = new() { Registry = r };
            w.Show();
            w.OnAssociate(null, new Avalonia.Interactivity.RoutedEventArgs());
            Assert.That(new FileAssociation(r).IsRegistered(), Is.True);
            w.OnUnassociate(null, new Avalonia.Interactivity.RoutedEventArgs());
            Assert.That(r.Keys, Is.Empty);
            w.Close();
        }
    }
}
