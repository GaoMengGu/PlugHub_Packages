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

            PolygonLoop2d footprint = RevitElementGeometry.RectangleFootprint(bounds);
            GeometryConfidence confidence = GeometryConfidence.BoundingBoxFallback;
            double bottom = bounds.MinZ;
            if (element is HostObject host && TryGetHorizontalBottomFace(host, source.Transform, out PolygonLoop2d exact, out double faceBottom))
            {
                footprint = exact;
                bottom = faceBottom;
                confidence = GeometryConfidence.Exact;
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
                kind.Value,
                footprint,
                ElevationPlane.Constant(bottom),
                bottom,
                bounds.MaxZ,
                confidence);
        }

        private static bool TryGetHorizontalBottomFace(
            HostObject host,
            Transform transform,
            out PolygonLoop2d footprint,
            out double bottomMillimeters)
        {
            foreach (Reference reference in HostObjectUtils.GetBottomFaces(host))
            {
                if (!(host.GetGeometryObjectFromReference(reference) is PlanarFace face)) continue;
                XYZ normal = transform.OfVector(face.FaceNormal).Normalize();
                if (Math.Abs(normal.Z) < 0.999) continue;
                var loops = face.GetEdgesAsCurveLoops()
                    .Select(loop => RevitCurveLoopConverter.Convert(loop, transform)).ToList();
                if (loops.Count == 0) continue;
                footprint = loops.OrderByDescending(loop => Math.Abs(PolygonMath.SignedArea(loop))).First();
                bottomMillimeters = Services.UnitConversion.FeetToMillimeters(transform.OfPoint(face.Origin).Z);
                return true;
            }
            footprint = null!;
            bottomMillimeters = 0;
            return false;
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
