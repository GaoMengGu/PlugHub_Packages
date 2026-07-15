#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using PlugHub.ClearHeightAnalysis.Core.Geometry;
using PlugHub.ClearHeightAnalysis.Core.Models;
using PlugHub.ClearHeightAnalysis.Core.Services;

namespace PlugHub.ClearHeightAnalysis.Revit
{
    public sealed class RevitBoundaryProvider
    {
        public AnalysisBoundary GetBoundary(UIDocument uiDocument, RevitAnalysisContext context)
        {
            switch (context.Request.BoundaryMode)
            {
                case AnalysisBoundaryMode.SelectFloor:
                    return ExtractSelectedFloor(uiDocument, context);
                case AnalysisBoundaryMode.AutomaticHostFloors:
                    return ExtractAutomaticHostFloors(context);
                case AnalysisBoundaryMode.ActiveViewCrop:
                    return ExtractCrop(context.HostDocument.ActiveView);
                case AnalysisBoundaryMode.ManualRectangle:
                    return ExtractManualRectangle(uiDocument);
                default:
                    throw new InvalidOperationException("不支持的分析范围模式。");
            }
        }

        private static AnalysisBoundary ExtractSelectedFloor(UIDocument uiDocument, RevitAnalysisContext context)
        {
            Reference reference = uiDocument.Selection.PickObject(ObjectType.PointOnElement, "选择当前模型或链接模型中的楼板");
            Element hostElement = context.HostDocument.GetElement(reference.ElementId);
            if (reference.LinkedElementId != ElementId.InvalidElementId)
            {
                var link = hostElement as RevitLinkInstance ?? throw new InvalidOperationException("选择对象不是有效链接实例。");
                Document linked = link.GetLinkDocument() ?? throw new InvalidOperationException("所选链接模型未加载。");
                var floor = linked.GetElement(reference.LinkedElementId) as Floor ?? throw new InvalidOperationException("请选择楼板。");
                return ExtractFloors(new[] { floor }, link.GetTotalTransform());
            }

            var hostFloor = hostElement as Floor ?? throw new InvalidOperationException("请选择楼板。");
            return ExtractFloors(new[] { hostFloor }, Transform.Identity);
        }

        private static AnalysisBoundary ExtractAutomaticHostFloors(RevitAnalysisContext context)
        {
            List<Floor> floors = new FilteredElementCollector(context.HostDocument)
                .OfCategory(BuiltInCategory.OST_Floors).WhereElementIsNotElementType()
                .Cast<Floor>()
                .Where(floor => floor.get_Parameter(BuiltInParameter.LEVEL_PARAM)?.AsElementId() == context.Level.Id)
                .ToList();
            if (floors.Count == 0)
                throw new InvalidOperationException("所选楼层没有可用的当前模型楼板，请改用选择楼板或矩形范围。");
            return ExtractFloors(floors, Transform.Identity);
        }

        private static AnalysisBoundary ExtractFloors(IEnumerable<Floor> floors, Transform transform)
        {
            var regions = new List<BoundaryRegion>();
            foreach (Floor floor in floors)
            {
                foreach (Reference topReference in HostObjectUtils.GetTopFaces(floor))
                {
                    if (!(floor.GetGeometryObjectFromReference(topReference) is PlanarFace face))
                        continue;
                    List<PolygonLoop2d> loops = face.GetEdgesAsCurveLoops()
                        .Select(loop => RevitCurveLoopConverter.Convert(loop, transform)).ToList();
                    if (loops.Count > 0)
                        regions.AddRange(BoundaryLoopClassifier.Classify(loops).Regions);
                }
            }
            if (regions.Count == 0)
                throw new InvalidOperationException("无法从楼板顶面提取有效边界。");
            return new AnalysisBoundary(regions);
        }

        private static AnalysisBoundary ExtractCrop(View view)
        {
            BoundingBoxXYZ crop = view.CropBox;
            if (crop == null || !view.CropBoxActive)
                throw new InvalidOperationException("当前视图没有启用裁剪框。");
            XYZ p1 = crop.Transform.OfPoint(new XYZ(crop.Min.X, crop.Min.Y, crop.Min.Z));
            XYZ p2 = crop.Transform.OfPoint(new XYZ(crop.Max.X, crop.Min.Y, crop.Min.Z));
            XYZ p3 = crop.Transform.OfPoint(new XYZ(crop.Max.X, crop.Max.Y, crop.Min.Z));
            XYZ p4 = crop.Transform.OfPoint(new XYZ(crop.Min.X, crop.Max.Y, crop.Min.Z));
            return FromHostPoints(new[] { p1, p2, p3, p4 });
        }

        private static AnalysisBoundary ExtractManualRectangle(UIDocument uiDocument)
        {
            XYZ first = uiDocument.Selection.PickPoint("选择矩形第一个角点");
            XYZ second = uiDocument.Selection.PickPoint("选择矩形对角点");
            double minX = Math.Min(first.X, second.X);
            double minY = Math.Min(first.Y, second.Y);
            double maxX = Math.Max(first.X, second.X);
            double maxY = Math.Max(first.Y, second.Y);
            return FromHostPoints(new[]
            {
                new XYZ(minX, minY, first.Z), new XYZ(maxX, minY, first.Z),
                new XYZ(maxX, maxY, first.Z), new XYZ(minX, maxY, first.Z)
            });
        }

        private static AnalysisBoundary FromHostPoints(IEnumerable<XYZ> points)
        {
            var loop = new PolygonLoop2d(points.Select(point => new Point2d(
                Services.UnitConversion.FeetToMillimeters(point.X),
                Services.UnitConversion.FeetToMillimeters(point.Y))));
            return new AnalysisBoundary(new[] { new BoundaryRegion(loop, Array.Empty<PolygonLoop2d>()) });
        }
    }
}
