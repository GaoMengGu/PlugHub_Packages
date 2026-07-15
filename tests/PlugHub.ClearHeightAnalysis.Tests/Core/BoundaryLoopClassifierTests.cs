using System.Linq;
using PlugHub.ClearHeightAnalysis.Core.Geometry;
using PlugHub.ClearHeightAnalysis.Core.Models;
using PlugHub.ClearHeightAnalysis.Core.Services;
using Xunit;

namespace PlugHub.ClearHeightAnalysis.Tests.Core
{
    public sealed class BoundaryLoopClassifierTests
    {
        [Fact]
        public void ClassifyAssignsContainedLoopAsHoleRegardlessOfOrientation()
        {
            PolygonLoop2d outer = TestGeometry.Rectangle(0, 0, 4000, 4000);
            PolygonLoop2d inner = TestGeometry.Rectangle(1000, 1000, 2000, 2000);

            AnalysisBoundary boundary = BoundaryLoopClassifier.Classify(new[] { inner, outer });

            Assert.Single(boundary.Regions);
            Assert.Single(boundary.Regions[0].Holes);
            Assert.True(PolygonMath.SignedArea(boundary.Regions[0].Outer) > 0);
            Assert.True(PolygonMath.SignedArea(boundary.Regions[0].Holes[0]) < 0);
        }

        [Fact]
        public void ClassifyPreservesDisconnectedLoopsAsSeparateRegions()
        {
            AnalysisBoundary boundary = BoundaryLoopClassifier.Classify(new[]
            {
                TestGeometry.Rectangle(0, 0, 1000, 1000),
                TestGeometry.Rectangle(3000, 0, 4000, 1000)
            });

            Assert.Equal(2, boundary.Regions.Count);
            Assert.All(boundary.Regions, region => Assert.Empty(region.Holes));
        }

        [Fact]
        public void ClassifyAssignsHoleToTheContainingDisconnectedRegion()
        {
            PolygonLoop2d left = TestGeometry.Rectangle(0, 0, 4000, 4000);
            PolygonLoop2d right = TestGeometry.Rectangle(6000, 0, 10000, 4000);
            PolygonLoop2d rightHole = TestGeometry.Rectangle(7000, 1000, 8000, 2000);

            AnalysisBoundary boundary = BoundaryLoopClassifier.Classify(
                new[] { rightHole, left, right });

            Assert.Equal(2, boundary.Regions.Count);
            BoundaryRegion regionWithHole = boundary.Regions.Single(region => region.Holes.Count == 1);
            Assert.True(regionWithHole.Outer.MinX >= 6000);
        }
    }
}
