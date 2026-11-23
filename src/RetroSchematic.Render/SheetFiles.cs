//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.IO;

namespace RetroSchematic.Render
{
    /// <summary>
    /// Finds the sheet files the programs offer by name.
    /// </summary>
    public static class SheetFiles
    {
        /// <summary>
        /// The demo sheet's place under a <c>sheets</c> folder.
        /// </summary>
        public const string DemoRelativePath = "sheets/demo/demo-gates.rsc";

        /// <summary>
        /// Walks up from a folder to the first one holding <see cref="DemoRelativePath"/>: the
        /// program's own folder when the file is copied there, else the repository's <c>src</c>
        /// folder above it.
        /// </summary>
        /// <param name="start">
        /// The folder to start from, usually the program's folder.
        /// </param>
        /// <returns>
        /// The demo sheet's full path, or null when it is not found.
        /// </returns>
        public static string? FindDemo(string start)
        {
            string? dir = Path.GetFullPath(start);
            while (dir != null)
            {
                string candidate = Path.Combine(dir, "sheets", "demo", "demo-gates.rsc");
                if (File.Exists(candidate))
                    return candidate;
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }
    }
}
