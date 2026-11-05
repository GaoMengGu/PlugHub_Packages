using System;
using PlugHub.ClearHeightAnalysis.Core.Geometry;

namespace PlugHub.ClearHeightAnalysis.Core.Models
{
    public sealed class BoundingBoxObstacle : ObstacleGeometry
    {
        public BoundingBoxObstacle(double minX,double minY,double maxX,double maxY,double bottom,double top,ObstacleKind kind=ObstacleKind.Overhead)
        { if(maxX<=minX||maxY<=minY||top<bottom)throw new ArgumentException("包围盒范围无效。");MinX=minX;MinY=minY;MaxX=maxX;MaxY=maxY;MinimumBottomElevationMillimeters=bottom;TopElevationMillimeters=top;Kind=kind;Footprint=new PolygonLoop2d(new[]{new Point2d(minX,minY),new Point2d(maxX,minY),new Point2d(maxX,maxY),new Point2d(minX,maxY)});}
        public override ObstacleGeometryType GeometryType=>ObstacleGeometryType.BoundingBox;public override ObstacleKind Kind{get;}public override GeometryConfidence RequiredConfidence=>GeometryConfidence.BoundingBoxFallback;public override double MinX{get;}public override double MinY{get;}public override double MaxX{get;}public override double MaxY{get;}public override double MinimumBottomElevationMillimeters{get;}public override double TopElevationMillimeters{get;}public override PolygonLoop2d Footprint{get;}
    }
}
