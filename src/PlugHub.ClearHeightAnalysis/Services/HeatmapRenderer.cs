using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using PlugHub.ClearHeightAnalysis.Models;

namespace PlugHub.ClearHeightAnalysis.Services
{
    public sealed class HeatmapRenderer
    {
        public IReadOnlyList<ElementId> Render(Document document, View view, IEnumerable<ClearHeightResult> results, AnalysisSettings settings, string batchId)
        {
            var createdIds = new List<ElementId>();
            FilledRegionType regionType = GetDefaultFilledRegionType(document);
            if (regionType == null)
            {
                throw new InvalidOperationException("当前项目未找到可用的填充区域类型。 ");
            }

            foreach (ClearHeightResult result in results)
            {
                if (settings.RenderOnlyProblemCells && result.RiskLevel == RiskLevel.Passed)
                {
                    continue;
                }

                CurveLoop loop = CreateCellLoop(result.Cell.Bounds, result.Cell.LevelElevationMillimeters);
                FilledRegion region = FilledRegion.Create(document, regionType.Id, view.Id, new List<CurveLoop> { loop });
                ApplyRiskOverride(document, view, region.Id, result.RiskLevel);
                ResultTagService.TagResult(region, batchId);
                createdIds.Add(region.Id);
            }

            return createdIds;
        }

        private static FilledRegionType GetDefaultFilledRegionType(Document document)
        {
            foreach (FilledRegionType type in new FilteredElementCollector(document).OfClass(typeof(FilledRegionType)))
            {
                return type;
            }

            return null;
        }

        private static CurveLoop CreateCellLoop(Rect2d bounds, double levelElevationMillimeters)
        {
            double z = UnitConversion.MillimetersToFeet(levelElevationMillimeters + 10);
            XYZ p1 = new XYZ(UnitConversion.MillimetersToFeet(bounds.MinX), UnitConversion.MillimetersToFeet(bounds.MinY), z);
            XYZ p2 = new XYZ(UnitConversion.MillimetersToFeet(bounds.MaxX), UnitConversion.MillimetersToFeet(bounds.MinY), z);
            XYZ p3 = new XYZ(UnitConversion.MillimetersToFeet(bounds.MaxX), UnitConversion.MillimetersToFeet(bounds.MaxY), z);
            XYZ p4 = new XYZ(UnitConversion.MillimetersToFeet(bounds.MinX), UnitConversion.MillimetersToFeet(bounds.MaxY), z);

            var loop = new CurveLoop();
            loop.Append(Line.CreateBound(p1, p2));
            loop.Append(Line.CreateBound(p2, p3));
            loop.Append(Line.CreateBound(p3, p4));
            loop.Append(Line.CreateBound(p4, p1));
            return loop;
        }

        private static void ApplyRiskOverride(Document document, View view, ElementId elementId, RiskLevel riskLevel)
        {
            Color color = GetRiskColor(riskLevel);
            OverrideGraphicSettings settings = new OverrideGraphicSettings();
            settings.SetProjectionLineColor(color);
            settings.SetSurfaceTransparency(riskLevel == RiskLevel.Unknown ? 65 : 35);
            FillPatternElement pattern = GetSolidFillPattern(document);
            if (pattern != null)
            {
                settings.SetSurfaceForegroundPatternId(pattern.Id);
                settings.SetSurfaceForegroundPatternColor(color);
            }

            view.SetElementOverrides(elementId, settings);
        }

        private static Color GetRiskColor(RiskLevel riskLevel)
        {
            switch (riskLevel)
            {
                case RiskLevel.Severe:
                    return new Color(220, 53, 69);
                case RiskLevel.Insufficient:
                    return new Color(245, 124, 0);
                case RiskLevel.Warning:
                    return new Color(251, 192, 45);
                case RiskLevel.Passed:
                    return new Color(76, 175, 80);
                default:
                    return new Color(158, 158, 158);
            }
        }

        private static FillPatternElement GetSolidFillPattern(Document document)
        {
            foreach (FillPatternElement pattern in new FilteredElementCollector(document).OfClass(typeof(FillPatternElement)))
            {
                if (pattern.GetFillPattern().IsSolidFill)
                {
                    return pattern;
                }
            }

            return null;
        }
    }
}
