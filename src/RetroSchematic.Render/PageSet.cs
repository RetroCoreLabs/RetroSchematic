//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;
using System.Globalization;
using RetroSchematic.Model;

namespace RetroSchematic.Render
{
    /// <summary>
    /// One page of the redrawn drawing: the parts, connector boxes and block instances drawn on
    /// it. With the card's page layout "as the original" it is one card page; with "split
    /// automatically" a crowded card page becomes several drawn pages.
    /// </summary>
    public sealed class DrawnPage
    {
        /// <summary>
        /// The drawn page's id: the card page's id ("1"), or the card page's id, a dot and a
        /// running number when the page is split ("1.2").
        /// </summary>
        public string Id { get; init; } = "";

        /// <summary>
        /// The title shown on its tab and at the top of the drawing.
        /// </summary>
        public string Title { get; init; } = "";

        /// <summary>
        /// The card page it shows (all of it, or a part of it when split).
        /// </summary>
        public string CardPage { get; init; } = "";

        /// <summary>
        /// The original page it was read from: the card page's place in the sources list (1 =
        /// first), or null.
        /// </summary>
        public int? Source { get; init; }

        /// <summary>
        /// The parts drawn on it, by reference.
        /// </summary>
        public HashSet<string> Parts { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// The connector boxes drawn on it (P, F, P.A ...).
        /// </summary>
        public HashSet<string> Connectors { get; } = new(StringComparer.Ordinal);

        /// <summary>
        /// The block instances drawn on it, by name.
        /// </summary>
        public HashSet<string> Instances { get; } = new(StringComparer.Ordinal);
    }

    /// <summary>
    /// The drawn pages of a card (docs/FORMAT.md, "Format version 3: pages", "Drawn pages"):
    /// which page each part, connector box and instance is drawn on, and which drawn pages a
    /// signal reaches. A card without pages has one drawn page with everything on it.
    /// </summary>
    public sealed class PageSet
    {
        /// <summary>
        /// A card page with more chips than this (passive parts not counted) is split when the
        /// page layout is "split automatically". The limit is a choice, not read from anything:
        /// about the number of chips that fit an A3 drawing at the size the viewer draws.
        /// </summary>
        public const int DefaultChipLimit = 16;

        private readonly Dictionary<string, int> _partPage = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _connectorPage = new(StringComparer.Ordinal);
        private readonly Dictionary<string, int> _instancePage = new(StringComparer.Ordinal);

        private PageSet(Netlist netlist)
        {
            Netlist = netlist;
        }

        /// <summary>
        /// The card the pages are of.
        /// </summary>
        public Netlist Netlist { get; }

        /// <summary>
        /// The drawn pages, in order.
        /// </summary>
        public List<DrawnPage> Pages { get; } = new();

        /// <summary>
        /// The layout used.
        /// </summary>
        public PageLayoutMode Mode { get; private set; }

        /// <summary>
        /// Works out the drawn pages of a card with its own page layout setting.
        /// </summary>
        /// <param name="netlist">
        /// The card (the top level).
        /// </param>
        /// <returns>
        /// The pages.
        /// </returns>
        public static PageSet Of(Netlist netlist) => Of(netlist, netlist.PageLayout ?? PageLayoutMode.AsOriginal, DefaultChipLimit);

        /// <summary>
        /// Works out the drawn pages of a card.
        /// </summary>
        /// <param name="netlist">
        /// The card (the top level).
        /// </param>
        /// <param name="mode">
        /// As the original, or split automatically.
        /// </param>
        /// <param name="chipLimit">
        /// For a split: a card page with more chips than this is split.
        /// </param>
        /// <returns>
        /// The pages.
        /// </returns>
        public static PageSet Of(Netlist netlist, PageLayoutMode mode, int chipLimit)
        {
            PageSet set = new(netlist) { Mode = mode };
            if (!CardPages.HasPages(netlist))
            {
                DrawnPage all = new() { Id = "", Title = netlist.Sheet.Title, CardPage = "" };
                set.Pages.Add(all);
                for (int i = 0; i < netlist.Parts.Count; i++)
                    set.Add(0, netlist.Parts[i].Ref);
            }
            else
            {
                Dictionary<string, string> partPages = CardPages.PartPages(netlist);
                for (int p = 0; p < netlist.Pages!.Count; p++)
                {
                    CardPage page = netlist.Pages[p];
                    List<Part> parts = new();
                    for (int i = 0; i < netlist.Parts.Count; i++)
                    {
                        if (partPages[netlist.Parts[i].Ref] == page.Id)
                            parts.Add(netlist.Parts[i]);
                    }
                    List<List<Part>> groups = mode == PageLayoutMode.Split ? Split(netlist, parts, chipLimit) : new List<List<Part>> { parts };
                    for (int g = 0; g < groups.Count; g++)
                    {
                        string id = groups.Count == 1 ? page.Id : page.Id + "." + (g + 1).ToString(CultureInfo.InvariantCulture);
                        string title = page.Title ?? ("Page " + page.Id);
                        if (groups.Count > 1)
                            title += " (" + (g + 1).ToString(CultureInfo.InvariantCulture) + " of " + groups.Count.ToString(CultureInfo.InvariantCulture) + ")";
                        set.Pages.Add(new DrawnPage { Id = id, Title = title, CardPage = page.Id, Source = page.Source });
                        for (int i = 0; i < groups[g].Count; i++)
                            set.Add(set.Pages.Count - 1, groups[g][i].Ref);
                    }
                }
            }
            set.PlaceConnectors();
            set.PlaceInstances();
            return set;
        }

        private void Add(int page, string part)
        {
            Pages[page].Parts.Add(part);
            _partPage[part] = page;
        }

        /// <summary>
        /// The drawn page a part is on, or -1.
        /// </summary>
        /// <param name="reference">
        /// The part.
        /// </param>
        /// <returns>
        /// Its page's place in <see cref="Pages"/>.
        /// </returns>
        public int PageOfPart(string reference) => _partPage.TryGetValue(reference, out int p) ? p : -1;

        /// <summary>
        /// The drawn page a connector box is on, or -1.
        /// </summary>
        /// <param name="reference">
        /// The connector (P, F, P.A ...).
        /// </param>
        /// <returns>
        /// Its page's place in <see cref="Pages"/>.
        /// </returns>
        public int PageOfConnector(string reference) => _connectorPage.TryGetValue(reference, out int p) ? p : -1;

        /// <summary>
        /// The drawn page an instance box is on, or -1.
        /// </summary>
        /// <param name="name">
        /// The instance.
        /// </param>
        /// <returns>
        /// Its page's place in <see cref="Pages"/>.
        /// </returns>
        public int PageOfInstance(string name) => _instancePage.TryGetValue(name, out int p) ? p : -1;

        /// <summary>
        /// The drawn page a pin's owner is on: a part, a connector or an instance port end.
        /// </summary>
        /// <param name="at">
        /// The pin, such as 11D.3 or P.81.
        /// </param>
        /// <returns>
        /// The page's place, or -1.
        /// </returns>
        public int PageOfPin(string at)
        {
            int dot = at.LastIndexOf('.');
            if (dot <= 0)
                return -1;
            string r = at.Substring(0, dot);
            int p = PageOfPart(r);
            return p >= 0 ? p : PageOfConnector(r);
        }

        /// <summary>
        /// The drawn pages a signal reaches, in page order.
        /// </summary>
        /// <param name="net">
        /// The signal.
        /// </param>
        /// <returns>
        /// Places in <see cref="Pages"/>.
        /// </returns>
        public List<int> PagesOf(Net net)
        {
            bool[] on = new bool[Pages.Count];
            for (int j = 0; j < net.Pins.Count; j++)
            {
                int p = PageOfPin(net.Pins[j].At);
                if (p >= 0)
                    on[p] = true;
            }
            for (int i = 0; Netlist.Instances != null && i < Netlist.Instances.Count; i++)
            {
                BlockInstance inst = Netlist.Instances[i];
                for (int c = 0; inst.Connections != null && c < inst.Connections.Count; c++)
                {
                    for (int n = 0; n < inst.Connections[c].Nets.Count; n++)
                    {
                        SignalRef r = inst.Connections[c].Nets[n];
                        if (r.Net == net.Name && (r.Active == null || r.Active == net.Active))
                            on[PageOfInstance(inst.Name)] = true;
                    }
                }
            }
            List<int> list = new();
            for (int i = 0; i < on.Length; i++)
            {
                if (on[i])
                    list.Add(i);
            }
            return list;
        }

        /// <summary>
        /// The place of a drawn page by id, or -1.
        /// </summary>
        /// <param name="id">
        /// The drawn page's id.
        /// </param>
        /// <returns>
        /// Its place in <see cref="Pages"/>.
        /// </returns>
        public int IndexOf(string id)
        {
            for (int i = 0; i < Pages.Count; i++)
            {
                if (Pages[i].Id == id)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// The first drawn page of a card page, or -1.
        /// </summary>
        /// <param name="cardPage">
        /// The card page's id.
        /// </param>
        /// <returns>
        /// The place in <see cref="Pages"/>.
        /// </returns>
        public int FirstOfCardPage(string cardPage)
        {
            for (int i = 0; i < Pages.Count; i++)
            {
                if (Pages[i].CardPage == cardPage)
                    return i;
            }
            return -1;
        }

        // A connector box goes on its own card page. When that page is split, it goes on the
        // drawn page that shares the most signals with its pins.
        private void PlaceConnectors()
        {
            Dictionary<string, string> cardPages = CardPages.ConnectorPages(Netlist);
            Dictionary<string, int[]> score = new(StringComparer.Ordinal);
            for (int i = 0; i < Netlist.Nets.Count; i++)
            {
                Net net = Netlist.Nets[i];
                List<string>? conns = null;
                for (int j = 0; j < net.Pins.Count; j++)
                {
                    string r = net.Pins[j].PartRef;
                    if (ConnectorPins.IsConnector(r))
                        (conns ??= new List<string>()).Add(r);
                }
                if (conns == null)
                    continue;
                for (int j = 0; j < net.Pins.Count; j++)
                {
                    int p = PageOfPart(net.Pins[j].PartRef);
                    if (p < 0)
                        continue;
                    for (int c = 0; c < conns.Count; c++)
                    {
                        if (!score.TryGetValue(conns[c], out int[]? s))
                        {
                            s = new int[Pages.Count];
                            score[conns[c]] = s;
                        }
                        s[p]++;
                    }
                }
                for (int c = 0; c < conns.Count; c++)
                {
                    if (!score.ContainsKey(conns[c]))
                        score[conns[c]] = new int[Pages.Count];
                }
            }
            foreach (KeyValuePair<string, int[]> kv in score)
            {
                string? cardPage = cardPages.TryGetValue(kv.Key, out string? cp) ? cp : null;
                int best = -1;
                for (int p = 0; p < Pages.Count; p++)
                {
                    if (cardPage != null && Pages[p].CardPage != cardPage)
                        continue;
                    if (best < 0 || kv.Value[p] > kv.Value[best])
                        best = p;
                }
                if (best < 0)
                    best = 0;
                _connectorPage[kv.Key] = best;
                Pages[best].Connectors.Add(kv.Key);
            }
        }

        private void PlaceInstances()
        {
            for (int i = 0; Netlist.Instances != null && i < Netlist.Instances.Count; i++)
            {
                BlockInstance inst = Netlist.Instances[i];
                string? cardPage = CardPages.PageOrFirst(Netlist, inst.Page);
                int p = cardPage == null ? 0 : Math.Max(0, FirstOfCardPage(cardPage));
                _instancePage[inst.Name] = p;
                Pages[p].Instances.Add(inst.Name);
            }
        }

        /// <summary>
        /// Splits the parts of one crowded card page into groups for drawn pages
        /// (docs/FORMAT.md, "Drawn pages"). The steps:
        /// 1. every chip is a unit; a passive part joins the chip it serves, else the first chip
        ///    on the page it shares a signal with (supply lines not counted);
        /// 2. a register group stays together: chips of one type that share two or more signals
        ///    (a common clock and clear, a common select) are one unit;
        /// 3. the number of pages is the chip count divided by the limit, rounded up, and a page
        ///    may take a quarter more than an even share;
        /// 4. the two units that share the most signals are joined, over and over, as long as the
        ///    result fits a page; then the smallest groups are joined until there are no more
        ///    groups than pages.
        /// Groups come out in the order of their first part in the file. Nothing in the file
        /// changes.
        /// </summary>
        /// <param name="netlist">
        /// The card.
        /// </param>
        /// <param name="parts">
        /// The parts of the card page, in file order.
        /// </param>
        /// <param name="chipLimit">
        /// A page with more chips than this is split.
        /// </param>
        /// <returns>
        /// One list of parts per drawn page.
        /// </returns>
        public static List<List<Part>> Split(Netlist netlist, List<Part> parts, int chipLimit)
        {
            int n = parts.Count;
            int chips = 0;
            Dictionary<string, int> index = new(StringComparer.Ordinal);
            for (int i = 0; i < n; i++)
            {
                index[parts[i].Ref] = i;
                if (!PassiveParts.IsPassive(parts[i].Type))
                    chips++;
            }
            if (chips <= chipLimit || chipLimit < 1)
                return new List<List<Part>> { parts };

            // the signals of each part on this page, supply lines left out
            List<int>[] netsOf = new List<int>[n];
            for (int i = 0; i < n; i++)
                netsOf[i] = new List<int>();
            for (int k = 0; k < netlist.Nets.Count; k++)
            {
                Net net = netlist.Nets[k];
                if (SupplyNets.IsSupply(net.Name))
                    continue;
                for (int j = 0; j < net.Pins.Count; j++)
                {
                    if (index.TryGetValue(net.Pins[j].PartRef, out int p) && !netsOf[p].Contains(k))
                        netsOf[p].Add(k);
                }
            }

            // 1 + 2: the units, by joining (a small union-find over the page's parts)
            int[] root = new int[n];
            for (int i = 0; i < n; i++)
                root[i] = i;
            for (int i = 0; i < n; i++)
            {
                if (!PassiveParts.IsPassive(parts[i].Type))
                    continue;
                int chip = -1;
                if (parts[i].Serves != null && index.TryGetValue(parts[i].Serves!, out int served))
                    chip = served;
                for (int j = 0; chip < 0 && j < n; j++)
                {
                    if (j != i && !PassiveParts.IsPassive(parts[j].Type) && Shared(netsOf[i], netsOf[j]) > 0)
                        chip = j;
                }
                if (chip >= 0)
                    Join(root, i, chip);
            }
            for (int i = 0; i < n; i++)
            {
                if (PassiveParts.IsPassive(parts[i].Type))
                    continue;
                for (int j = i + 1; j < n; j++)
                {
                    if (parts[j].Type == parts[i].Type && Shared(netsOf[i], netsOf[j]) >= 2)
                        Join(root, i, j);
                }
            }
            // the groups: members and chip count
            List<List<int>> groups = new();
            Dictionary<int, int> groupOf = new();
            for (int i = 0; i < n; i++)
            {
                int r = Find(root, i);
                if (!groupOf.TryGetValue(r, out int g))
                {
                    g = groups.Count;
                    groupOf[r] = g;
                    groups.Add(new List<int>());
                }
                groups[g].Add(i);
            }

            // 3
            int pages = (chips + chipLimit - 1) / chipLimit;
            int cap = ((chips + pages - 1) / pages) + Math.Max(1, ((chips + pages - 1) / pages) / 4);

            // 4: join the pair that shares the most signals while it fits
            while (true)
            {
                int bestA = -1, bestB = -1, bestW = 0, bestSize = int.MaxValue;
                for (int a = 0; a < groups.Count; a++)
                {
                    int sa = ChipCount(parts, groups[a]);
                    for (int b = a + 1; b < groups.Count; b++)
                    {
                        int size = sa + ChipCount(parts, groups[b]);
                        if (size > cap)
                            continue;
                        int w = Weight(netsOf, groups[a], groups[b]);
                        if (w > bestW || (w == bestW && w > 0 && size < bestSize))
                        {
                            bestA = a;
                            bestB = b;
                            bestW = w;
                            bestSize = size;
                        }
                    }
                }
                if (bestA < 0)
                    break;
                groups[bestA].AddRange(groups[bestB]);
                groups.RemoveAt(bestB);
            }
            // then the smallest groups together until there are no more groups than pages
            while (groups.Count > pages)
            {
                groups.Sort((x, y) => ChipCount(parts, x).CompareTo(ChipCount(parts, y)));
                // the smallest group joins the group it shares the most signals with; on a tie (or
                // none shared) the next smallest
                int best = 1, bestW = -1;
                for (int b = 1; b < groups.Count; b++)
                {
                    int w = Weight(netsOf, groups[0], groups[b]);
                    if (w > bestW)
                    {
                        best = b;
                        bestW = w;
                    }
                }
                groups[best].AddRange(groups[0]);
                groups.RemoveAt(0);
            }
            for (int g = 0; g < groups.Count; g++)
                groups[g].Sort();
            groups.Sort((x, y) => x[0].CompareTo(y[0]));
            List<List<Part>> result = new();
            for (int g = 0; g < groups.Count; g++)
            {
                List<Part> list = new();
                for (int i = 0; i < groups[g].Count; i++)
                    list.Add(parts[groups[g][i]]);
                result.Add(list);
            }
            return result;
        }

        private static int ChipCount(List<Part> parts, List<int> group)
        {
            int c = 0;
            for (int i = 0; i < group.Count; i++)
            {
                if (!PassiveParts.IsPassive(parts[group[i]].Type))
                    c++;
            }
            return c;
        }

        // how many signals two groups share
        private static int Weight(List<int>[] netsOf, List<int> a, List<int> b)
        {
            HashSet<int> inA = new();
            for (int i = 0; i < a.Count; i++)
            {
                for (int k = 0; k < netsOf[a[i]].Count; k++)
                    inA.Add(netsOf[a[i]][k]);
            }
            HashSet<int> both = new();
            for (int i = 0; i < b.Count; i++)
            {
                for (int k = 0; k < netsOf[b[i]].Count; k++)
                {
                    if (inA.Contains(netsOf[b[i]][k]))
                        both.Add(netsOf[b[i]][k]);
                }
            }
            return both.Count;
        }

        private static int Shared(List<int> a, List<int> b)
        {
            int c = 0;
            for (int i = 0; i < a.Count; i++)
            {
                if (b.Contains(a[i]))
                    c++;
            }
            return c;
        }

        private static int Find(int[] root, int i)
        {
            while (root[i] != i)
            {
                root[i] = root[root[i]];
                i = root[i];
            }
            return i;
        }

        private static void Join(int[] root, int a, int b)
        {
            int ra = Find(root, a), rb = Find(root, b);
            if (ra != rb)
                root[Math.Max(ra, rb)] = Math.Min(ra, rb);
        }
    }
}
