//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Diagnostics;
using System.IO;
using NUnit.Framework;
using RetroSchematic.Model;

namespace RetroSchematic.Tests
{
    /// <summary>
    /// The build stamp (Directory.Build.props, read by <see cref="BuildIdentity"/>): that it is
    /// really there and names a real commit. Copied in shape from RetroTerm's BuildIdentityTests.
    /// The stamp lives in MSBuild, where a mistyped property fails silently and the build stays
    /// green, so nothing else would notice a stamp that quietly reads "unknown".
    /// </summary>
    [TestFixture]
    public class BuildIdentityTests
    {
        [Test]
        public void TheCommitIsRecorded_TwelveHexDigits_NotTheFallback()
        {
            string commit = BuildIdentity.Commit;
            Assert.That(commit, Is.Not.EqualTo(BuildIdentity.Unknown));
            Assert.That(commit.Length, Is.EqualTo(12));
            for (int i = 0; i < commit.Length; i++)
            {
                char c = commit[i];
                bool hex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f');
                Assert.That(hex, Is.True, "the commit contains '" + c + "', which is not a hex digit");
            }
        }

        [Test]
        public void TheStampedCommitIsARealCommitInThisRepository()
        {
            // Deliberately NOT compared with HEAD: the order is build, test, commit, so right after
            // a commit the binary carries the parent commit and a HEAD check would go red.
            string kind = RunGit("cat-file -t " + BuildIdentity.Commit);
            if (kind.Length == 0)
                Assert.Ignore("git could not answer here (no git, or not a checkout)");
            Assert.That(kind, Is.EqualTo("commit"));
        }

        [Test]
        public void TheBranchAndBuildDateAndTimeAreRecorded()
        {
            Assert.That(BuildIdentity.Branch, Is.Not.EqualTo(BuildIdentity.Unknown));
            Assert.That(BuildIdentity.BuildDate, Is.Not.EqualTo(BuildIdentity.Unknown));
            Assert.That(BuildIdentity.BuildTime, Is.Not.EqualTo(BuildIdentity.Unknown));
            Assert.That(BuildIdentity.Version, Is.Not.EqualTo(BuildIdentity.Unknown));
            Assert.That(BuildIdentity.Version.Contains('+'), Is.False, "the SDK's '+<full hash>' must be cut off");
        }

        [Test]
        public void TheDescriptionNamesEverything_AndSaysWhenUncommitted()
        {
            string d = BuildIdentity.Describe("retroschematic");
            Assert.That(d, Does.StartWith("retroschematic " + BuildIdentity.Version));
            Assert.That(d, Does.Contain(BuildIdentity.Commit));
            Assert.That(d, Does.Contain(BuildIdentity.Branch));
            Assert.That(d, Does.Contain(BuildIdentity.BuildDate + " " + BuildIdentity.BuildTime));
            Assert.That(d.Contains("(uncommitted changes)"), Is.EqualTo(BuildIdentity.IsDirty));
        }

        /// <summary>
        /// Asks git something, from the folder the test assembly is in (inside the checkout).
        /// </summary>
        /// <param name="arguments">
        /// The git arguments.
        /// </param>
        /// <returns>
        /// The trimmed output, or an empty string when git could not answer.
        /// </returns>
        private static string RunGit(string arguments)
        {
            try
            {
                ProcessStartInfo start = new("git", arguments)
                {
                    WorkingDirectory = Path.GetDirectoryName(typeof(BuildIdentityTests).Assembly.Location) ?? ".",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                };
                using Process? p = Process.Start(start);
                if (p == null)
                    return string.Empty;
                string output = p.StandardOutput.ReadToEnd();
                if (!p.WaitForExit(10_000) || p.ExitCode != 0)
                    return string.Empty;
                return output.Trim();
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
