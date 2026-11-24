//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using NUnit.Framework;
using RetroSchematic.Model;
using RetroSchematic.Recognition;

namespace RetroSchematic.Tests
{
    /// <summary>
    /// The page analyser (RetroSchematic.Recognition, src/models/README.md) against the offline
    /// Python prototype it is ported from. The prototype's numbers are in Data/p64-prototype.json:
    /// made by running the prototype on the 2A50 page (NORD-10 drawings page 64, the source page
    /// stored with sheet 2A50) with the detector only (DETMODE=replace FIX6 FIX7 FIX10 FIX11, det-L3,
    /// letters-A, threshold 0.3). The page was never used to train any of the networks.
    /// </summary>
    [TestFixture]
    public class RecognitionTests
    {
        private static readonly string PagePng = Path.Combine(TestContext.CurrentContext.TestDirectory,
            "sheets", "nord-10", "2A50-files", "sources", "2A50-sync-modem-buffer-1050-p64.png");

        private static readonly string Sheet2A50 = Path.Combine(TestContext.CurrentContext.TestDirectory,
            "sheets", "nord-10", "2A50-sync-modem-buffer-1050.rsc");

        private static PageAnalyser? analyser;
        private static GreyImage? page;
        private static JsonElement proto;

        [OneTimeSetUp]
        public void Load()
        {
            analyser = new PageAnalyser(PageAnalyser.DefaultModelsDir);
            page = GreyImage.FromFile(PagePng);
            proto = JsonDocument.Parse(File.ReadAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, "Data", "p64-prototype.json"))).RootElement;
        }

        [OneTimeTearDown]
        public void Unload() => analyser?.Dispose();

        /// <summary>The prototype's test crop: rows 1000-1511, columns 2000-2511 of the page.</summary>
        private static GreyImage Crop()
        {
            byte[] c = new byte[512 * 512];
            for (int y = 0; y < 512; y++)
            {
                Array.Copy(page!.Pixels, ((1000 + y) * page.Width) + 2000, c, y * 512, 512);
            }

            return new GreyImage(512, 512, c);
        }

        [Test]
        public void ThePageReadsAsTheGreyThePrototypeRead()
        {
            Assert.That(page!.Width, Is.EqualTo(3507));
            Assert.That(page.Height, Is.EqualTo(2496));
            ImageOps.OtsuInk(page, out int t);
            Assert.That(t, Is.EqualTo(proto.GetProperty("otsu").GetDouble()));
            Assert.That(PageAnalyser.LetterSize(page), Is.EqualTo(proto.GetProperty("letter_size").GetDouble()));
        }

        [Test]
        public void OpeningABarShiftsTheRunOnePixelAsOpenCvDoes()
        {
            // measured with OpenCV 4: a run at 5..69 opened with a 60-long bar comes out at 6..70
            byte[] m = new byte[100];
            for (int i = 5; i <= 69; i++)
            {
                m[i] = 255;
            }

            byte[] o = ImageOps.OpenBar(m, 100, 1, 60, true);
            Assert.That(o[5], Is.EqualTo(0));
            Assert.That(o[6], Is.EqualTo(255));
            Assert.That(o[70], Is.EqualTo(255));
            Assert.That(o[71], Is.EqualTo(0));
        }

        [Test]
        public void LetterPiecesAndStyleMeasuresMatchThePrototype()
        {
            List<Blob> pcs = PageAnalyser.PageWork.LetterPieces(page!);
            Assert.That(pcs.Count, Is.EqualTo(proto.GetProperty("pieces_of").GetInt32()));
            PageAnalyser.PageWork w = new PageAnalyser.PageWork(page!, analyser!.Letters, BoardGrid.Nord10);
            (double spread, double thick) = w.StyleMeasures();
            Assert.That(spread, Is.EqualTo(proto.GetProperty("spread").GetDouble()).Within(1e-9));
            Assert.That(thick, Is.EqualTo(proto.GetProperty("thick").GetDouble()).Within(1e-9));
            Assert.That(PageAnalyser.StyleOf(w), Is.EqualTo(PageStyle.Hand));
        }

        [Test]
        public void HandDetectorOnTheCropGivesThePrototypesShapeAndFinds()
        {
            CheckDetector(analyser!.HandDetector, "crop_dets");
        }

        [Test]
        public void CadDetectorOnTheCropGivesThePrototypesShapeAndFinds()
        {
            CheckDetector(analyser!.CadDetector, "crop_dets_A");
        }

        private static void CheckDetector(SymbolDetector d, string key)
        {
            List<Detection> found = d.Detect(Crop(), 0.3);
            Assert.That(d.LastShape, Is.EqualTo((14, 128, 128)));
            JsonElement want = proto.GetProperty(key);

            // the prototype's five strongest finds are all here, same class, box within a pixel
            for (int i = 0; i < want.GetArrayLength(); i++)
            {
                JsonElement wv = want[i];
                string cls = wv.GetProperty("cls").GetString()!;
                JsonElement box = wv.GetProperty("box");
                bool hit = false;
                for (int k = 0; k < found.Count && !hit; k++)
                {
                    Detection f = found[k];
                    hit = SymbolDetector.Classes[f.Class] == cls
                        && Math.Abs(f.X - box[0].GetInt32()) <= 1 && Math.Abs(f.Y - box[1].GetInt32()) <= 1
                        && Math.Abs(f.W - box[2].GetInt32()) <= 1 && Math.Abs(f.H - box[3].GetInt32()) <= 1
                        && Math.Abs(f.Score - wv.GetProperty("score").GetDouble()) <= 0.002;
                }

                Assert.That(hit, Is.True, "prototype find " + wv.ToString());
            }

            if (key == "crop_dets")
            {
                Assert.That(found.Count, Is.EqualTo(proto.GetProperty("crop_dets_n").GetInt32()));
            }
        }

        [Test]
        public void LetterNetworkReadsTheCropsLettersAsThePrototypeDid()
        {
            JsonElement want = proto.GetProperty("crop_letters");
            List<(int X, int Y)> cs = new List<(int X, int Y)>();
            for (int i = 0; i < want.GetArrayLength(); i++)
            {
                JsonElement p = want[i].GetProperty("piece");
                cs.Add((p[0].GetInt32() + (p[2].GetInt32() / 2), p[1].GetInt32() + (p[3].GetInt32() / 2)));
            }

            float[] pr = analyser!.Letters.Probabilities(Crop(), cs);
            int nc = LetterNet.Classes.Length;
            Assert.That(pr.Length, Is.EqualTo(cs.Count * nc));
            for (int i = 0; i < cs.Count; i++)
            {
                int best = 0;
                for (int c = 1; c < nc; c++)
                {
                    if (pr[(i * nc) + c] > pr[(i * nc) + best])
                    {
                        best = c;
                    }
                }

                Assert.That(LetterNet.Classes[best], Is.EqualTo(want[i].GetProperty("cls").GetString()), "piece " + i);
                Assert.That(pr[(i * nc) + best], Is.EqualTo(want[i].GetProperty("p").GetDouble()).Within(0.002), "piece " + i);
            }
        }

        [Test]
        public void SnapTakesTheOneNearestCatalogueName()
        {
            HashSet<string> ids = new HashSet<string> { "74157", "74158", "7400" };
            Assert.That(PageAnalyser.Snap("74I57", ids), Is.EqualTo("74157"), "I reads as 1 at no cost");
            Assert.That(PageAnalyser.Snap("7415", ids), Is.EqualTo("7415"), "74157 and 74158 are equally near");
            Assert.That(PageAnalyser.Snap("7400", ids), Is.EqualTo("7400"));
        }

        [Test]
        public void ThePageIsAnalysedLikeThePrototypeAndQuickly()
        {
            ChipCatalog cat = new ChipCatalog(ChipCatalog.Find(Sheet2A50)!);
            PageAnalysis a = analyser!.Analyse(page!, BoardGrid.Nord10, new HashSet<string>(cat.Types()));
            int chips = 0;
            int gates = 0;
            int coded = 0;
            int typed = 0;
            for (int i = 0; i < a.Finds.Count; i++)
            {
                PageFind f = a.Finds[i];
                if (f.Kind == "chip")
                {
                    chips++;
                    typed += f.TypeId != null ? 1 : 0;
                }

                if (f.Kind == "gate")
                {
                    gates++;
                }

                if ((f.Kind == "chip" || f.Kind == "gate") && f.Code != null)
                {
                    coded++;
                }
            }

            int pChips = proto.GetProperty("chips").GetArrayLength();
            int pGates = proto.GetProperty("gates").GetArrayLength();
            TestContext.Out.WriteLine($"C#: {chips} chips ({typed} typed), {gates} gates, {coded} codes, turn {a.TurnCw}, style {a.Style}, {a.Pieces} pieces, {a.Milliseconds} ms (turn {a.TurnMs}, pieces {a.PiecesMs}, detector {a.DetectMs}); prototype {pChips} chips, {pGates} gates");
            Assert.That(a.TurnCw, Is.EqualTo(0));
            Assert.That(a.Scale, Is.EqualTo(1.0));
            Assert.That(a.Style, Is.EqualTo(PageStyle.Hand));

            // the prototype's counts are the floor, with a margin of 2 for the port
            Assert.That(chips, Is.InRange(pChips - 2, pChips + 2));
            Assert.That(gates, Is.InRange(pGates - 2, pGates + 2));
            Assert.That(a.Milliseconds, Is.LessThan(15000), "a few seconds on a desktop CPU");

            // same symbols: every prototype chip and gate has a find of the same kind at the same place
            int sameBox = 0;
            int sameCode = 0;
            int sameType = 0;
            int total = 0;
            string[] kinds = { "chips", "gates" };
            for (int k = 0; k < kinds.Length; k++)
            {
                JsonElement list = proto.GetProperty(kinds[k]);
                for (int i = 0; i < list.GetArrayLength(); i++)
                {
                    total++;
                    JsonElement b = list[i].GetProperty("box");
                    JsonElement r = list[i].GetProperty("ref");
                    for (int j = 0; j < a.Finds.Count; j++)
                    {
                        PageFind f = a.Finds[j];
                        if (f.Kind == kinds[k].TrimEnd('s') && Math.Abs(f.X - b[0].GetInt32()) <= 2 && Math.Abs(f.Y - b[1].GetInt32()) <= 2
                            && Math.Abs(f.Width - b[2].GetInt32()) <= 2 && Math.Abs(f.Height - b[3].GetInt32()) <= 2)
                        {
                            sameBox++;
                            string? want = r.ValueKind == JsonValueKind.String ? r.GetString() : null;
                            sameCode += f.Code == want ? 1 : 0;
                            if (k == 0)
                            {
                                JsonElement t = list[i].GetProperty("type");
                                sameType += f.TypeId == (t.ValueKind == JsonValueKind.String ? t.GetString() : null) ? 1 : 0;
                            }
                            break;
                        }
                    }
                }
            }

            TestContext.Out.WriteLine($"same box {sameBox} of {total}, same code {sameCode} of {total}, same chip type {sameType} of {pChips}");
            Assert.That(sameType, Is.GreaterThanOrEqualTo(pChips - 1));
            Assert.That(sameBox, Is.GreaterThanOrEqualTo(total - 2));
            Assert.That(sameCode, Is.GreaterThanOrEqualTo(total - 4));
        }
    }
}
