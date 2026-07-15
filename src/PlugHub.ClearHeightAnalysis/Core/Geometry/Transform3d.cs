using System;

namespace PlugHub.ClearHeightAnalysis.Core.Geometry
{
    public readonly struct Transform3d
    {
        public Transform3d(
            double m11, double m12, double m13, double offsetX,
            double m21, double m22, double m23, double offsetY,
            double m31, double m32, double m33, double offsetZ)
        {
            M11 = m11; M12 = m12; M13 = m13; OffsetX = offsetX;
            M21 = m21; M22 = m22; M23 = m23; OffsetY = offsetY;
            M31 = m31; M32 = m32; M33 = m33; OffsetZ = offsetZ;
        }

        public double M11 { get; }
        public double M12 { get; }
        public double M13 { get; }
        public double OffsetX { get; }
        public double M21 { get; }
        public double M22 { get; }
        public double M23 { get; }
        public double OffsetY { get; }
        public double M31 { get; }
        public double M32 { get; }
        public double M33 { get; }
        public double OffsetZ { get; }

        public Point3d OfPoint(Point3d point)
        {
            return new Point3d(
                M11 * point.X + M12 * point.Y + M13 * point.Z + OffsetX,
                M21 * point.X + M22 * point.Y + M23 * point.Z + OffsetY,
                M31 * point.X + M32 * point.Y + M33 * point.Z + OffsetZ);
        }

        public static Transform3d RotationZ(double radians)
        {
            double cosine = Math.Cos(radians);
            double sine = Math.Sin(radians);
            return new Transform3d(
                cosine, -sine, 0, 0,
                sine, cosine, 0, 0,
                0, 0, 1, 0);
        }
    }
}
