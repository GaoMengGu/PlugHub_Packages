using System;
using System.Collections.Generic;
using System.Linq;
using PlugHub.ClearHeightAnalysis.Core.Geometry;
using PlugHub.ClearHeightAnalysis.Core.Services;

namespace PlugHub.ClearHeightAnalysis.Core.Models
{
    public enum ObstacleGeometryType { PlanarRegion, Linear, BlockedRegion, BoundingBox }

    public abstract class ObstacleGeometry
    {
        public abstract ObstacleGeometryType GeometryType { get; }
        public abstract ObstacleKind Kind { get; }
        public abstract GeometryConfidence RequiredConfidence { get; }
        public abstract double MinX { get; }
        public abstract double MinY { get; }
        public abstract double MaxX { get; }
        public abstract double MaxY { get; }
        public abstract double MinimumBottomElevationMillimeters { get; }
        public abstract double TopElevationMillimeters { get; }
        public abstract PolygonLoop2d Footprint { get; }

        public virtual bool TryGetMinimumBottom(
            IReadOnlyList<EffectiveCellRegion> cellRegions, out double minimumBottom)
        {
            var points = cellRegions.SelectMany(region =>
                EffectiveRegionBuilder.Intersect(Footprint, region)).Distinct().ToList();
            if (points.Count < 3) { minimumBottom = 0; return false; }
            minimumBottom = points.Min(BottomAt);
            return true;
        }

        public virtual double BottomAt(Point2d point) => MinimumBottomElevationMillimeters;
    }
}
