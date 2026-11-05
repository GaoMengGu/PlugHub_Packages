using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using PlugHub.ClearHeightAnalysis.Core.Geometry;
using PlugHub.ClearHeightAnalysis.Core.Models;

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

        public static bool TryGetHorizontalBottomRegions(Element element,Transform sourceTransform,
            double topElevationMillimeters,out PlanarRegionObstacle obstacle)
        {
            var regions=new List<PlanarObstacleRegion>();
            GeometryElement geometry=element.get_Geometry(new Options{ComputeReferences=false,IncludeNonVisibleObjects=false,DetailLevel=ViewDetailLevel.Fine});
            if(geometry!=null)CollectBottomRegions(geometry,sourceTransform,regions);
            if(regions.Count==0){obstacle=null;return false;}
            obstacle=new PlanarRegionObstacle(regions,topElevationMillimeters,GeometryConfidence.CategoryApproximation);return true;
        }

        private static void CollectBottomRegions(IEnumerable<GeometryObject> geometry,Transform transform,ICollection<PlanarObstacleRegion> regions)
        {
            foreach(GeometryObject item in geometry)
            {
                if(item is GeometryInstance instance)
                {
                    CollectBottomRegions(instance.GetInstanceGeometry(),transform,regions);
                    continue;
                }
                if(!(item is Solid solid)||solid.Volume<=0)continue;
                foreach(Face rawFace in solid.Faces)
                {
                    if(!(rawFace is PlanarFace face))continue;XYZ normal=transform.OfVector(face.FaceNormal).Normalize();if(Math.Abs(normal.Z)<0.999||normal.Z>=0)continue;
                    List<PolygonLoop2d> loops=face.GetEdgesAsCurveLoops().Select(loop=>RevitCurveLoopConverter.Convert(loop,transform)).ToList();if(loops.Count==0)continue;
                    PolygonLoop2d outer=loops.OrderByDescending(loop=>Math.Abs(PolygonMath.SignedArea(loop))).First();List<PolygonLoop2d> holes=loops.Where(loop=>!ReferenceEquals(loop,outer)&&PolygonMath.ContainsPoint(outer,loop.Points[0])).ToList();
                    double bottom=Services.UnitConversion.FeetToMillimeters(transform.OfPoint(face.Origin).Z);regions.Add(new PlanarObstacleRegion(new EffectiveCellRegion(outer,holes),ElevationPlane.Constant(bottom)));
                }
            }
        }
    }
}
