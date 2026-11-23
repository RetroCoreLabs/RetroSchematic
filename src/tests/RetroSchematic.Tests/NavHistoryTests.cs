//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.IO;
using NUnit.Framework;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Tests
{
    /// <summary>
    /// Back / Forward: the history list, and a session going back to a place.
    /// </summary>
    [TestFixture]
    public class NavHistoryTests
    {
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");
        private static readonly string Catalog = Path.Combine(TestContext.CurrentContext.TestDirectory, "catalog");

        private static NavPlace At(string owner, params string[] level) => new() { Selected = owner, Level = level, Label = owner };

        [Test]
        public void Push_Back_Forward()
        {
            NavHistory h = new();
            Assert.That(h.CanBack, Is.False);
            Assert.That(h.CanForward, Is.False);
            h.Push(At("part:11D"));
            h.Push(At("net:LEV12_0"));
            h.Push(At("part:13D"));
            Assert.That(h.Count, Is.EqualTo(3));
            Assert.That(h.BackPlace!.Selected, Is.EqualTo("net:LEV12_0"));
            Assert.That(h.Back()!.Selected, Is.EqualTo("net:LEV12_0"));
            Assert.That(h.Back()!.Selected, Is.EqualTo("part:11D"));
            Assert.That(h.CanBack, Is.False);
            Assert.That(h.Back(), Is.Null);
            Assert.That(h.ForwardPlace!.Selected, Is.EqualTo("net:LEV12_0"));
            Assert.That(h.Forward()!.Selected, Is.EqualTo("net:LEV12_0"));
            Assert.That(h.Forward()!.Selected, Is.EqualTo("part:13D"));
            Assert.That(h.Forward(), Is.Null);
        }

        [Test]
        public void ANewPlace_AfterGoingBack_DropsThePlacesAhead()
        {
            NavHistory h = new();
            h.Push(At("part:1"));
            h.Push(At("part:2"));
            h.Push(At("part:3"));
            h.Back();
            h.Back();
            Assert.That(h.Push(At("part:9")), Is.True);
            Assert.That(h.Count, Is.EqualTo(2));
            Assert.That(h.CanForward, Is.False);
            Assert.That(h.BackPlace!.Selected, Is.EqualTo("part:1"));
        }

        [Test]
        public void TheSamePlace_OrAnEmptyPickOnTheSameLevel_IsNotANewEntry()
        {
            NavHistory h = new();
            h.Push(At("net:A"));
            NavPlace again = At("net:A");
            again.Traced = "A";
            Assert.That(h.Push(again), Is.False);
            Assert.That(h.Current!.Traced, Is.EqualTo("A"), "the trace is brought up to date");
            Assert.That(h.Push(At("")), Is.False);
            Assert.That(h.Push(At("", "U1")), Is.True, "a level change is a place even with nothing picked");
            Assert.That(h.Count, Is.EqualTo(2));
        }

        [Test]
        public void TheHistory_KeepsAtMostItsLimit()
        {
            NavHistory h = new(5);
            for (int i = 0; i < 12; i++)
                h.Push(At("part:" + i));
            Assert.That(h.Count, Is.EqualTo(5));
            NavPlace? p = null;
            while (h.CanBack)
                p = h.Back();
            Assert.That(p!.Selected, Is.EqualTo("part:7"), "the oldest went first");
        }

        [Test]
        public void ASession_GoesBackToAPlace_WithItsPickAndTrace()
        {
            SheetSession s = new(Sheet2A50, SymbolStyle.NorskData, Catalog);
            s.Select("part:11D");
            NavPlace chip = s.PlaceNow();
            Assert.That(chip.Label, Is.EqualTo("11D 7402"));
            s.Trace("LEV12_0");
            Assert.That(s.TraceBelongsToPick, Is.False, "traced before the pick moved");
            s.Select("net:LEV12_0");
            Assert.That(s.TraceBelongsToPick, Is.True);
            NavPlace sig = s.PlaceNow();
            Assert.That(sig.Label, Is.EqualTo("/LEV12"));
            s.GoTo(chip);
            Assert.That(s.Selected, Is.EqualTo("part:11D"));
            Assert.That(s.Traced, Is.Empty);
            s.GoTo(sig);
            Assert.That(s.Selected, Is.EqualTo("net:LEV12_0"));
            Assert.That(s.Traced, Is.EqualTo("LEV12_0"));
        }
    }
}
