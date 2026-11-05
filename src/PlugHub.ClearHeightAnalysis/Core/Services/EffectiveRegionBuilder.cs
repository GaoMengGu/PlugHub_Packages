using System;
using System.Collections.Generic;
using System.Linq;
using PlugHub.ClearHeightAnalysis.Core.Geometry;
using PlugHub.ClearHeightAnalysis.Core.Models;

namespace PlugHub.ClearHeightAnalysis.Core.Services
{
    public static class EffectiveRegionBuilder
    {
        private const double Tolerance = 0.001;

        public static IReadOnlyList<EffectiveCellRegion> Build(
            BoundaryRegion source, double minX, double minY, double maxX, double maxY)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (maxX <= minX || maxY <= minY) throw new ArgumentException("网格范围必须具有正面积。");

            var segments = new Dictionary<SegmentKey, Segment>();
            AddClippedLoopSegments(source.Outer, minX, minY, maxX, maxY, segments);
            foreach (PolygonLoop2d hole in source.Holes)
            {
                AddClippedLoopSegments(hole, minX, minY, maxX, maxY, segments);
            }

            AddRectangleBoundarySegments(source, minX, minY, maxX, maxY, segments);
            List<PolygonLoop2d> loops = TraceLoops(segments.Values);
            if (loops.Count == 0)
            {
                return Array.Empty<EffectiveCellRegion>();
            }

            var nodes = loops.Select(loop => new LoopNode(loop, FindInteriorPoint(loop))).ToList();
            foreach (LoopNode node in nodes)
            {
                node.Depth = nodes.Count(other => !ReferenceEquals(node, other) &&
                    Math.Abs(PolygonMath.SignedArea(other.Loop)) > Math.Abs(PolygonMath.SignedArea(node.Loop)) &&
                    PolygonMath.ContainsPoint(other.Loop, node.InteriorPoint, false));
            }

            var regions = new List<EffectiveCellRegion>();
            foreach (LoopNode outer in nodes.Where(node => node.Depth % 2 == 0))
            {
                List<PolygonLoop2d> holes = nodes
                    .Where(node => node.Depth == outer.Depth + 1 &&
                        PolygonMath.ContainsPoint(outer.Loop, node.InteriorPoint, false))
                    .Select(node => node.Loop)
                    .ToList();
                regions.Add(new EffectiveCellRegion(outer.Loop, holes));
            }

            return regions.AsReadOnly();
        }

        public static Point2d FindRepresentativePoint(EffectiveCellRegion region)
        {
            if (region == null) throw new ArgumentNullException(nameof(region));
            var yValues = region.Outer.Points.Select(point => point.Y)
                .Concat(region.Holes.SelectMany(hole => hole.Points.Select(point => point.Y)))
                .Distinct().OrderBy(value => value).ToList();
            var candidates = new List<double> { (region.Outer.MinY + region.Outer.MaxY) / 2.0 };
            for (int index = 0; index + 1 < yValues.Count; index++)
            {
                if (yValues[index + 1] - yValues[index] > Tolerance)
                    candidates.Add((yValues[index] + yValues[index + 1]) / 2.0);
            }

            foreach (double y in candidates.Distinct())
            {
                List<double> crossings = HorizontalCrossings(region.Outer, y);
                for (int index = 0; index + 1 < crossings.Count; index += 2)
                {
                    double x = (crossings[index] + crossings[index + 1]) / 2.0;
                    var point = new Point2d(x, y);
                    if (region.Contains(point, false)) return point;
                }
            }

            throw new InvalidOperationException("无法为有效网格区域确定内部代表点。");
        }

        public static IReadOnlyList<Point2d> Intersect(PolygonLoop2d footprint, EffectiveCellRegion region)
        {
            if (footprint == null) throw new ArgumentNullException(nameof(footprint));
            if (region == null) throw new ArgumentNullException(nameof(region));
            var points = new List<Point2d>();
            points.AddRange(footprint.Points.Where(point => region.Contains(point)));
            points.AddRange(region.Outer.Points.Where(point =>
                PolygonMath.ContainsPoint(footprint, point) && region.Contains(point)));
            foreach (PolygonLoop2d hole in region.Holes)
            {
                points.AddRange(hole.Points.Where(point =>
                    PolygonMath.ContainsPoint(footprint, point) && region.Contains(point)));
            }

            AddIntersections(footprint, region.Outer, points);
            foreach (PolygonLoop2d hole in region.Holes) AddIntersections(footprint, hole, points);

            List<Point2d> unique = points.Distinct().ToList();
            if (!HasPositiveOverlap(footprint, region, unique)) return Array.Empty<Point2d>();
            return unique.AsReadOnly();
        }

        private static bool HasPositiveOverlap(PolygonLoop2d footprint, EffectiveCellRegion region, IReadOnlyList<Point2d> points)
        {
            if (footprint.Points.Any(point => region.Contains(point, false))) return true;
            if (PolygonMath.ContainsPoint(footprint, FindRepresentativePoint(region), false)) return true;
            if (PolygonMath.EdgesProperlyIntersect(footprint, region.Outer)) return true;
            if (region.Holes.Any(hole => PolygonMath.EdgesProperlyIntersect(footprint, hole))) return true;
            if (points.Count >= 3)
            {
                Point2d average = new Point2d(points.Average(point => point.X), points.Average(point => point.Y));
                return region.Contains(average, false) && PolygonMath.ContainsPoint(footprint, average, false);
            }
            return false;
        }

        private static void AddIntersections(PolygonLoop2d first, PolygonLoop2d second, ICollection<Point2d> output)
        {
            for (int firstIndex = 0; firstIndex < first.Points.Count; firstIndex++)
            {
                Point2d a = first.Points[firstIndex];
                Point2d b = first.Points[(firstIndex + 1) % first.Points.Count];
                for (int secondIndex = 0; secondIndex < second.Points.Count; secondIndex++)
                {
                    Point2d c = second.Points[secondIndex];
                    Point2d d = second.Points[(secondIndex + 1) % second.Points.Count];
                    if (TryIntersect(a, b, c, d, out Point2d intersection)) output.Add(intersection);
                }
            }
        }

        private static void AddClippedLoopSegments(PolygonLoop2d loop, double minX, double minY, double maxX, double maxY, IDictionary<SegmentKey, Segment> output)
        {
            for (int index = 0; index < loop.Points.Count; index++)
            {
                Point2d start = loop.Points[index];
                Point2d end = loop.Points[(index + 1) % loop.Points.Count];
                if (TryClipSegment(start, end, minX, minY, maxX, maxY, out Point2d clippedStart, out Point2d clippedEnd))
                {
                    if (!IsOnRectangleBoundary(clippedStart, clippedEnd, minX, minY, maxX, maxY))
                        AddSegment(clippedStart, clippedEnd, output);
                }
            }
        }

        private static void AddRectangleBoundarySegments(BoundaryRegion source, double minX, double minY, double maxX, double maxY, IDictionary<SegmentKey, Segment> output)
        {
            Point2d[] corners = { new Point2d(minX, minY), new Point2d(maxX, minY), new Point2d(maxX, maxY), new Point2d(minX, maxY) };
            var loops = new[] { source.Outer }.Concat(source.Holes).ToList();
            for (int side = 0; side < 4; side++)
            {
                Point2d start = corners[side];
                Point2d end = corners[(side + 1) % 4];
                var values = new List<double> { 0, 1 };
                foreach (PolygonLoop2d loop in loops)
                {
                    for (int index = 0; index < loop.Points.Count; index++)
                    {
                        if (TryIntersect(start, end, loop.Points[index], loop.Points[(index + 1) % loop.Points.Count], out Point2d point))
                        {
                            double value = Math.Abs(end.X - start.X) >= Math.Abs(end.Y - start.Y)
                                ? (point.X - start.X) / (end.X - start.X)
                                : (point.Y - start.Y) / (end.Y - start.Y);
                            if (value >= -Tolerance && value <= 1 + Tolerance) values.Add(Math.Max(0, Math.Min(1, value)));
                        }
                    }
                }

                values = values.Distinct().OrderBy(value => value).ToList();
                for (int index = 0; index + 1 < values.Count; index++)
                {
                    double from = values[index];
                    double to = values[index + 1];
                    if (to - from <= Tolerance) continue;
                    Point2d midpoint = MoveInsideRectangle(
                        Interpolate(start, end, (from + to) / 2.0), side);
                    if (InsideSource(source, midpoint))
                        AddSegment(Interpolate(start, end, from), Interpolate(start, end, to), output);
                }
            }
        }

        private static bool InsideSource(BoundaryRegion source, Point2d point)
        {
            return PolygonMath.ContainsPoint(source.Outer, point) &&
                   !source.Holes.Any(hole => PolygonMath.ContainsPoint(hole, point, false));
        }

        private static bool IsOnRectangleBoundary(
            Point2d start, Point2d end, double minX, double minY, double maxX, double maxY)
        {
            return (Math.Abs(start.X - minX) <= Tolerance && Math.Abs(end.X - minX) <= Tolerance) ||
                   (Math.Abs(start.X - maxX) <= Tolerance && Math.Abs(end.X - maxX) <= Tolerance) ||
                   (Math.Abs(start.Y - minY) <= Tolerance && Math.Abs(end.Y - minY) <= Tolerance) ||
                   (Math.Abs(start.Y - maxY) <= Tolerance && Math.Abs(end.Y - maxY) <= Tolerance);
        }

        private static Point2d MoveInsideRectangle(Point2d point, int side)
        {
            double offset = Tolerance * 10;
            if (side == 0) return new Point2d(point.X, point.Y + offset);
            if (side == 1) return new Point2d(point.X - offset, point.Y);
            if (side == 2) return new Point2d(point.X, point.Y - offset);
            return new Point2d(point.X + offset, point.Y);
        }

        private static List<PolygonLoop2d> TraceLoops(IEnumerable<Segment> segments)
        {
            List<Segment> edges = segments.ToList();
            var adjacency = new Dictionary<PointKey, List<int>>();
            for (int index = 0; index < edges.Count; index++)
            {
                AddAdjacency(adjacency, new PointKey(edges[index].Start), index);
                AddAdjacency(adjacency, new PointKey(edges[index].End), index);
            }

            var visited = new bool[edges.Count];
            var loops = new List<PolygonLoop2d>();
            for (int seed = 0; seed < edges.Count; seed++)
            {
                if (visited[seed]) continue;
                var points = new List<Point2d>();
                int currentEdge = seed;
                Point2d current = edges[seed].Start;
                PointKey origin = new PointKey(current);
                while (true)
                {
                    if (visited[currentEdge]) throw new InvalidOperationException("有效区域边界拓扑无法闭合。");
                    visited[currentEdge] = true;
                    Segment edge = edges[currentEdge];
                    Point2d next = new PointKey(edge.Start).Equals(new PointKey(current)) ? edge.End : edge.Start;
                    points.Add(current);
                    PointKey nextKey = new PointKey(next);
                    if (nextKey.Equals(origin)) break;
                    List<int> candidates = adjacency[nextKey].Where(index => !visited[index]).ToList();
                    if (candidates.Count != 1) throw new InvalidOperationException("有效区域边界存在分叉或断点。");
                    current = next;
                    currentEdge = candidates[0];
                }

                points = RemoveCollinear(points);
                if (points.Count >= 3 && Math.Abs(SignedArea(points)) > Tolerance) loops.Add(new PolygonLoop2d(points));
            }
            return loops;
        }

        private static Point2d FindInteriorPoint(PolygonLoop2d loop)
        {
            foreach (double y in loop.Points.Select(point => point.Y).Distinct().OrderBy(value => value)
                .Zip(loop.Points.Select(point => point.Y).Distinct().OrderBy(value => value).Skip(1), (a, b) => (a + b) / 2.0))
            {
                List<double> crossings = HorizontalCrossings(loop, y);
                if (crossings.Count >= 2) return new Point2d((crossings[0] + crossings[1]) / 2.0, y);
            }
            throw new InvalidOperationException("无法判定有效区域环的内部点。");
        }

        private static List<double> HorizontalCrossings(PolygonLoop2d loop, double y)
        {
            var values = new List<double>();
            for (int index = 0; index < loop.Points.Count; index++)
            {
                Point2d start = loop.Points[index];
                Point2d end = loop.Points[(index + 1) % loop.Points.Count];
                if ((start.Y > y) == (end.Y > y)) continue;
                values.Add(start.X + (y - start.Y) * (end.X - start.X) / (end.Y - start.Y));
            }
            values.Sort();
            return values;
        }

        private static bool TryClipSegment(Point2d start, Point2d end, double minX, double minY, double maxX, double maxY, out Point2d clippedStart, out Point2d clippedEnd)
        {
            double from = 0, to = 1;
            double dx = end.X - start.X, dy = end.Y - start.Y;
            if (!ClipTest(-dx, start.X - minX, ref from, ref to) || !ClipTest(dx, maxX - start.X, ref from, ref to) ||
                !ClipTest(-dy, start.Y - minY, ref from, ref to) || !ClipTest(dy, maxY - start.Y, ref from, ref to) || to - from <= Tolerance)
            { clippedStart = default(Point2d); clippedEnd = default(Point2d); return false; }
            clippedStart = Interpolate(start, end, from); clippedEnd = Interpolate(start, end, to); return true;
        }

        private static bool ClipTest(double direction, double distance, ref double from, ref double to)
        {
            if (Math.Abs(direction) <= Tolerance) return distance >= -Tolerance;
            double ratio = distance / direction;
            if (direction < 0) { if (ratio > to) return false; if (ratio > from) from = ratio; }
            else { if (ratio < from) return false; if (ratio < to) to = ratio; }
            return true;
        }

        private static bool TryIntersect(Point2d a, Point2d b, Point2d c, Point2d d, out Point2d point)
        {
            double cross = Cross(b.X - a.X, b.Y - a.Y, d.X - c.X, d.Y - c.Y);
            if (Math.Abs(cross) <= Tolerance) { point = default(Point2d); return false; }
            double t = Cross(c.X - a.X, c.Y - a.Y, d.X - c.X, d.Y - c.Y) / cross;
            double u = Cross(c.X - a.X, c.Y - a.Y, b.X - a.X, b.Y - a.Y) / cross;
            if (t < -Tolerance || t > 1 + Tolerance || u < -Tolerance || u > 1 + Tolerance) { point = default(Point2d); return false; }
            point = Interpolate(a, b, Math.Max(0, Math.Min(1, t))); return true;
        }

        private static double Cross(double ax, double ay, double bx, double by) => ax * by - ay * bx;
        private static Point2d Interpolate(Point2d start, Point2d end, double t) => new Point2d(start.X + (end.X - start.X) * t, start.Y + (end.Y - start.Y) * t);
        private static double SignedArea(IReadOnlyList<Point2d> points) { double area = 0; for (int i = 0; i < points.Count; i++) { Point2d a = points[i], b = points[(i + 1) % points.Count]; area += a.X * b.Y - b.X * a.Y; } return area / 2.0; }

        private static List<Point2d> RemoveCollinear(IReadOnlyList<Point2d> points)
        {
            var result = new List<Point2d>();
            for (int index = 0; index < points.Count; index++)
            {
                Point2d previous = points[(index + points.Count - 1) % points.Count], current = points[index], next = points[(index + 1) % points.Count];
                if (Math.Abs(Cross(current.X - previous.X, current.Y - previous.Y, next.X - current.X, next.Y - current.Y)) > Tolerance) result.Add(current);
            }
            return result;
        }

        private static void AddSegment(Point2d start, Point2d end, IDictionary<SegmentKey, Segment> output)
        {
            if (DistanceSquared(start, end) <= Tolerance * Tolerance) return;
            var segment = new Segment(start, end);
            var key = new SegmentKey(segment);
            if (!output.ContainsKey(key)) output.Add(key, segment);
        }
        private static double DistanceSquared(Point2d a, Point2d b) { double dx = a.X - b.X, dy = a.Y - b.Y; return dx * dx + dy * dy; }
        private static void AddAdjacency(IDictionary<PointKey, List<int>> map, PointKey point, int edge) { if (!map.TryGetValue(point, out List<int> values)) { values = new List<int>(); map.Add(point, values); } values.Add(edge); }

        private sealed class LoopNode { public LoopNode(PolygonLoop2d loop, Point2d interiorPoint) { Loop = loop; InteriorPoint = interiorPoint; } public PolygonLoop2d Loop { get; } public Point2d InteriorPoint { get; } public int Depth { get; set; } }
        private struct Segment { public Segment(Point2d start, Point2d end) { Start = start; End = end; } public Point2d Start { get; } public Point2d End { get; } }
        private struct PointKey : IEquatable<PointKey> { private readonly long _x, _y; public PointKey(Point2d point) { _x = (long)Math.Round(point.X / Tolerance); _y = (long)Math.Round(point.Y / Tolerance); } public int CompareTo(PointKey other) { int x = _x.CompareTo(other._x); return x != 0 ? x : _y.CompareTo(other._y); } public bool Equals(PointKey other) => _x == other._x && _y == other._y; public override bool Equals(object obj) => obj is PointKey other && Equals(other); public override int GetHashCode() => (_x.GetHashCode() * 397) ^ _y.GetHashCode(); }
        private struct SegmentKey : IEquatable<SegmentKey> { private readonly PointKey _a, _b; public SegmentKey(Segment segment) { var a = new PointKey(segment.Start); var b = new PointKey(segment.End); if (a.CompareTo(b) <= 0) { _a = a; _b = b; } else { _a = b; _b = a; } } public bool Equals(SegmentKey other) => _a.Equals(other._a) && _b.Equals(other._b); public override bool Equals(object obj) => obj is SegmentKey other && Equals(other); public override int GetHashCode() => (_a.GetHashCode() * 397) ^ _b.GetHashCode(); }
    }
}
