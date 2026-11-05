using PlugHub.ClearHeightAnalysis.Core.Geometry;

namespace PlugHub.ClearHeightAnalysis.Tests.Core
{
    internal static class TestGeometry
    {
        public static PolygonLoop2d Rectangle(double minX, double minY, double maxX, double maxY)
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
