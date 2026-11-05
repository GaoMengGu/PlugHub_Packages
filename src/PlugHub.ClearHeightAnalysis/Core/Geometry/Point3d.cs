using System;

namespace PlugHub.ClearHeightAnalysis.Core.Geometry
{
    public readonly struct Point3d : IEquatable<Point3d>
    {
        public Point3d(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public double X { get; }
        public double Y { get; }
        public double Z { get; }

        public bool Equals(Point3d other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        public override bool Equals(object obj) => obj is Point3d other && Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = X.GetHashCode();
                hash = (hash * 397) ^ Y.GetHashCode();
                return (hash * 397) ^ Z.GetHashCode();
            }
        }
        public static bool operator ==(Point3d left, Point3d right) => left.Equals(right);
        public static bool operator !=(Point3d left, Point3d right) => !left.Equals(right);
    }
}
