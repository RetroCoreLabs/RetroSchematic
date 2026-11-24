//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Reflection;

namespace RetroSchematic.Model
{
    /// <summary>
    /// Which build of RetroSchematic is running: version, git commit, branch, whether the tree had
    /// uncommitted changes, and when it was built.
    /// </summary>
    /// <remarks>
    /// <para><b>Where the values come from</b></para>
    /// Directory.Build.props stamps every assembly with <c>AssemblyMetadata</c> entries (BuildDate,
    /// BuildTime, GitCommit, GitBranch, GitDirty) at build time. This class reads them back. The
    /// mechanism is copied from RetroTerm (RetroTerm.Core/BuildIdentity.cs), where it was added on
    /// 27 August 2026 after two copies of that program ran from one folder and nobody could tell
    /// which build was answering. It is shown in the Desktop About box, by
    /// <c>retroschematic --version</c> and in the MCP server's version.
    ///
    /// <para><b>The running program first, this library second</b></para>
    /// The stamp is read from the entry assembly (the program that was launched) when it carries
    /// our stamp. Under a test runner the entry assembly is the runner, which carries none, so this
    /// library is used instead - both are built from the same tree by the same rules, and it keeps
    /// the stamp testable.
    ///
    /// <para><b>Never throws</b></para>
    /// Every value falls back to "unknown". A missing stamp loses a little information; an exception
    /// while a program reports who it is would stop it starting.
    /// </remarks>
    public static class BuildIdentity
    {
        /// <summary>
        /// What a value reads when the build did not record it.
        /// </summary>
        public const string Unknown = "unknown";

        /// <summary>
        /// The assembly whose stamp is reported: the entry assembly when it is one of ours,
        /// otherwise this library.
        /// </summary>
        private static readonly Assembly s_stamped = ChooseStampedAssembly();

        /// <summary>
        /// Gets the version, from the informational version with the part after '+' cut off.
        /// </summary>
        public static string Version { get; } = ReadVersion();

        /// <summary>
        /// Gets the copyright line, as set in Directory.Build.props.
        /// </summary>
        public static string Copyright { get; } = ReadCopyright();

        /// <summary>
        /// Gets the short (12 hex digit) git commit the build came from, or "unknown".
        /// </summary>
        public static string Commit { get; } = ReadMetadata("GitCommit");

        /// <summary>
        /// Gets the branch the build came from, or "unknown".
        /// </summary>
        public static string Branch { get; } = ReadMetadata("GitBranch");

        /// <summary>
        /// Gets a value indicating whether the working tree had uncommitted changes at build time.
        /// </summary>
        /// <remarks>
        /// A commit alone claims the build is exactly that commit; during ordinary work it usually
        /// is not, so the hash is always shown with this flag.
        /// </remarks>
        public static bool IsDirty { get; } =
            string.Equals(ReadMetadata("GitDirty"), "true", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the date the build was made, as "dd.MMM.yyyy".
        /// </summary>
        public static string BuildDate { get; } = ReadMetadata("BuildDate");

        /// <summary>
        /// Gets the time the build was made, as "HH:mm:ss".
        /// </summary>
        public static string BuildTime { get; } = ReadMetadata("BuildTime");

        /// <summary>
        /// Gets the commit line, as the About box shows it.
        /// </summary>
        /// <returns>
        /// For example "commit 94375af1c2d3 on master (uncommitted changes)".
        /// </returns>
        public static string CommitLine()
            => "commit " + Commit + " on " + Branch + (IsDirty ? " (uncommitted changes)" : string.Empty);

        /// <summary>
        /// Names the build in one line, for a log, a version command or a server's version.
        /// </summary>
        /// <param name="program">
        /// The program name to start the line with.
        /// </param>
        /// <returns>
        /// For example "retroschematic 0.1.0, commit 94375af1c2d3 on master, built 24.Nov.2026 10:12:03".
        /// </returns>
        public static string Describe(string program)
            => program + " " + Version + ", " + CommitLine() + ", built " + BuildDate + " " + BuildTime;

        /// <summary>
        /// Picks the assembly to read the stamp from.
        /// </summary>
        /// <returns>
        /// The entry assembly when it carries our stamp, otherwise this library.
        /// </returns>
        private static Assembly ChooseStampedAssembly()
        {
            Assembly self = typeof(BuildIdentity).Assembly;
            try
            {
                Assembly? entry = Assembly.GetEntryAssembly();
                return entry != null && Find(entry, "GitCommit") != null ? entry : self;
            }
            catch
            {
                return self;
            }
        }

        /// <summary>
        /// Reads the stamped assembly's version.
        /// </summary>
        /// <returns>
        /// The version, or "unknown".
        /// </returns>
        private static string ReadVersion()
        {
            try
            {
                AssemblyInformationalVersionAttribute? informational = s_stamped.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
                if (informational != null && !string.IsNullOrEmpty(informational.InformationalVersion))
                {
                    // The SDK appends the full forty-character commit after a '+'. It is cut off
                    // here because the short commit is reported beside the version anyway.
                    string text = informational.InformationalVersion;
                    int plus = text.IndexOf('+');
                    return plus > 0 ? text.Substring(0, plus) : text;
                }

                Version? version = s_stamped.GetName().Version;
                return version == null ? Unknown : version.ToString();
            }
            catch
            {
                return Unknown;
            }
        }

        /// <summary>
        /// Reads the stamped assembly's copyright line.
        /// </summary>
        /// <returns>
        /// The copyright, or "unknown".
        /// </returns>
        private static string ReadCopyright()
        {
            try
            {
                AssemblyCopyrightAttribute? a = s_stamped.GetCustomAttribute<AssemblyCopyrightAttribute>();
                return a == null || string.IsNullOrEmpty(a.Copyright) ? Unknown : a.Copyright;
            }
            catch
            {
                return Unknown;
            }
        }

        /// <summary>
        /// Reads one build-time metadata value from the stamped assembly.
        /// </summary>
        /// <param name="key">
        /// The metadata key, as spelled in Directory.Build.props.
        /// </param>
        /// <returns>
        /// The value, or "unknown".
        /// </returns>
        private static string ReadMetadata(string key)
        {
            try
            {
                string? value = Find(s_stamped, key);
                return string.IsNullOrEmpty(value) ? Unknown : value;
            }
            catch
            {
                return Unknown;
            }
        }

        /// <summary>
        /// Finds one metadata value on an assembly.
        /// </summary>
        /// <param name="assembly">
        /// The assembly to look in.
        /// </param>
        /// <param name="key">
        /// The metadata key.
        /// </param>
        /// <returns>
        /// The value, or null when the assembly has no entry with that key.
        /// </returns>
        private static string? Find(Assembly assembly, string key)
        {
            // GetCustomAttributes(Type, bool) gives an array, walked by index (no LINQ)
            object[] all = assembly.GetCustomAttributes(typeof(AssemblyMetadataAttribute), false);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] is AssemblyMetadataAttribute m && string.Equals(m.Key, key, StringComparison.Ordinal))
                    return m.Value ?? string.Empty;
            }

            return null;
        }
    }
}
