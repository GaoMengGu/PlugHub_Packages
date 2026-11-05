using System;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Text;
using PlugHub.ClearHeightAnalysis.Core.Models;
using PlugHub.ClearHeightAnalysis.Core.Services;
using PlugHub.ClearHeightAnalysis.Core.Geometry;
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
            Assert.Contains("有效面积_m2", csv);
            Assert.Contains("有效面积比例", csv);
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

            Assert.Equal(16, raster.Width);
            Assert.Equal(8, raster.Height);
            Assert.Equal(new Rgba32(220, 53, 69, 230), raster.GetPixel(0, 0));
            Assert.Equal(new Rgba32(0, 0, 0, 0), raster.GetPixel(8, 0));
            Assert.Equal(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }, png.Take(8));
            Assert.True(png.Length > 50);
            Assert.Equal(520, DecodeImageData(png).Length);
        }

        [Fact]
        public void PerCellAreaAndRasterUseEffectiveFragmentsAndMask()
        {
            var triangle = new EffectiveCellRegion(new PolygonLoop2d(new[]
            {
                new Point2d(0, 0), new Point2d(1000, 0), new Point2d(0, 1000)
            }), Array.Empty<PolygonLoop2d>());
            var cell = new GridCellData("L1-edge", 0, 0, 0, 0, 1000, 1000,
                "level-1", "L1", 0, new[] { 0 }, new[] { triangle });
            var result = new CellAnalysisResult(cell, 2800, 3000, CellStatus.Insufficient, null, null);

            AreaOutputRegion area = Assert.Single(AreaOutputPlanner.Build(
                new[] { result }, AreaOutputMode.PerCell).Areas);
            RasterHeatmap raster = RasterHeatmapRenderer.Render(new[] { result }, includePassed: false);

            Assert.Equal(3, area.OuterLoop.Points.Count);
            Assert.True(triangle.Contains(area.PlacementPoint, includeBoundary: false));
            Assert.Equal(8, raster.Width);
            Assert.Equal(8, raster.Height);
            Assert.NotEqual(new Rgba32(0, 0, 0, 0), raster.GetPixel(0, 7));
            Assert.Equal(new Rgba32(0, 0, 0, 0), raster.GetPixel(7, 0));
        }

        [Fact]
        public void FormalPngContainsPlotLegendAndBatchMetadata()
        {
            AnalysisBatch batch = AnalysisBatchSerializerTestsAccessor.CreateBatchForOutputs();

            FormalHeatmap formal = FormalHeatmapRenderer.Render(batch, includePassed: false);
            byte[] png = formal.ToPngBytes();
            string raw = Encoding.Latin1.GetString(png);

            Assert.True(formal.Width >= formal.PlotWidth + 32);
            Assert.True(formal.PlotTop > 40);
            Assert.Equal(6, formal.Legend.Count);
            Assert.Equal("一层", formal.Metadata["Level"]);
            Assert.Equal("3000 mm", formal.Metadata["Threshold"]);
            Assert.Equal("batch-output", formal.Metadata["BatchId"]);
            Assert.Contains("Title\0Clear Height Analysis", raw);
            Assert.True(ContainsSequence(png, Encoding.UTF8.GetBytes("一层")));
        }

        private static bool ContainsSequence(byte[] source, byte[] expected)
        {
            for (int offset = 0; offset <= source.Length - expected.Length; offset++)
                if (expected.Select((value, index) => source[offset + index] == value).All(value => value))
                    return true;
            return false;
        }

        private static byte[] DecodeImageData(byte[] png)
        {
            using var compressed=new MemoryStream();
            int offset=8;
            while(offset<png.Length)
            {
                int length=(png[offset]<<24)|(png[offset+1]<<16)|(png[offset+2]<<8)|png[offset+3];
                string type=Encoding.ASCII.GetString(png,offset+4,4);
                if(type=="IDAT")compressed.Write(png,offset+8,length);
                offset+=12+length;
            }
            compressed.Position=0;
            using var zlib=new ZLibStream(compressed,CompressionMode.Decompress);
            using var decoded=new MemoryStream();
            zlib.CopyTo(decoded);
            return decoded.ToArray();
        }

        private static CellAnalysisResult Result(int column, int row, CellStatus status, double? height, string obstacle)
        {
            var cell = new GridCellData("L1-" + column, column, row, column * 1000, row * 1000,
                (column + 1) * 1000, (row + 1) * 1000, "level-1", "L1", 0, new[] { 0 });
            return new CellAnalysisResult(cell, height, 3000, status, obstacle, GeometryConfidence.Exact);
        }
    }
}
