using System;
using System.Collections.Generic;
using PlugHub.ClearHeightAnalysis.Core.Models;

namespace PlugHub.ClearHeightAnalysis.Core.Services
{
    public sealed class ObstacleSpatialIndex
    {
        private const double MaximumEdgeToleranceMillimeters = 0.001;
        private readonly Dictionary<(int Column, int Row), List<ObstacleSnapshot>> _buckets =
            new Dictionary<(int Column, int Row), List<ObstacleSnapshot>>();

        public ObstacleSpatialIndex(
            double originX,
            double originY,
            double gridSizeMillimeters,
            IEnumerable<ObstacleSnapshot> obstacles)
        {
            if (gridSizeMillimeters <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(gridSizeMillimeters), "网格尺寸必须大于零。");
            }

            if (obstacles == null)
            {
                throw new ArgumentNullException(nameof(obstacles));
            }

            OriginX = originX;
            OriginY = originY;
            GridSizeMillimeters = gridSizeMillimeters;

            foreach (ObstacleSnapshot obstacle in obstacles)
            {
                if (obstacle == null)
                {
                    throw new ArgumentException("障碍物集合不能包含 null。", nameof(obstacles));
                }

                Register(obstacle);
            }
        }

        public double OriginX { get; }
        public double OriginY { get; }
        public double GridSizeMillimeters { get; }

        public IReadOnlyList<ObstacleSnapshot> Query(GridCellData cell)
        {
            if (cell == null)
            {
                throw new ArgumentNullException(nameof(cell));
            }

            if (_buckets.TryGetValue((cell.Column, cell.Row), out List<ObstacleSnapshot> values))
            {
                return values;
            }

            return Array.Empty<ObstacleSnapshot>();
        }

        private void Register(ObstacleSnapshot obstacle)
        {
            int minColumn = ToColumn(obstacle.MinX);
            int minRow = ToRow(obstacle.MinY);
            int maxColumn = ToColumn(Math.Max(obstacle.MinX, obstacle.MaxX - MaximumEdgeToleranceMillimeters));
            int maxRow = ToRow(Math.Max(obstacle.MinY, obstacle.MaxY - MaximumEdgeToleranceMillimeters));

            for (int row = minRow; row <= maxRow; row++)
            {
                for (int column = minColumn; column <= maxColumn; column++)
                {
                    var key = (column, row);
                    if (!_buckets.TryGetValue(key, out List<ObstacleSnapshot> values))
                    {
                        values = new List<ObstacleSnapshot>();
                        _buckets.Add(key, values);
                    }

                    values.Add(obstacle);
                }
            }
        }

        private int ToColumn(double x)
        {
            return (int)Math.Floor((x - OriginX) / GridSizeMillimeters);
        }

        private int ToRow(double y)
        {
            return (int)Math.Floor((y - OriginY) / GridSizeMillimeters);
        }
    }
}
