#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using PlugHub.ClearHeightAnalysis.Core.Geometry;
using PlugHub.ClearHeightAnalysis.Core.Models;

namespace PlugHub.ClearHeightAnalysis.Core.Services
{
    public static class ClearHeightEngine
    {
        private const double FloorContactToleranceMillimeters = 100;
        private const double ElevationTieToleranceMillimeters = 0.001;

        public static CellAnalysisResult Calculate(
            GridCellData cell,
            IEnumerable<ObstacleSnapshot> candidates,
            CoreAnalysisSettings settings)
        {
            if (cell == null)
            {
                throw new ArgumentNullException(nameof(cell));
            }

            if (candidates == null)
            {
                throw new ArgumentNullException(nameof(candidates));
            }

            if (settings == null)
            {
                throw new ArgumentNullException(nameof(settings));
            }

            var overlapping = candidates
                .Select(candidate => new CandidateOverlap(candidate, cell.EffectiveRegions))
                .Where(item => item.HasOverlap)
                .ToList();

            double finishedFloor = settings.FinishedFloorElevationMillimeters;
            ObstacleSnapshot? blocker = overlapping
                .Select(item => item.Candidate)
                .Where(candidate => candidate.Kind == ObstacleKind.Blocked)
                .Where(candidate => candidate.BottomElevationMillimeters <= finishedFloor + FloorContactToleranceMillimeters)
                .Where(candidate => candidate.TopElevationMillimeters > finishedFloor + FloorContactToleranceMillimeters)
                .OrderByDescending(candidate => candidate.Confidence)
                .ThenBy(candidate => candidate.Key, StringComparer.Ordinal)
                .FirstOrDefault();

            if (blocker != null)
            {
                return new CellAnalysisResult(
                    cell,
                    null,
                    settings.ClearHeightThresholdMillimeters,
                    CellStatus.Blocked,
                    blocker.Key,
                    blocker.Confidence);
            }

            ObstacleSnapshot? controlling = null;
            double controllingBottom = double.MaxValue;
            foreach (CandidateOverlap item in overlapping)
            {
                ObstacleSnapshot candidate = item.Candidate;
                if (candidate.Kind != ObstacleKind.Overhead ||
                    candidate.TopElevationMillimeters <= finishedFloor + FloorContactToleranceMillimeters ||
                    candidate.BottomElevationMillimeters > settings.LevelElevationMillimeters + settings.SearchHeightMillimeters)
                {
                    continue;
                }

                double localBottom = item.MinimumBottom;
                bool isLower = localBottom < controllingBottom - ElevationTieToleranceMillimeters;
                bool isSameHeightWithEarlierKey =
                    Math.Abs(localBottom - controllingBottom) <= ElevationTieToleranceMillimeters &&
                    (controlling == null || string.CompareOrdinal(candidate.Key, controlling.Key) < 0);
                if (isLower || isSameHeightWithEarlierKey)
                {
                    controlling = candidate;
                    controllingBottom = localBottom;
                }
            }

            if (controlling == null)
            {
                return new CellAnalysisResult(
                    cell,
                    null,
                    settings.ClearHeightThresholdMillimeters,
                    CellStatus.Unknown,
                    null,
                    null);
            }

            double clearHeight = controllingBottom - finishedFloor;
            return new CellAnalysisResult(
                cell,
                clearHeight,
                settings.ClearHeightThresholdMillimeters,
                Classify(clearHeight, settings.ClearHeightThresholdMillimeters),
                controlling.Key,
                controlling.Confidence);
        }

        public static CellStatus Classify(double clearHeightMillimeters, double thresholdMillimeters)
        {
            if (clearHeightMillimeters < thresholdMillimeters - 300)
            {
                return CellStatus.Severe;
            }

            if (clearHeightMillimeters < thresholdMillimeters)
            {
                return CellStatus.Insufficient;
            }

            if (clearHeightMillimeters < thresholdMillimeters + 300)
            {
                return CellStatus.Warning;
            }

            return CellStatus.Passed;
        }

        private sealed class CandidateOverlap
        {
            public CandidateOverlap(ObstacleSnapshot candidate, IReadOnlyList<EffectiveCellRegion> regions)
            {
                Candidate = candidate;
                HasOverlap = candidate.Geometry.TryGetMinimumBottom(regions, out double minimum);
                MinimumBottom = minimum;
            }

            public ObstacleSnapshot Candidate { get; }
            public bool HasOverlap { get; }
            public double MinimumBottom { get; }
        }
    }
}
