using System.Collections.Generic;
using System.Linq;
using PlugHub.ClearHeightAnalysis.Models;
using PlugHub.ClearHeightAnalysis.Services;
using Xunit;

namespace PlugHub.ClearHeightAnalysis.Tests
{
    public sealed class GridBuilderTests
    {
        [Fact]
        public void BuildInsideRectangleCreatesExpectedOneMeterCells()
        {
            var boundary = new Rect2d(0, 0, 2000, 1000);

            List<GridCell> cells = GridBuilder.BuildInsideRectangle(boundary, 1000, "1F", 0).ToList();

            Assert.Equal(2, cells.Count);
            Assert.Equal("1F-001", cells[0].Number);
            Assert.Equal(new Rect2d(0, 0, 1000, 1000), cells[0].Bounds);
            Assert.Equal(new Rect2d(1000, 0, 2000, 1000), cells[1].Bounds);
        }

        [Fact]
        public void BuildInsideRectangleSkipsCellsOutsidePolygonMask()
        {
            var boundary = new Rect2d(0, 0, 2000, 2000);
            var mask = new List<Rect2d>
            {
                new Rect2d(0, 0, 1000, 2000)
            };

            List<GridCell> cells = GridBuilder.BuildInsideRectangles(boundary, mask, 1000, "1F", 0).ToList();

            Assert.Equal(2, cells.Count);
            Assert.All(cells, cell => Assert.True(cell.Bounds.MaxX <= 1000));
        }
    }
}
