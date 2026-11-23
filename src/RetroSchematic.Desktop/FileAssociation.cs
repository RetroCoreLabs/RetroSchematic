//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Runtime.Versioning;

namespace RetroSchematic.Desktop
{
    /// <summary>
    /// Writes and reads registry values under HKEY_CURRENT_USER. The file association talks to
    /// the registry only through this, so tests can hand it a fake and never touch the real one.
    /// </summary>
    public interface IRegistryWriter
    {
        /// <summary>
        /// Reads a key's default value, or null when the key or value is missing.
        /// </summary>
        /// <param name="subKey">
        /// The key under HKEY_CURRENT_USER (Software\Classes\.rsc).
        /// </param>
        /// <returns>
        /// The value, or null.
        /// </returns>
        string? GetDefault(string subKey);

        /// <summary>
        /// Sets a key's default value, making the key when it is missing.
        /// </summary>
        /// <param name="subKey">
        /// The key under HKEY_CURRENT_USER.
        /// </param>
        /// <param name="value">
        /// The text to store.
        /// </param>
        void SetDefault(string subKey, string value);

        /// <summary>
        /// Deletes a key and everything under it; nothing happens when it is missing.
        /// </summary>
        /// <param name="subKey">
        /// The key under HKEY_CURRENT_USER.
        /// </param>
        void DeleteTree(string subKey);
    }

    /// <summary>
    /// The real registry, HKEY_CURRENT_USER only (no administrator rights needed).
    /// </summary>
    [SupportedOSPlatform("windows")]
    public sealed class CurrentUserRegistry : IRegistryWriter
    {
        /// <inheritdoc/>
        public string? GetDefault(string subKey)
        {
            using Microsoft.Win32.RegistryKey? k = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(subKey);
            return k?.GetValue(null) as string;
        }

        /// <inheritdoc/>
        public void SetDefault(string subKey, string value)
        {
            using Microsoft.Win32.RegistryKey k = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(subKey);
            k.SetValue(null, value);
        }

        /// <inheritdoc/>
        public void DeleteTree(string subKey) => Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree(subKey, throwOnMissingSubKey: false);
    }

    /// <summary>
    /// The per-user Windows file association for sheet files: .rsc and .rsx open in
    /// RetroSchematic on a double-click. Only keys under HKEY_CURRENT_USER\Software\Classes are
    /// written, so no administrator rights are needed and other users are not touched.
    /// </summary>
    public sealed class FileAssociation
    {
        /// <summary>
        /// The program id both endings point at.
        /// </summary>
        public const string ProgId = "RetroSchematic.Sheet";

        private const string Classes = @"Software\Classes\";

        private static readonly string[] Endings = { ".rsc", ".rsx" };

        private readonly IRegistryWriter _registry;

        /// <summary>
        /// Makes an association that writes through the given registry.
        /// </summary>
        /// <param name="registry">
        /// The registry (the real one, or a fake in tests).
        /// </param>
        public FileAssociation(IRegistryWriter registry) => _registry = registry;

        /// <summary>
        /// Makes .rsc and .rsx open with the given program: each ending names
        /// <see cref="ProgId"/>, whose open command runs the program with the file.
        /// </summary>
        /// <param name="exePath">
        /// The full path of RetroSchematic.exe.
        /// </param>
        public void Register(string exePath)
        {
            _registry.SetDefault(Classes + ProgId, "RetroSchematic sheet");
            _registry.SetDefault(Classes + ProgId + @"\DefaultIcon", "\"" + exePath + "\",0");
            _registry.SetDefault(Classes + ProgId + @"\shell\open\command", "\"" + exePath + "\" \"%1\"");
            for (int i = 0; i < Endings.Length; i++)
                _registry.SetDefault(Classes + Endings[i], ProgId);
        }

        /// <summary>
        /// Takes the association away again: the program id is deleted, and an ending is
        /// deleted only while it still names <see cref="ProgId"/> (another program that took
        /// it over keeps it).
        /// </summary>
        public void Unregister()
        {
            for (int i = 0; i < Endings.Length; i++)
            {
                if (string.Equals(_registry.GetDefault(Classes + Endings[i]), ProgId, StringComparison.Ordinal))
                    _registry.DeleteTree(Classes + Endings[i]);
            }
            _registry.DeleteTree(Classes + ProgId);
        }

        /// <summary>
        /// True when both endings name <see cref="ProgId"/>.
        /// </summary>
        /// <returns>
        /// True when sheet files open with RetroSchematic.
        /// </returns>
        public bool IsRegistered()
        {
            for (int i = 0; i < Endings.Length; i++)
            {
                if (!string.Equals(_registry.GetDefault(Classes + Endings[i]), ProgId, StringComparison.Ordinal))
                    return false;
            }
            return true;
        }
    }
}
