using System;
using System.Collections.Generic;
using System.Threading;
using PlugHub.ClearHeightAnalysis.Core.Models;
using PlugHub.ClearHeightAnalysis.Core.Services;
using Xunit;
using PlugHub.ClearHeightAnalysis.Core.Geometry;
using PlugHub.ClearHeightAnalysis.Core.Persistence;

namespace PlugHub.ClearHeightAnalysis.Tests.Core
{
    public sealed class AnalysisCancellationTests
    {
        [Fact]
        public void BatchAnalyzerReportsMonotonicProgressAndHonoursCancellation()
        {
            var cells = new List<GridCellData>();
            for (int column = 0; column < 10; column++)
                cells.Add(new GridCellData("L1-" + column, column, 0, column * 1000, 0,
                    (column + 1) * 1000, 1000, "level-1", "L1", 0, new[] { 0 }));
            using var cancellation = new CancellationTokenSource();
            var reported = new List<AnalysisProgress>();
            var progress = new InlineProgress(value =>
            {
                reported.Add(value);
                if (value.Completed >= 4) cancellation.Cancel();
            });
            var analyzer = new ClearHeightBatchAnalyzer(0, 0, 1000, Array.Empty<ObstacleSnapshot>());

            Assert.Throws<OperationCanceledException>(() => analyzer.Analyze(
                cells, new CoreAnalysisSettings(0, 0, 6000, 3000), cancellation.Token, progress));
            Assert.NotEmpty(reported);
            for (int index = 1; index < reported.Count; index++)
                Assert.True(reported[index].Completed >= reported[index - 1].Completed);
            Assert.All(reported, value => Assert.Equal(10, value.Total));
        }

        [Fact]
        public void SpatialIndexHonoursCancellationWhileRegisteringObstacles()
        {
            var obstacles = new List<ObstacleSnapshot>();
            for (int index = 0; index < 100; index++)
                obstacles.Add(new ObstacleSnapshot("o-" + index, "o", "测试", "模型", null,
                    ObstacleKind.Overhead, TestGeometry.Rectangle(index * 10, 0, index * 10 + 5000, 5000),
                    ElevationPlane.Constant(3000), 3000, 3500, GeometryConfidence.Exact));
            using var cancellation = new CancellationTokenSource();
            var progress = new InlineProgress(value => { if (value.Completed >= 2) cancellation.Cancel(); });

            Assert.Throws<OperationCanceledException>(() => new ObstacleSpatialIndex(
                0, 0, 1000, obstacles, cancellation.Token, progress));
        }

        [Fact]
        public void BatchSerializationHonoursCancellationBeforePersistence()
        {
            AnalysisBatch batch = AnalysisBatchSerializerTestsAccessor.CreateBatchForOutputs();
            using var cancellation = new CancellationTokenSource();
            var progress = new InlineProgress(value =>
            {
                if (value.Stage == "序列化批次" && value.Completed >= 1) cancellation.Cancel();
            });

            Assert.Throws<OperationCanceledException>(() => AnalysisBatchSerializer.Serialize(
                batch, cancellation.Token, progress));
        }

        [Fact]
        public void RiskRegionMergeHonoursCancellation()
        {
            var results = new List<CellAnalysisResult>();
            for (int column = 0; column < 20; column++)
            {
                var cell = new GridCellData("L1-" + column, column, 0, column * 1000, 0,
                    (column + 1) * 1000, 1000, "level-1", "L1", 0, new[] { 0 });
                results.Add(new CellAnalysisResult(cell, 2800, 3000, CellStatus.Insufficient, null, null));
            }
            using var cancellation = new CancellationTokenSource();
            var progress = new InlineProgress(value =>
            {
                if (value.Stage == "合并风险区域" && value.Completed > 0) cancellation.Cancel();
            });

            Assert.Throws<OperationCanceledException>(() => RiskRegionBuilder.Build(
                results, false, cancellation.Token, progress));
        }

        private sealed class InlineProgress : IProgress<AnalysisProgress>
        {
            private readonly Action<AnalysisProgress> _report;
            public InlineProgress(Action<AnalysisProgress> report) { _report = report; }
            public void Report(AnalysisProgress value) { _report(value); }
        }
    }
}
