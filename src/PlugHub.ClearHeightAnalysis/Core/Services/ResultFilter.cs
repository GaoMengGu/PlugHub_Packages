#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using PlugHub.ClearHeightAnalysis.Core.Models;

namespace PlugHub.ClearHeightAnalysis.Core.Services
{
    public sealed class ResultFilterCriteria
    {
        public CellStatus? Status { get; set; }
        public GeometryConfidence? Confidence { get; set; }
        public string? SourceModelName { get; set; }
        public string? CategoryName { get; set; }
        public IReadOnlyCollection<string>? MemberCellNumbers { get; set; }
    }

    public static class ResultFilter
    {
        public static IReadOnlyList<CellAnalysisResult> Apply(
            IEnumerable<CellAnalysisResult> results,
            IEnumerable<ObstacleSnapshot> obstacles,
            ResultFilterCriteria criteria)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            if (obstacles == null) throw new ArgumentNullException(nameof(obstacles));
            if (criteria == null) throw new ArgumentNullException(nameof(criteria));
            var byKey = obstacles.ToDictionary(item => item.Key, StringComparer.Ordinal);
            HashSet<string>? members = criteria.MemberCellNumbers == null
                ? null : new HashSet<string>(criteria.MemberCellNumbers, StringComparer.Ordinal);
            return results.Where(result =>
            {
                if (criteria.Status.HasValue && result.Status != criteria.Status.Value) return false;
                if (criteria.Confidence.HasValue && result.Confidence != criteria.Confidence.Value) return false;
                if (members != null && !members.Contains(result.Cell.Number)) return false;
                ObstacleSnapshot? obstacle = null;
                if (!string.IsNullOrWhiteSpace(result.ControllingObstacleKey))
                    byKey.TryGetValue(result.ControllingObstacleKey!, out obstacle);
                if (!string.IsNullOrWhiteSpace(criteria.SourceModelName) && obstacle?.SourceModelName != criteria.SourceModelName) return false;
                if (!string.IsNullOrWhiteSpace(criteria.CategoryName) && obstacle?.CategoryName != criteria.CategoryName) return false;
                return true;
            }).OrderBy(result => result.Cell.Row).ThenBy(result => result.Cell.Column).ToList().AsReadOnly();
        }
    }
}
