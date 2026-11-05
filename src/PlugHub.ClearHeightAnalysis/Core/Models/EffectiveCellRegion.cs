using System;
using System.Collections.Generic;
using System.Linq;
using PlugHub.ClearHeightAnalysis.Core.Geometry;

namespace PlugHub.ClearHeightAnalysis.Core.Models
{
    public sealed class EffectiveCellRegion
    {
        public EffectiveCellRegion(PolygonLoop2d outer, IEnumerable<PolygonLoop2d> holes)
        {
            Outer = outer ?? throw new ArgumentNullException(nameof(outer));
            Holes = (holes ?? throw new ArgumentNullException(nameof(holes))).ToList().AsReadOnly();
            double area = Math.Abs(PolygonMath.SignedArea(Outer));
            area -= Holes.Sum(hole => Math.Abs(PolygonMath.SignedArea(hole)));
            if (area <= 0.001)
            {
                throw new ArgumentException("有效区域必须具有正面积。", nameof(outer));
            }

            AreaSquareMillimeters = area;
        }

        public PolygonLoop2d Outer { get; }
        public IReadOnlyList<PolygonLoop2d> Holes { get; }
        public double AreaSquareMillimeters { get; }

        public bool Contains(Point2d point, bool includeBoundary = true)
        {
            if (!PolygonMath.ContainsPoint(Outer, point, includeBoundary))
            {
                return false;
            }

            return !Holes.Any(hole => PolygonMath.ContainsPoint(hole, point, !includeBoundary));
        }
    }
}
