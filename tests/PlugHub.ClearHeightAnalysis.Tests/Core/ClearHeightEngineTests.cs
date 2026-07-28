using PlugHub.ClearHeightAnalysis.Core.Geometry;
using PlugHub.ClearHeightAnalysis.Core.Models;
using PlugHub.ClearHeightAnalysis.Core.Services;
using Xunit;

namespace PlugHub.ClearHeightAnalysis.Tests.Core
{
    public sealed class ClearHeightEngineTests
    {
        [Fact]
        public void CalculateUsesTheLowestActuallyOverlappingObstacle()
        {
            GridCellData cell = Cell();
            ObstacleSnapshot beam = Overhead(
                "beam",
                TestGeometry.Rectangle(0, 0, 1000, 400),
                ElevationPlane.Constant(3200),
                3200,
                GeometryConfidence.Exact);
            ObstacleSnapshot duct = Overhead(
                "duct",
                TestGeometry.Rectangle(500, 0, 1000, 1000),
                ElevationPlane.Constant(2800),
                2800,
                GeometryConfidence.CategoryApproximation);

            CellAnalysisResult result = ClearHeightEngine.Calculate(
                cell,
                new[] { beam, duct },
                Settings(finishOffset: 50, threshold: 3000));

            Assert.Equal(2750, result.ClearHeightMillimeters);
            Assert.Equal(CellStatus.Insufficient, result.Status);
            Assert.Equal("duct", result.ControllingObstacleKey);
            Assert.Equal(GeometryConfidence.CategoryApproximation, result.Confidence);
        }

        [Fact]
        public void CalculateReturnsUnknownWhenNoOverheadObstacleIntersects()
        {
            CellAnalysisResult result = ClearHeightEngine.Calculate(
                Cell(),
                new ObstacleSnapshot[0],
                Settings(0, 3000));

            Assert.Equal(CellStatus.Unknown, result.Status);
            Assert.Null(result.ClearHeightMillimeters);
            Assert.Null(result.ControllingObstacleKey);
            Assert.Null(result.Confidence);
        }

        [Fact]
        public void CalculateReturnsBlockedBeforeEvaluatingOverheadClearance()
        {
            ObstacleSnapshot column = Blocked(
                "column",
                TestGeometry.Rectangle(200, 200, 400, 400),
                -500,
                4000);
            ObstacleSnapshot slab = Overhead(
                "slab",
                TestGeometry.Rectangle(0, 0, 1000, 1000),
                ElevationPlane.Constant(3200),
                3200,
                GeometryConfidence.Exact);

            CellAnalysisResult result = ClearHeightEngine.Calculate(
                Cell(),
                new[] { slab, column },
                Settings(0, 3000));

            Assert.Equal(CellStatus.Blocked, result.Status);
            Assert.Equal("column", result.ControllingObstacleKey);
            Assert.Equal(GeometryConfidence.Exact, result.Confidence);
        }

        [Fact]
        public void CalculateIgnoresAnAxisAlignedBoundsFalsePositive()
        {
            var triangleOutsideCell = new PolygonLoop2d(new[]
            {
                new Point2d(900, 1100),
                new Point2d(1100, 900),
                new Point2d(1100, 1100)
            });
            ObstacleSnapshot obstacle = Overhead(
                "outside",
                triangleOutsideCell,
                ElevationPlane.Constant(2000),
                2000,
                GeometryConfidence.BoundingBoxFallback);

            CellAnalysisResult result = ClearHeightEngine.Calculate(
                Cell(),
                new[] { obstacle },
                Settings(0, 3000));

            Assert.Equal(CellStatus.Unknown, result.Status);
        }

        [Fact]
        public void CalculateUsesTheMinimumSlopedPlaneElevationInsideTheCell()
        {
            ObstacleSnapshot sloped = Overhead(
                "sloped",
                TestGeometry.Rectangle(0, 0, 1000, 1000),
                new ElevationPlane(-0.2, 0, 3200),
                3000,
                GeometryConfidence.CategoryApproximation);

            CellAnalysisResult result = ClearHeightEngine.Calculate(
                Cell(),
                new[] { sloped },
                Settings(0, 3000));

            Assert.Equal(3000, result.ClearHeightMillimeters);
            Assert.Equal(CellStatus.Warning, result.Status);
        }

        [Theory]
        [InlineData(2699, CellStatus.Severe)]
        [InlineData(2700, CellStatus.Insufficient)]
        [InlineData(2999, CellStatus.Insufficient)]
        [InlineData(3000, CellStatus.Warning)]
        [InlineData(3299, CellStatus.Warning)]
        [InlineData(3300, CellStatus.Passed)]
        public void ClassifyUsesTheApprovedThreeHundredMillimetreBand(
            double clearHeight,
            CellStatus expected)
        {
            Assert.Equal(expected, ClearHeightEngine.Classify(clearHeight, 3000));
        }

        private static GridCellData Cell()
        {
            return new GridCellData(
                "1F-000001",
                0,
                0,
                0,
                0,
                1000,
                1000,
                "level-1",
                "1F",
                0,
                new[] { 0 });
        }

        private static CoreAnalysisSettings Settings(double finishOffset, double threshold)
        {
            return new CoreAnalysisSettings(0, finishOffset, 6000, threshold);
        }

        private static ObstacleSnapshot Overhead(
            string key,
            PolygonLoop2d footprint,
            ElevationPlane bottomPlane,
            double minimumBottomElevation,
            GeometryConfidence confidence)
        {
            return new ObstacleSnapshot(
                key,
                key,
                "顶部障碍",
                "测试模型",
                null,
                ObstacleKind.Overhead,
                footprint,
                bottomPlane,
                minimumBottomElevation,
                minimumBottomElevation + 500,
                confidence);
        }

        private static ObstacleSnapshot Blocked(
            string key,
            PolygonLoop2d footprint,
            double bottomElevation,
            double topElevation)
        {
            return new ObstacleSnapshot(
                key,
                key,
                "结构柱",
                "测试模型",
                null,
                ObstacleKind.Blocked,
                footprint,
                ElevationPlane.Constant(bottomElevation),
                bottomElevation,
                topElevation,
                GeometryConfidence.Exact);
        }
    }
}
