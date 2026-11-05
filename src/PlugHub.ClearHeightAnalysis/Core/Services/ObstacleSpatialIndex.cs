#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
            : this(originX, originY, gridSizeMillimeters, obstacles, CancellationToken.None, null)
        {
        }

        public ObstacleSpatialIndex(
            double originX, double originY, double gridSizeMillimeters,
            IEnumerable<ObstacleSnapshot> obstacles, CancellationToken cancellationToken,
            IProgress<AnalysisProgress>? progress)
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

            List<ObstacleSnapshot> values = obstacles.ToList();
            progress?.Report(new AnalysisProgress("建立空间索引", 0, values.Count));
            int completed = 0;
            foreach (ObstacleSnapshot obstacle in values)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (obstacle == null)
                {
                    throw new ArgumentException("障碍物集合不能包含 null。", nameof(obstacles));
                }

                Register(obstacle, cancellationToken);
                completed++;
                progress?.Report(new AnalysisProgress("建立空间索引", completed, values.Count));
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

            if (_buckets.TryGetValue((cell.Column, cell.Row), out List<ObstacleSnapshot>? values))
            {
                return values;
            }

            return Array.Empty<ObstacleSnapshot>();
        }

        private void Register(ObstacleSnapshot obstacle, CancellationToken cancellationToken)
        {
            int minColumn = ToColumn(obstacle.MinX);
            int minRow = ToRow(obstacle.MinY);
            int maxColumn = ToColumn(Math.Max(obstacle.MinX, obstacle.MaxX - MaximumEdgeToleranceMillimeters));
            int maxRow = ToRow(Math.Max(obstacle.MinY, obstacle.MaxY - MaximumEdgeToleranceMillimeters));

            for (int row = minRow; row <= maxRow; row++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                for (int column = minColumn; column <= maxColumn; column++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var key = (column, row);
                    if (!_buckets.TryGetValue(key, out List<ObstacleSnapshot>? values))
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
