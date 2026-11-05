#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PlugHub.ClearHeightAnalysis.Core.Geometry;
using PlugHub.ClearHeightAnalysis.Core.Models;

namespace PlugHub.ClearHeightAnalysis.Core.Services
{
    public static class GridDomainBuilder
    {
        public static IReadOnlyList<GridCellData> Build(
            AnalysisBoundary boundary,
            double gridSizeMillimeters,
            string levelUniqueId,
            string levelName,
            double levelElevationMillimeters)
        {
            return Build(boundary, gridSizeMillimeters, levelUniqueId, levelName,
                levelElevationMillimeters, CancellationToken.None, null);
        }

        public static IReadOnlyList<GridCellData> Build(
            AnalysisBoundary boundary, double gridSizeMillimeters, string levelUniqueId,
            string levelName, double levelElevationMillimeters,
            CancellationToken cancellationToken, IProgress<AnalysisProgress>? progress)
        {
            if (boundary == null)
            {
                throw new ArgumentNullException(nameof(boundary));
            }

            if (gridSizeMillimeters <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gridSizeMillimeters), "网格尺寸必须大于零。");
            }

            double originX = Math.Floor(boundary.MinX / gridSizeMillimeters) * gridSizeMillimeters;
            double originY = Math.Floor(boundary.MinY / gridSizeMillimeters) * gridSizeMillimeters;
            double rawColumns=Math.Ceiling((boundary.MaxX-originX)/gridSizeMillimeters);
            double rawRows=Math.Ceiling((boundary.MaxY-originY)/gridSizeMillimeters);
            if(rawColumns>int.MaxValue||rawRows>int.MaxValue||double.IsInfinity(rawColumns)||double.IsInfinity(rawRows))
                throw new OverflowException("网格行列数超过可表达范围，请增大网格尺寸或缩小分析范围。");
            int columnCount = checked((int)rawColumns);
            int rowCount = checked((int)rawRows);
            var cells = new List<GridCellData>();
            long total = checked((long)columnCount * rowCount);
            long completed = 0;
            progress?.Report(new AnalysisProgress("生成有效网格", 0, total));

            for (int row = 0; row < rowCount; row++)
            {
                double minY = originY + row * gridSizeMillimeters;
                double maxY = minY + gridSizeMillimeters;
                for (int column = 0; column < columnCount; column++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    double minX = originX + column * gridSizeMillimeters;
                    double maxX = minX + gridSizeMillimeters;
                    List<RegionFragment> fragments = BuildEffectiveRegions(boundary, minX, minY, maxX, maxY);
                    if (fragments.Count == 0)
                    {
                        completed++;
                        progress?.Report(new AnalysisProgress("生成有效网格", completed, total));
                        continue;
                    }

                    string number = levelName + "-" + (cells.Count + 1).ToString("000000");
                    cells.Add(new GridCellData(
                        number,
                        column,
                        row,
                        minX,
                        minY,
                        maxX,
                        maxY,
                        levelUniqueId,
                        levelName,
                        levelElevationMillimeters,
                        fragments.Select(fragment => fragment.RegionIndex),
                        fragments.SelectMany(fragment => fragment.EffectiveRegions)));
                    completed++;
                    progress?.Report(new AnalysisProgress("生成有效网格", completed, total));
                }
            }

            return cells.AsReadOnly();
        }

        private static List<RegionFragment> BuildEffectiveRegions(
            AnalysisBoundary boundary,
            double minX,
            double minY,
            double maxX,
            double maxY)
        {
            var fragments = new List<RegionFragment>();
            for (int index = 0; index < boundary.Regions.Count; index++)
            {
                BoundaryRegion region = boundary.Regions[index];
                IReadOnlyList<EffectiveCellRegion> effectiveRegions =
                    EffectiveRegionBuilder.Build(region, minX, minY, maxX, maxY);
                if (effectiveRegions.Count > 0)
                {
                    fragments.Add(new RegionFragment(index, effectiveRegions));
                }
            }

            return fragments;
        }

        private sealed class RegionFragment
        {
            public RegionFragment(int regionIndex, IReadOnlyList<EffectiveCellRegion> effectiveRegions)
            {
                RegionIndex = regionIndex;
                EffectiveRegions = effectiveRegions;
            }

            public int RegionIndex { get; }
            public IReadOnlyList<EffectiveCellRegion> EffectiveRegions { get; }
        }
    }
}
