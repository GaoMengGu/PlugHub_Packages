using System;
using System.Collections.Generic;

namespace PlugHub.ClearHeightAnalysis.Core.Geometry
{
    public static class PolygonMath
    {
        private const double CoordinateTolerance = 0.001;
        private const double AreaTolerance = 0.001;

        public static double SignedArea(PolygonLoop2d loop)
        {
            if (loop == null)
            {
                throw new ArgumentNullException(nameof(loop));
            }

            return SignedArea(loop.Points);
        }

        public static bool ContainsPoint(PolygonLoop2d loop, Point2d point)
        {
            if (loop == null)
            {
                throw new ArgumentNullException(nameof(loop));
            }

            bool inside = false;
            for (int index = 0; index < loop.Points.Count; index++)
            {
                Point2d start = loop.Points[index];
                Point2d end = loop.Points[(index + 1) % loop.Points.Count];
                if (IsPointOnSegment(point, start, end))
                {
                    return true;
                }

                bool crossesRay = (start.Y > point.Y) != (end.Y > point.Y);
                if (!crossesRay)
                {
                    continue;
                }

                double crossingX = start.X + ((point.Y - start.Y) * (end.X - start.X) / (end.Y - start.Y));
                if (crossingX > point.X)
                {
                    inside = !inside;
                }
            }

            return inside;
        }

        public static bool IntersectsRectangle(
            PolygonLoop2d loop,
            double minX,
            double minY,
            double maxX,
            double maxY)
        {
            return Math.Abs(SignedArea(ClipToRectangle(loop, minX, minY, maxX, maxY))) > AreaTolerance;
        }

        public static IReadOnlyList<Point2d> ClipToRectangle(
            PolygonLoop2d loop,
            double minX,
            double minY,
            double maxX,
            double maxY)
        {
            if (loop == null)
            {
                throw new ArgumentNullException(nameof(loop));
            }

            if (maxX <= minX || maxY <= minY)
            {
                throw new ArgumentException("矩形范围必须具有正面积。", nameof(maxX));
            }

            if (loop.MaxX <= minX || loop.MinX >= maxX || loop.MaxY <= minY || loop.MinY >= maxY)
            {
                return Array.Empty<Point2d>();
            }

            IReadOnlyList<Point2d> output = loop.Points;
            output = Clip(output, point => point.X >= minX, (start, end) => IntersectVertical(start, end, minX));
            output = Clip(output, point => point.X <= maxX, (start, end) => IntersectVertical(start, end, maxX));
            output = Clip(output, point => point.Y >= minY, (start, end) => IntersectHorizontal(start, end, minY));
            output = Clip(output, point => point.Y <= maxY, (start, end) => IntersectHorizontal(start, end, maxY));
            return output;
        }

        private static IReadOnlyList<Point2d> Clip(
            IReadOnlyList<Point2d> input,
            Func<Point2d, bool> isInside,
            Func<Point2d, Point2d, Point2d> intersection)
        {
            if (input.Count == 0)
            {
                return input;
            }

            var output = new List<Point2d>();
            Point2d start = input[input.Count - 1];
            bool startInside = isInside(start);
            foreach (Point2d end in input)
            {
                bool endInside = isInside(end);
                if (endInside)
                {
                    if (!startInside)
                    {
                        output.Add(intersection(start, end));
                    }

                    output.Add(end);
                }
                else if (startInside)
                {
                    output.Add(intersection(start, end));
                }

                start = end;
                startInside = endInside;
            }

            return output;
        }

        private static Point2d IntersectVertical(Point2d start, Point2d end, double x)
        {
            double ratio = (x - start.X) / (end.X - start.X);
            return new Point2d(x, start.Y + ratio * (end.Y - start.Y));
        }

        private static Point2d IntersectHorizontal(Point2d start, Point2d end, double y)
        {
            double ratio = (y - start.Y) / (end.Y - start.Y);
            return new Point2d(start.X + ratio * (end.X - start.X), y);
        }

        private static bool IsPointOnSegment(Point2d point, Point2d start, Point2d end)
        {
            double cross = (point.Y - start.Y) * (end.X - start.X) -
                           (point.X - start.X) * (end.Y - start.Y);
            if (Math.Abs(cross) > CoordinateTolerance)
            {
                return false;
            }

            return point.X >= Math.Min(start.X, end.X) - CoordinateTolerance &&
                   point.X <= Math.Max(start.X, end.X) + CoordinateTolerance &&
                   point.Y >= Math.Min(start.Y, end.Y) - CoordinateTolerance &&
                   point.Y <= Math.Max(start.Y, end.Y) + CoordinateTolerance;
        }

        private static double SignedArea(IReadOnlyList<Point2d> points)
        {
            if (points.Count < 3)
            {
                return 0;
            }

            double twiceArea = 0;
            for (int index = 0; index < points.Count; index++)
            {
                Point2d current = points[index];
                Point2d next = points[(index + 1) % points.Count];
                twiceArea += current.X * next.Y - next.X * current.Y;
            }

            return twiceArea / 2.0;
        }
    }
}
