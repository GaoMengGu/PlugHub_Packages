using System;
using System.Collections.Generic;
using System.Linq;

namespace PlugHub.ClearHeightAnalysis.Core.Models
{
    public sealed class AnalysisRunData
    {
        public AnalysisRunData(
            AnalysisRequest request,
            AnalysisBoundary boundary,
            IEnumerable<GridCellData> cells,
            IEnumerable<ObstacleSnapshot> obstacles,
            CoreAnalysisSettings settings,
            CoreAnalysisSummary summary)
        {
            Request = request ?? throw new ArgumentNullException(nameof(request));
            Boundary = boundary ?? throw new ArgumentNullException(nameof(boundary));
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Summary = summary ?? throw new ArgumentNullException(nameof(summary));
            Cells = (cells ?? throw new ArgumentNullException(nameof(cells))).ToList().AsReadOnly();
            Obstacles = (obstacles ?? throw new ArgumentNullException(nameof(obstacles))).ToList().AsReadOnly();
            if (Cells.Count != Summary.Results.Count ||
                !Cells.Select(cell => cell.Number).SequenceEqual(Summary.Results.Select(result => result.Cell.Number)))
                throw new ArgumentException("汇总结果与分析网格不一致。", nameof(summary));
        }

        public AnalysisRequest Request { get; }
        public AnalysisBoundary Boundary { get; }
        public IReadOnlyList<GridCellData> Cells { get; }
        public IReadOnlyList<ObstacleSnapshot> Obstacles { get; }
        public CoreAnalysisSettings Settings { get; }
        public CoreAnalysisSummary Summary { get; }
    }
}
