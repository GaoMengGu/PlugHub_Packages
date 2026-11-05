#nullable enable
using System;

namespace PlugHub.ClearHeightAnalysis.Core.Models
{
    public sealed class CellAnalysisResult
    {
        public CellAnalysisResult(
            GridCellData cell,
            double? clearHeightMillimeters,
            double thresholdMillimeters,
            CellStatus status,
            string? controllingObstacleKey,
            GeometryConfidence? confidence)
        {
            Cell = cell ?? throw new ArgumentNullException(nameof(cell));
            ClearHeightMillimeters = clearHeightMillimeters;
            ThresholdMillimeters = thresholdMillimeters;
            Status = status;
            ControllingObstacleKey = controllingObstacleKey;
            Confidence = confidence;
        }

        public GridCellData Cell { get; }
        public double? ClearHeightMillimeters { get; }
        public double ThresholdMillimeters { get; }
        public double? DifferenceMillimeters =>
            ClearHeightMillimeters.HasValue
                ? ClearHeightMillimeters.Value - ThresholdMillimeters
                : (double?)null;
        public CellStatus Status { get; }
        public string? ControllingObstacleKey { get; }
        public GeometryConfidence? Confidence { get; }
    }
}
