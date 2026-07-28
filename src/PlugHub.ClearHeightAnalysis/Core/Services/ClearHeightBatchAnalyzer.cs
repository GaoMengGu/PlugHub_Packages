using System;
using System.Collections.Generic;
using System.Linq;
using PlugHub.ClearHeightAnalysis.Core.Models;

namespace PlugHub.ClearHeightAnalysis.Core.Services
{
    public sealed class ClearHeightBatchAnalyzer
    {
        private readonly ObstacleSpatialIndex _index;

        public ClearHeightBatchAnalyzer(
            double originX,
            double originY,
            double gridSizeMillimeters,
            IEnumerable<ObstacleSnapshot> obstacles)
        {
            _index = new ObstacleSpatialIndex(originX, originY, gridSizeMillimeters, obstacles);
        }

        public CoreAnalysisSummary Analyze(
            IEnumerable<GridCellData> cells,
            CoreAnalysisSettings settings)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            long candidateVisitCount = 0;
            var results = new List<CellAnalysisResult>();
            foreach (GridCellData cell in cells.OrderBy(cell => cell.Row).ThenBy(cell => cell.Column))
            {
                IReadOnlyList<ObstacleSnapshot> candidates = _index.Query(cell);
                candidateVisitCount += candidates.Count;
                results.Add(ClearHeightEngine.Calculate(cell, candidates, settings));
            }

            return new CoreAnalysisSummary(results, candidateVisitCount);
        }
    }
}
