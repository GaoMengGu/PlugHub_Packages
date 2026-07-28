using System;
using System.Collections.Generic;
using System.Linq;

namespace PlugHub.ClearHeightAnalysis.Core.Geometry
{
    public readonly struct Bounds3d
    {
        public Bounds3d(double minX, double minY, double minZ, double maxX, double maxY, double maxZ)
        {
            if (maxX < minX || maxY < minY || maxZ < minZ)
            {
                throw new ArgumentException("三维边界最大坐标不能小于最小坐标。");
            }

            MinX = minX; MinY = minY; MinZ = minZ;
            MaxX = maxX; MaxY = maxY; MaxZ = maxZ;
        }

        public double MinX { get; }
        public double MinY { get; }
        public double MinZ { get; }
        public double MaxX { get; }
        public double MaxY { get; }
        public double MaxZ { get; }

        public Bounds3d TransformAllCorners(Transform3d transform)
        {
            var points = new List<Point3d>(8);
            foreach (double x in new[] { MinX, MaxX })
            foreach (double y in new[] { MinY, MaxY })
            foreach (double z in new[] { MinZ, MaxZ })
            {
                points.Add(transform.OfPoint(new Point3d(x, y, z)));
            }

            return new Bounds3d(
                points.Min(point => point.X),
                points.Min(point => point.Y),
                points.Min(point => point.Z),
                points.Max(point => point.X),
                points.Max(point => point.Y),
                points.Max(point => point.Z));
        }
    }
}
