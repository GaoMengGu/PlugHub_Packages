using System;
using PlugHub.ClearHeightAnalysis.Core.Geometry;
using PlugHub.ClearHeightAnalysis.Core.Models;
using Xunit;
using System.Collections.Generic;

namespace PlugHub.ClearHeightAnalysis.Tests.Core
{
    public sealed class ObstacleGeometryTests
    {
        [Fact]
        public void RotatedLinearObstacleUsesDirectionWidthAndInterpolatedBottom()
        {
            var geometry = new LinearObstacle(
                new Point2d(0, 0), new Point2d(1000, 1000), 100, 2800, 3000);

            Assert.True(geometry.MinX < 0);
            Assert.True(geometry.MaxY > 1000);
            Assert.Equal(4, geometry.Footprint.Points.Count);
            Assert.Equal(2900, geometry.BottomAt(new Point2d(500, 500)), 3);
            Assert.Equal(2800, geometry.MinimumBottomElevationMillimeters, 3);
        }

        [Fact]
        public void PlanarObstaclePreservesMultipleRegionsAndHoles()
        {
            var first = new EffectiveCellRegion(TestGeometry.Rectangle(0, 0, 1000, 1000),
                new[] { TestGeometry.Rectangle(400, 400, 600, 600) });
            var second = new EffectiveCellRegion(TestGeometry.Rectangle(2000, 0, 2500, 500),
                Array.Empty<PolygonLoop2d>());
            var geometry = new PlanarRegionObstacle(new[]
            {
                new PlanarObstacleRegion(first, ElevationPlane.Constant(3000)),
                new PlanarObstacleRegion(second, ElevationPlane.Constant(2800))
            });

            Assert.Equal(2, geometry.Regions.Count);
            Assert.Single(geometry.Regions[0].Region.Holes);
            Assert.Equal(2800, geometry.MinimumBottomElevationMillimeters);
            Assert.Equal(2500, geometry.MaxX);
        }

        [Fact]
        public void BlockedAndBoundingBoxGeometryExposeExplicitTypes()
        {
            var blocked = new BlockedRegionObstacle(
                new EffectiveCellRegion(TestGeometry.Rectangle(0, 0, 100, 100), Array.Empty<PolygonLoop2d>()),
                -100, 4000);
            var fallback = new BoundingBoxObstacle(0, 0, 100, 100, 2500, 3000);

            Assert.Equal(ObstacleGeometryType.BlockedRegion, blocked.GeometryType);
            Assert.Equal(ObstacleGeometryType.BoundingBox, fallback.GeometryType);
            Assert.Equal(ObstacleKind.Blocked, blocked.Kind);
            Assert.Equal(GeometryConfidence.BoundingBoxFallback, fallback.RequiredConfidence);
        }

        [Fact]
        public void BlockedRegionDoesNotOverlapCellInsideItsHole()
        {
            var blocked = new BlockedRegionObstacle(new EffectiveCellRegion(
                TestGeometry.Rectangle(0, 0, 1000, 1000),
                new[] { TestGeometry.Rectangle(300, 300, 700, 700) }), -100, 4000);
            var cellRegion = new EffectiveCellRegion(
                TestGeometry.Rectangle(400, 400, 600, 600), Array.Empty<PolygonLoop2d>());

            Assert.False(blocked.TryGetMinimumBottom(
                new List<EffectiveCellRegion> { cellRegion }, out _));
        }
    }
}
