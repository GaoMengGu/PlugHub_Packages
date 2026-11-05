using System;

namespace PlugHub.ClearHeightAnalysis.Core.Models
{
    public sealed class CoreAnalysisSettings
    {
        public CoreAnalysisSettings(
            double levelElevationMillimeters,
            double finishFloorOffsetMillimeters,
            double searchHeightMillimeters,
            double clearHeightThresholdMillimeters)
        {
            if (finishFloorOffsetMillimeters < 0 || finishFloorOffsetMillimeters > 1000)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(finishFloorOffsetMillimeters),
                    "完成面偏移必须在 0-1000mm 之间。");
            }

            if (searchHeightMillimeters <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(searchHeightMillimeters), "搜索高度必须大于零。");
            }

            if (clearHeightThresholdMillimeters <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(clearHeightThresholdMillimeters), "净高阈值必须大于零。");
            }

            LevelElevationMillimeters = levelElevationMillimeters;
            FinishFloorOffsetMillimeters = finishFloorOffsetMillimeters;
            SearchHeightMillimeters = searchHeightMillimeters;
            ClearHeightThresholdMillimeters = clearHeightThresholdMillimeters;
        }

        public double LevelElevationMillimeters { get; }
        public double FinishFloorOffsetMillimeters { get; }
        public double SearchHeightMillimeters { get; }
        public double ClearHeightThresholdMillimeters { get; }
        public double FinishedFloorElevationMillimeters =>
            LevelElevationMillimeters + FinishFloorOffsetMillimeters;
    }
}
