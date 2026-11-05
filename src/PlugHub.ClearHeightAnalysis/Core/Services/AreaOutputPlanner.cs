using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using PlugHub.ClearHeightAnalysis.Core.Geometry;
using PlugHub.ClearHeightAnalysis.Core.Models;

namespace PlugHub.ClearHeightAnalysis.Core.Services
{
    public static class AreaOutputPlanner
    {
        public static AreaOutputPlan Build(IEnumerable<CellAnalysisResult> results, AreaOutputMode mode, bool includePassed = false)
        {
            if (results == null) throw new ArgumentNullException(nameof(results));
            List<CellAnalysisResult> included = results.Where(result => includePassed || result.Status != CellStatus.Passed).ToList();
            List<AreaOutputRegion> areas;
            if (mode == AreaOutputMode.MergedRegions)
            {
                areas = RiskRegionBuilder.Build(included, includePassed).Select(region => new AreaOutputRegion(
                    region.Number, region.Status, region.OuterLoop, region.Holes, region.MarkerPoint,
                    region.MemberCells.Select(cell => cell.Number), region.MinimumClearHeightMillimeters)).ToList();
            }
            else
            {
                areas = included.OrderBy(result => result.Cell.Row).ThenBy(result => result.Cell.Column)
                    .SelectMany(result => result.Cell.EffectiveRegions.Select(region => new { Result = result, Region = region }))
                    .Select((item, index) => new AreaOutputRegion(
                        "C-" + (index + 1).ToString("000000", CultureInfo.InvariantCulture),
                        item.Result.Status, item.Region.Outer, item.Region.Holes,
                        EffectiveRegionBuilder.FindRepresentativePoint(item.Region),
                        new[] { item.Result.Cell.Number }, item.Result.ClearHeightMillimeters)).ToList();
            }
            return new AreaOutputPlan(mode, areas, CountUniqueSegments(areas));
        }

        private static int CountUniqueSegments(IEnumerable<AreaOutputRegion> areas)
        {
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (PolygonLoop2d loop in areas.SelectMany(area => new[] { area.OuterLoop }.Concat(area.Holes)))
            for (int index = 0; index < loop.Points.Count; index++)
            {
                Point2d a = loop.Points[index];
                Point2d b = loop.Points[(index + 1) % loop.Points.Count];
                string first = PointKey(a);
                string second = PointKey(b);
                keys.Add(string.CompareOrdinal(first, second) <= 0 ? first + "|" + second : second + "|" + first);
            }
            return keys.Count;
        }

        private static string PointKey(Point2d point) => point.X.ToString("R", CultureInfo.InvariantCulture) + "," + point.Y.ToString("R", CultureInfo.InvariantCulture);
    }
}
