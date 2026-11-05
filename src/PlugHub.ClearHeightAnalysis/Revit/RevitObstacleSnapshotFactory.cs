#nullable enable
using System;
using System.Linq;
using Autodesk.Revit.DB;
using PlugHub.ClearHeightAnalysis.Core.Geometry;
using PlugHub.ClearHeightAnalysis.Core.Models;
using PlugHub.ClearHeightAnalysis.Core.Services;

namespace PlugHub.ClearHeightAnalysis.Revit
{
    public sealed class RevitObstacleSnapshotFactory
    {
        public ObstacleSnapshot? TryCreate(Element element, RevitSourceContext source, CoreAnalysisSettings settings, AnalysisRequest request)
        {
            ObstacleSemanticCategory? semantic = GetSemanticCategory(element);
            if (!semantic.HasValue ||
                (semantic == ObstacleSemanticCategory.Ceiling && !request.IncludeCeilings) ||
                (IsMep(semantic.Value) && !request.IncludeMep))
                return null;

            if (semantic == ObstacleSemanticCategory.Pipe)
            {
                Parameter? diameter = element.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
                if (diameter != null && diameter.HasValue &&
                    !ObstacleRuleFilter.ShouldIncludePipe(
                        Services.UnitConversion.FeetToMillimeters(diameter.AsDouble()),
                        request.MinimumPipeDiameterMillimeters))
                    return null;
            }

            Bounds3d bounds;
            try { bounds = RevitElementGeometry.GetHostBounds(element, source.Transform); }
            catch (InvalidOperationException) { return null; }

            ObstacleKind? kind = ObstacleRuleFilter.ClassifyKind(
                semantic.Value, bounds.MinZ, bounds.MaxZ, settings);
            if (!kind.HasValue) return null;

            ObstacleGeometry geometry;
            GeometryConfidence confidence;
            if (element is HostObject host &&
                TryGetHorizontalBottomRegions(host, source.Transform, bounds.MaxZ, out PlanarRegionObstacle planar))
            { geometry = planar; confidence = GeometryConfidence.Exact; }
            else if (kind == ObstacleKind.Overhead && IsLinear(semantic.Value) &&
                TryGetLinearObstacle(element, semantic.Value, source.Transform, bounds, out LinearObstacle linear))
            { geometry = linear; confidence = linear.RequiredConfidence; }
            else if(kind==ObstacleKind.Overhead&&RevitElementGeometry.TryGetHorizontalBottomRegions(
                element,source.Transform,bounds.MaxZ,out PlanarRegionObstacle solidPlanar))
            {geometry=solidPlanar;confidence=GeometryConfidence.CategoryApproximation;}
            else if (kind == ObstacleKind.Blocked)
            {
                geometry = new BlockedRegionObstacle(
                    new EffectiveCellRegion(RevitElementGeometry.RectangleFootprint(bounds), Array.Empty<PolygonLoop2d>()),
                    bounds.MinZ, bounds.MaxZ, GeometryConfidence.CategoryApproximation);
                confidence = GeometryConfidence.CategoryApproximation;
            }
            else
            {
                geometry = new BoundingBoxObstacle(bounds.MinX, bounds.MinY, bounds.MaxX, bounds.MaxY,
                    bounds.MinZ, bounds.MaxZ, kind.Value);
                confidence = GeometryConfidence.BoundingBoxFallback;
            }

            string key = source.IsHost
                ? "host:" + element.UniqueId
                : "link:" + source.LinkInstance!.UniqueId + ":" + element.UniqueId;
            return new ObstacleSnapshot(
                key,
                string.IsNullOrWhiteSpace(element.Name) ? key : element.Name,
                element.Category?.Name ?? semantic.Value.ToString(),
                source.Document.Title,
                source.LinkInstance?.UniqueId,
                geometry,
                confidence);
        }

        private static bool TryGetHorizontalBottomRegions(
            HostObject host,
            Transform transform,
            double topElevationMillimeters,
            out PlanarRegionObstacle obstacle)
        {
            var regions = new System.Collections.Generic.List<PlanarObstacleRegion>();
            foreach (Reference reference in HostObjectUtils.GetBottomFaces(host))
            {
                if (!(host.GetGeometryObjectFromReference(reference) is PlanarFace face)) continue;
                XYZ normal = transform.OfVector(face.FaceNormal).Normalize();
                if (Math.Abs(normal.Z) < 0.999) continue;
                var loops = face.GetEdgesAsCurveLoops()
                    .Select(loop => RevitCurveLoopConverter.Convert(loop, transform)).ToList();
                if (loops.Count == 0) continue;
                PolygonLoop2d outer = loops.OrderByDescending(loop => Math.Abs(PolygonMath.SignedArea(loop))).First();
                var holes = loops.Where(loop => !ReferenceEquals(loop, outer))
                    .Where(loop => PolygonMath.ContainsPoint(outer, loop.Points[0])).ToList();
                double bottom = Services.UnitConversion.FeetToMillimeters(transform.OfPoint(face.Origin).Z);
                regions.Add(new PlanarObstacleRegion(
                    new EffectiveCellRegion(outer, holes), ElevationPlane.Constant(bottom)));
            }
            if (regions.Count == 0) { obstacle = null!; return false; }
            obstacle = new PlanarRegionObstacle(regions, topElevationMillimeters, GeometryConfidence.Exact);
            return true;
        }

        private static bool TryGetLinearObstacle(Element element, ObstacleSemanticCategory semantic,
            Transform transform, Bounds3d bounds, out LinearObstacle obstacle)
        {
            if (!(element.Location is LocationCurve location)) { obstacle = null!; return false; }
            XYZ rawStart = location.Curve.GetEndPoint(0);
            XYZ rawEnd = location.Curve.GetEndPoint(1);
            XYZ start = transform.OfPoint(rawStart);
            XYZ end = transform.OfPoint(rawEnd);
            var start2d = new Point2d(Services.UnitConversion.FeetToMillimeters(start.X), Services.UnitConversion.FeetToMillimeters(start.Y));
            var end2d = new Point2d(Services.UnitConversion.FeetToMillimeters(end.X), Services.UnitConversion.FeetToMillimeters(end.Y));
            double width = ParameterMillimeters(element, WidthParameter(semantic));
            double height = ParameterMillimeters(element, HeightParameter(semantic));
            if (semantic == ObstacleSemanticCategory.Pipe) { height = width; }
            GeometryConfidence confidence = GeometryConfidence.Exact;
            if (width <= 0)
            {
                width = Math.Max(1, Math.Min(bounds.MaxX - bounds.MinX, bounds.MaxY - bounds.MinY));
                confidence = GeometryConfidence.CategoryApproximation;
            }
            if (height <= 0) height = Math.Max(1, bounds.MaxZ - bounds.MinZ);
            double startBottom = Services.UnitConversion.FeetToMillimeters(start.Z) - height / 2.0;
            double endBottom = Services.UnitConversion.FeetToMillimeters(end.Z) - height / 2.0;
            obstacle = new LinearObstacle(start2d, end2d, width / 2.0, startBottom, endBottom, bounds.MaxZ, confidence);
            return true;
        }

        private static BuiltInParameter WidthParameter(ObstacleSemanticCategory semantic)
        {
            if (semantic == ObstacleSemanticCategory.Duct) return BuiltInParameter.RBS_CURVE_WIDTH_PARAM;
            if (semantic == ObstacleSemanticCategory.CableTray) return BuiltInParameter.RBS_CABLETRAY_WIDTH_PARAM;
            if (semantic == ObstacleSemanticCategory.Pipe) return BuiltInParameter.RBS_PIPE_DIAMETER_PARAM;
            return BuiltInParameter.STRUCTURAL_SECTION_COMMON_WIDTH;
        }

        private static BuiltInParameter HeightParameter(ObstacleSemanticCategory semantic)
        {
            if (semantic == ObstacleSemanticCategory.Duct) return BuiltInParameter.RBS_CURVE_HEIGHT_PARAM;
            if (semantic == ObstacleSemanticCategory.CableTray) return BuiltInParameter.RBS_CABLETRAY_HEIGHT_PARAM;
            if (semantic == ObstacleSemanticCategory.Pipe) return BuiltInParameter.RBS_PIPE_DIAMETER_PARAM;
            return BuiltInParameter.STRUCTURAL_SECTION_COMMON_HEIGHT;
        }

        private static double ParameterMillimeters(Element element, BuiltInParameter id)
        {
            Parameter parameter = element.get_Parameter(id);
            if((parameter==null||!parameter.HasValue)&&element.Document.GetElement(element.GetTypeId()) is Element type)parameter=type.get_Parameter(id);
            if (parameter == null || !parameter.HasValue) return 0;
            return Services.UnitConversion.FeetToMillimeters(parameter.AsDouble());
        }

        private static bool IsLinear(ObstacleSemanticCategory category)
        {
            return category == ObstacleSemanticCategory.StructuralFraming ||
                   category == ObstacleSemanticCategory.Duct ||
                   category == ObstacleSemanticCategory.CableTray ||
                   category == ObstacleSemanticCategory.Pipe;
        }

        private static bool IsMep(ObstacleSemanticCategory category)
        {
            return category == ObstacleSemanticCategory.Duct ||
                   category == ObstacleSemanticCategory.CableTray ||
                   category == ObstacleSemanticCategory.Pipe ||
                   category == ObstacleSemanticCategory.FittingOrAccessory;
        }

        private static ObstacleSemanticCategory? GetSemanticCategory(Element element)
        {
            int id = element.Category?.Id.IntegerValue ?? 0;
            if (id == (int)BuiltInCategory.OST_Floors) return ObstacleSemanticCategory.Floor;
            if (id == (int)BuiltInCategory.OST_StructuralFraming) return ObstacleSemanticCategory.StructuralFraming;
            if (id == (int)BuiltInCategory.OST_StructuralColumns) return ObstacleSemanticCategory.StructuralColumn;
            if (id == (int)BuiltInCategory.OST_Ceilings) return ObstacleSemanticCategory.Ceiling;
            if (id == (int)BuiltInCategory.OST_DuctCurves) return ObstacleSemanticCategory.Duct;
            if (id == (int)BuiltInCategory.OST_CableTray) return ObstacleSemanticCategory.CableTray;
            if (id == (int)BuiltInCategory.OST_PipeCurves) return ObstacleSemanticCategory.Pipe;
            if (id == (int)BuiltInCategory.OST_DuctFitting || id == (int)BuiltInCategory.OST_DuctAccessory ||
                id == (int)BuiltInCategory.OST_CableTrayFitting || id == (int)BuiltInCategory.OST_PipeFitting ||
                id == (int)BuiltInCategory.OST_PipeAccessory)
                return ObstacleSemanticCategory.FittingOrAccessory;
            return null;
        }
    }
}
