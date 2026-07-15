using System;
using System.Collections.Generic;
using System.Linq;

namespace PlugHub.ClearHeightAnalysis.Core.Models
{
    public sealed class GridCellData
    {
        public GridCellData(
            string number,
            int column,
            int row,
            double minX,
            double minY,
            double maxX,
            double maxY,
            string levelUniqueId,
            string levelName,
            double levelElevationMillimeters,
            IEnumerable<int> boundaryRegionIndexes)
        {
            if (string.IsNullOrWhiteSpace(number))
            {
                throw new ArgumentException("网格编号不能为空。", nameof(number));
            }

            if (maxX <= minX || maxY <= minY)
            {
                throw new ArgumentException("网格范围必须具有正面积。", nameof(maxX));
            }

            if (string.IsNullOrWhiteSpace(levelUniqueId))
            {
                throw new ArgumentException("楼层唯一标识不能为空。", nameof(levelUniqueId));
            }

            if (boundaryRegionIndexes == null)
            {
                throw new ArgumentNullException(nameof(boundaryRegionIndexes));
            }

            Number = number;
            Column = column;
            Row = row;
            MinX = minX;
            MinY = minY;
            MaxX = maxX;
            MaxY = maxY;
            LevelUniqueId = levelUniqueId;
            LevelName = string.IsNullOrWhiteSpace(levelName) ? levelUniqueId : levelName;
            LevelElevationMillimeters = levelElevationMillimeters;
            BoundaryRegionIndexes = boundaryRegionIndexes.Distinct().OrderBy(index => index).ToList().AsReadOnly();
        }

        public string Number { get; }
        public int Column { get; }
        public int Row { get; }
        public double MinX { get; }
        public double MinY { get; }
        public double MaxX { get; }
        public double MaxY { get; }
        public double CenterX => (MinX + MaxX) / 2.0;
        public double CenterY => (MinY + MaxY) / 2.0;
        public string LevelUniqueId { get; }
        public string LevelName { get; }
        public double LevelElevationMillimeters { get; }
        public IReadOnlyList<int> BoundaryRegionIndexes { get; }
    }
}
