using System;
using System.Collections.Generic;
using PlugHub.ClearHeightAnalysis.Models;

namespace PlugHub.ClearHeightAnalysis.Services
{
    public static class GridBuilder
    {
        public static IEnumerable<GridCell> BuildInsideRectangle(Rect2d boundary, double gridSizeMillimeters, string levelName, double levelElevationMillimeters)
        {
            return BuildInsideRectangles(boundary, new[] { boundary }, gridSizeMillimeters, levelName, levelElevationMillimeters);
        }

        public static IEnumerable<GridCell> BuildInsideRectangles(Rect2d boundary, IReadOnlyCollection<Rect2d> masks, double gridSizeMillimeters, string levelName, double levelElevationMillimeters)
        {
            if (gridSizeMillimeters <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gridSizeMillimeters), "网格尺寸必须大于 0。");
            }

            int index = 1;
            for (double y = boundary.MinY; y < boundary.MaxY; y += gridSizeMillimeters)
            {
                for (double x = boundary.MinX; x < boundary.MaxX; x += gridSizeMillimeters)
                {
                    var cellBounds = new Rect2d(x, y, Math.Min(x + gridSizeMillimeters, boundary.MaxX), Math.Min(y + gridSizeMillimeters, boundary.MaxY));
                    if (!IsCellInsideAnyMask(cellBounds, masks))
                    {
                        continue;
                    }

                    yield return new GridCell(levelName + "-" + index.ToString("000"), cellBounds, levelName, levelElevationMillimeters);
                    index++;
                }
            }
        }

        private static bool IsCellInsideAnyMask(Rect2d cellBounds, IEnumerable<Rect2d> masks)
        {
            foreach (Rect2d mask in masks)
            {
                if (mask.ContainsCenterOf(cellBounds))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
