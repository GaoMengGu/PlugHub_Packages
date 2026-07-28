using System;

namespace PlugHub.ClearHeightAnalysis.Core.Geometry
{
    public readonly struct Point2d : IEquatable<Point2d>
    {
        public Point2d(double x, double y)
        {
            X = x;
            Y = y;
        }

        public double X { get; }
        public double Y { get; }

        public bool Equals(Point2d other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }

        public override bool Equals(object obj)
        {
            return obj is Point2d other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        public static bool operator ==(Point2d left, Point2d right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Point2d left, Point2d right)
        {
            return !left.Equals(right);
        }
    }
}
