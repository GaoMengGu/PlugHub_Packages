using System;

namespace PlugHub.ClearHeightAnalysis.Core.Models
{
    public sealed class AnalysisProgress
    {
        public AnalysisProgress(string stage,long completed,long total)
        { if(string.IsNullOrWhiteSpace(stage))throw new ArgumentException("阶段不能为空。",nameof(stage));if(completed<0||total<0||completed>total)throw new ArgumentOutOfRangeException(nameof(completed));Stage=stage;Completed=completed;Total=total; }
        public string Stage{get;} public long Completed{get;} public long Total{get;} public double Fraction=>Total==0?1:(double)Completed/Total;
    }
}
