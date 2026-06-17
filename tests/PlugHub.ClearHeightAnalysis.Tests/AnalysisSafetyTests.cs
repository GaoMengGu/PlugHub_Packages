using System.Collections.Generic;
using System.Linq;
using PlugHub.ClearHeightAnalysis.Models;
using PlugHub.ClearHeightAnalysis.Services;
using Xunit;

namespace PlugHub.ClearHeightAnalysis.Tests
{
    public sealed class AnalysisSafetyTests
    {
        [Fact]
        public void ShouldIncludeOverheadObstacleExcludesCurrentFloorAtAnalysisLevel()
        {
            var settings = new AnalysisSettings
            {
                LevelElevationMillimeters = 0,
                SearchHeightMillimeters = 6000
            };

            bool includeCurrentFloor = ObstacleVerticalFilter.ShouldIncludeOverheadObstacle(-150, 0, settings);
            bool includeUpperSlab = ObstacleVerticalFilter.ShouldIncludeOverheadObstacle(3000, 3300, settings);

            Assert.False(includeCurrentFloor);
            Assert.True(includeUpperSlab);
        }

        [Fact]
        public void LimitForRenderingKeepsWorstResultsAndCapsFilledRegionCount()
        {
            var settings = new AnalysisSettings
            {
                MaximumRenderedCells = 2,
                RenderOnlyProblemCells = true
            };
            var results = new List<ClearHeightResult>
            {
                CreateResult("passed", RiskLevel.Passed),
                CreateResult("warning", RiskLevel.Warning),
                CreateResult("severe", RiskLevel.Severe),
                CreateResult("insufficient", RiskLevel.Insufficient)
            };

            List<ClearHeightResult> renderable = HeatmapResultLimiter.LimitForRendering(results, settings).ToList();

            Assert.Equal(2, renderable.Count);
            Assert.Equal("severe", renderable[0].Cell.Number);
            Assert.Equal("insufficient", renderable[1].Cell.Number);
        }

        private static ClearHeightResult CreateResult(string number, RiskLevel riskLevel)
        {
            var cell = new GridCell(number, new Rect2d(0, 0, 1000, 1000), "1F", 0);
            return new ClearHeightResult(cell, 2500, 3000, riskLevel, null);
        }
    }
}
