namespace PlugHub.ClearHeightAnalysis.Core.Geometry
{
    public readonly struct Segment2d
    {
        public Segment2d(Point2d start, Point2d end)
        {
            Start = start;
            End = end;
        }

        public Point2d Start { get; }
        public Point2d End { get; }
        public double MinX => Start.X < End.X ? Start.X : End.X;
        public double MinY => Start.Y < End.Y ? Start.Y : End.Y;
        public double MaxX => Start.X > End.X ? Start.X : End.X;
        public double MaxY => Start.Y > End.Y ? Start.Y : End.Y;
    }
}
