//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System.Collections.Generic;
using RetroSchematic.Model;

namespace RetroSchematic.Render
{
    /// <summary>
    /// The machine-bus side of a session: which bus the card is for (sheet.bus), and the
    /// bus-signal catalogue that choice picks.
    /// </summary>
    public sealed partial class SheetSession
    {
        private BusCatalog? _busSignals;

        /// <summary>
        /// Which machine bus the card is for. A sheet that does not say is
        /// <see cref="MachineBus.Unknown"/>.
        /// </summary>
        public MachineBus Bus => Editor.Root.Sheet.Bus ?? MachineBus.Unknown;

        /// <summary>
        /// The bus-signal catalogue for <see cref="Bus"/>: only the files written for that bus.
        /// Empty for an unknown bus. Read once, again after the bus changes.
        /// </summary>
        public BusCatalog BusSignals
        {
            get
            {
                if (_busSignals == null || _busSignals.Bus != Bus)
                    _busSignals = BusCatalog.Load(_options.Catalog?.Folder, Bus);
                return _busSignals;
            }
        }

        /// <summary>
        /// A warning to show when the sheet does not say which bus it is for, else null.
        /// </summary>
        public string? BusWarning => Bus == MachineBus.Unknown
            ? "This sheet does not say which machine bus it is for (Settings, This sheet), so no bus signal or connector catalogue is used."
            : null;

        /// <summary>
        /// The bus as text for the title bar and the settings window: NORD-10, ND-100 or unknown bus.
        /// </summary>
        /// <param name="bus">
        /// The bus.
        /// </param>
        /// <returns>
        /// The text.
        /// </returns>
        public static string BusText(MachineBus bus) => bus switch
        {
            MachineBus.Nord10 => "NORD-10",
            MachineBus.Nd100 => "ND-100",
            _ => "unknown bus",
        };

        /// <summary>
        /// The side-panel text for a connector box: its name, the catalogue entry the sheet
        /// names for it, and its pins with their signals.
        /// </summary>
        /// <param name="reference">
        /// The connector, such as P or P.A.
        /// </param>
        /// <returns>
        /// The text.
        /// </returns>
        private string DescribeConnector(string reference)
        {
            System.Text.StringBuilder sb = new();
            sb.Append(ConnectorPins.Name(reference, Bus)).Append('\n');
            List<ConnectorUse>? uses = Editor.Root.Connectors;
            for (int i = 0; uses != null && i < uses.Count; i++)
            {
                if (uses[i].Ref != reference)
                    continue;
                if (uses[i].Catalog != null)
                    sb.Append("Catalogue: ").Append(uses[i].Catalog).Append('\n');
                if (!string.IsNullOrEmpty(uses[i].Note))
                    sb.Append("Note: ").Append(uses[i].Note).Append('\n');
            }
            for (int i = 0; i < Netlist.Nets.Count; i++)
            {
                Net n = Netlist.Nets[i];
                for (int j = 0; j < n.Pins.Count; j++)
                {
                    if (n.Pins[j].PartRef == reference)
                        sb.Append(ConnectorPins.Show(reference, n.Pins[j].Pin, Bus)).Append("  ").Append(NameOf(n)).Append('\n');
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Sets which bus the card is for. Marks the sheet as changed; can be undone.
        /// </summary>
        /// <param name="bus">
        /// The bus.
        /// </param>
        public void SetBus(MachineBus bus)
        {
            if (bus == Bus && Editor.Root.Sheet.Bus != null)
                return;
            Editor.SetBus(bus);
        }
    }
}
