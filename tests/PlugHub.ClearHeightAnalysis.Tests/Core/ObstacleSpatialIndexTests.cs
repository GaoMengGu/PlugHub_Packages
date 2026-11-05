using System.Linq;
using PlugHub.ClearHeightAnalysis.Core.Models;
using PlugHub.ClearHeightAnalysis.Core.Services;
using Xunit;

namespace PlugHub.ClearHeightAnalysis.Tests.Core
{
    public sealed class ObstacleSpatialIndexTests
    {
        [Fact]
        public void QueryReturnsOnlyObstaclesRegisteredForTheCell()
        {
            ObstacleSnapshot near = Snapshot("near", 100, 100, 900, 900, 2800);
            ObstacleSnapshot far = Snapshot("far", 5000, 5000, 6000, 6000, 2600);
            var index = new ObstacleSpatialIndex(0, 0, 1000, new[] { near, far });

            var keys = index.Query(CellAt(0, 0)).Select(item => item.Key).ToArray();

            Assert.Equal(new[] { "near" }, keys);
        }

        [Fact]
        public void IndexRegistersAnObstacleCrossingMultipleCellsInEachBucket()
        {
            ObstacleSnapshot crossing = Snapshot("crossing", 500, 100, 2500, 900, 3000);
            var index = new ObstacleSpatialIndex(0, 0, 1000, new[] { crossing });

            Assert.Single(index.Query(CellAt(0, 0)));
            Assert.Single(index.Query(CellAt(1, 0)));
            Assert.Single(index.Query(CellAt(2, 0)));
            Assert.Empty(index.Query(CellAt(3, 0)));
        }

        [Fact]
        public void IndexDoesNotRegisterAnObstacleThatOnlyTouchesTheNextCellEdge()
        {
            ObstacleSnapshot firstCell = Snapshot("first", 0, 0, 1000, 1000, 3000);
            var index = new ObstacleSpatialIndex(0, 0, 1000, new[] { firstCell });

            Assert.Single(index.Query(CellAt(0, 0)));
            Assert.Empty(index.Query(CellAt(1, 0)));
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

        private static ObstacleSnapshot Snapshot(
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
                "测试类别",
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
