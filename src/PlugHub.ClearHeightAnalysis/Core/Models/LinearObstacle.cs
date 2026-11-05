using System;
using PlugHub.ClearHeightAnalysis.Core.Geometry;

namespace PlugHub.ClearHeightAnalysis.Core.Models
{
    public sealed class LinearObstacle : ObstacleGeometry
    {
        private readonly double _lengthSquared;
        public LinearObstacle(Point2d start, Point2d end, double halfWidthMillimeters,
            double startBottomElevationMillimeters, double endBottomElevationMillimeters,
            double? topElevationMillimeters = null, GeometryConfidence confidence = GeometryConfidence.Exact)
        {
            if (halfWidthMillimeters <= 0) throw new ArgumentOutOfRangeException(nameof(halfWidthMillimeters));
            double dx=end.X-start.X, dy=end.Y-start.Y; _lengthSquared=dx*dx+dy*dy;
            if (_lengthSquared <= 0.001) throw new ArgumentException("线性障碍物端点不能重合。", nameof(end));
            Start=start; End=end; HalfWidthMillimeters=halfWidthMillimeters;
            StartBottomElevationMillimeters=startBottomElevationMillimeters; EndBottomElevationMillimeters=endBottomElevationMillimeters;
            TopElevationMillimeters=topElevationMillimeters ?? Math.Max(startBottomElevationMillimeters,endBottomElevationMillimeters);
            RequiredConfidence=confidence;
            double length=Math.Sqrt(_lengthSquared), nx=-dy/length*halfWidthMillimeters, ny=dx/length*halfWidthMillimeters;
            Footprint=new PolygonLoop2d(new[]{new Point2d(start.X+nx,start.Y+ny),new Point2d(end.X+nx,end.Y+ny),new Point2d(end.X-nx,end.Y-ny),new Point2d(start.X-nx,start.Y-ny)});
        }
        public Point2d Start { get; } public Point2d End { get; } public double HalfWidthMillimeters { get; }
        public double StartBottomElevationMillimeters { get; } public double EndBottomElevationMillimeters { get; }
        public override ObstacleGeometryType GeometryType=>ObstacleGeometryType.Linear; public override ObstacleKind Kind=>ObstacleKind.Overhead;
        public override GeometryConfidence RequiredConfidence { get; } public override PolygonLoop2d Footprint { get; }
        public override double MinX=>Footprint.MinX; public override double MinY=>Footprint.MinY; public override double MaxX=>Footprint.MaxX; public override double MaxY=>Footprint.MaxY;
        public override double MinimumBottomElevationMillimeters=>Math.Min(StartBottomElevationMillimeters,EndBottomElevationMillimeters);
        public override double TopElevationMillimeters { get; }
        public override double BottomAt(Point2d point)
        {
            double t=((point.X-Start.X)*(End.X-Start.X)+(point.Y-Start.Y)*(End.Y-Start.Y))/_lengthSquared;
            t=Math.Max(0,Math.Min(1,t)); return StartBottomElevationMillimeters+t*(EndBottomElevationMillimeters-StartBottomElevationMillimeters);
        }
    }
}
