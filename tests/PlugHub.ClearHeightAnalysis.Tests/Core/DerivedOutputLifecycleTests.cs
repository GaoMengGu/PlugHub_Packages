using System;
using PlugHub.ClearHeightAnalysis.Core.Models;
using PlugHub.ClearHeightAnalysis.Core.Services;
using Xunit;

namespace PlugHub.ClearHeightAnalysis.Tests.Core
{
    public sealed class DerivedOutputLifecycleTests
    {
        [Fact]
        public void ReplaceKeepsOtherTypesAndReplacesOnlyMatchingType()
        {
            var oldLine = Output("old-line", DerivedOutputType.LightweightDrawing);
            var csv = Output("csv", DerivedOutputType.Csv);
            var replacement = Output("new-line", DerivedOutputType.LightweightDrawing);

            DerivedOutputTransition transition = DerivedOutputLifecyclePlanner.Replace(
                new[] { oldLine, csv }, replacement);

            Assert.Equal(new[] { "old-line" }, transition.OutputsToDelete[0].OutputId == "old-line" ? new[] { "old-line" } : Array.Empty<string>());
            Assert.Contains(transition.NextOutputs, item => item.OutputId == "csv");
            Assert.Contains(transition.NextOutputs, item => item.OutputId == "new-line");
            Assert.DoesNotContain(transition.NextOutputs, item => item.OutputId == "old-line");
        }

        [Fact]
        public void DeleteToleratesPartiallyMissingElementIdsInState()
        {
            DerivedOutputRecord value = Output("line", DerivedOutputType.LightweightDrawing);
            DerivedOutputTransition transition = DerivedOutputLifecyclePlanner.Delete(new[] { value }, "line");
            Assert.Single(transition.OutputsToDelete);
            Assert.Empty(transition.NextOutputs);
        }

        [Fact]
        public void CreateInEmptyRegistryAddsOutputWithoutDeletingAnything()
        {
            DerivedOutputRecord created = Output("png", DerivedOutputType.RasterHeatmap);

            DerivedOutputTransition transition = DerivedOutputLifecyclePlanner.Replace(
                Array.Empty<DerivedOutputRecord>(), created);

            Assert.Empty(transition.OutputsToDelete);
            Assert.Same(created, Assert.Single(transition.NextOutputs));
        }

        [Theory]
        [InlineData(true, true, true, true)]
        [InlineData(false, true, true, false)]
        [InlineData(true, false, true, false)]
        [InlineData(true, true, false, false)]
        public void CommitRequiresEveryLifecycleStage(
            bool modelCreated, bool registryUpdated, bool filePromoted, bool expected)
        {
            Assert.Equal(expected, DerivedOutputLifecyclePlanner.ShouldCommit(
                modelCreated, registryUpdated, filePromoted));
        }

        private static DerivedOutputRecord Output(string id, DerivedOutputType type) =>
            new DerivedOutputRecord(id, type, DateTime.UtcNow, 1, null, new[] { 101, 999999 });
    }
}
