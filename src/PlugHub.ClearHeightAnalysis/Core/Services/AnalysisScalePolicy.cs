using System;
using PlugHub.ClearHeightAnalysis.Core.Models;

namespace PlugHub.ClearHeightAnalysis.Core.Services
{
    public static class AnalysisScalePolicy
    {
        public const long SoftLimit=20_000; public const long HardLimit=100_000;
        public static AnalysisScaleDecision Decide(long candidateCells)
        { if(candidateCells<0)throw new ArgumentOutOfRangeException(nameof(candidateCells));if(candidateCells>HardLimit)return AnalysisScaleDecision.BlockUnlessAdvancedOverride;if(candidateCells>SoftLimit)return AnalysisScaleDecision.RequireConfirmation;return AnalysisScaleDecision.Allow; }
        public static AnalysisScaleEstimate Estimate(AnalysisBoundary boundary,double gridSizeMillimeters)
        {
            if(boundary==null)throw new ArgumentNullException(nameof(boundary));if(gridSizeMillimeters<=0)throw new ArgumentOutOfRangeException(nameof(gridSizeMillimeters));
            double originX=Math.Floor(boundary.MinX/gridSizeMillimeters)*gridSizeMillimeters;double originY=Math.Floor(boundary.MinY/gridSizeMillimeters)*gridSizeMillimeters;
            long columns=CheckedCeiling((boundary.MaxX-originX)/gridSizeMillimeters);long rows=CheckedCeiling((boundary.MaxY-originY)/gridSizeMillimeters);
            long count;try{count=checked(columns*rows);}catch(OverflowException ex){throw new OverflowException("候选网格数量超过 64 位整数范围。",ex);}
            return new AnalysisScaleEstimate(columns,rows,count,Decide(count));
        }
        private static long CheckedCeiling(double value)
        { if(double.IsNaN(value)||double.IsInfinity(value)||value<0||value>long.MaxValue)throw new OverflowException("网格行列数超过 64 位整数范围。");return checked((long)Math.Ceiling(value)); }
    }
}
