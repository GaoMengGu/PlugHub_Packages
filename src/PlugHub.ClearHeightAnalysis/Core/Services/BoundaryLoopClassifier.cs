using System;
using System.Collections.Generic;
using System.Linq;
using PlugHub.ClearHeightAnalysis.Core.Geometry;
using PlugHub.ClearHeightAnalysis.Core.Models;

namespace PlugHub.ClearHeightAnalysis.Core.Services
{
    public static class BoundaryLoopClassifier
    {
        public static AnalysisBoundary Classify(IEnumerable<PolygonLoop2d> loops)
        {
            if (loops == null) throw new ArgumentNullException(nameof(loops));
            List<PolygonLoop2d> ordered = loops
                .Where(loop => loop != null)
                .OrderByDescending(loop => Math.Abs(PolygonMath.SignedArea(loop)))
                .ToList();
            if (ordered.Count == 0)
                throw new ArgumentException("至少需要一个边界环。", nameof(loops));

            var depths = new Dictionary<PolygonLoop2d, int>();
            for (int index = 0; index < ordered.Count; index++)
            {
                PolygonLoop2d loop = ordered[index];
                Point2d sample = loop.Points[0];
                depths[loop] = ordered.Take(index).Count(parent => PolygonMath.ContainsPoint(parent, sample));
            }

            List<PolygonLoop2d> outers = ordered.Where(loop => depths[loop] % 2 == 0).ToList();
            var regions = new List<BoundaryRegion>();
            foreach (PolygonLoop2d outer in outers)
            {
                List<PolygonLoop2d> holes = ordered
                    .Where(loop => depths[loop] % 2 == 1)
                    .Where(loop => PolygonMath.ContainsPoint(outer, loop.Points[0]))
                    .Where(loop => !outers.Any(smallerOuter =>
                        !ReferenceEquals(smallerOuter, outer) &&
                        Math.Abs(PolygonMath.SignedArea(smallerOuter)) < Math.Abs(PolygonMath.SignedArea(outer)) &&
                        PolygonMath.ContainsPoint(smallerOuter, loop.Points[0])))
                    .ToList();
                regions.Add(new BoundaryRegion(outer, holes));
            }

            return new AnalysisBoundary(regions);
        }
    }
}
