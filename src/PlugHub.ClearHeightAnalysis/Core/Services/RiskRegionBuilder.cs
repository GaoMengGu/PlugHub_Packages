using System;
using System.Collections.Generic;
using System.Linq;
using PlugHub.ClearHeightAnalysis.Core.Geometry;
using PlugHub.ClearHeightAnalysis.Core.Models;
using System.Threading;

namespace PlugHub.ClearHeightAnalysis.Core.Services
{
    public static class RiskRegionBuilder
    {
        public static IReadOnlyList<RiskRegion> Build(IEnumerable<CellAnalysisResult> results, bool includePassed = false)
        {
            return Build(results,includePassed,CancellationToken.None,null);
        }

        public static IReadOnlyList<RiskRegion> Build(IEnumerable<CellAnalysisResult> results,bool includePassed,
            CancellationToken cancellationToken,IProgress<AnalysisProgress> progress)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            List<CellAnalysisResult> candidates = results
                .Where(result => includePassed || result.Status != CellStatus.Passed)
                .OrderBy(result => result.Cell.Row).ThenBy(result => result.Cell.Column).ThenBy(result => result.Status)
                .ToList();
            var lookup = candidates.ToDictionary(result => Key(result.Cell.Column, result.Cell.Row, result.Status));
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var components = new List<List<CellAnalysisResult>>();
            progress?.Report(new AnalysisProgress("合并风险区域",0,candidates.Count));
            int processed=0;
            foreach (CellAnalysisResult seed in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();
                string seedKey = Key(seed.Cell.Column, seed.Cell.Row, seed.Status);
                if (!visited.Add(seedKey)) continue;
                var component = new List<CellAnalysisResult>();
                var queue = new Queue<CellAnalysisResult>();
                queue.Enqueue(seed);
                while (queue.Count > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
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
                processed+=component.Count;progress?.Report(new AnalysisProgress("合并风险区域",Math.Min(processed,candidates.Count),candidates.Count));
            }

            components = components.OrderBy(component => component.Min(x => x.Cell.Row))
                .ThenBy(component => component.Min(x => x.Cell.Column)).ThenBy(component => component[0].Status).ToList();
            var regions = new List<RiskRegion>();
            int regionNumber = 1;
            foreach (List<CellAnalysisResult> component in components)
            {
                cancellationToken.ThrowIfCancellationRequested();
                List<PolygonLoop2d> loops = BuildBoundaryLoops(component,cancellationToken);
                List<PolygonLoop2d> outers = loops.Where(loop => PolygonMath.SignedArea(loop) > 0)
                    .OrderBy(loop => loop.MinY).ThenBy(loop => loop.MinX).ToList();
                if (outers.Count == 0 && loops.Count > 0)
                    outers.Add(Reverse(loops.OrderByDescending(loop => Math.Abs(PolygonMath.SignedArea(loop))).First()));
                foreach (PolygonLoop2d outer in outers)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    List<PolygonLoop2d> holes = loops.Where(loop => PolygonMath.SignedArea(loop) < 0)
                        .Where(loop => PolygonMath.ContainsPoint(outer, FindInteriorPoint(loop), false)).ToList();
                    List<CellAnalysisResult> members = component.Where(result =>
                        ResultTouchesRegion(result, outer, holes)).ToList();
                    if (members.Count == 0) throw new InvalidOperationException("风险区域没有关联分析网格。");
                    Point2d marker = FindMarker(outer, holes, members);
                    regions.Add(new RiskRegion("R-" + regionNumber++.ToString("000"), component[0].Status,
                        outer, holes, marker, members));
                }
            }
            progress?.Report(new AnalysisProgress("合并风险区域",candidates.Count,candidates.Count));
            return regions.AsReadOnly();
        }

        private static readonly Tuple<int, int>[] Neighbours =
        {
            Tuple.Create(-1, 0), Tuple.Create(1, 0), Tuple.Create(0, -1), Tuple.Create(0, 1)
        };

        private static string Key(int column, int row, CellStatus status) => column + ":" + row + ":" + (int)status;

        private static List<PolygonLoop2d> BuildBoundaryLoops(IEnumerable<CellAnalysisResult> component,CancellationToken cancellationToken)
        {
            var sourceEdges = new List<DirectedEdge>();
            foreach (EffectiveCellRegion region in component.SelectMany(result => result.Cell.EffectiveRegions))
            {
                cancellationToken.ThrowIfCancellationRequested();
                AddLoopEdges(sourceEdges, EnsureOrientation(region.Outer, counterClockwise: true));
                foreach (PolygonLoop2d hole in region.Holes)
                    AddLoopEdges(sourceEdges, EnsureOrientation(hole, counterClockwise: false));
            }

            var edges = new Dictionary<UndirectedEdge, DirectedEdge>();
            foreach (DirectedEdge edge in SplitAtCollinearEndpoints(sourceEdges,cancellationToken))
            {
                Toggle(edges, edge);
            }

            var remaining = new HashSet<DirectedEdge>(edges.Values);
            var byStart = edges.Values.GroupBy(edge => edge.Start).ToDictionary(group => group.Key, group => group.ToList());
            var loops = new List<PolygonLoop2d>();
            while (remaining.Count > 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                DirectedEdge first = remaining.OrderBy(edge => edge.Start.Y).ThenBy(edge => edge.Start.X).First();
                var points = new List<Point2d> { first.Start };
                DirectedEdge current = first;
                while (true)
                {
                    cancellationToken.ThrowIfCancellationRequested();
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

        private static void AddLoopEdges(ICollection<DirectedEdge> edges, PolygonLoop2d loop)
        {
            for (int index = 0; index < loop.Points.Count; index++)
                edges.Add(new DirectedEdge(Snap(loop.Points[index]), Snap(loop.Points[(index + 1) % loop.Points.Count])));
        }

        private static IEnumerable<DirectedEdge> SplitAtCollinearEndpoints(IReadOnlyList<DirectedEdge> edges,CancellationToken cancellationToken)
        {
            foreach(IGrouping<LineKey,DirectedEdge> group in edges.GroupBy(edge=>new LineKey(edge)))
            {
                List<DirectedEdge> collinear=group.ToList();
                foreach (DirectedEdge edge in collinear)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var values = new List<double> { 0, 1 };
                    foreach (DirectedEdge other in collinear)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        AddParameterIfOnSegment(other.Start, edge, values);
                        AddParameterIfOnSegment(other.End, edge, values);
                    }

                    values = values.Distinct().OrderBy(value => value).ToList();
                    for (int index = 0; index + 1 < values.Count; index++)
                    {
                        if (values[index + 1] - values[index] <= 0.000000001) continue;
                        yield return new DirectedEdge(
                            Interpolate(edge, values[index]), Interpolate(edge, values[index + 1]));
                    }
                }
            }
        }

        private static void AddParameterIfOnSegment(Point2d point, DirectedEdge edge, ICollection<double> values)
        {
            double dx = edge.End.X - edge.Start.X;
            double dy = edge.End.Y - edge.Start.Y;
            double cross = dx * (point.Y - edge.Start.Y) - dy * (point.X - edge.Start.X);
            if (Math.Abs(cross) > 0.001) return;
            double lengthSquared = dx * dx + dy * dy;
            double parameter = ((point.X - edge.Start.X) * dx + (point.Y - edge.Start.Y) * dy) / lengthSquared;
            if (parameter > 0.000000001 && parameter < 0.999999999) values.Add(parameter);
        }

        private static Point2d Interpolate(DirectedEdge edge, double parameter) => new Point2d(
            edge.Start.X + (edge.End.X - edge.Start.X) * parameter,
            edge.Start.Y + (edge.End.Y - edge.Start.Y) * parameter);

        private static Point2d Snap(Point2d point) => new Point2d(
            Math.Round(point.X, 3, MidpointRounding.AwayFromZero),
            Math.Round(point.Y, 3, MidpointRounding.AwayFromZero));

        private static PolygonLoop2d EnsureOrientation(PolygonLoop2d loop, bool counterClockwise)
        {
            bool isCounterClockwise = PolygonMath.SignedArea(loop) > 0;
            return isCounterClockwise == counterClockwise ? loop : Reverse(loop);
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
                Point2d point = cell.RepresentativePoint;
                if (PolygonMath.ContainsPoint(outer, point, false) && !holes.Any(hole => PolygonMath.ContainsPoint(hole, point)))
                    return point;
                foreach (EffectiveCellRegion region in cell.EffectiveRegions)
                {
                    point = EffectiveRegionBuilder.FindRepresentativePoint(region);
                    if (PolygonMath.ContainsPoint(outer, point, false) && !holes.Any(hole => PolygonMath.ContainsPoint(hole, point)))
                        return point;
                }
            }
            throw new InvalidOperationException("无法为风险区域确定内部标记点。");
        }

        private static bool ResultTouchesRegion(CellAnalysisResult result, PolygonLoop2d outer, IReadOnlyList<PolygonLoop2d> holes)
        {
            foreach (EffectiveCellRegion effective in result.Cell.EffectiveRegions)
            {
                Point2d point = EffectiveRegionBuilder.FindRepresentativePoint(effective);
                if (PolygonMath.ContainsPoint(outer, point) && !holes.Any(hole => PolygonMath.ContainsPoint(hole, point)))
                    return true;
                if (effective.Outer.Points.Any(vertex => PolygonMath.ContainsPoint(outer, vertex) &&
                    !holes.Any(hole => PolygonMath.ContainsPoint(hole, vertex, false)))) return true;
            }
            return false;
        }

        private static Point2d FindInteriorPoint(PolygonLoop2d loop)
        {
            var region = new EffectiveCellRegion(
                PolygonMath.SignedArea(loop) > 0 ? loop : Reverse(loop), Array.Empty<PolygonLoop2d>());
            return EffectiveRegionBuilder.FindRepresentativePoint(region);
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

        private readonly struct LineKey : IEquatable<LineKey>
        {
            private readonly long _directionX;private readonly long _directionY;private readonly long _offset;
            public LineKey(DirectedEdge edge)
            {
                double dx=edge.End.X-edge.Start.X,dy=edge.End.Y-edge.Start.Y,length=Math.Sqrt(dx*dx+dy*dy);
                double nx=dx/length,ny=dy/length;if(nx<-0.000000001||(Math.Abs(nx)<=0.000000001&&ny<0)){nx=-nx;ny=-ny;}
                _directionX=(long)Math.Round(nx*1_000_000);_directionY=(long)Math.Round(ny*1_000_000);
                _offset=(long)Math.Round((-ny*edge.Start.X+nx*edge.Start.Y)*1000);
            }
            public bool Equals(LineKey other)=>_directionX==other._directionX&&_directionY==other._directionY&&_offset==other._offset;
            public override bool Equals(object obj)=>obj is LineKey other&&Equals(other);
            public override int GetHashCode()=>((_directionX.GetHashCode()*397)^_directionY.GetHashCode())*397^_offset.GetHashCode();
        }
    }
}
