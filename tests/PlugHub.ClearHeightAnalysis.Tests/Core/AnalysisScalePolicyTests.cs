using System;
using PlugHub.ClearHeightAnalysis.Core.Geometry;
using PlugHub.ClearHeightAnalysis.Core.Models;
using PlugHub.ClearHeightAnalysis.Core.Services;
using Xunit;

namespace PlugHub.ClearHeightAnalysis.Tests.Core
{
    public sealed class AnalysisScalePolicyTests
    {
        [Theory]
        [InlineData(100, AnalysisScaleDecision.Allow)]
        [InlineData(20_001, AnalysisScaleDecision.RequireConfirmation)]
        [InlineData(100_001, AnalysisScaleDecision.BlockUnlessAdvancedOverride)]
        public void AppliesCentralSoftAndHardLimits(long cells, AnalysisScaleDecision expected)
        {
            Assert.Equal(expected, AnalysisScalePolicy.Decide(cells));
        }

        [Fact]
        public void EstimateUsesLongArithmeticForLargeBounds()
        {
            var boundary = new AnalysisBoundary(new[]
            {
                new BoundaryRegion(TestGeometry.Rectangle(0, 0, 3_000_000_000, 3_000_000_000),
                    Array.Empty<PolygonLoop2d>())
            });

            AnalysisScaleEstimate estimate = AnalysisScalePolicy.Estimate(boundary, 1);

            Assert.Equal(3_000_000_000L, estimate.ColumnCount);
            Assert.Equal(3_000_000_000L, estimate.RowCount);
            Assert.Equal(9_000_000_000_000_000_000L, estimate.CandidateCellCount);
            Assert.Equal(AnalysisScaleDecision.BlockUnlessAdvancedOverride, estimate.Decision);
        }

        [Fact]
        public void GridBuilderRejectsDimensionsThatCannotBeRepresentedByCellIndexes()
        {
            var boundary = new AnalysisBoundary(new[]
            {
                new BoundaryRegion(TestGeometry.Rectangle(0, 0, 3_000_000_000, 1),
                    Array.Empty<PolygonLoop2d>())
            });

            Assert.Throws<OverflowException>(() => GridDomainBuilder.Build(
                boundary, 1, "level", "L", 0));
        }
    }
}
