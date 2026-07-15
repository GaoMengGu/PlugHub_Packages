using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using PlugHub.ClearHeightAnalysis.Core.Geometry;

namespace PlugHub.ClearHeightAnalysis.Revit
{
    public static class RevitElementGeometry
    {
        public static Bounds3d GetHostBounds(Element element, Transform sourceTransform)
        {
            BoundingBoxXYZ box = element.get_BoundingBox(null)
                ?? throw new InvalidOperationException("构件没有可用边界框：" + element.Id.IntegerValue);
            var points = new List<XYZ>(8);
            foreach (double x in new[] { box.Min.X, box.Max.X })
            foreach (double y in new[] { box.Min.Y, box.Max.Y })
            foreach (double z in new[] { box.Min.Z, box.Max.Z })
                points.Add(sourceTransform.OfPoint(box.Transform.OfPoint(new XYZ(x, y, z))));

            return new Bounds3d(
                Services.UnitConversion.FeetToMillimeters(points.Min(point => point.X)),
                Services.UnitConversion.FeetToMillimeters(points.Min(point => point.Y)),
                Services.UnitConversion.FeetToMillimeters(points.Min(point => point.Z)),
                Services.UnitConversion.FeetToMillimeters(points.Max(point => point.X)),
                Services.UnitConversion.FeetToMillimeters(points.Max(point => point.Y)),
                Services.UnitConversion.FeetToMillimeters(points.Max(point => point.Z)));
        }

        public static PolygonLoop2d RectangleFootprint(Bounds3d bounds)
        {
            return new PolygonLoop2d(new[]
            {
                new Point2d(bounds.MinX, bounds.MinY), new Point2d(bounds.MaxX, bounds.MinY),
                new Point2d(bounds.MaxX, bounds.MaxY), new Point2d(bounds.MinX, bounds.MaxY)
            });
        }
    }
}
