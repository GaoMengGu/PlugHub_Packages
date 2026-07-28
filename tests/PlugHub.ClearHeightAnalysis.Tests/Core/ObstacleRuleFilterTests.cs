using PlugHub.ClearHeightAnalysis.Core.Models;
using PlugHub.ClearHeightAnalysis.Core.Services;
using Xunit;

namespace PlugHub.ClearHeightAnalysis.Tests.Core
{
    public sealed class ObstacleRuleFilterTests
    {
        private static readonly CoreAnalysisSettings Settings = new CoreAnalysisSettings(0, 0, 6000, 3000);

        [Fact]
        public void CurrentFloorIsExcludedButUpperSlabIsIncluded()
        {
            Assert.False(ObstacleRuleFilter.ShouldIncludeOverhead(-150, 0, Settings));
            Assert.True(ObstacleRuleFilter.ShouldIncludeOverhead(3000, 3300, Settings));
        }

        [Fact]
        public void ObstacleAboveSearchHeightIsExcluded()
        {
            Assert.False(ObstacleRuleFilter.ShouldIncludeOverhead(6100, 6500, Settings));
        }

        [Fact]
        public void ColumnCrossingFinishedFloorIsBlocked()
        {
            Assert.Equal(
                ObstacleKind.Blocked,
                ObstacleRuleFilter.ClassifyKind(ObstacleSemanticCategory.StructuralColumn, -500, 4000, Settings));
        }

        [Fact]
        public void ColumnEntirelyAboveFinishedFloorIsIgnored()
        {
            Assert.Null(ObstacleRuleFilter.ClassifyKind(
                ObstacleSemanticCategory.StructuralColumn, 1000, 4000, Settings));
        }

        [Theory]
        [InlineData(49, 50, false)]
        [InlineData(50, 50, true)]
        [InlineData(100, 50, true)]
        public void PipeDiameterFilterUsesConfiguredMillimetres(double diameter, double minimum, bool expected)
        {
            Assert.Equal(expected, ObstacleRuleFilter.ShouldIncludePipe(diameter, minimum));
        }
    }
}
