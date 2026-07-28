#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using PlugHub.ClearHeightAnalysis.Core.Geometry;

namespace PlugHub.ClearHeightAnalysis.Core.Models
{
    public sealed class RiskRegion
    {
        public RiskRegion(
            string number,
            CellStatus status,
            PolygonLoop2d outerLoop,
            IEnumerable<PolygonLoop2d> holes,
            Point2d markerPoint,
            IEnumerable<CellAnalysisResult> memberResults)
        {
            Number = string.IsNullOrWhiteSpace(number) ? throw new ArgumentException("区域编号不能为空。", nameof(number)) : number;
            Status = status;
            OuterLoop = outerLoop ?? throw new ArgumentNullException(nameof(outerLoop));
            Holes = (holes ?? throw new ArgumentNullException(nameof(holes))).ToList().AsReadOnly();
            MarkerPoint = markerPoint;
            MemberResults = (memberResults ?? throw new ArgumentNullException(nameof(memberResults)))
                .OrderBy(result => result.Cell.Row).ThenBy(result => result.Cell.Column).ToList().AsReadOnly();
            if (MemberResults.Count == 0) throw new ArgumentException("风险区域必须包含网格。", nameof(memberResults));
            MemberCells = MemberResults.Select(result => result.Cell).ToList().AsReadOnly();
            MinimumClearHeightMillimeters = MemberResults.Where(result => result.ClearHeightMillimeters.HasValue)
                .Select(result => result.ClearHeightMillimeters!.Value).DefaultIfEmpty().Min();
            if (!MemberResults.Any(result => result.ClearHeightMillimeters.HasValue)) MinimumClearHeightMillimeters = null;
            GeometryConfidence[] confidence = MemberResults.Where(result => result.Confidence.HasValue)
                .Select(result => result.Confidence!.Value).ToArray();
            WorstConfidence = confidence.Length == 0 ? (GeometryConfidence?)null : confidence.Max();
            ControllingObstacleKeys = MemberResults.Select(result => result.ControllingObstacleKey)
                .Where(key => !string.IsNullOrWhiteSpace(key)).Cast<string>().Distinct(StringComparer.Ordinal)
                .OrderBy(key => key, StringComparer.Ordinal).ToList().AsReadOnly();
        }

        public string Number { get; }
        public CellStatus Status { get; }
        public PolygonLoop2d OuterLoop { get; }
        public IReadOnlyList<PolygonLoop2d> Holes { get; }
        public Point2d MarkerPoint { get; }
        public IReadOnlyList<CellAnalysisResult> MemberResults { get; }
        public IReadOnlyList<GridCellData> MemberCells { get; }
        public double? MinimumClearHeightMillimeters { get; private set; }
        public GeometryConfidence? WorstConfidence { get; }
        public IReadOnlyList<string> ControllingObstacleKeys { get; }
    }
}
