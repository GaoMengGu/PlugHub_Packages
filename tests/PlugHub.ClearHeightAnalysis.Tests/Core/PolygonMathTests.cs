using PlugHub.ClearHeightAnalysis.Core.Geometry;
using Xunit;

namespace PlugHub.ClearHeightAnalysis.Tests.Core
{
    public sealed class PolygonMathTests
    {
        [Fact]
        public void ContainsPointReturnsTrueForInsidePoint()
        {
            PolygonLoop2d loop = Rectangle(0, 0, 2000, 1000);

            Assert.True(PolygonMath.ContainsPoint(loop, new Point2d(500, 500)));
        }

        [Fact]
        public void ContainsPointReturnsTrueForBoundaryPoint()
        {
            PolygonLoop2d loop = Rectangle(0, 0, 2000, 1000);

            Assert.True(PolygonMath.ContainsPoint(loop, new Point2d(0, 500)));
        }

        [Fact]
        public void ContainsPointReturnsFalseForOutsidePoint()
        {
            PolygonLoop2d loop = Rectangle(0, 0, 2000, 1000);

            Assert.False(PolygonMath.ContainsPoint(loop, new Point2d(2500, 500)));
        }

        [Fact]
        public void IntersectsRectangleDetectsAnEdgeCrossingWithoutContainingItsCenter()
        {
            var crossing = new PolygonLoop2d(new[]
            {
                new Point2d(-100, 450),
                new Point2d(1100, 450),
                new Point2d(1100, 550),
                new Point2d(-100, 550)
            });

            Assert.True(PolygonMath.IntersectsRectangle(crossing, 0, 0, 1000, 1000));
            Assert.False(PolygonMath.IntersectsRectangle(crossing, 1200, 0, 2200, 1000));
        }

        [Fact]
        public void SignedAreaDistinguishesClockwiseAndCounterClockwiseLoops()
        {
            PolygonLoop2d counterClockwise = Rectangle(0, 0, 1000, 1000);
            var clockwise = new PolygonLoop2d(new[]
            {
                new Point2d(0, 0),
                new Point2d(0, 1000),
                new Point2d(1000, 1000),
                new Point2d(1000, 0)
            });

            Assert.True(PolygonMath.SignedArea(counterClockwise) > 0);
            Assert.True(PolygonMath.SignedArea(clockwise) < 0);
        }

        [Fact]
        public void IntersectsRectangleReturnsFalseForRectangleInsideAConcaveGap()
        {
            var uShape = new PolygonLoop2d(new[]
            {
                new Point2d(0, 0),
                new Point2d(3000, 0),
                new Point2d(3000, 3000),
                new Point2d(2000, 3000),
                new Point2d(2000, 1000),
                new Point2d(1000, 1000),
                new Point2d(1000, 3000),
                new Point2d(0, 3000)
            });

            Assert.False(PolygonMath.IntersectsRectangle(uShape, 1100, 1500, 1900, 2500));
        }

        private static PolygonLoop2d Rectangle(double minX, double minY, double maxX, double maxY)
        {
            return new PolygonLoop2d(new[]
            {
                new Point2d(minX, minY),
                new Point2d(maxX, minY),
                new Point2d(maxX, maxY),
                new Point2d(minX, maxY)
            });
        }
    }
}
