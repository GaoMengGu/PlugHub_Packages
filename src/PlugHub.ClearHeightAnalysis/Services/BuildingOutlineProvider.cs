using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using PlugHub.ClearHeightAnalysis.Models;

namespace PlugHub.ClearHeightAnalysis.Services
{
    public sealed class BuildingOutlineProvider
    {
        public IReadOnlyList<Rect2d> GetOutlineMasks(UIDocument uiDocument, Document document, AnalysisSettings settings)
        {
            var selectedFloorMask = TryPickFloorMask(uiDocument, document);
            if (selectedFloorMask.Count > 0)
            {
                return selectedFloorMask;
            }

            var floorMasks = CollectFloorMasks(document, settings).ToList();
            if (floorMasks.Count > 0)
            {
                return floorMasks;
            }

            if (settings.UseActiveViewCropAsFallback)
            {
                var cropMask = TryGetActiveViewCropMask(document.ActiveView);
                if (cropMask != null)
                {
                    return new[] { cropMask.Value };
                }
            }

            return new List<Rect2d>();
        }

        public Rect2d GetBoundary(IReadOnlyList<Rect2d> masks)
        {
            if (masks == null || masks.Count == 0)
            {
                throw new InvalidOperationException("未能识别建筑外轮廓，请选择楼板或使用当前视图裁剪框。");
            }

            return new Rect2d(
                masks.Min(mask => mask.MinX),
                masks.Min(mask => mask.MinY),
                masks.Max(mask => mask.MaxX),
                masks.Max(mask => mask.MaxY));
        }

        private static IReadOnlyList<Rect2d> TryPickFloorMask(UIDocument uiDocument, Document document)
        {
            try
            {
                Reference reference = uiDocument.Selection.PickObject(ObjectType.Element, new FloorSelectionFilter(), "选择楼板作为净高分析外轮廓，按 Esc 使用自动识别。 ");
                Element element = document.GetElement(reference);
                Rect2d? mask = TryGetElementBounds(element, Transform.Identity);
                if (mask.HasValue)
                {
                    return new List<Rect2d> { mask.Value };
                }

                return new List<Rect2d>();
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return new List<Rect2d>();
            }
        }

        private static IEnumerable<Rect2d> CollectFloorMasks(Document document, AnalysisSettings settings)
        {
            var collector = new FilteredElementCollector(document)
                .OfCategory(BuiltInCategory.OST_Floors)
                .WhereElementIsNotElementType();

            double minZ = settings.LevelElevationMillimeters - 500;
            double maxZ = settings.LevelElevationMillimeters + settings.SearchHeightMillimeters;

            foreach (Element floor in collector)
            {
                Rect2d? bounds = TryGetElementBounds(floor, Transform.Identity, minZ, maxZ);
                if (bounds.HasValue)
                {
                    yield return bounds.Value;
                }
            }
        }

        private static Rect2d? TryGetActiveViewCropMask(View view)
        {
            if (view?.CropBox == null || !view.CropBoxActive)
            {
                return null;
            }

            BoundingBoxXYZ cropBox = view.CropBox;
            XYZ min = cropBox.Transform.OfPoint(cropBox.Min);
            XYZ max = cropBox.Transform.OfPoint(cropBox.Max);
            return ToRect(min, max);
        }

        private static Rect2d? TryGetElementBounds(Element element, Transform transform, double? minZMillimeters = null, double? maxZMillimeters = null)
        {
            BoundingBoxXYZ boundingBox = element?.get_BoundingBox(null);
            if (boundingBox == null)
            {
                return null;
            }

            XYZ min = transform.OfPoint(boundingBox.Min);
            XYZ max = transform.OfPoint(boundingBox.Max);
            double bottom = UnitConversion.FeetToMillimeters(Math.Min(min.Z, max.Z));
            double top = UnitConversion.FeetToMillimeters(Math.Max(min.Z, max.Z));

            if (minZMillimeters.HasValue && top < minZMillimeters.Value)
            {
                return null;
            }

            if (maxZMillimeters.HasValue && bottom > maxZMillimeters.Value)
            {
                return null;
            }

            return ToRect(min, max);
        }

        private static Rect2d ToRect(XYZ first, XYZ second)
        {
            return new Rect2d(
                UnitConversion.FeetToMillimeters(Math.Min(first.X, second.X)),
                UnitConversion.FeetToMillimeters(Math.Min(first.Y, second.Y)),
                UnitConversion.FeetToMillimeters(Math.Max(first.X, second.X)),
                UnitConversion.FeetToMillimeters(Math.Max(first.Y, second.Y)));
        }

        private sealed class FloorSelectionFilter : ISelectionFilter
        {
            public bool AllowElement(Element element)
            {
                return element?.Category != null && element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Floors;
            }

            public bool AllowReference(Reference reference, XYZ position)
            {
                return false;
            }
        }
    }
}
