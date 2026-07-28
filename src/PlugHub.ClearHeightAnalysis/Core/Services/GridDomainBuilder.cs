using System;
using System.Collections.Generic;
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
            int columnCount = (int)Math.Ceiling((boundary.MaxX - originX) / gridSizeMillimeters);
            int rowCount = (int)Math.Ceiling((boundary.MaxY - originY) / gridSizeMillimeters);
            var cells = new List<GridCellData>();

            for (int row = 0; row < rowCount; row++)
            {
                double minY = originY + row * gridSizeMillimeters;
                double maxY = minY + gridSizeMillimeters;
                for (int column = 0; column < columnCount; column++)
                {
                    double minX = originX + column * gridSizeMillimeters;
                    double maxX = minX + gridSizeMillimeters;
                    List<int> regionIndexes = FindIntersectingRegions(boundary, minX, minY, maxX, maxY);
                    if (regionIndexes.Count == 0)
                    {
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
                        regionIndexes));
                }
            }

            return cells.AsReadOnly();
        }

        private static List<int> FindIntersectingRegions(
            AnalysisBoundary boundary,
            double minX,
            double minY,
            double maxX,
            double maxY)
        {
            var indexes = new List<int>();
            for (int index = 0; index < boundary.Regions.Count; index++)
            {
                BoundaryRegion region = boundary.Regions[index];
                if (!PolygonMath.IntersectsRectangle(region.Outer, minX, minY, maxX, maxY))
                {
                    continue;
                }

                bool coveredByHole = false;
                foreach (PolygonLoop2d hole in region.Holes)
                {
                    if (RectangleIsInsideLoop(hole, minX, minY, maxX, maxY))
                    {
                        coveredByHole = true;
                        break;
                    }
                }

                if (!coveredByHole)
                {
                    indexes.Add(index);
                }
            }

            return indexes;
        }

        private static bool RectangleIsInsideLoop(
            PolygonLoop2d loop,
            double minX,
            double minY,
            double maxX,
            double maxY)
        {
            return PolygonMath.ContainsPoint(loop, new Point2d(minX, minY)) &&
                   PolygonMath.ContainsPoint(loop, new Point2d(maxX, minY)) &&
                   PolygonMath.ContainsPoint(loop, new Point2d(maxX, maxY)) &&
                   PolygonMath.ContainsPoint(loop, new Point2d(minX, maxY));
        }
    }
}
