using System;
using System.Collections.Generic;
using System.Linq;
using PlugHub.ClearHeightAnalysis.Core.Geometry;

namespace PlugHub.ClearHeightAnalysis.Core.Models
{
    public sealed class BoundaryRegion
    {
        public BoundaryRegion(PolygonLoop2d outer, IEnumerable<PolygonLoop2d> holes)
        {
            if (outer == null)
            {
                throw new ArgumentNullException(nameof(outer));
            }

            if (holes == null)
            {
                throw new ArgumentNullException(nameof(holes));
            }

            Outer = Normalize(outer, counterClockwise: true);
            Holes = holes.Select(hole =>
            {
                if (hole == null)
                {
                    throw new ArgumentException("洞口边界不能为 null。", nameof(holes));
                }

                return Normalize(hole, counterClockwise: false);
            }).ToList().AsReadOnly();
        }

        public PolygonLoop2d Outer { get; }
        public IReadOnlyList<PolygonLoop2d> Holes { get; }

        private static PolygonLoop2d Normalize(PolygonLoop2d loop, bool counterClockwise)
        {
            bool isCounterClockwise = PolygonMath.SignedArea(loop) > 0;
            return isCounterClockwise == counterClockwise
                ? loop
                : new PolygonLoop2d(loop.Points.Reverse());
        }
    }

    public sealed class AnalysisBoundary
    {
        public AnalysisBoundary(IEnumerable<BoundaryRegion> regions)
        {
            if (regions == null)
            {
                throw new ArgumentNullException(nameof(regions));
            }

            List<BoundaryRegion> values = regions.ToList();
            if (values.Count == 0 || values.Any(region => region == null))
            {
                throw new ArgumentException("分析边界至少需要一个有效区域。", nameof(regions));
            }

            Regions = values.AsReadOnly();
            MinX = values.Min(region => region.Outer.MinX);
            MinY = values.Min(region => region.Outer.MinY);
            MaxX = values.Max(region => region.Outer.MaxX);
            MaxY = values.Max(region => region.Outer.MaxY);
        }

        public IReadOnlyList<BoundaryRegion> Regions { get; }
        public double MinX { get; }
        public double MinY { get; }
        public double MaxX { get; }
        public double MaxY { get; }
    }
}
