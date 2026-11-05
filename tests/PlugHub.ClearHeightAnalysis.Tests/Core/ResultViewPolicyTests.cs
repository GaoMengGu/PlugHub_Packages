using PlugHub.ClearHeightAnalysis.Core.Services;
using Xunit;

namespace PlugHub.ClearHeightAnalysis.Tests.Core
{
    public sealed class ResultViewPolicyTests
    {
        [Theory]
        [InlineData("FloorPlan")]
        [InlineData("EngineeringPlan")]
        [InlineData("AreaPlan")]
        public void AllowsOnlyApprovedPlanViewTypes(string viewType)
        {
            Assert.True(ResultViewPolicy.IsSupported(viewType));
        }

        [Theory]
        [InlineData("CeilingPlan")]
        [InlineData("Section")]
        [InlineData("Elevation")]
        [InlineData("ThreeD")]
        [InlineData("DraftingView")]
        public void RejectsViewsWithoutApprovedFloorPlaneSemantics(string viewType)
        {
            Assert.False(ResultViewPolicy.IsSupported(viewType));
        }
    }
}
