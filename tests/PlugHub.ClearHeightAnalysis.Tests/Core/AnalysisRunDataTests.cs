using System;
using PlugHub.ClearHeightAnalysis.Core.Models;
using Xunit;

namespace PlugHub.ClearHeightAnalysis.Tests.Core
{
    public sealed class AnalysisRunDataTests
    {
        [Fact]
        public void ConstructorPreservesCandidateVisitDiagnostics()
        {
            GridCellData cell = Cell("cell-1");
            var result = new CellAnalysisResult(cell, null, 3000, CellStatus.Unknown, null, null);
            var summary = new CoreAnalysisSummary(new[] { result }, 17);

            AnalysisRunData run = new AnalysisRunData(
                Request(), Boundary(), new[] { cell }, new ObstacleSnapshot[0],
                new CoreAnalysisSettings(0, 0, 6000, 3000), summary);

            Assert.Equal(17, run.Summary.CandidateVisitCount);
        }

        [Fact]
        public void ConstructorRejectsSummaryForDifferentCells()
        {
            GridCellData expected = Cell("expected");
            GridCellData different = Cell("different");
            var summary = new CoreAnalysisSummary(new[]
            {
                new CellAnalysisResult(different, null, 3000, CellStatus.Unknown, null, null)
            }, 0);

            Assert.Throws<ArgumentException>(() => new AnalysisRunData(
                Request(), Boundary(), new[] { expected }, new ObstacleSnapshot[0],
                new CoreAnalysisSettings(0, 0, 6000, 3000), summary));
        }

        private static GridCellData Cell(string number) => new GridCellData(
            number, 0, 0, 0, 0, 1000, 1000, "level-1", "1F", 0, new[] { 0 });

        private static AnalysisBoundary Boundary() => new AnalysisBoundary(new[]
        {
            new BoundaryRegion(TestGeometry.Rectangle(0, 0, 1000, 1000), new PlugHub.ClearHeightAnalysis.Core.Geometry.PolygonLoop2d[0])
        });

        private static AnalysisRequest Request()
        {
            var level = new AnalysisLevelChoice("level-1", "1F", 0);
            return new AnalysisRequest(new[] { level }, new[]
            {
                new SourceModelChoice("host", "当前模型", true, true, true)
            }) { SelectedLevel = level };
        }
    }
}
