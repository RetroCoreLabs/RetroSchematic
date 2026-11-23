//
// SPDX-License-Identifier: MIT
// Copyright (c) 1985-2026 Ronny Hansen
// RetroCore Labs — https://github.com/RetroCoreLabs
//

using System;
using System.Collections.Generic;

namespace RetroSchematic.Render
{
    /// <summary>
    /// Routes wires on a square grid with A* search, around obstacles: chip and gate frames (with
    /// a margin), text, and the room kept free for signal labels. Wires use horizontal and
    /// vertical pieces only. Two wires may cross at a right angle, but never run along the same
    /// grid line on top of each other, never turn on another wire, and never end on one. A turn
    /// costs extra, so a route with few bends wins over a slightly shorter one with many.
    /// <para>
    /// A signal with more than two pins is routed as a tree: each further pin is routed to the
    /// nearest point of the wires already laid for that signal, and joins it at a right angle
    /// (a junction dot is drawn there) or at a pin.
    /// </para>
    /// </summary>
    public sealed class WireRouter
    {
        /// <summary>
        /// Grid step in drawing units. Every pin end lies on the grid.
        /// </summary>
        public const double Step = 6;

        private const int BendCost = 6;
        private const int Hard = -1;
        private const byte Horizontal = 1;
        private const byte Vertical = 2;
        private const byte Corner = 4;

        // direction 0 = +x, 1 = -x, 2 = +y, 3 = -y
        private static readonly int[] s_dx = { 1, -1, 0, 0 };
        private static readonly int[] s_dy = { 0, 0, 1, -1 };

        private readonly int _width;
        private readonly int _height;
        private readonly int[] _owner;
        private readonly byte[] _used;
        private readonly int[] _usedBy;
        private readonly int[] _cost;
        private readonly int[] _from;
        private readonly int[] _seen;
        private int _stamp;

        /// <summary>
        /// A grid covering a drawing of the given size.
        /// </summary>
        /// <param name="width">
        /// Drawing width in units.
        /// </param>
        /// <param name="height">
        /// Drawing height in units.
        /// </param>
        public WireRouter(double width, double height)
        {
            _width = (int)Math.Ceiling(width / Step) + 1;
            _height = (int)Math.Ceiling(height / Step) + 1;
            int n = _width * _height;
            _owner = new int[n];
            _used = new byte[n];
            _usedBy = new int[n];
            _cost = new int[n * 4];
            _from = new int[n * 4];
            _seen = new int[n * 4];
        }

        /// <summary>
        /// Marks every grid point inside a box (edges included) as closed to all wires.
        /// </summary>
        /// <param name="left">
        /// Left edge.
        /// </param>
        /// <param name="top">
        /// Top edge.
        /// </param>
        /// <param name="right">
        /// Right edge.
        /// </param>
        /// <param name="bottom">
        /// Bottom edge.
        /// </param>
        public void Block(double left, double top, double right, double bottom) => Mark(left, top, right, bottom, Hard);

        /// <summary>
        /// Marks every grid point inside a box as open only to one signal (its own pin end and the
        /// room kept for its label). A point already closed stays closed.
        /// </summary>
        /// <param name="left">
        /// Left edge.
        /// </param>
        /// <param name="top">
        /// Top edge.
        /// </param>
        /// <param name="right">
        /// Right edge.
        /// </param>
        /// <param name="bottom">
        /// Bottom edge.
        /// </param>
        /// <param name="net">
        /// The signal's number, 1 or more.
        /// </param>
        public void Reserve(double left, double top, double right, double bottom, int net) => Mark(left, top, right, bottom, net);

        /// <summary>
        /// Routes one signal between its pin ends. Each pin end is given with the direction a wire
        /// must leave it in (away from its chip). Nothing is kept when any pin cannot be reached.
        /// </summary>
        /// <param name="net">
        /// The signal's number, 1 or more, as given to <see cref="Reserve"/>.
        /// </param>
        /// <param name="pins">
        /// The pin ends, on the grid.
        /// </param>
        /// <param name="outward">
        /// For each pin, the direction a wire leaves it: +1 to the right, -1 to the left.
        /// </param>
        /// <param name="reach">
        /// How far (units) a route may stray outside the box round the pins.
        /// </param>
        /// <param name="segments">
        /// Filled with the wire pieces as (x1, y1, x2, y2, p): p is the index of the pin whose
        /// branch the piece belongs to (the branch from pin p to the wires laid before it).
        /// </param>
        /// <param name="junctions">
        /// Filled with the points where a branch joins the middle of a wire.
        /// </param>
        /// <returns>
        /// True when every pin was reached.
        /// </returns>
        public bool Route(int net, List<DrawPoint> pins, List<int> outward, double reach,
            List<double[]> segments, List<DrawPoint> junctions)
        {
            segments.Clear();
            junctions.Clear();
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            int[] cells = new int[pins.Count];
            for (int i = 0; i < pins.Count; i++)
            {
                int cx = (int)Math.Round(pins[i].X / Step), cy = (int)Math.Round(pins[i].Y / Step);
                if (cx < 0 || cy < 0 || cx >= _width || cy >= _height)
                    return false;
                cells[i] = (cy * _width) + cx;
                minX = Math.Min(minX, cx); maxX = Math.Max(maxX, cx);
                minY = Math.Min(minY, cy); maxY = Math.Max(maxY, cy);
            }
            int pad = (int)Math.Ceiling(reach / Step);
            minX = Math.Max(0, minX - pad); minY = Math.Max(0, minY - pad);
            maxX = Math.Min(_width - 1, maxX + pad); maxY = Math.Min(_height - 1, maxY + pad);

            // the tree: cells of this signal's wires, and which pin end each tree pin cell is
            Dictionary<int, int> treePins = new() { [cells[0]] = outward[0] };
            HashSet<int> tree = new() { cells[0] };
            List<int> marked = new();
            List<int> path = new();
            for (int p = 1; p < pins.Count; p++)
            {
                if (!Search(net, cells[p], outward[p], tree, treePins, minX, minY, maxX, maxY, path))
                {
                    Unmark(marked);
                    segments.Clear();
                    junctions.Clear();
                    return false;
                }
                int end = path[^1];
                if (!treePins.ContainsKey(end))
                    junctions.Add(new DrawPoint((end % _width) * Step, (end / _width) * Step));
                Lay(net, path, marked);
                AddSegments(path, segments, p);
                for (int i = 0; i < path.Count; i++)
                    tree.Add(path[i]);
                treePins[cells[p]] = outward[p];
            }
            return true;
        }

        /// <summary>
        /// Lays a stored wire path (one saved in the sheet's layout) when it still obeys every
        /// routing rule: straight pieces on the grid, nothing through a frame, text or another
        /// signal's label room, no running along or turning on another wire, the first branch
        /// joining two pins, and every later branch running from a pin to the wire laid so far.
        /// Nothing is kept when any rule is broken; the caller then routes the signal afresh.
        /// </summary>
        /// <param name="net">
        /// The signal's number, as given to <see cref="Reserve"/>.
        /// </param>
        /// <param name="pins">
        /// The signal's pin ends.
        /// </param>
        /// <param name="branches">
        /// The stored branches, each a list of bend points in drawing units.
        /// </param>
        /// <param name="segments">
        /// Filled as by <see cref="Route"/>: (x1, y1, x2, y2, p), p the pin the branch starts at.
        /// </param>
        /// <param name="junctions">
        /// Filled with the points where a branch joins the middle of a wire.
        /// </param>
        /// <returns>
        /// True when the path was kept.
        /// </returns>
        public bool Keep(int net, List<DrawPoint> pins, List<List<DrawPoint>> branches, List<double[]> segments, List<DrawPoint> junctions)
        {
            Refusal = "";
            segments.Clear();
            junctions.Clear();
            Dictionary<int, int> pinIndex = new();
            for (int i = 0; i < pins.Count; i++)
            {
                // a pin end is taken to its nearest cell, exactly as Route does (some pin ends,
                // such as the ND AND gate's output, are not on the grid)
                int cx = (int)Math.Round(pins[i].X / Step), cy = (int)Math.Round(pins[i].Y / Step);
                if (cx < 0 || cy < 0 || cx >= _width || cy >= _height)
                    return false;
                pinIndex[(cy * _width) + cx] = i;
            }
            HashSet<int> tree = new();
            HashSet<int> reachedPins = new();
            List<int> marked = new();
            List<int> cells = new();
            for (int b = 0; b < branches.Count; b++)
            {
                if (!Expand(branches[b], cells))
                    return Fail(marked, segments, junctions, $"branch {b + 1} is off the grid, slanted or too short");
                int first = cells[0], last = cells[^1];
                bool firstPin = pinIndex.ContainsKey(first), lastPin = pinIndex.ContainsKey(last);
                // the branch's own pin is the end that is a pin not yet wired
                int start;
                if (firstPin && !reachedPins.Contains(first))
                    start = first;
                else if (lastPin && !reachedPins.Contains(last))
                    start = last;
                else
                    return Fail(marked, segments, junctions, $"branch {b + 1} does not start at a pin that is not yet wired");
                int other = start == first ? last : first;
                bool joins = b == 0 ? pinIndex.ContainsKey(other) && other != start : tree.Contains(other);
                if (!joins)
                    return Fail(marked, segments, junctions, b == 0 ? "the first branch does not join two pins" : $"branch {b + 1} does not end on the wire before it");
                if (start == last)
                    cells.Reverse();
                // every cell between the ends: free of frames, of other labels, and of other wires
                // except a right-angle crossing
                for (int i = 1; i < cells.Count - 1; i++)
                {
                    int d = Direction(cells[i - 1], cells[i]);
                    int next = Direction(cells[i], cells[i + 1]);
                    if (!CanEnter(net, cells[i], d, d, tree))
                        return Fail(marked, segments, junctions, $"branch {b + 1} runs through a chip, a text, another signal's label or along another wire at {Where(cells[i])}");
                    if (d != next && !tree.Contains(cells[i]) && _used[cells[i]] != 0)
                        return Fail(marked, segments, junctions, $"branch {b + 1} turns on another wire at {Where(cells[i])}");
                }
                // the far end: its own wire, or one of its own pins (a pin end may lie under its
                // own pin number, which Route never checks either), or a free cell
                if (!tree.Contains(cells[^1]) && !pinIndex.ContainsKey(cells[^1]) && !CanEnter(net, cells[^1], Direction(cells[^2], cells[^1]), 0, tree))
                    return Fail(marked, segments, junctions, $"branch {b + 1} ends on something that is not its own pin or wire at {Where(cells[^1])}");
                int end = cells[^1];
                if (b > 0 && !pinIndex.ContainsKey(end))
                    junctions.Add(new DrawPoint((end % _width) * Step, (end / _width) * Step));
                Lay(net, cells, marked);
                AddSegments(cells, segments, pinIndex[cells[0]]);
                for (int i = 0; i < cells.Count; i++)
                    tree.Add(cells[i]);
                reachedPins.Add(cells[0]);
                if (b == 0)
                    reachedPins.Add(cells[^1]);
            }
            // every pin must be on the wire
            if (reachedPins.Count != pinIndex.Count)
                return Fail(marked, segments, junctions, $"only {reachedPins.Count} of the {pinIndex.Count} pins are on the wire");
            return true;
        }

        /// <summary>
        /// Why the last <see cref="Keep"/> refused its path, in plain words; empty when it kept it.
        /// </summary>
        public string Refusal { get; private set; } = "";

        private string Where(int cell) => $"({(cell % _width) * Step}, {(cell / _width) * Step})";

        private bool Fail(List<int> marked, List<double[]> segments, List<DrawPoint> junctions, string why)
        {
            Refusal = why;
            Unmark(marked);
            segments.Clear();
            junctions.Clear();
            return false;
        }

        private bool ToCell(double x, double y, out int cell)
        {
            double fx = x / Step, fy = y / Step;
            int cx = (int)Math.Round(fx), cy = (int)Math.Round(fy);
            cell = (cy * _width) + cx;
            // on the grid and on the board
            return Math.Abs(fx - cx) < 1e-6 && Math.Abs(fy - cy) < 1e-6 && cx >= 0 && cy >= 0 && cx < _width && cy < _height;
        }

        /// <summary>
        /// Turns a list of bend points into the cells it passes through, refusing a slanted piece
        /// or a point off the grid.
        /// </summary>
        /// <param name="points">
        /// The bend points.
        /// </param>
        /// <param name="cells">
        /// Filled with the cells, first point first.
        /// </param>
        /// <returns>
        /// False for a broken path.
        /// </returns>
        private bool Expand(List<DrawPoint> points, List<int> cells)
        {
            cells.Clear();
            if (points.Count < 2 || !ToCell(points[0].X, points[0].Y, out int c))
                return false;
            cells.Add(c);
            for (int i = 1; i < points.Count; i++)
            {
                if (!ToCell(points[i].X, points[i].Y, out int to))
                    return false;
                int fx = c % _width, fy = c / _width, tx = to % _width, ty = to / _width;
                if (fx != tx && fy != ty)
                    return false; // slanted
                if (to == c)
                    continue;
                int sx = Math.Sign(tx - fx), sy = Math.Sign(ty - fy);
                while (c != to)
                {
                    fx += sx;
                    fy += sy;
                    c = (fy * _width) + fx;
                    cells.Add(c);
                }
            }
            return cells.Count >= 2;
        }

        private int Direction(int from, int to)
        {
            int d = to - from;
            return d == 1 ? 0 : d == -1 ? 1 : d == _width ? 2 : 3;
        }

        /// <summary>
        /// The bend points of the branches laid for a signal, in the order <see cref="Route"/>
        /// or <see cref="Keep"/> laid them: each branch from its own pin to the wire before it.
        /// </summary>
        /// <param name="segments">
        /// The pieces as filled by <see cref="Route"/> or <see cref="Keep"/>.
        /// </param>
        /// <returns>
        /// One list of bend points per branch.
        /// </returns>
        public static List<List<DrawPoint>> Branches(List<double[]> segments)
        {
            List<List<DrawPoint>> list = new();
            int current = -1;
            for (int i = 0; i < segments.Count; i++)
            {
                double[] g = segments[i];
                if ((int)g[4] != current)
                {
                    current = (int)g[4];
                    list.Add(new List<DrawPoint> { new(g[0], g[1]) });
                }
                list[^1].Add(new DrawPoint(g[2], g[3]));
            }
            return list;
        }

        private void Mark(double left, double top, double right, double bottom, int value)
        {
            int x0 = Math.Max(0, (int)Math.Ceiling(left / Step)), x1 = Math.Min(_width - 1, (int)Math.Floor(right / Step));
            int y0 = Math.Max(0, (int)Math.Ceiling(top / Step)), y1 = Math.Min(_height - 1, (int)Math.Floor(bottom / Step));
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    int i = (y * _width) + x;
                    if (_owner[i] != Hard)
                        _owner[i] = value;
                }
            }
        }

        /// <summary>
        /// A* from a pin end to any cell of the tree. States are (cell, direction of the last step).
        /// </summary>
        /// <param name="net">
        /// The signal.
        /// </param>
        /// <param name="start">
        /// The pin end's cell.
        /// </param>
        /// <param name="outward">
        /// The direction the wire must leave the pin in.
        /// </param>
        /// <param name="tree">
        /// The signal's wire cells so far.
        /// </param>
        /// <param name="treePins">
        /// The pin end cells in the tree, with their outward direction.
        /// </param>
        /// <param name="minX">
        /// Search box left.
        /// </param>
        /// <param name="minY">
        /// Search box top.
        /// </param>
        /// <param name="maxX">
        /// Search box right.
        /// </param>
        /// <param name="maxY">
        /// Search box bottom.
        /// </param>
        /// <param name="path">
        /// Filled with the cells from the pin to the tree.
        /// </param>
        /// <returns>
        /// True when the tree was reached.
        /// </returns>
        private bool Search(int net, int start, int outward, HashSet<int> tree, Dictionary<int, int> treePins,
            int minX, int minY, int maxX, int maxY, List<int> path)
        {
            _stamp++;
            path.Clear();
            // the goal for the heuristic: the nearest tree cell is not known cheaply, so the
            // distance to the tree's first pin is used, scaled down so it never overestimates much
            int gx = int.MaxValue, gy = 0;
            foreach (int c in tree)
            {
                gx = c % _width;
                gy = c / _width;
                break;
            }
            PriorityQueue<int, int> open = new();
            int firstDir = outward > 0 ? 0 : 1;
            int sx = start % _width, sy = start / _width;
            int nx0 = sx + s_dx[firstDir], ny0 = sy + s_dy[firstDir];
            if (nx0 < minX || nx0 > maxX || ny0 < minY || ny0 > maxY)
                return false;
            int first = (ny0 * _width) + nx0;
            if (!CanEnter(net, first, firstDir, firstDir, tree))
                return false;
            int s0 = (first * 4) + firstDir;
            _seen[s0] = _stamp;
            _cost[s0] = 1;
            _from[s0] = -1 - start;
            open.Enqueue(s0, 1 + Heuristic(nx0, ny0, gx, gy, tree.Count));
            int goal = -1;
            while (open.TryDequeue(out int state, out _))
            {
                int cell = state >> 2, dir = state & 3;
                int g = _cost[state];
                if (tree.Contains(cell))
                {
                    if (Arrives(cell, dir, treePins))
                    {
                        goal = state;
                        break;
                    }
                    continue;
                }
                int cx = cell % _width, cy = cell / _width;
                for (int d = 0; d < 4; d++)
                {
                    if (d == (dir ^ 1))
                        continue; // no going straight back
                    int nx = cx + s_dx[d], ny = cy + s_dy[d];
                    if (nx < minX || nx > maxX || ny < minY || ny > maxY)
                        continue;
                    int next = (ny * _width) + nx;
                    if (d != dir && _used[cell] != 0)
                        continue; // never turn on another wire
                    if (!CanEnter(net, next, d, dir, tree))
                        continue;
                    int ns = (next * 4) + d;
                    int ng = g + 1 + (d != dir ? BendCost : 0);
                    if (_seen[ns] == _stamp && _cost[ns] <= ng)
                        continue;
                    _seen[ns] = _stamp;
                    _cost[ns] = ng;
                    _from[ns] = state;
                    open.Enqueue(ns, ng + Heuristic(nx, ny, gx, gy, tree.Count));
                }
            }
            if (goal < 0)
                return false;
            // walk back
            List<int> back = new();
            int s = goal;
            while (s >= 0)
            {
                back.Add(s >> 2);
                s = _from[s];
            }
            back.Add(-1 - s);
            for (int i = back.Count - 1; i >= 0; i--)
                path.Add(back[i]);
            return true;
        }

        private static int Heuristic(int x, int y, int gx, int gy, int treeSize)
            => treeSize == 1 ? Math.Abs(x - gx) + Math.Abs(y - gy) : 0;

        /// <summary>
        /// May a wire of this signal step into a cell, moving in direction <paramref name="d"/>?
        /// </summary>
        /// <param name="net">
        /// The signal.
        /// </param>
        /// <param name="cell">
        /// The cell stepped into.
        /// </param>
        /// <param name="d">
        /// The step's direction.
        /// </param>
        /// <param name="previous">
        /// The direction of the step before.
        /// </param>
        /// <param name="tree">
        /// The signal's own wire cells (always enterable here; whether the join is allowed is
        /// decided when the cell is taken from the queue).
        /// </param>
        /// <returns>
        /// True when the step is allowed.
        /// </returns>
        private bool CanEnter(int net, int cell, int d, int previous, HashSet<int> tree)
        {
            if (tree.Contains(cell))
                return true;
            int owner = _owner[cell];
            if (owner == Hard || (owner > 0 && owner != net))
                return false;
            byte u = _used[cell];
            if (u == 0)
                return true;
            if ((u & Corner) != 0)
                return false;
            bool horizontal = d < 2;
            // crossing another wire at a right angle is allowed; running along it is not
            return horizontal ? (u & Horizontal) == 0 : (u & Vertical) == 0;
        }

        /// <summary>
        /// Is arriving at a tree cell in this direction a proper join? At a pin end the wire must
        /// come in from outside the chip; on a straight wire it must meet it at a right angle.
        /// </summary>
        /// <param name="cell">
        /// The tree cell reached.
        /// </param>
        /// <param name="dir">
        /// The direction of the last step.
        /// </param>
        /// <param name="treePins">
        /// The pin end cells in the tree, with their outward direction.
        /// </param>
        /// <returns>
        /// True for a proper join.
        /// </returns>
        private bool Arrives(int cell, int dir, Dictionary<int, int> treePins)
        {
            byte u = _used[cell];
            if (treePins.TryGetValue(cell, out int outward))
            {
                // a bare pin end (the first pin, no wire yet) is entered from outside its chip;
                // a pin end that already has its wire is not joined again
                return u == 0 && dir == (outward > 0 ? 1 : 0);
            }
            if ((u & Corner) != 0)
                return false;
            // a straight wire is joined at a right angle only
            return dir < 2 ? u == Vertical : u == Horizontal;
        }

        private void Lay(int net, List<int> path, List<int> marked)
        {
            for (int i = 0; i < path.Count; i++)
            {
                int c = path[i];
                byte flag;
                if (i == 0 || i == path.Count - 1)
                    flag = Corner | Horizontal | Vertical;
                else
                {
                    bool inH = Math.Abs(path[i] - path[i - 1]) == 1, outH = Math.Abs(path[i + 1] - path[i]) == 1;
                    flag = inH != outH ? (byte)(Corner | Horizontal | Vertical) : inH ? Horizontal : Vertical;
                }
                _used[c] |= flag;
                _usedBy[c] = net;
                marked.Add(c);
            }
        }

        private void Unmark(List<int> marked)
        {
            for (int i = 0; i < marked.Count; i++)
            {
                _used[marked[i]] = 0;
                _usedBy[marked[i]] = 0;
            }
        }

        private void AddSegments(List<int> path, List<double[]> segments, int branch)
        {
            int start = 0;
            for (int i = 1; i < path.Count; i++)
            {
                bool last = i == path.Count - 1;
                bool turn = !last && (Math.Abs(path[i] - path[i - 1]) == 1) != (Math.Abs(path[i + 1] - path[i]) == 1);
                if (turn || last)
                {
                    segments.Add(new[]
                    {
                        (path[start] % _width) * Step, (path[start] / _width) * Step,
                        (path[i] % _width) * Step, (path[i] / _width) * Step, branch,
                    });
                    start = i;
                }
            }
        }
    }
}
