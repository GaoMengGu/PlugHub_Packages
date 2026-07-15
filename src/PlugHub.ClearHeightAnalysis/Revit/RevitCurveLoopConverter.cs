using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using PlugHub.ClearHeightAnalysis.Core.Geometry;

namespace PlugHub.ClearHeightAnalysis.Revit
{
    public static class RevitCurveLoopConverter
    {
        public static PolygonLoop2d Convert(CurveLoop loop, Transform transform)
        {
            var points = new List<Point2d>();
            foreach (Curve curve in loop)
            {
                foreach (XYZ raw in curve.Tessellate())
                {
                    XYZ host = transform.OfPoint(raw);
                    var point = new Point2d(
                        Services.UnitConversion.FeetToMillimeters(host.X),
                        Services.UnitConversion.FeetToMillimeters(host.Y));
                    if (points.Count == 0 || DistanceSquared(points[points.Count - 1], point) > 0.000001)
                        points.Add(point);
                }
            }
            if (points.Count > 1 && DistanceSquared(points[0], points[points.Count - 1]) <= 0.000001)
                points.RemoveAt(points.Count - 1);
            return new PolygonLoop2d(points);
        }

        private static double DistanceSquared(Point2d first, Point2d second)
        {
            double dx = first.X - second.X;
            double dy = first.Y - second.Y;
            return dx * dx + dy * dy;
        }
    }
}
