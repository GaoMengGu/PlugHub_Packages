#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlugHub.ClearHeightAnalysis.Core.Models
{
    public sealed class AnalysisRequest
    {
        public AnalysisRequest(
            IEnumerable<AnalysisLevelChoice> levels,
            IEnumerable<SourceModelChoice> sourceModels)
        {
            if (levels == null) throw new ArgumentNullException(nameof(levels));
            if (sourceModels == null) throw new ArgumentNullException(nameof(sourceModels));
            Levels = levels.ToList().AsReadOnly();
            SourceModels = sourceModels.ToList().AsReadOnly();
        }

        public IReadOnlyList<AnalysisLevelChoice> Levels { get; }
        public IReadOnlyList<SourceModelChoice> SourceModels { get; }
        public AnalysisLevelChoice? SelectedLevel { get; set; }
        public AnalysisBoundaryMode BoundaryMode { get; set; } = AnalysisBoundaryMode.SelectFloor;
        public double GridSizeMillimeters { get; set; } = 1000;
        public double ClearHeightThresholdMillimeters { get; set; } = 3000;
        public double FinishFloorOffsetMillimeters { get; set; }
        public double SearchHeightMillimeters { get; set; } = 6000;
        public double MinimumPipeDiameterMillimeters { get; set; } = 50;
        public bool IncludeCeilings { get; set; } = true;
        public bool IncludeMep { get; set; } = true;
    }
}
