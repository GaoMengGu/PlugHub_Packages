using System.Collections.Generic;
using System.Linq;
using PlugHub.ClearHeightAnalysis.Models;

namespace PlugHub.ClearHeightAnalysis.Services
{
    public static class ClearHeightCalculator
    {
        public static ClearHeightResult Calculate(GridCell cell, IEnumerable<ObstacleProjection> coveringObstacles, AnalysisSettings settings)
        {
            ObstacleProjection controllingObstacle = coveringObstacles
                .OrderBy(obstacle => obstacle.BottomElevationMillimeters)
                .FirstOrDefault();

            if (controllingObstacle == null)
            {
                return new ClearHeightResult(cell, null, settings.ClearHeightThresholdMillimeters, RiskLevel.Unknown, null);
            }

            double clearHeight = controllingObstacle.BottomElevationMillimeters - cell.LevelElevationMillimeters - settings.FinishFloorOffsetMillimeters;
            RiskLevel riskLevel = Classify(clearHeight, settings.ClearHeightThresholdMillimeters);
            return new ClearHeightResult(cell, clearHeight, settings.ClearHeightThresholdMillimeters, riskLevel, controllingObstacle);
        }

        public static RiskLevel Classify(double clearHeightMillimeters, double thresholdMillimeters)
        {
            if (clearHeightMillimeters < thresholdMillimeters - 300)
            {
                return RiskLevel.Severe;
            }

            if (clearHeightMillimeters < thresholdMillimeters)
            {
                return RiskLevel.Insufficient;
            }

            if (clearHeightMillimeters < thresholdMillimeters + 300)
            {
                return RiskLevel.Warning;
            }

            return RiskLevel.Passed;
        }
    }
}
