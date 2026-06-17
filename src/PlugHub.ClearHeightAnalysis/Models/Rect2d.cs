using System;

namespace PlugHub.ClearHeightAnalysis.Models
{
    public readonly struct Rect2d : IEquatable<Rect2d>
    {
        public Rect2d(double minX, double minY, double maxX, double maxY)
        {
            if (maxX < minX || maxY < minY)
            {
                throw new ArgumentException("矩形最大坐标必须大于或等于最小坐标。");
            }

            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
        }

        public double MinX { get; }
        public double MinY { get; }
        public double MaxX { get; }
        public double MaxY { get; }
        public double CenterX => (MinX + MaxX) / 2.0;
        public double CenterY => (MinY + MaxY) / 2.0;

        public bool Intersects(Rect2d other)
        {
            return MinX < other.MaxX && MaxX > other.MinX && MinY < other.MaxY && MaxY > other.MinY;
        }

        public bool ContainsCenterOf(Rect2d other)
        {
            return other.CenterX >= MinX && other.CenterX <= MaxX && other.CenterY >= MinY && other.CenterY <= MaxY;
        }

        public bool Equals(Rect2d other)
        {
            return MinX.Equals(other.MinX) && MinY.Equals(other.MinY) && MaxX.Equals(other.MaxX) && MaxY.Equals(other.MaxY);
        }

        public override bool Equals(object obj)
        {
            return obj is Rect2d other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = MinX.GetHashCode();
                hash = (hash * 397) ^ MinY.GetHashCode();
                hash = (hash * 397) ^ MaxX.GetHashCode();
                hash = (hash * 397) ^ MaxY.GetHashCode();
                return hash;
            }
        }
    }
}
