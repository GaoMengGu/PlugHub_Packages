using System;
using System.Collections.Generic;
using System.Linq;
using PlugHub.ClearHeightAnalysis.Core.Geometry;
using PlugHub.ClearHeightAnalysis.Core.Models;

namespace PlugHub.ClearHeightAnalysis.Core.Services
{
    public static class RiskRegionBuilder
    {
        public static IReadOnlyList<RiskRegion> Build(IEnumerable<CellAnalysisResult> results, bool includePassed = false)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            List<CellAnalysisResult> candidates = results
                .Where(result => includePassed || result.Status != CellStatus.Passed)
                .OrderBy(result => result.Cell.Row).ThenBy(result => result.Cell.Column).ThenBy(result => result.Status)
                .ToList();
            var lookup = candidates.ToDictionary(result => Key(result.Cell.Column, result.Cell.Row, result.Status));
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var components = new List<List<CellAnalysisResult>>();
            foreach (CellAnalysisResult seed in candidates)
            {
                string seedKey = Key(seed.Cell.Column, seed.Cell.Row, seed.Status);
                if (!visited.Add(seedKey)) continue;
                var component = new List<CellAnalysisResult>();
                var queue = new Queue<CellAnalysisResult>();
                queue.Enqueue(seed);
                while (queue.Count > 0)
                {
                    CellAnalysisResult current = queue.Dequeue();
                    component.Add(current);
                    foreach (var delta in Neighbours)
                    {
                        string neighbourKey = Key(current.Cell.Column + delta.Item1, current.Cell.Row + delta.Item2, current.Status);
                        if (lookup.TryGetValue(neighbourKey, out CellAnalysisResult neighbour) && visited.Add(neighbourKey))
                            queue.Enqueue(neighbour);
                    }
                }
                components.Add(component);
            }

            components = components.OrderBy(component => component.Min(x => x.Cell.Row))
                .ThenBy(component => component.Min(x => x.Cell.Column)).ThenBy(component => component[0].Status).ToList();
            var regions = new List<RiskRegion>();
            for (int index = 0; index < components.Count; index++)
            {
                List<PolygonLoop2d> loops = BuildBoundaryLoops(components[index]);
                PolygonLoop2d outer = loops.Where(loop => PolygonMath.SignedArea(loop) > 0)
                    .OrderByDescending(loop => Math.Abs(PolygonMath.SignedArea(loop))).FirstOrDefault()
                    ?? Reverse(loops.OrderByDescending(loop => Math.Abs(PolygonMath.SignedArea(loop))).First());
                List<PolygonLoop2d> holes = loops.Where(loop => PolygonMath.SignedArea(loop) < 0)
                    .Where(loop => PolygonMath.ContainsPoint(outer, loop.Points[0])).ToList();
                Point2d marker = FindMarker(outer, holes, components[index]);
                regions.Add(new RiskRegion("R-" + (index + 1).ToString("000"), components[index][0].Status,
                    outer, holes, marker, components[index]));
            }
            return regions.AsReadOnly();
        }

        private static readonly Tuple<int, int>[] Neighbours =
        {
            Tuple.Create(-1, 0), Tuple.Create(1, 0), Tuple.Create(0, -1), Tuple.Create(0, 1)
        };

        private static string Key(int column, int row, CellStatus status) => column + ":" + row + ":" + (int)status;

        private static List<PolygonLoop2d> BuildBoundaryLoops(IEnumerable<CellAnalysisResult> component)
        {
            var edges = new Dictionary<UndirectedEdge, DirectedEdge>();
            foreach (GridCellData cell in component.Select(result => result.Cell))
            {
                var p0 = new Point2d(cell.MinX, cell.MinY);
                var p1 = new Point2d(cell.MaxX, cell.MinY);
                var p2 = new Point2d(cell.MaxX, cell.MaxY);
                var p3 = new Point2d(cell.MinX, cell.MaxY);
                Toggle(edges, new DirectedEdge(p0, p1));
                Toggle(edges, new DirectedEdge(p1, p2));
                Toggle(edges, new DirectedEdge(p2, p3));
                Toggle(edges, new DirectedEdge(p3, p0));
            }

            var remaining = new HashSet<DirectedEdge>(edges.Values);
            var byStart = edges.Values.GroupBy(edge => edge.Start).ToDictionary(group => group.Key, group => group.ToList());
            var loops = new List<PolygonLoop2d>();
            while (remaining.Count > 0)
            {
                DirectedEdge first = remaining.OrderBy(edge => edge.Start.Y).ThenBy(edge => edge.Start.X).First();
                var points = new List<Point2d> { first.Start };
                DirectedEdge current = first;
                while (true)
                {
                    remaining.Remove(current);
                    points.Add(current.End);
                    if (current.End == first.Start) break;
                    if (!byStart.TryGetValue(current.End, out List<DirectedEdge> nextEdges))
                        throw new InvalidOperationException("风险区域边界不闭合。");
                    List<DirectedEdge> available = nextEdges.Where(remaining.Contains).ToList();
                    if (available.Count == 0) throw new InvalidOperationException("风险区域边界不连续。");
                    current = available.Count == 1 ? available[0] : ChooseBoundaryContinuation(current, available);
                }
                points.RemoveAt(points.Count - 1);
                loops.Add(new PolygonLoop2d(SimplifyCollinear(points)));
            }
            return loops;
        }

        private static void Toggle(IDictionary<UndirectedEdge, DirectedEdge> edges, DirectedEdge edge)
        {
            var key = new UndirectedEdge(edge.Start, edge.End);
            if (edges.ContainsKey(key)) edges.Remove(key); else edges.Add(key, edge);
        }

        private static DirectedEdge ChooseBoundaryContinuation(DirectedEdge incoming, IEnumerable<DirectedEdge> candidates)
        {
            double inX = incoming.End.X - incoming.Start.X;
            double inY = incoming.End.Y - incoming.Start.Y;
            return candidates.OrderByDescending(edge =>
            {
                double outX = edge.End.X - edge.Start.X;
                double outY = edge.End.Y - edge.Start.Y;
                return inX * outY - inY * outX;
            }).ThenByDescending(edge => inX * (edge.End.X - edge.Start.X) + inY * (edge.End.Y - edge.Start.Y)).First();
        }

        private static IReadOnlyList<Point2d> SimplifyCollinear(IReadOnlyList<Point2d> points)
        {
            var result = new List<Point2d>();
            for (int index = 0; index < points.Count; index++)
            {
                Point2d previous = points[(index - 1 + points.Count) % points.Count];
                Point2d current = points[index];
                Point2d next = points[(index + 1) % points.Count];
                double cross = (current.X - previous.X) * (next.Y - current.Y) -
                               (current.Y - previous.Y) * (next.X - current.X);
                if (Math.Abs(cross) > 0.001) result.Add(current);
            }
            return result;
        }

        private static Point2d FindMarker(PolygonLoop2d outer, IReadOnlyList<PolygonLoop2d> holes, IEnumerable<CellAnalysisResult> members)
        {
            foreach (GridCellData cell in members.Select(result => result.Cell))
            {
                var point = new Point2d(cell.CenterX, cell.CenterY);
                if (PolygonMath.ContainsPoint(outer, point) && !holes.Any(hole => PolygonMath.ContainsPoint(hole, point)))
                    return point;
            }
            throw new InvalidOperationException("无法为风险区域确定内部标记点。");
        }

        private static PolygonLoop2d Reverse(PolygonLoop2d loop) => new PolygonLoop2d(loop.Points.Reverse());

        private readonly struct DirectedEdge : IEquatable<DirectedEdge>
        {
            public DirectedEdge(Point2d start, Point2d end) { Start = start; End = end; }
            public Point2d Start { get; }
            public Point2d End { get; }
            public bool Equals(DirectedEdge other) => Start == other.Start && End == other.End;
            public override bool Equals(object obj) => obj is DirectedEdge other && Equals(other);
            public override int GetHashCode() => (Start.GetHashCode() * 397) ^ End.GetHashCode();
        }

        private readonly struct UndirectedEdge : IEquatable<UndirectedEdge>
        {
            private readonly Point2d _a;
            private readonly Point2d _b;
            public UndirectedEdge(Point2d first, Point2d second)
            {
                bool firstBefore = first.X < second.X || (first.X.Equals(second.X) && first.Y <= second.Y);
                _a = firstBefore ? first : second;
                _b = firstBefore ? second : first;
            }
            public bool Equals(UndirectedEdge other) => _a == other._a && _b == other._b;
            public override bool Equals(object obj) => obj is UndirectedEdge other && Equals(other);
            public override int GetHashCode() => (_a.GetHashCode() * 397) ^ _b.GetHashCode();
        }
    }
}
