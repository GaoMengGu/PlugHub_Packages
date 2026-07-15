using System;
using System.Collections.Generic;
using System.Linq;

namespace PlugHub.ClearHeightAnalysis.Core.Geometry
{
    public sealed class PolygonLoop2d
    {
        private const double AreaTolerance = 0.001;

        public PolygonLoop2d(IEnumerable<Point2d> points)
        {
            if (points == null)
            {
                throw new ArgumentNullException(nameof(points));
            }

            List<Point2d> values = points.ToList();
            if (values.Count > 1 && values[0] == values[values.Count - 1])
            {
                values.RemoveAt(values.Count - 1);
            }

            if (values.Distinct().Count() < 3)
            {
                throw new ArgumentException("多边形至少需要三个不同的点。", nameof(points));
            }

            if (Math.Abs(CalculateSignedArea(values)) <= AreaTolerance)
            {
                throw new ArgumentException("多边形面积必须大于零。", nameof(points));
            }

            Points = values.AsReadOnly();
            MinX = values.Min(point => point.X);
            MinY = values.Min(point => point.Y);
            MaxX = values.Max(point => point.X);
            MaxY = values.Max(point => point.Y);
        }

        public IReadOnlyList<Point2d> Points { get; }
        public double MinX { get; }
        public double MinY { get; }
        public double MaxX { get; }
        public double MaxY { get; }

        private static double CalculateSignedArea(IReadOnlyList<Point2d> points)
        {
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
