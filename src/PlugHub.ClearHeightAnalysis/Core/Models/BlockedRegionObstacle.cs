using System;
using PlugHub.ClearHeightAnalysis.Core.Geometry;
using System.Collections.Generic;
using System.Linq;
using PlugHub.ClearHeightAnalysis.Core.Services;

namespace PlugHub.ClearHeightAnalysis.Core.Models
{
    public sealed class BlockedRegionObstacle : ObstacleGeometry
    {
        public BlockedRegionObstacle(EffectiveCellRegion region,double bottom,double top,GeometryConfidence confidence=GeometryConfidence.Exact)
        { Region=region??throw new ArgumentNullException(nameof(region)); if(top<bottom)throw new ArgumentException("顶部标高不能低于底部标高。"); MinimumBottomElevationMillimeters=bottom;TopElevationMillimeters=top;RequiredConfidence=confidence; }
        public EffectiveCellRegion Region{get;} public override ObstacleGeometryType GeometryType=>ObstacleGeometryType.BlockedRegion; public override ObstacleKind Kind=>ObstacleKind.Blocked; public override GeometryConfidence RequiredConfidence{get;}
        public override double MinX=>Region.Outer.MinX;public override double MinY=>Region.Outer.MinY;public override double MaxX=>Region.Outer.MaxX;public override double MaxY=>Region.Outer.MaxY;public override double MinimumBottomElevationMillimeters{get;}public override double TopElevationMillimeters{get;}public override PolygonLoop2d Footprint=>Region.Outer;
        public override bool TryGetMinimumBottom(IReadOnlyList<EffectiveCellRegion> cells,out double minimumBottom)
        {
            foreach(EffectiveCellRegion cell in cells)
            {
                List<Point2d> overlap=EffectiveRegionBuilder.Intersect(Region.Outer,cell)
                    .Where(point=>!Region.Holes.Any(hole=>PolygonMath.ContainsPoint(hole,point,false))).ToList();
                if(overlap.Count>=3){minimumBottom=MinimumBottomElevationMillimeters;return true;}
            }
            minimumBottom=0;return false;
        }
    }
}
