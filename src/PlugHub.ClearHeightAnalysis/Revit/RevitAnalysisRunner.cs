using System;
using System.Collections.Generic;
using Autodesk.Revit.UI;
using PlugHub.ClearHeightAnalysis.Core.Models;
using PlugHub.ClearHeightAnalysis.Core.Services;

namespace PlugHub.ClearHeightAnalysis.Revit
{
    public sealed class RevitAnalysisRunner
    {
        public AnalysisRunData Run(UIDocument uiDocument, AnalysisRequest request)
        {
            if (uiDocument == null) throw new ArgumentNullException(nameof(uiDocument));
            RevitAnalysisContext context = RevitAnalysisContextBuilder.Build(uiDocument.Document, request);
            var settings = new CoreAnalysisSettings(
                Services.UnitConversion.FeetToMillimeters(context.Level.Elevation),
                request.FinishFloorOffsetMillimeters,
                request.SearchHeightMillimeters,
                request.ClearHeightThresholdMillimeters);
            AnalysisBoundary boundary = new RevitBoundaryProvider().GetBoundary(uiDocument, context);
            IReadOnlyList<GridCellData> cells = GridDomainBuilder.Build(
                boundary,
                request.GridSizeMillimeters,
                context.Level.UniqueId,
                context.Level.Name,
                settings.LevelElevationMillimeters);
            if (cells.Count == 0)
                throw new InvalidOperationException("分析范围内没有生成有效网格。");

            IReadOnlyList<ObstacleSnapshot> obstacles =
                new RevitObstacleSnapshotCollector().Collect(context, boundary, settings);
            double originX = cells[0].MinX - cells[0].Column * request.GridSizeMillimeters;
            double originY = cells[0].MinY - cells[0].Row * request.GridSizeMillimeters;
            CoreAnalysisSummary summary = new ClearHeightBatchAnalyzer(
                originX, originY, request.GridSizeMillimeters, obstacles).Analyze(cells, settings);
            return new AnalysisRunData(request, boundary, cells, obstacles, settings, summary);
        }
    }
}
