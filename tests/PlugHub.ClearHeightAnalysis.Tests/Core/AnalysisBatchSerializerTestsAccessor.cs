using System;
using PlugHub.ClearHeightAnalysis.Core.Geometry;
using PlugHub.ClearHeightAnalysis.Core.Models;

namespace PlugHub.ClearHeightAnalysis.Tests.Core
{
    internal static class AnalysisBatchSerializerTestsAccessor
    {
        public static AnalysisBatch CreateBatchForOutputs()
        {
            var level = new AnalysisLevelChoice("level-1", "一层", 0);
            var host = new SourceModelChoice("host", "当前模型", true, true, true);
            var request = new AnalysisRequest(new[] { level }, new[] { host }) { SelectedLevel = level };
            var loop = new PolygonLoop2d(new[] { new Point2d(0, 0), new Point2d(2000, 0), new Point2d(2000, 1000), new Point2d(0, 1000) });
            var boundary = new AnalysisBoundary(new[] { new BoundaryRegion(loop, Array.Empty<PolygonLoop2d>()) });
            var first = new GridCellData("一层-1", 0, 0, 0, 0, 1000, 1000, "level-1", "一层", 0, new[] { 0 });
            var second = new GridCellData("一层-2", 1, 0, 1000, 0, 2000, 1000, "level-1", "一层", 0, new[] { 0 });
            var obstacle = new ObstacleSnapshot("host:obstacle-1", "风管 1", "风管", "当前模型", null,
                ObstacleKind.Overhead, loop, ElevationPlane.Constant(2850), 2850, 3200, GeometryConfidence.Exact);
            var settings = new CoreAnalysisSettings(0, 0, 6000, 3000);
            var results = new[]
            {
                new CellAnalysisResult(first, 2850, 3000, CellStatus.Insufficient, obstacle.Key, GeometryConfidence.Exact),
                new CellAnalysisResult(second, null, 3000, CellStatus.Unknown, null, null)
            };
            var run = new AnalysisRunData(request, boundary, new[] { first, second }, new[] { obstacle }, settings, new CoreAnalysisSummary(results, 2));
            return new AnalysisBatch(AnalysisBatch.CurrentSchemaVersion, "batch-output", new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc),
                "doc", "示例项目", run, Array.Empty<DerivedOutputRecord>());
        }
    }
}
