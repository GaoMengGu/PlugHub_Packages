using System.Collections.Generic;
using System.Linq;
using PlugHub.ClearHeightAnalysis.Core.Models;
using PlugHub.ClearHeightAnalysis.Core.Services;
using Xunit;

namespace PlugHub.ClearHeightAnalysis.Tests.Core
{
    public sealed class ClearHeightBatchAnalyzerTests
    {
        [Fact]
        public void AnalyzeReturnsRowMajorResultsAndStatusCounts()
        {
            IReadOnlyList<GridCellData> cells = new[] { CellAt(1, 0), CellAt(0, 0) };
            ObstacleSnapshot low = Overhead("low", 0, 0, 1000, 1000, 2500);
            var analyzer = new ClearHeightBatchAnalyzer(0, 0, 1000, new[] { low });

            CoreAnalysisSummary summary = analyzer.Analyze(cells, Settings());

            Assert.Equal(new[] { 0, 1 }, summary.Results.Select(item => item.Cell.Column));
            Assert.Equal(1, summary.Count(CellStatus.Severe));
            Assert.Equal(1, summary.Count(CellStatus.Unknown));
            Assert.Equal(1, summary.CandidateVisitCount);
        }

        [Fact]
        public void AnalyzeDoesNotScanEveryObstacleForEveryCell()
        {
            const int obstacleColumns = 500;
            const int obstacleCount = 50000;
            var obstacles = new List<ObstacleSnapshot>(obstacleCount);
            for (int index = 0; index < obstacleCount; index++)
            {
                int column = index % obstacleColumns;
                int row = index / obstacleColumns;
                double minX = column * 1000 + 100;
                double minY = row * 1000 + 100;
                obstacles.Add(Overhead(
                    "obstacle-" + index,
                    minX,
                    minY,
                    minX + 800,
                    minY + 800,
                    3200));
            }

            var cells = new List<GridCellData>(2000);
            for (int row = 0; row < 40; row++)
            {
                for (int column = 0; column < 50; column++)
                {
                    cells.Add(CellAt(column, row));
                }
            }

            var analyzer = new ClearHeightBatchAnalyzer(0, 0, 1000, obstacles);

            CoreAnalysisSummary summary = analyzer.Analyze(cells, Settings());

            Assert.Equal(2000, summary.Results.Count);
            Assert.True(summary.CandidateVisitCount < cells.Count * 100);
        }

        private static CoreAnalysisSettings Settings()
        {
            return new CoreAnalysisSettings(0, 0, 6000, 3000);
        }

        private static GridCellData CellAt(int column, int row)
        {
            return new GridCellData(
                "1F-" + row + "-" + column,
                column,
                row,
                column * 1000,
                row * 1000,
                (column + 1) * 1000,
                (row + 1) * 1000,
                "level-1",
                "1F",
                0,
                new[] { 0 });
        }

        private static ObstacleSnapshot Overhead(
            string key,
            double minX,
            double minY,
            double maxX,
            double maxY,
            double bottomElevation)
        {
            return new ObstacleSnapshot(
                key,
                key,
                "顶部障碍",
                "测试模型",
                null,
                ObstacleKind.Overhead,
                TestGeometry.Rectangle(minX, minY, maxX, maxY),
                ElevationPlane.Constant(bottomElevation),
                bottomElevation,
                bottomElevation + 500,
                GeometryConfidence.Exact);
        }
    }
}
