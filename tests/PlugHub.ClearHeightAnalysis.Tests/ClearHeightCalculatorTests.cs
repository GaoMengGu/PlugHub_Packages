using System.Collections.Generic;
using PlugHub.ClearHeightAnalysis.Models;
using PlugHub.ClearHeightAnalysis.Services;
using Xunit;

namespace PlugHub.ClearHeightAnalysis.Tests
{
    public sealed class ClearHeightCalculatorTests
    {
        [Fact]
        public void CalculateUsesLowestObstacleBottomAsWorstCaseClearHeight()
        {
            var cell = new GridCell("1F-001", new Rect2d(0, 0, 1000, 1000), "1F", 0);
            var projections = new List<ObstacleProjection>
            {
                new ObstacleProjection("beam-a", "梁A", "结构梁", "结构模型", false, new Rect2d(0, 0, 1000, 500), 3200),
                new ObstacleProjection("duct-b", "风管B", "风管", "机电模型", true, new Rect2d(500, 0, 1000, 1000), 2800)
            };
            var settings = new AnalysisSettings
            {
                ClearHeightThresholdMillimeters = 3000,
                FinishFloorOffsetMillimeters = 50
            };

            ClearHeightResult result = ClearHeightCalculator.Calculate(cell, projections, settings);

            Assert.Equal(2750, result.ClearHeightMillimeters);
            Assert.Equal(RiskLevel.Insufficient, result.RiskLevel);
            Assert.Equal("duct-b", result.ControllingObstacle.ElementKey);
        }

        [Fact]
        public void CalculateMarksCellUnknownWhenNoObstacleCoversIt()
        {
            var cell = new GridCell("1F-001", new Rect2d(0, 0, 1000, 1000), "1F", 0);
            var settings = new AnalysisSettings { ClearHeightThresholdMillimeters = 3000 };

            ClearHeightResult result = ClearHeightCalculator.Calculate(cell, new List<ObstacleProjection>(), settings);

            Assert.Equal(RiskLevel.Unknown, result.RiskLevel);
            Assert.Null(result.ClearHeightMillimeters);
        }
    }
}
