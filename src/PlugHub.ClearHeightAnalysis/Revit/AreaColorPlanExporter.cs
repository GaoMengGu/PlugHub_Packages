#nullable enable
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
    public sealed class AreaOutputPreflight
    {
        public AreaOutputPreflight(AreaOutputPlan plan, bool schemeAvailable, bool templateAvailable, string warning)
        { Plan=plan; SchemeAvailable=schemeAvailable; TemplateAvailable=templateAvailable; Warning=warning; }
        public AreaOutputPlan Plan { get; }
        public bool SchemeAvailable { get; }
        public bool TemplateAvailable { get; }
        public string Warning { get; }
        public bool ExceedsRecommendedSize => Plan.EstimatedAreaCount > 500 || Plan.EstimatedBoundaryLineCount > 5000;
    }

    public sealed class AreaColorPlanExporter
    {
        public const string RequiredAreaSchemeName = "PH_净高分析";
        public const string RequiredViewTemplateName = "PH_净高分析_色块";

        public AreaOutputPreflight Preflight(Document document, AnalysisBatch batch, AreaOutputMode mode)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (batch == null) throw new ArgumentNullException(nameof(batch));
            AreaOutputPlan plan = AreaOutputPlanner.Build(batch.RunData.Summary.Results, mode);
            bool scheme = FindScheme(document) != null;
            bool template = FindTemplate(document) != null;
            string warning = !scheme ? "缺少面积方案 PH_净高分析；未修改项目中的任何面积方案。"
                : !template ? "缺少视图模板 PH_净高分析_色块；可生成面积数据，但需要手动配置颜色填充。" : string.Empty;
            return new AreaOutputPreflight(plan, scheme, template, warning);
        }

        public DerivedOutputRecord Export(Document document, AnalysisBatch batch, AreaOutputMode mode)
        {
            AreaOutputPreflight preflight = Preflight(document, batch, mode);
            if (!preflight.SchemeAvailable) throw new InvalidOperationException(preflight.Warning);
            AnalysisLevelChoice? selectedLevel = batch.RunData.Request.SelectedLevel;
            if (selectedLevel == null) throw new InvalidOperationException("批次没有有效的楼层信息。");
            Level? level = document.GetElement(selectedLevel.UniqueId) as Level;
            if (level == null) throw new InvalidOperationException("批次对应的真实楼层已不存在。");
            AreaScheme scheme = FindScheme(document)!;
            View? template = FindTemplate(document);
            string outputId = Guid.NewGuid().ToString("N");
            var ids = new List<ElementId>();
            using (var group = new TransactionGroup(document, "生成净高分析面积色块图"))
            {
                group.Start();
                try
                {
                    using (var transaction = new Transaction(document, "创建净高分析面积平面"))
                    {
                        transaction.Start();
                        ViewPlan view = ViewPlan.CreateAreaPlan(document, scheme.Id, level.Id);
                        view.Name = UniqueViewName(document, "PH_净高分析_" + level.Name + "_" + batch.BatchId.Substring(0, 8));
                        if (template != null) view.ViewTemplateId = template.Id;
                        OutputTagService.Tag(view, batch.BatchId, outputId, DerivedOutputType.AreaColorPlan);
                        ids.Add(view.Id);
                        SketchPlane plane = SketchPlane.Create(document, Plane.CreateByNormalAndOrigin(XYZ.BasisZ, new XYZ(0,0,level.Elevation)));
                        OutputTagService.Tag(plane,batch.BatchId,outputId,DerivedOutputType.AreaColorPlan);
                        ids.Add(plane.Id);
                        var createdSegments = new HashSet<string>(StringComparer.Ordinal);
                        foreach (AreaOutputRegion areaData in preflight.Plan.Areas)
                        {
                            foreach (PolygonLoop2d loop in new[] { areaData.OuterLoop }.Concat(areaData.Holes))
                            for (int index=0; index<loop.Points.Count; index++)
                            {
                                Point2d a=loop.Points[index], b=loop.Points[(index+1)%loop.Points.Count];
                                string key=SegmentKey(a,b);
                                if (!createdSegments.Add(key)) continue;
                                ModelCurve boundary=document.Create.NewAreaBoundaryLine(plane,
                                    Line.CreateBound(ToXyz(a,level.Elevation),ToXyz(b,level.Elevation)),view);
                                OutputTagService.Tag(boundary,batch.BatchId,outputId,DerivedOutputType.AreaColorPlan,areaData.Number);
                                ids.Add(boundary.Id);
                            }
                        }
                        document.Regenerate();
                        foreach (AreaOutputRegion areaData in preflight.Plan.Areas)
                        {
                            Area area=document.Create.NewArea(view,new UV(
                                Services.UnitConversion.MillimetersToFeet(areaData.PlacementPoint.X),
                                Services.UnitConversion.MillimetersToFeet(areaData.PlacementPoint.Y)));
                            area.Number=areaData.Number;
                            area.Name=LightweightDrawingExporter.StatusName(areaData.Status);
                            OutputTagService.Tag(area,batch.BatchId,outputId,DerivedOutputType.AreaColorPlan,areaData.Number);
                            ids.Add(area.Id);
                        }
                        transaction.Commit();
                    }
                    group.Assimilate();
                }
                catch { group.RollBack(); throw; }
            }
            int viewId=ids[0].IntegerValue;
            return new DerivedOutputRecord(outputId,DerivedOutputType.AreaColorPlan,DateTime.UtcNow,viewId,null,ids.Select(id=>id.IntegerValue));
        }

        private static AreaScheme? FindScheme(Document document) => new FilteredElementCollector(document).OfClass(typeof(AreaScheme)).Cast<AreaScheme>().FirstOrDefault(item=>item.Name==RequiredAreaSchemeName);
        private static View? FindTemplate(Document document) => new FilteredElementCollector(document).OfClass(typeof(View)).Cast<View>().FirstOrDefault(item=>item.IsTemplate && item.Name==RequiredViewTemplateName);
        private static XYZ ToXyz(Point2d point,double z)=>new XYZ(Services.UnitConversion.MillimetersToFeet(point.X),Services.UnitConversion.MillimetersToFeet(point.Y),z);
        private static string SegmentKey(Point2d a,Point2d b)
        {
            string first=a.X.ToString("R",CultureInfo.InvariantCulture)+","+a.Y.ToString("R",CultureInfo.InvariantCulture);
            string second=b.X.ToString("R",CultureInfo.InvariantCulture)+","+b.Y.ToString("R",CultureInfo.InvariantCulture);
            return string.CompareOrdinal(first,second)<=0?first+"|"+second:second+"|"+first;
        }
        private static string UniqueViewName(Document document,string baseName)
        {
            var names=new HashSet<string>(new FilteredElementCollector(document).OfClass(typeof(View)).Cast<View>().Select(view=>view.Name),StringComparer.OrdinalIgnoreCase);
            if(!names.Contains(baseName))return baseName; for(int i=2;;i++){string candidate=baseName+"_"+i;if(!names.Contains(candidate))return candidate;}
        }
    }
}
