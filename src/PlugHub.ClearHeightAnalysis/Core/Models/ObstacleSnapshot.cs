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

            if (footprint == null)
            {
                throw new ArgumentNullException(nameof(footprint));
            }

            if (topElevationMillimeters < bottomElevationMillimeters)
            {
                throw new ArgumentException("障碍物顶部标高不能低于底部标高。", nameof(topElevationMillimeters));
            }

            Key = key;
            DisplayName = displayName;
            CategoryName = categoryName;
            SourceModelName = sourceModelName;
            LinkInstanceKey = linkInstanceKey;
            Kind = kind;
            Footprint = footprint;
            BottomPlane = bottomPlane;
            BottomElevationMillimeters = bottomElevationMillimeters;
            TopElevationMillimeters = topElevationMillimeters;
            Confidence = confidence;
        }

        public string Key { get; }
        public string DisplayName { get; }
        public string CategoryName { get; }
        public string SourceModelName { get; }
        public string? LinkInstanceKey { get; }
        public ObstacleKind Kind { get; }
        public PolygonLoop2d Footprint { get; }
        public ElevationPlane BottomPlane { get; }
        public double BottomElevationMillimeters { get; }
        public double TopElevationMillimeters { get; }
        public GeometryConfidence Confidence { get; }
        public double MinX => Footprint.MinX;
        public double MinY => Footprint.MinY;
        public double MaxX => Footprint.MaxX;
        public double MaxY => Footprint.MaxY;
    }
}
