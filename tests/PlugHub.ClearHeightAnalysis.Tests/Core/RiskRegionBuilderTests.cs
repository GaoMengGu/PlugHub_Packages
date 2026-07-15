using System.Collections.Generic;
using System.Linq;
using PlugHub.ClearHeightAnalysis.Core.Geometry;
using PlugHub.ClearHeightAnalysis.Core.Models;
using PlugHub.ClearHeightAnalysis.Core.Services;
using Xunit;

namespace PlugHub.ClearHeightAnalysis.Tests.Core
{
    public sealed class RiskRegionBuilderTests
    {
        [Fact]
        public void FourNeighboursMergeAndSharedEdgeDisappears()
        {
            IReadOnlyList<RiskRegion> regions = RiskRegionBuilder.Build(new[]
            {
                Result(0, 0, CellStatus.Insufficient, 2800),
                Result(1, 0, CellStatus.Insufficient, 2700)
            });

            RiskRegion region = Assert.Single(regions);
            Assert.Equal(2, region.MemberCells.Count);
            Assert.Equal(4, region.OuterLoop.Points.Count);
            Assert.Equal(2700, region.MinimumClearHeightMillimeters);
        }

        [Fact]
        public void DiagonalCellsRemainSeparateAndStatusSeparatesComponents()
        {
            IReadOnlyList<RiskRegion> regions = RiskRegionBuilder.Build(new[]
            {
                Result(0, 0, CellStatus.Warning, 3100),
                Result(1, 1, CellStatus.Warning, 3150),
                Result(1, 0, CellStatus.Severe, 2500)
            });

            Assert.Equal(3, regions.Count);
        }

        [Fact]
        public void RingOfCellsProducesOuterLoopAndHoleWithInternalMarker()
        {
            var results = new List<CellAnalysisResult>();
            for (int row = 0; row < 3; row++)
            for (int column = 0; column < 3; column++)
                if (!(column == 1 && row == 1)) results.Add(Result(column, row, CellStatus.Blocked, null));

            RiskRegion region = Assert.Single(RiskRegionBuilder.Build(results));

            Assert.Single(region.Holes);
            Assert.Equal(4, region.OuterLoop.Points.Count);
            Assert.Equal(4, region.Holes[0].Points.Count);
            Assert.True(PolygonMath.ContainsPoint(region.OuterLoop, region.MarkerPoint));
            Assert.False(PolygonMath.ContainsPoint(region.Holes[0], region.MarkerPoint));
        }

        [Fact]
        public void PassedCellsAreExcludedAndRegionMetadataIsDeterministic()
        {
            IReadOnlyList<RiskRegion> regions = RiskRegionBuilder.Build(new[]
            {
                Result(3, 0, CellStatus.Passed, 3500),
                Result(1, 0, CellStatus.Warning, 3200, "b", GeometryConfidence.BoundingBoxFallback),
                Result(0, 0, CellStatus.Warning, 3100, "a", GeometryConfidence.Exact)
            });

            RiskRegion region = Assert.Single(regions);
            Assert.Equal("R-001", region.Number);
            Assert.Equal(new[] { "L1-000", "L1-001" }, region.MemberCells.Select(cell => cell.Number).OrderBy(x => x));
            Assert.Equal(new[] { "a", "b" }, region.ControllingObstacleKeys);
            Assert.Equal(GeometryConfidence.BoundingBoxFallback, region.WorstConfidence);
        }

        private static CellAnalysisResult Result(
            int column, int row, CellStatus status, double? height,
            string obstacle = null, GeometryConfidence? confidence = null)
        {
            var cell = new GridCellData(
                "L1-" + (row * 10 + column).ToString("000"), column, row,
                column * 1000, row * 1000, (column + 1) * 1000, (row + 1) * 1000,
                "level-1", "L1", 0, new[] { 0 });
            return new CellAnalysisResult(cell, height, 3000, status, obstacle, confidence);
        }
    }
}
