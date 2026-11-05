using System;
using System.Collections.Generic;
using System.Linq;
using PlugHub.ClearHeightAnalysis.Core.Geometry;
using PlugHub.ClearHeightAnalysis.Core.Services;

namespace PlugHub.ClearHeightAnalysis.Core.Models
{
    public sealed class PlanarObstacleRegion
    {
        public PlanarObstacleRegion(EffectiveCellRegion region, ElevationPlane bottomPlane)
        { Region = region ?? throw new ArgumentNullException(nameof(region)); BottomPlane = bottomPlane; }
        public EffectiveCellRegion Region { get; }
        public ElevationPlane BottomPlane { get; }
    }

    public sealed class PlanarRegionObstacle : ObstacleGeometry
    {
        public PlanarRegionObstacle(IEnumerable<PlanarObstacleRegion> regions,
            double? topElevationMillimeters = null,
            GeometryConfidence confidence = GeometryConfidence.Exact)
        {
            Regions = (regions ?? throw new ArgumentNullException(nameof(regions))).ToList().AsReadOnly();
            if (Regions.Count == 0) throw new ArgumentException("平面障碍物至少需要一个区域。", nameof(regions));
            MinimumBottomElevationMillimeters = Regions.Min(region => region.Region.Outer.Points
                .Min(point => region.BottomPlane.At(point.X, point.Y)));
            TopElevationMillimeters = topElevationMillimeters ?? MinimumBottomElevationMillimeters;
            RequiredConfidence = confidence;
        }
        public IReadOnlyList<PlanarObstacleRegion> Regions { get; }
        public override ObstacleGeometryType GeometryType => ObstacleGeometryType.PlanarRegion;
        public override ObstacleKind Kind => ObstacleKind.Overhead;
        public override GeometryConfidence RequiredConfidence { get; }
        public override double MinX => Regions.Min(x => x.Region.Outer.MinX);
        public override double MinY => Regions.Min(x => x.Region.Outer.MinY);
        public override double MaxX => Regions.Max(x => x.Region.Outer.MaxX);
        public override double MaxY => Regions.Max(x => x.Region.Outer.MaxY);
        public override double MinimumBottomElevationMillimeters { get; }
        public override double TopElevationMillimeters { get; }
        public override PolygonLoop2d Footprint => Regions[0].Region.Outer;
        public override bool TryGetMinimumBottom(IReadOnlyList<EffectiveCellRegion> cells, out double minimumBottom)
        {
            minimumBottom = double.MaxValue; bool found = false;
            foreach (PlanarObstacleRegion planar in Regions)
            foreach (EffectiveCellRegion cell in cells)
            {
                var points = EffectiveRegionBuilder.Intersect(planar.Region.Outer, cell)
                    .Where(point => !planar.Region.Holes.Any(hole => PolygonMath.ContainsPoint(hole, point, false))).ToList();
                if (points.Count < 3) continue;
                found = true; minimumBottom = Math.Min(minimumBottom, points.Min(point => planar.BottomPlane.At(point.X, point.Y)));
            }
            if (!found) minimumBottom = 0;
            return found;
        }
    }
}
