using System.Collections.Generic;
using System.Linq;
using PlugHub.ClearHeightAnalysis.Models;

namespace PlugHub.ClearHeightAnalysis.Services
{
    public static class ObstacleProjectionMapper
    {
        public static IReadOnlyList<ObstacleProjection> FindCoveringObstacles(GridCell cell, IEnumerable<ObstacleProjection> obstacles)
        {
            return obstacles.Where(obstacle => obstacle.Bounds.Intersects(cell.Bounds)).ToList();
        }
    }
}
