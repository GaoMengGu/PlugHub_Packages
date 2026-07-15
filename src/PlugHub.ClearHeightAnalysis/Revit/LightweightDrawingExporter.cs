using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Autodesk.Revit.DB;
using PlugHub.ClearHeightAnalysis.Core.Geometry;
using PlugHub.ClearHeightAnalysis.Core.Models;
using PlugHub.ClearHeightAnalysis.Core.Services;

namespace PlugHub.ClearHeightAnalysis.Revit
{
    public sealed class LightweightDrawingExporter
    {
        public DerivedOutputRecord Export(Document document, View view, AnalysisBatch batch)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (view == null || view.IsTemplate || !view.CanBePrinted || !SupportsDetailCurves(view.ViewType))
                throw new InvalidOperationException("请选择可打印的二维视图生成轻量成果。");
            if (batch == null) throw new ArgumentNullException(nameof(batch));
            IReadOnlyList<RiskRegion> regions = RiskRegionBuilder.Build(batch.RunData.Summary.Results);
            if (regions.Count == 0) throw new InvalidOperationException("该批次没有需要输出的问题区域。");
            string outputId = Guid.NewGuid().ToString("N");
            var ids = new List<ElementId>();
            using (var group = new TransactionGroup(document, "生成净高分析轻量成果"))
            {
                group.Start();
                try
                {
                    using (var transaction = new Transaction(document, "创建净高风险轮廓和编号"))
                    {
                        transaction.Start();
                        var styles = new Dictionary<CellStatus, GraphicsStyle>();
                        TextNoteType textType = GetOrCreateTextType(document);
                        double z = GetViewElevation(view);
                        foreach (RiskRegion region in regions)
                        {
                            if (!styles.TryGetValue(region.Status, out GraphicsStyle style))
                            {
                                style = GetOrCreateLineStyle(document, region.Status);
                                styles.Add(region.Status, style);
                            }
                            foreach (PolygonLoop2d loop in new[] { region.OuterLoop }.Concat(region.Holes))
                            for (int index = 0; index < loop.Points.Count; index++)
                            {
                                DetailCurve curve = document.Create.NewDetailCurve(view, Line.CreateBound(
                                    ToXyz(loop.Points[index], z), ToXyz(loop.Points[(index + 1) % loop.Points.Count], z)));
                                curve.LineStyle = style;
                                OutputTagService.Tag(curve, batch.BatchId, outputId, DerivedOutputType.LightweightDrawing, region.Number);
                                ids.Add(curve.Id);
                            }
                            string height = region.MinimumClearHeightMillimeters.HasValue
                                ? region.MinimumClearHeightMillimeters.Value.ToString("0", CultureInfo.InvariantCulture) + "mm"
                                : region.Status == CellStatus.Blocked ? "不可通行" : "未判断";
                            TextNote note = TextNote.Create(document, view.Id, ToXyz(region.MarkerPoint, z),
                                region.Number + "  " + height, textType.Id);
                            OutputTagService.Tag(note, batch.BatchId, outputId, DerivedOutputType.LightweightDrawing, region.Number);
                            ids.Add(note.Id);
                        }
                        transaction.Commit();
                    }
                    group.Assimilate();
                }
                catch { group.RollBack(); throw; }
            }
            return new DerivedOutputRecord(outputId, DerivedOutputType.LightweightDrawing, DateTime.UtcNow,
                view.Id.IntegerValue, null, ids.Select(id => id.IntegerValue));
        }

        private static double GetViewElevation(View view) => view.GenLevel != null ? view.GenLevel.Elevation : view.Origin.Z;
        private static bool SupportsDetailCurves(ViewType type) =>
            type == ViewType.FloorPlan || type == ViewType.CeilingPlan || type == ViewType.EngineeringPlan ||
            type == ViewType.AreaPlan || type == ViewType.DraftingView || type == ViewType.Section || type == ViewType.Elevation;
        private static XYZ ToXyz(Point2d point, double z) => new XYZ(
            Services.UnitConversion.MillimetersToFeet(point.X), Services.UnitConversion.MillimetersToFeet(point.Y), z);

        private static GraphicsStyle GetOrCreateLineStyle(Document document, CellStatus status)
        {
            string name = "PH_净高_" + StatusName(status);
            Category lines = document.Settings.Categories.get_Item(BuiltInCategory.OST_Lines);
            Category category = lines.SubCategories.Cast<Category>().FirstOrDefault(item => item.Name == name);
            if (category == null) category = document.Settings.Categories.NewSubcategory(lines, name);
            category.LineColor = StatusColor(status);
            category.SetLineWeight(status == CellStatus.Severe || status == CellStatus.Blocked ? 5 : 3, GraphicsStyleType.Projection);
            return category.GetGraphicsStyle(GraphicsStyleType.Projection);
        }

        private static TextNoteType GetOrCreateTextType(Document document)
        {
            const string name = "PH_净高分析_标记";
            TextNoteType type = new FilteredElementCollector(document).OfClass(typeof(TextNoteType)).Cast<TextNoteType>()
                .FirstOrDefault(item => item.Name == name);
            if (type != null) return type;
            TextNoteType source = new FilteredElementCollector(document).OfClass(typeof(TextNoteType)).Cast<TextNoteType>().FirstOrDefault();
            if (source == null) throw new InvalidOperationException("项目中没有可用的文字类型。");
            type = (TextNoteType)source.Duplicate(name);
            Parameter size = type.get_Parameter(BuiltInParameter.TEXT_SIZE);
            if (size != null && !size.IsReadOnly) size.Set(Services.UnitConversion.MillimetersToFeet(2.5));
            return type;
        }

        internal static string StatusName(CellStatus status)
        {
            switch (status) { case CellStatus.Severe:return "严重不足"; case CellStatus.Insufficient:return "不足";
                case CellStatus.Warning:return "临界"; case CellStatus.Blocked:return "不可通行";
                case CellStatus.Passed:return "达标"; default:return "未判断"; }
        }
        private static Color StatusColor(CellStatus status)
        {
            Rgba32 value = RasterHeatmapRenderer.ColorFor(status); return new Color(value.Red, value.Green, value.Blue);
        }
    }
}
