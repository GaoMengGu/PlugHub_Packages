using PlugHub.ClearHeightAnalysis.Core.Models;

namespace PlugHub.ClearHeightAnalysis.Core.Services
{
    public static class ObstacleRuleFilter
    {
        private const double FloorContactToleranceMillimeters = 100;

        public static bool ShouldIncludeOverhead(
            double bottomElevationMillimeters,
            double topElevationMillimeters,
            CoreAnalysisSettings settings)
        {
            double finishedFloor = settings.FinishedFloorElevationMillimeters;
            if (topElevationMillimeters <= finishedFloor + FloorContactToleranceMillimeters)
                return false;
            return bottomElevationMillimeters <=
                   settings.LevelElevationMillimeters + settings.SearchHeightMillimeters;
        }

        public static ObstacleKind? ClassifyKind(
            ObstacleSemanticCategory category,
            double bottomElevationMillimeters,
            double topElevationMillimeters,
            CoreAnalysisSettings settings)
        {
            if (category == ObstacleSemanticCategory.StructuralColumn)
            {
                double finishedFloor = settings.FinishedFloorElevationMillimeters;
                return bottomElevationMillimeters <= finishedFloor + FloorContactToleranceMillimeters &&
                       topElevationMillimeters > finishedFloor + FloorContactToleranceMillimeters
                    ? ObstacleKind.Blocked
                    : (ObstacleKind?)null;
            }

            return ShouldIncludeOverhead(bottomElevationMillimeters, topElevationMillimeters, settings)
                ? ObstacleKind.Overhead
                : (ObstacleKind?)null;
        }

        public static bool ShouldIncludePipe(double diameterMillimeters, double minimumDiameterMillimeters)
        {
            return diameterMillimeters >= minimumDiameterMillimeters;
        }
    }
}
