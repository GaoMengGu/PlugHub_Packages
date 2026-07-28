#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using PlugHub.ClearHeightAnalysis.Core.Models;

namespace PlugHub.ClearHeightAnalysis.Revit
{
    public sealed class RevitObstacleSnapshotCollector
    {
        private static readonly BuiltInCategory[] Categories =
        {
            BuiltInCategory.OST_Floors, BuiltInCategory.OST_StructuralFraming,
            BuiltInCategory.OST_StructuralColumns, BuiltInCategory.OST_Ceilings,
            BuiltInCategory.OST_DuctCurves, BuiltInCategory.OST_DuctFitting,
            BuiltInCategory.OST_DuctAccessory, BuiltInCategory.OST_CableTray,
            BuiltInCategory.OST_CableTrayFitting, BuiltInCategory.OST_PipeCurves,
            BuiltInCategory.OST_PipeFitting, BuiltInCategory.OST_PipeAccessory
        };

        public IReadOnlyList<ObstacleSnapshot> Collect(
            RevitAnalysisContext context,
            AnalysisBoundary boundary,
            CoreAnalysisSettings settings)
        {
            var factory = new RevitObstacleSnapshotFactory();
            var results = new List<ObstacleSnapshot>();
            foreach (RevitSourceContext source in context.Sources)
            {
                Outline sourceOutline = CreateSourceOutline(source, boundary, settings);
                foreach (BuiltInCategory category in Categories)
                {
                    var collector = new FilteredElementCollector(source.Document)
                        .OfCategory(category)
                        .WhereElementIsNotElementType()
                        .WherePasses(new BoundingBoxIntersectsFilter(sourceOutline));
                    foreach (Element element in collector)
                    {
                        ObstacleSnapshot? snapshot = factory.TryCreate(element, source, settings, context.Request);
                        if (snapshot != null) results.Add(snapshot);
                    }
                }
            }
            return results;
        }

        private static Outline CreateSourceOutline(
            RevitSourceContext source,
            AnalysisBoundary boundary,
            CoreAnalysisSettings settings)
        {
            double minZ = settings.FinishedFloorElevationMillimeters - 500;
            double maxZ = settings.LevelElevationMillimeters + settings.SearchHeightMillimeters;
            var hostPoints = new List<XYZ>(8);
            foreach (double x in new[] { boundary.MinX, boundary.MaxX })
            foreach (double y in new[] { boundary.MinY, boundary.MaxY })
            foreach (double z in new[] { minZ, maxZ })
            {
                var host = new XYZ(
                    Services.UnitConversion.MillimetersToFeet(x),
                    Services.UnitConversion.MillimetersToFeet(y),
                    Services.UnitConversion.MillimetersToFeet(z));
                hostPoints.Add(source.Transform.Inverse.OfPoint(host));
            }
            return new Outline(
                new XYZ(hostPoints.Min(p => p.X), hostPoints.Min(p => p.Y), hostPoints.Min(p => p.Z)),
                new XYZ(hostPoints.Max(p => p.X), hostPoints.Max(p => p.Y), hostPoints.Max(p => p.Z)));
        }
    }
}
