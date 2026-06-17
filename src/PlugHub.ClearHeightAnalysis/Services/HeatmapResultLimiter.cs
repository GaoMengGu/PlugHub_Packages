using System.Collections.Generic;
using System.Linq;
using PlugHub.ClearHeightAnalysis.Models;

namespace PlugHub.ClearHeightAnalysis.Services
{
    public static class HeatmapResultLimiter
    {
        public static IReadOnlyList<ClearHeightResult> LimitForRendering(IEnumerable<ClearHeightResult> results, AnalysisSettings settings)
        {
            IEnumerable<ClearHeightResult> renderable = results;
            if (settings.RenderOnlyProblemCells)
            {
                renderable = renderable.Where(result => result.RiskLevel != RiskLevel.Passed);
            }

            return renderable
                .OrderBy(result => GetRiskSortOrder(result.RiskLevel))
                .ThenBy(result => result.ClearHeightMillimeters ?? double.MaxValue)
                .ThenBy(result => result.Cell.Number)
                .Take(settings.MaximumRenderedCells)
                .ToList();
        }

        private static int GetRiskSortOrder(RiskLevel riskLevel)
        {
            switch (riskLevel)
            {
                case RiskLevel.Severe:
                    return 0;
                case RiskLevel.Insufficient:
                    return 1;
                case RiskLevel.Warning:
                    return 2;
                case RiskLevel.Unknown:
                    return 3;
                default:
                    return 4;
            }
        }
    }
}
