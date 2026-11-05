using System.Linq;
using PlugHub.ClearHeightAnalysis.Core.Models;
using PlugHub.ClearHeightAnalysis.Core.Services;
using Xunit;

namespace PlugHub.ClearHeightAnalysis.Tests.Core
{
    public sealed class AnalysisRequestValidatorTests
    {
        [Fact]
        public void ValidateRejectsMissingLevel()
        {
            AnalysisRequest request = ValidRequest();
            request.SelectedLevel = null;

            Assert.Contains(AnalysisRequestValidator.Validate(request), error => error.Contains("楼层"));
        }

        [Fact]
        public void ValidateRejectsNoSelectedSourceModel()
        {
            AnalysisRequest request = ValidRequest();
            request.SourceModels.ToList().ForEach(source => source.IsSelected = false);

            Assert.Contains(AnalysisRequestValidator.Validate(request), error => error.Contains("模型"));
        }

        [Fact]
        public void ValidateRejectsAutomaticBoundaryWithoutHostModel()
        {
            AnalysisRequest request = ValidRequest();
            request.BoundaryMode = AnalysisBoundaryMode.AutomaticHostFloors;
            request.SourceModels.Single(source => source.IsHost).IsSelected = false;

            Assert.Contains(AnalysisRequestValidator.Validate(request), error => error.Contains("当前模型"));
        }

        [Theory]
        [InlineData(99, 3000, 6000, 50)]
        [InlineData(1000, 999, 6000, 50)]
        [InlineData(1000, 3000, 999, 50)]
        [InlineData(1000, 3000, 6000, -1)]
        public void ValidateRejectsInvalidNumericSettings(
            double grid,
            double threshold,
            double searchHeight,
            double minimumPipeDiameter)
        {
            AnalysisRequest request = ValidRequest();
            request.GridSizeMillimeters = grid;
            request.ClearHeightThresholdMillimeters = threshold;
            request.SearchHeightMillimeters = searchHeight;
            request.MinimumPipeDiameterMillimeters = minimumPipeDiameter;

            Assert.NotEmpty(AnalysisRequestValidator.Validate(request));
        }

        [Fact]
        public void ValidateAcceptsACompleteTypedRequest()
        {
            Assert.Empty(AnalysisRequestValidator.Validate(ValidRequest()));
        }

        private static AnalysisRequest ValidRequest()
        {
            return new AnalysisRequest(
                new[] { new AnalysisLevelChoice("level-1", "1F", 0) },
                new[]
                {
                    new SourceModelChoice("host", "当前模型", true, true, true),
                    new SourceModelChoice("link-1", "结构链接", false, true, true)
                })
            {
                SelectedLevel = new AnalysisLevelChoice("level-1", "1F", 0),
                BoundaryMode = AnalysisBoundaryMode.SelectFloor,
                GridSizeMillimeters = 1000,
                ClearHeightThresholdMillimeters = 3000,
                FinishFloorOffsetMillimeters = 0,
                SearchHeightMillimeters = 6000,
                MinimumPipeDiameterMillimeters = 50,
                IncludeCeilings = true,
                IncludeMep = true
            };
        }
    }
}
