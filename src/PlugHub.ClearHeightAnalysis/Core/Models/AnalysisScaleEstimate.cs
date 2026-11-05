namespace PlugHub.ClearHeightAnalysis.Core.Models
{
    public enum AnalysisScaleDecision { Allow, RequireConfirmation, BlockUnlessAdvancedOverride }

    public sealed class AnalysisScaleEstimate
    {
        public AnalysisScaleEstimate(long columns,long rows,long candidateCells,AnalysisScaleDecision decision)
        { ColumnCount=columns;RowCount=rows;CandidateCellCount=candidateCells;Decision=decision; }
        public long ColumnCount{get;} public long RowCount{get;} public long CandidateCellCount{get;} public AnalysisScaleDecision Decision{get;}
    }
}
