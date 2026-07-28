#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using PlugHub.ClearHeightAnalysis.Core.Geometry;

namespace PlugHub.ClearHeightAnalysis.Core.Models
{
    public enum AreaOutputMode { MergedRegions, PerCell }

    public sealed class AreaOutputRegion
    {
        public AreaOutputRegion(string number, CellStatus status, PolygonLoop2d outerLoop,
            IEnumerable<PolygonLoop2d> holes, Point2d placementPoint, IEnumerable<string> memberCellNumbers,
            double? minimumClearHeightMillimeters)
        {
            Number = number ?? throw new ArgumentNullException(nameof(number));
            Status = status;
            OuterLoop = outerLoop ?? throw new ArgumentNullException(nameof(outerLoop));
            Holes = (holes ?? throw new ArgumentNullException(nameof(holes))).ToList().AsReadOnly();
            PlacementPoint = placementPoint;
            MemberCellNumbers = (memberCellNumbers ?? throw new ArgumentNullException(nameof(memberCellNumbers))).ToList().AsReadOnly();
            MinimumClearHeightMillimeters = minimumClearHeightMillimeters;
        }
        public string Number { get; }
        public CellStatus Status { get; }
        public PolygonLoop2d OuterLoop { get; }
        public IReadOnlyList<PolygonLoop2d> Holes { get; }
        public Point2d PlacementPoint { get; }
        public IReadOnlyList<string> MemberCellNumbers { get; }
        public double? MinimumClearHeightMillimeters { get; }
    }

    public sealed class AreaOutputPlan
    {
        public AreaOutputPlan(AreaOutputMode mode, IEnumerable<AreaOutputRegion> areas, int estimatedBoundaryLineCount)
        {
            Mode = mode;
            Areas = (areas ?? throw new ArgumentNullException(nameof(areas))).ToList().AsReadOnly();
            if (estimatedBoundaryLineCount < 0) throw new ArgumentOutOfRangeException(nameof(estimatedBoundaryLineCount));
            EstimatedBoundaryLineCount = estimatedBoundaryLineCount;
        }
        public AreaOutputMode Mode { get; }
        public IReadOnlyList<AreaOutputRegion> Areas { get; }
        public int EstimatedAreaCount => Areas.Count;
        public int EstimatedBoundaryLineCount { get; }
    }

    public readonly struct Rgba32 : IEquatable<Rgba32>
    {
        public Rgba32(byte red, byte green, byte blue, byte alpha) { Red=red; Green=green; Blue=blue; Alpha=alpha; }
        public byte Red { get; }
        public byte Green { get; }
        public byte Blue { get; }
        public byte Alpha { get; }
        public bool Equals(Rgba32 other) => Red==other.Red && Green==other.Green && Blue==other.Blue && Alpha==other.Alpha;
        public override bool Equals(object? obj) => obj is Rgba32 other && Equals(other);
        public override int GetHashCode() => (((Red * 397) ^ Green) * 397 ^ Blue) * 397 ^ Alpha;
        public static bool operator ==(Rgba32 left, Rgba32 right) => left.Equals(right);
        public static bool operator !=(Rgba32 left, Rgba32 right) => !left.Equals(right);
    }
}
