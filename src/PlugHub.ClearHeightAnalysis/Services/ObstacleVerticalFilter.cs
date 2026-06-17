using PlugHub.ClearHeightAnalysis.Models;

namespace PlugHub.ClearHeightAnalysis.Services
{
    public static class ObstacleVerticalFilter
    {
        public static bool ShouldIncludeOverheadObstacle(double bottomMillimeters, double topMillimeters, AnalysisSettings settings)
        {
            double analysisBase = settings.LevelElevationMillimeters + settings.FinishFloorOffsetMillimeters;
            double analysisTop = settings.LevelElevationMillimeters + settings.SearchHeightMillimeters;

            if (topMillimeters <= analysisBase + 100)
            {
                return false;
            }

            return bottomMillimeters <= analysisTop;
        }
    }
}
