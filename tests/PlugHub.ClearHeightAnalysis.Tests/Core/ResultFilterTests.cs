using System.Linq;
using PlugHub.ClearHeightAnalysis.Core.Models;
using PlugHub.ClearHeightAnalysis.Core.Services;
using Xunit;

namespace PlugHub.ClearHeightAnalysis.Tests.Core
{
    public sealed class ResultFilterTests
    {
        [Fact]
        public void FiltersByStatusConfidenceSourceCategoryAndMemberCells()
        {
            AnalysisBatch batch=AnalysisBatchSerializerTestsAccessor.CreateBatchForOutputs();
            var criteria=new ResultFilterCriteria
            {
                Status=CellStatus.Insufficient,
                Confidence=GeometryConfidence.Exact,
                SourceModelName="当前模型",
                CategoryName="风管",
                MemberCellNumbers=new[]{"一层-1"}
            };

            CellAnalysisResult result=Assert.Single(ResultFilter.Apply(batch.RunData.Summary.Results,batch.RunData.Obstacles,criteria));
            Assert.Equal("一层-1",result.Cell.Number);
        }

        [Fact]
        public void EmptyCriteriaReturnsAllResults()
        {
            AnalysisBatch batch=AnalysisBatchSerializerTestsAccessor.CreateBatchForOutputs();
            Assert.Equal(2,ResultFilter.Apply(batch.RunData.Summary.Results,batch.RunData.Obstacles,new ResultFilterCriteria()).Count);
        }
    }
}
