//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using RetroSchematic.Model;
using RetroSchematic.Render;

namespace RetroSchematic.Tests
{
    /// <summary>
    /// The drawing's look: in every theme the main colours differ from each other and from the
    /// paper, the widths differ, wires branch with a dot, the chip label is split into a muted
    /// position and a bold type, block boxes have a header band, and the "Show buses" switch
    /// only changes the picture, never the sheet or its file.
    /// </summary>
    [TestFixture]
    public class StyleTests
    {
        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050.json");
        private static readonly string Blocks = Path.Combine(TestContext.CurrentContext.TestDirectory, "sheets", "nord-10", "2A50-sync-modem-buffer-1050-blocks.json");

        // the colours a reader must tell apart at a glance
        private static readonly SchematicColour[] s_mustDiffer =
        {
            SchematicColour.Paper, SchematicColour.ChipFill, SchematicColour.ChipOutline, SchematicColour.Wire,
            SchematicColour.Bus, SchematicColour.NetLabel, SchematicColour.PinNumber, SchematicColour.PartTitle,
            SchematicColour.Open, SchematicColour.Trace, SchematicColour.BlockHeader, SchematicColour.PortArrow,
        };

        private static IEnumerable<string> ThemeIds()
        {
            for (int i = 0; i < SchematicTheme.All.Count; i++)
                yield return SchematicTheme.All[i].Id;
        }

        [TestCaseSource(nameof(ThemeIds))]
        public void InEveryTheme_TheMainColoursDiffer(string id)
        {
            SchematicTheme t = SchematicTheme.Find(id)!;
            List<string> same = new();
            for (int i = 0; i < s_mustDiffer.Length; i++)
            {
                for (int j = i + 1; j < s_mustDiffer.Length; j++)
                {
                    if (string.Equals(t.Get(s_mustDiffer[i]), t.Get(s_mustDiffer[j]), StringComparison.OrdinalIgnoreCase))
                        same.Add(s_mustDiffer[i] + " = " + s_mustDiffer[j]);
                }
            }
            Assert.That(same, Is.Empty);
            // nothing a reader needs is drawn in the paper colour
            SchematicColour[] all = Enum.GetValues<SchematicColour>();
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == SchematicColour.Paper || all[i] == SchematicColour.ChipFill || all[i] == SchematicColour.GateFill || all[i] == SchematicColour.BlockFill)
                    continue;
                Assert.That(t.Get(all[i]), Is.Not.EqualTo(t.Get(SchematicColour.Paper)).IgnoreCase, all[i] + " is the paper colour");
            }
        }

        [Test]
        public void TheWidths_AreDistinct()
        {
            double[] w = { LineWidths.Pin, LineWidths.Wire, LineWidths.Label, LineWidths.Outline, LineWidths.Bus };
            for (int i = 1; i < w.Length; i++)
                Assert.That(w[i], Is.GreaterThan(w[i - 1]), "pin < wire < label < outline < bus");
            Assert.That(LineWidths.Outline, Is.GreaterThanOrEqualTo(3 * LineWidths.Wire));
        }

        [Test]
        public void TheDefaultTheme_IsLight()
        {
            Assert.That(SchematicTheme.Default.Id, Is.EqualTo("Light"));
            Assert.That(SchematicTheme.Default.IsDark, Is.False);
            Assert.That(SvgRenderer.Write(new SchematicDrawing(10, 10)), Does.Contain("fill=\"" + SchematicTheme.Default.Get(SchematicColour.Paper) + "\""));
        }

        [Test]
        public void AJunctionDot_MarksEveryBranch_AndOnlyBranches()
        {
            SchematicDrawing d = SheetLayout.Build(NetlistReader.ReadFile(Sheet2A50), SymbolStyle.NorskData, new LayoutOptions { Catalog = new ChipCatalog(ChipCatalog.Find(Sheet2A50)!) });
            int dots = 0;
            for (int i = 0; i < d.Items.Count; i++)
            {
                DrawItem dot = d.Items[i];
                if (dot.Role != DrawRole.Junction)
                    continue;
                dots++;
                // at least three ways leave the dot along its own signal's wires; each wire stops
                // at the dot's edge, it never runs through it
                int ways = 0;
                for (int j = 0; j < d.Items.Count; j++)
                {
                    DrawItem w = d.Items[j];
                    if (w.Role != DrawRole.Wire || w.Kind != DrawKind.Line || w.Owner != dot.Owner)
                        continue;
                    if (AtEdge(w.X1, w.Y1, dot) || AtEdge(w.X2, w.Y2, dot))
                        ways++;
                }
                Assert.That(ways, Is.GreaterThanOrEqualTo(3), "dot at " + dot.X1 + "," + dot.Y1 + " on " + dot.Owner);
            }
            Assert.That(dots, Is.GreaterThan(0), "2A50 has branching wires");
        }

        [Test]
        public void TheChipLabel_IsAMutedPositionThenABoldType()
        {
            SchematicDrawing d = SheetLayout.Build(NetlistReader.ReadFile(Sheet2A50), SymbolStyle.NorskData, new LayoutOptions { Catalog = new ChipCatalog(ChipCatalog.Find(Sheet2A50)!) });
            DrawItem? pos = null, type = null;
            for (int i = 0; i < d.Items.Count; i++)
            {
                if (d.Items[i].Owner == "part:13D" && d.Items[i].Role == DrawRole.BoardPosition)
                    pos = d.Items[i];
                if (d.Items[i].Owner == "part:13D" && d.Items[i].Role == DrawRole.PartLabel)
                    type = d.Items[i];
            }
            Assert.That(pos!.Text, Is.EqualTo("13D"));
            Assert.That(type!.Text, Is.EqualTo("7442"));
            Assert.That(type.X1, Is.GreaterThan(pos.X1), "type after the position");
            Assert.That(SchematicStyle.Of(type).Bold, Is.True);
            Assert.That(SchematicStyle.Of(pos).Bold, Is.False);
            Assert.That(SchematicStyle.Of(pos).Stroke, Is.EqualTo(SchematicColour.BoardPosition));
        }

        [Test]
        public void ABlockBox_HasAHeaderBand_WithItsTitleInside()
        {
            SchematicDrawing d = SheetLayout.Build(NetlistReader.ReadFile(Blocks), SymbolStyle.NorskData, new LayoutOptions { Catalog = new ChipCatalog(ChipCatalog.Find(Blocks)!) });
            PartFrame box = d.InstanceFrames["U1"];
            DrawItem? header = null, title = null;
            for (int i = 0; i < d.Items.Count; i++)
            {
                if (d.Items[i].Owner == "inst:U1" && d.Items[i].Role == DrawRole.BlockHeader)
                    header = d.Items[i];
                if (d.Items[i].Owner == "inst:U1" && d.Items[i].Role == DrawRole.PartLabel)
                    title = d.Items[i];
            }
            Assert.That(header, Is.Not.Null);
            Assert.That(header!.X1, Is.EqualTo(box.X));
            Assert.That(header.Y1, Is.EqualTo(box.Y));
            Assert.That(header.X2, Is.EqualTo(box.Width));
            Assert.That(title!.Y1, Is.GreaterThan(box.Y).And.LessThanOrEqualTo(box.Y + header.Y2), "the title sits in the band");
            Assert.That(SchematicStyle.Of(header).Fill, Is.EqualTo(SchematicColour.BlockHeader));
        }

        [Test]
        public void ShowBusesOff_DrawsMembersAlone_AndNeverChangesTheSheetOrItsFile()
        {
            string before = File.ReadAllText(Sheet2A50);
            SheetSession s = new(Sheet2A50, SymbolStyle.NorskData, ChipCatalog.Find(Sheet2A50));
            string saved = NetlistReader.Write(s.Netlist);
            Assert.That(s.ShowBuses, Is.True);
            Assert.That(Count(s.Drawing, DrawRole.Bus), Is.GreaterThan(0));

            s.SetShowBuses(false);
            Assert.That(Count(s.Drawing, DrawRole.Bus), Is.EqualTo(0), "no bus drawn");
            // every member of BD_0 now has its own wire or name tag
            int ownDrawn = 0;
            for (int i = 0; i < s.Drawing.Items.Count; i++)
            {
                DrawItem it = s.Drawing.Items[i];
                if (it.Owner == "net:BD3_0" && (it.Role == DrawRole.Wire || it.Role == DrawRole.NetLabel))
                    ownDrawn++;
            }
            Assert.That(ownDrawn, Is.GreaterThan(0), "BD3_0 drawn on its own");
            Assert.That(s.Netlist.Layout!.Buses, Is.Not.Null.And.Not.Empty, "the sheet keeps its buses");
            Assert.That(NetlistReader.Write(s.Netlist), Is.EqualTo(saved), "what would be saved is unchanged");

            s.SetShowBuses(true);
            Assert.That(Count(s.Drawing, DrawRole.Bus), Is.GreaterThan(0));
            Assert.That(NetlistReader.Write(s.Netlist), Is.EqualTo(saved));
            Assert.That(File.ReadAllText(Sheet2A50), Is.EqualTo(before), "the file on disk is untouched");
        }

        private static int Count(SchematicDrawing d, DrawRole role)
        {
            int n = 0;
            for (int i = 0; i < d.Items.Count; i++)
            {
                if (d.Items[i].Role == role)
                    n++;
            }
            return n;
        }

        private static bool AtEdge(double x, double y, DrawItem dot)
            => Math.Abs(Math.Sqrt(((x - dot.X1) * (x - dot.X1)) + ((y - dot.Y1) * (y - dot.Y1))) - dot.X2) < 0.05;
    }
}
