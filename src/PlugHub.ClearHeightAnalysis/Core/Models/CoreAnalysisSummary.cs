using System;
using System.Collections.Generic;
using System.Linq;

namespace PlugHub.ClearHeightAnalysis.Core.Models
{
    public sealed class CoreAnalysisSummary
    {
        private readonly IReadOnlyDictionary<CellStatus, int> _statusCounts;

        public CoreAnalysisSummary(
            IEnumerable<CellAnalysisResult> results,
            long candidateVisitCount)
        {
            if (results == null)
            {
                throw new ArgumentNullException(nameof(results));
            }

            if (candidateVisitCount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(candidateVisitCount));
            }

            List<CellAnalysisResult> values = results.ToList();
            Results = values.AsReadOnly();
            CandidateVisitCount = candidateVisitCount;
            _statusCounts = values
                .GroupBy(result => result.Status)
                .ToDictionary(group => group.Key, group => group.Count());
        }

        public IReadOnlyList<CellAnalysisResult> Results { get; }
        public long CandidateVisitCount { get; }

        public int Count(CellStatus status)
        {
            return _statusCounts.TryGetValue(status, out int count) ? count : 0;
        }
    }
}
