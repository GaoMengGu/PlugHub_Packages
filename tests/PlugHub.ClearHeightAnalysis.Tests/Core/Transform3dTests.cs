using System;
using PlugHub.ClearHeightAnalysis.Core.Geometry;
using Xunit;

namespace PlugHub.ClearHeightAnalysis.Tests.Core
{
    public sealed class Transform3dTests
    {
        [Fact]
        public void TransformAllCornersKeepsTheFullFortyFiveDegreeBounds()
        {
            var bounds = new Bounds3d(0, 0, 0, 10, 2, 3);

            Bounds3d rotated = bounds.TransformAllCorners(Transform3d.RotationZ(Math.PI / 4));

            Assert.Equal(-Math.Sqrt(2), rotated.MinX, 6);
            Assert.Equal(0, rotated.MinY, 6);
            Assert.Equal(5 * Math.Sqrt(2), rotated.MaxX, 6);
            Assert.Equal(6 * Math.Sqrt(2), rotated.MaxY, 6);
            Assert.Equal(0, rotated.MinZ, 6);
            Assert.Equal(3, rotated.MaxZ, 6);
        }

        [Fact]
        public void TransformAllCornersOrdersMirroredBounds()
        {
            var bounds = new Bounds3d(10, 20, 30, 40, 50, 60);
            var mirrorX = new Transform3d(
                -1, 0, 0, 100,
                0, 1, 0, 0,
                0, 0, 1, 0);

            Bounds3d mirrored = bounds.TransformAllCorners(mirrorX);

            Assert.Equal(60, mirrored.MinX);
            Assert.Equal(90, mirrored.MaxX);
            Assert.Equal(20, mirrored.MinY);
            Assert.Equal(50, mirrored.MaxY);
        }

        [Fact]
        public void OfPointAppliesRotationAndTranslation()
        {
            var transform = new Transform3d(
                0, -1, 0, 100,
                1, 0, 0, 200,
                0, 0, 1, 300);

            Point3d result = transform.OfPoint(new Point3d(10, 20, 30));

            Assert.Equal(new Point3d(80, 210, 330), result);
        }
    }
}
