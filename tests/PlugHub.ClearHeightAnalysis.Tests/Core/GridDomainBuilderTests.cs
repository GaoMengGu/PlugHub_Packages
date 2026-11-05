using System.Linq;
using PlugHub.ClearHeightAnalysis.Core.Geometry;
using PlugHub.ClearHeightAnalysis.Core.Models;
using PlugHub.ClearHeightAnalysis.Core.Services;
using Xunit;

namespace PlugHub.ClearHeightAnalysis.Tests.Core
{
    public sealed class GridDomainBuilderTests
    {
        [Fact]
        public void BuildExcludesCellsInsideAHole()
        {
            var boundary = new AnalysisBoundary(new[]
            {
                new BoundaryRegion(
                    TestGeometry.Rectangle(0, 0, 3000, 3000),
                    new[] { TestGeometry.Rectangle(1000, 1000, 2000, 2000) })
            });

            var cells = GridDomainBuilder.Build(boundary, 1000, "level-1", "1F", 0);

            Assert.Equal(8, cells.Count);
            Assert.DoesNotContain(cells, cell => cell.Column == 1 && cell.Row == 1);
        }

        [Fact]
        public void BuildKeepsCellsIntersectingAConcaveBoundary()
        {
            var lShape = new PolygonLoop2d(new[]
            {
                new Point2d(0, 0),
                new Point2d(2000, 0),
                new Point2d(2000, 1000),
                new Point2d(1000, 1000),
                new Point2d(1000, 2000),
                new Point2d(0, 2000)
            });
            var boundary = new AnalysisBoundary(new[]
            {
                new BoundaryRegion(lShape, new PolygonLoop2d[0])
            });

            var cells = GridDomainBuilder.Build(boundary, 1000, "level-1", "1F", 0);

            Assert.Equal(3, cells.Count);
            Assert.Equal(new[] { "1F-000001", "1F-000002", "1F-000003" }, cells.Select(cell => cell.Number));
        }

        [Fact]
        public void BuildPreservesTwoDisconnectedRegions()
        {
            var boundary = new AnalysisBoundary(new[]
            {
                new BoundaryRegion(TestGeometry.Rectangle(0, 0, 1000, 1000), new PolygonLoop2d[0]),
                new BoundaryRegion(TestGeometry.Rectangle(3000, 0, 4000, 1000), new PolygonLoop2d[0])
            });

            var cells = GridDomainBuilder.Build(boundary, 1000, "level-1", "1F", 0);

            Assert.Equal(2, cells.Count);
            Assert.Equal(new[] { 0 }, cells[0].BoundaryRegionIndexes);
            Assert.Equal(new[] { 1 }, cells[1].BoundaryRegionIndexes);
        }

        [Fact]
        public void BuildKeepsAPartialCellWithPositiveBoundaryArea()
        {
            var triangle = new PolygonLoop2d(new[]
            {
                new Point2d(0, 0),
                new Point2d(1200, 0),
                new Point2d(0, 1200)
            });
            var boundary = new AnalysisBoundary(new[]
            {
                new BoundaryRegion(triangle, new PolygonLoop2d[0])
            });

            var cells = GridDomainBuilder.Build(boundary, 1000, "level-1", "1F", 0);

            Assert.Equal(3, cells.Count);
            Assert.Contains(cells, cell => cell.Column == 1 && cell.Row == 0);
            Assert.Contains(cells, cell => cell.Column == 0 && cell.Row == 1);
        }
    }
}
