using System;
using System.Linq;
using PlugHub.ClearHeightAnalysis.Core.Geometry;
using PlugHub.ClearHeightAnalysis.Core.Models;
using PlugHub.ClearHeightAnalysis.Core.Services;
using Xunit;

namespace PlugHub.ClearHeightAnalysis.Tests.Core
{
    public sealed class EffectiveRegionBuilderTests
    {
        [Fact]
        public void TriangleEdgeCellStoresOnlyThePositiveIntersection()
        {
            var triangle = new PolygonLoop2d(new[]
            {
                new Point2d(0, 0), new Point2d(1200, 0), new Point2d(0, 1200)
            });
            var boundary = new AnalysisBoundary(new[]
            {
                new BoundaryRegion(triangle, Array.Empty<PolygonLoop2d>())
            });

            GridCellData cell = GridDomainBuilder.Build(boundary, 1000, "level-1", "1F", 0)
                .Single(value => value.Column == 1 && value.Row == 0);

            EffectiveCellRegion region = Assert.Single(cell.EffectiveRegions);
            Assert.Equal(20_000, region.AreaSquareMillimeters, 3);
            Assert.Equal(0.02, cell.EffectiveAreaRatio, 6);
            Assert.True(region.Contains(cell.RepresentativePoint, includeBoundary: false));
        }

        [Fact]
        public void OneCellPreservesTwoFragmentsConnectedOnlyOutsideTheCell()
        {
            var outsideConnected = new PolygonLoop2d(new[]
            {
                new Point2d(-100, -200), new Point2d(1100, -200),
                new Point2d(1100, 800), new Point2d(700, 800),
                new Point2d(700, 0), new Point2d(300, 0),
                new Point2d(300, 800), new Point2d(-100, 800)
            });
            var boundary = new AnalysisBoundary(new[]
            {
                new BoundaryRegion(outsideConnected, Array.Empty<PolygonLoop2d>())
            });

            GridCellData cell = GridDomainBuilder.Build(boundary, 1000, "level-1", "1F", 0)
                .Single(value => value.MinX == 0 && value.MinY == 0);

            Assert.Equal(2, cell.EffectiveRegions.Count);
            Assert.Equal(480_000, cell.EffectiveAreaSquareMillimeters, 3);
        }

        [Fact]
        public void HoleCrossingTheCellBoundaryBecomesAnOpenCutNotAClosedHole()
        {
            var boundary = new AnalysisBoundary(new[]
            {
                new BoundaryRegion(
                    TestGeometry.Rectangle(0, 0, 1000, 1000),
                    new[] { TestGeometry.Rectangle(500, -100, 1100, 500) })
            });

            GridCellData cell = Assert.Single(GridDomainBuilder.Build(boundary, 1000, "level-1", "1F", 0));
            EffectiveCellRegion region = Assert.Single(cell.EffectiveRegions);

            Assert.Empty(region.Holes);
            Assert.Equal(750_000, region.AreaSquareMillimeters, 3);
            Assert.False(region.Contains(new Point2d(750, 250), includeBoundary: false));
            Assert.True(region.Contains(new Point2d(250, 750), includeBoundary: false));
        }

        [Fact]
        public void OneCellPreservesDisconnectedBoundaryRegions()
        {
            var boundary = new AnalysisBoundary(new[]
            {
                new BoundaryRegion(TestGeometry.Rectangle(0, 0, 300, 1000), Array.Empty<PolygonLoop2d>()),
                new BoundaryRegion(TestGeometry.Rectangle(700, 0, 1000, 1000), Array.Empty<PolygonLoop2d>())
            });

            GridCellData cell = Assert.Single(GridDomainBuilder.Build(boundary, 1000, "level-1", "1F", 0));

            Assert.Equal(2, cell.EffectiveRegions.Count);
            Assert.Equal(new[] { 0, 1 }, cell.BoundaryRegionIndexes);
        }

        [Fact]
        public void HoleFullyInsideTheCellRemainsAHoleWithNetArea()
        {
            var boundary = new AnalysisBoundary(new[]
            {
                new BoundaryRegion(
                    TestGeometry.Rectangle(0, 0, 1000, 1000),
                    new[] { TestGeometry.Rectangle(400, 400, 600, 600) })
            });

            GridCellData cell = Assert.Single(
                GridDomainBuilder.Build(boundary, 1000, "level-1", "1F", 0));
            EffectiveCellRegion region = Assert.Single(cell.EffectiveRegions);

            Assert.Single(region.Holes);
            Assert.Equal(960_000, region.AreaSquareMillimeters, 3);
            Assert.False(region.Contains(new Point2d(500, 500), includeBoundary: false));
            Assert.True(region.Contains(cell.RepresentativePoint, includeBoundary: false));
        }
    }
}
