//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using NUnit.Framework;
using RetroSchematic.Model;

namespace RetroSchematic.Tests
{
    /// <summary>
    /// The path-delay sum: right edge per stage, missing figures never invented.
    /// </summary>
    [TestFixture]
    public class PathDelayTests
    {
        // Made-up figures, chosen so every sum below shows which edge was used.
        private static readonly PathStage Inverter = new("inv", new DelayNs(2, 10, 20), new DelayNs(1, 5, 15), true);
        private static readonly PathStage Buffer = new("buf", new DelayNs(3, 7, 30), new DelayNs(4, 8, 40), false);

        [Test]
        public void InverterFlipsTheEdge_SoTheNextStageUsesTheOtherFigure()
        {
            // rising input -> inverter output falls (1/5/15) -> buffer output falls (4/8/40)
            PathDelayResult r = PathDelay.Calculate(new[] { Inverter, Buffer }, OutputEdge.LowToHigh);
            Assert.That(r.MinNs, Is.EqualTo(5));
            Assert.That(r.TypNs, Is.EqualTo(13));
            Assert.That(r.MaxNs, Is.EqualTo(55));
            Assert.That(r.FinalEdge, Is.EqualTo(OutputEdge.HighToLow));
            Assert.That(r.MinIsLowerBoundOnly, Is.False);
        }

        [Test]
        public void TwoInvertersBringTheEdgeBack()
        {
            PathDelayResult r = PathDelay.Calculate(new[] { Inverter, Inverter }, OutputEdge.LowToHigh);
            // falls (15 max) then rises (20 max)
            Assert.That(r.MaxNs, Is.EqualTo(35));
            Assert.That(r.FinalEdge, Is.EqualTo(OutputEdge.LowToHigh));
        }

        [Test]
        public void MissingMinimum_CountsZero_AndSaysSo()
        {
            PathStage noMin = new("x", new DelayNs(null, 10, 20), new DelayNs(null, 10, 20), false);
            PathDelayResult r = PathDelay.Calculate(new[] { noMin, Buffer }, OutputEdge.LowToHigh);
            Assert.That(r.MinNs, Is.EqualTo(3));
            Assert.That(r.MinIsLowerBoundOnly, Is.True);
        }

        [Test]
        public void MissingMaximum_LeavesTheMaximumUnknown()
        {
            PathStage noMax = new("x", new DelayNs(1, 10, null), new DelayNs(1, 10, null), false);
            PathDelayResult r = PathDelay.Calculate(new[] { noMax, Buffer }, OutputEdge.LowToHigh);
            Assert.That(r.MaxNs, Is.Null);
        }

        [Test]
        public void EmptyPath_IsZero()
        {
            PathDelayResult r = PathDelay.Calculate(ReadOnlySpan<PathStage>.Empty, OutputEdge.HighToLow);
            Assert.That(r.MinNs, Is.EqualTo(0));
            Assert.That(r.MaxNs, Is.EqualTo(0));
        }
    }
}
