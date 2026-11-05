#nullable enable
using System;
using PlugHub.ClearHeightAnalysis.Core.Geometry;

namespace PlugHub.ClearHeightAnalysis.Core.Models
{
    public sealed class ObstacleSnapshot
    {
        public ObstacleSnapshot(
            string key,
            string displayName,
            string categoryName,
            string sourceModelName,
            string? linkInstanceKey,
            ObstacleKind kind,
            PolygonLoop2d footprint,
            ElevationPlane bottomPlane,
            double bottomElevationMillimeters,
            double topElevationMillimeters,
            GeometryConfidence confidence)
            : this(key, displayName, categoryName, sourceModelName, linkInstanceKey,
                kind == ObstacleKind.Blocked
                    ? (ObstacleGeometry)new BlockedRegionObstacle(new EffectiveCellRegion(footprint, Array.Empty<PolygonLoop2d>()), bottomElevationMillimeters, topElevationMillimeters, confidence)
                    : new PlanarRegionObstacle(new[] { new PlanarObstacleRegion(new EffectiveCellRegion(footprint, Array.Empty<PolygonLoop2d>()), bottomPlane) }, topElevationMillimeters, confidence),
                confidence)
        {
        }

        public ObstacleSnapshot(string key,string displayName,string categoryName,string sourceModelName,string? linkInstanceKey,ObstacleGeometry geometry,GeometryConfidence? confidence=null)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("障碍物唯一标识不能为空。", nameof(key));
            }

            if (string.IsNullOrWhiteSpace(displayName))
            {
                throw new ArgumentException("障碍物名称不能为空。", nameof(displayName));
            }

            if (string.IsNullOrWhiteSpace(categoryName))
            {
                throw new ArgumentException("障碍物类别不能为空。", nameof(categoryName));
            }

            if (string.IsNullOrWhiteSpace(sourceModelName))
            {
                throw new ArgumentException("来源模型不能为空。", nameof(sourceModelName));
            }

            Geometry = geometry ?? throw new ArgumentNullException(nameof(geometry));

            Key = key;
            DisplayName = displayName;
            CategoryName = categoryName;
            SourceModelName = sourceModelName;
            LinkInstanceKey = linkInstanceKey;
            Confidence = confidence ?? geometry.RequiredConfidence;
        }

        public string Key { get; }
        public string DisplayName { get; }
        public string CategoryName { get; }
        public string SourceModelName { get; }
        public string? LinkInstanceKey { get; }
        public ObstacleGeometry Geometry { get; }
        public ObstacleKind Kind => Geometry.Kind;
        public PolygonLoop2d Footprint => Geometry.Footprint;
        public ElevationPlane BottomPlane => Geometry is PlanarRegionObstacle planar ? planar.Regions[0].BottomPlane : ElevationPlane.Constant(Geometry.MinimumBottomElevationMillimeters);
        public double BottomElevationMillimeters => Geometry.MinimumBottomElevationMillimeters;
        public double TopElevationMillimeters => Geometry.TopElevationMillimeters;
        public GeometryConfidence Confidence { get; }
        public double MinX => Geometry.MinX; public double MinY => Geometry.MinY; public double MaxX => Geometry.MaxX; public double MaxY => Geometry.MaxY;
    }
}
