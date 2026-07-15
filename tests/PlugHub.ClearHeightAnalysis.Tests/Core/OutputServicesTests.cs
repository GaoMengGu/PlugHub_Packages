using System;
using System.Linq;
using System.Text;
using PlugHub.ClearHeightAnalysis.Core.Models;
using PlugHub.ClearHeightAnalysis.Core.Services;
using Xunit;

namespace PlugHub.ClearHeightAnalysis.Tests.Core
{
    public sealed class OutputServicesTests
    {
        [Fact]
        public void AreaPlannerReportsMergedAndPerCellCountsWithoutSilentTruncation()
        {
            CellAnalysisResult[] results =
            {
                Result(0, 0, CellStatus.Insufficient, 2800, "host:a"),
                Result(1, 0, CellStatus.Insufficient, 2750, "host:a"),
                Result(2, 0, CellStatus.Warning, 3150, "link:l:b")
            };

            AreaOutputPlan merged = AreaOutputPlanner.Build(results, AreaOutputMode.MergedRegions);
            AreaOutputPlan cells = AreaOutputPlanner.Build(results, AreaOutputMode.PerCell);

            Assert.Equal(2, merged.EstimatedAreaCount);
            Assert.Equal(3, cells.EstimatedAreaCount);
            Assert.Equal(10, cells.EstimatedBoundaryLineCount);
            Assert.All(cells.Areas, area => Assert.Single(area.MemberCellNumbers));
        }

        [Fact]
        public void CsvContainsEveryGridAndControllingObstacleMetadata()
        {
            AnalysisBatch batch = AnalysisBatchSerializerTestsAccessor.CreateBatchForOutputs();

            byte[] bytes = CsvResultExporter.Export(batch);
            string csv = Encoding.UTF8.GetString(bytes);

            Assert.StartsWith("\uFEFF批次ID,分析时间UTC", csv);
            Assert.Contains("网格编号", csv);
            Assert.Contains("host:obstacle-1", csv);
            Assert.Contains("风管 1", csv);
            Assert.Contains("当前模型", csv);
            Assert.Contains("Exact", csv);
            Assert.Equal(batch.RunData.Cells.Count + 1, csv.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Length);
        }

        [Fact]
        public void RasterUsesStableColorsAndTransparentExcludedPassedCells()
        {
            CellAnalysisResult[] results =
            {
                Result(0, 0, CellStatus.Severe, 2400, "a"),
                Result(1, 0, CellStatus.Passed, 3500, "b")
            };

            RasterHeatmap raster = RasterHeatmapRenderer.Render(results, includePassed: false);
            byte[] png = raster.ToPngBytes();

            Assert.Equal(2, raster.Width);
            Assert.Equal(1, raster.Height);
            Assert.Equal(new Rgba32(220, 53, 69, 230), raster.GetPixel(0, 0));
            Assert.Equal(new Rgba32(0, 0, 0, 0), raster.GetPixel(1, 0));
            Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, png.Take(8));
            Assert.True(png.Length > 50);
        }

        private static CellAnalysisResult Result(int column, int row, CellStatus status, double? height, string obstacle)
        {
            var cell = new GridCellData("L1-" + column, column, row, column * 1000, row * 1000,
                (column + 1) * 1000, (row + 1) * 1000, "level-1", "L1", 0, new[] { 0 });
            return new CellAnalysisResult(cell, height, 3000, status, obstacle, GeometryConfidence.Exact);
        }
    }
}
