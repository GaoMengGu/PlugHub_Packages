#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
            : this(originX, originY, gridSizeMillimeters, obstacles, CancellationToken.None, null)
        {
        }

        public ClearHeightBatchAnalyzer(
            double originX, double originY, double gridSizeMillimeters,
            IEnumerable<ObstacleSnapshot> obstacles, CancellationToken cancellationToken,
            IProgress<AnalysisProgress>? progress)
        {
            _index = new ObstacleSpatialIndex(originX, originY, gridSizeMillimeters, obstacles, cancellationToken, progress);
        }

        public CoreAnalysisSummary Analyze(
            IEnumerable<GridCellData> cells,
            CoreAnalysisSettings settings)
        {
            return Analyze(cells, settings, CancellationToken.None, null);
        }

        public CoreAnalysisSummary Analyze(
            IEnumerable<GridCellData> cells, CoreAnalysisSettings settings,
            CancellationToken cancellationToken, IProgress<AnalysisProgress>? progress)
        {
            if (cells == null)
            {
                throw new ArgumentNullException(nameof(cells));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            List<GridCellData> ordered = cells.OrderBy(cell => cell.Row).ThenBy(cell => cell.Column).ToList();
            long candidateVisitCount = 0;
            var results = new List<CellAnalysisResult>();
            progress?.Report(new AnalysisProgress("计算净高", 0, ordered.Count));
            foreach (GridCellData cell in ordered)
            {
                cancellationToken.ThrowIfCancellationRequested();
                IReadOnlyList<ObstacleSnapshot> candidates = _index.Query(cell);
                candidateVisitCount += candidates.Count;
                results.Add(ClearHeightEngine.Calculate(cell, candidates, settings));
                progress?.Report(new AnalysisProgress("计算净高", results.Count, ordered.Count));
            }

            return new CoreAnalysisSummary(results, candidateVisitCount);
        }
    }
}
