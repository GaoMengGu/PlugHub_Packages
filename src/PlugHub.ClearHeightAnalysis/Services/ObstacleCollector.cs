using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using PlugHub.ClearHeightAnalysis.Models;

namespace PlugHub.ClearHeightAnalysis.Services
{
    public sealed class ObstacleCollector
    {
        private static readonly BuiltInCategory[] StructuralCategories =
        {
            BuiltInCategory.OST_Floors,
            BuiltInCategory.OST_StructuralFraming,
            BuiltInCategory.OST_StructuralColumns,
            BuiltInCategory.OST_Ceilings
        };

        private static readonly BuiltInCategory[] MepCategories =
        {
            BuiltInCategory.OST_DuctCurves,
            BuiltInCategory.OST_DuctFitting,
            BuiltInCategory.OST_DuctAccessory,
            BuiltInCategory.OST_CableTray,
            BuiltInCategory.OST_CableTrayFitting,
            BuiltInCategory.OST_PipeCurves,
            BuiltInCategory.OST_PipeFitting,
            BuiltInCategory.OST_PipeAccessory
        };

        public IReadOnlyList<ObstacleProjection> Collect(Document document, AnalysisSettings settings)
        {
            var obstacles = new List<ObstacleProjection>();
            if (settings.IncludeCurrentModel)
            {
                CollectFromDocument(document, document.Title, false, Transform.Identity, settings, obstacles);
            }

            if (settings.IncludeLinkedModels)
            {
                foreach (RevitLinkInstance linkInstance in new FilteredElementCollector(document).OfClass(typeof(RevitLinkInstance)))
                {
                    Document linkDocument = linkInstance.GetLinkDocument();
                    if (linkDocument == null)
                    {
                        continue;
                    }

                    CollectFromDocument(linkDocument, linkDocument.Title, true, linkInstance.GetTotalTransform(), settings, obstacles);
                }
            }

            return obstacles;
        }

        private static void CollectFromDocument(Document document, string sourceModelName, bool isFromLink, Transform transform, AnalysisSettings settings, List<ObstacleProjection> obstacles)
        {
            foreach (BuiltInCategory category in StructuralCategories)
            {
                if (category == BuiltInCategory.OST_Ceilings && !settings.IncludeCeilings)
                {
                    continue;
                }

                CollectCategory(document, sourceModelName, isFromLink, transform, category, settings, obstacles);
            }

            if (!settings.IncludeMep)
            {
                return;
            }

            foreach (BuiltInCategory category in MepCategories)
            {
                CollectCategory(document, sourceModelName, isFromLink, transform, category, settings, obstacles);
            }
        }

        private static void CollectCategory(Document document, string sourceModelName, bool isFromLink, Transform transform, BuiltInCategory category, AnalysisSettings settings, List<ObstacleProjection> obstacles)
        {
            var collector = new FilteredElementCollector(document)
                .OfCategory(category)
                .WhereElementIsNotElementType();

            foreach (Element element in collector)
            {
                ObstacleProjection projection = TryCreateProjection(element, sourceModelName, isFromLink, transform, settings);
                if (projection != null)
                {
                    obstacles.Add(projection);
                }
            }
        }

        private static ObstacleProjection TryCreateProjection(Element element, string sourceModelName, bool isFromLink, Transform transform, AnalysisSettings settings)
        {
            BoundingBoxXYZ boundingBox = element.get_BoundingBox(null);
            if (boundingBox == null)
            {
                return null;
            }

            XYZ min = transform.OfPoint(boundingBox.Min);
            XYZ max = transform.OfPoint(boundingBox.Max);
            double bottomMillimeters = UnitConversion.FeetToMillimeters(Math.Min(min.Z, max.Z));
            double topMillimeters = UnitConversion.FeetToMillimeters(Math.Max(min.Z, max.Z));
            if (!ObstacleVerticalFilter.ShouldIncludeOverheadObstacle(bottomMillimeters, topMillimeters, settings))
            {
                return null;
            }

            string categoryName = element.Category?.Name ?? string.Empty;
            string elementKey = (isFromLink ? "link:" : "host:") + element.Id.IntegerValue;
            return new ObstacleProjection(
                elementKey,
                string.IsNullOrWhiteSpace(element.Name) ? elementKey : element.Name,
                categoryName,
                sourceModelName,
                isFromLink,
                ToRect(min, max),
                bottomMillimeters);
        }

        private static Rect2d ToRect(XYZ first, XYZ second)
        {
            return new Rect2d(
                UnitConversion.FeetToMillimeters(Math.Min(first.X, second.X)),
                UnitConversion.FeetToMillimeters(Math.Min(first.Y, second.Y)),
                UnitConversion.FeetToMillimeters(Math.Max(first.X, second.X)),
                UnitConversion.FeetToMillimeters(Math.Max(first.Y, second.Y)));
        }
    }
}
