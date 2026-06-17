namespace PlugHub.ClearHeightAnalysis.Models
{
    public sealed class ClearHeightResult
    {
        public ClearHeightResult(GridCell cell, double? clearHeightMillimeters, double thresholdMillimeters, RiskLevel riskLevel, ObstacleProjection controllingObstacle)
        {
            Cell = cell;
            ClearHeightMillimeters = clearHeightMillimeters;
            ThresholdMillimeters = thresholdMillimeters;
            RiskLevel = riskLevel;
            ControllingObstacle = controllingObstacle;
        }

        public GridCell Cell { get; }
        public double? ClearHeightMillimeters { get; }
        public double ThresholdMillimeters { get; }
        public double? DifferenceMillimeters => ClearHeightMillimeters.HasValue ? ClearHeightMillimeters.Value - ThresholdMillimeters : (double?)null;
        public RiskLevel RiskLevel { get; }
        public ObstacleProjection ControllingObstacle { get; }
    }
}
