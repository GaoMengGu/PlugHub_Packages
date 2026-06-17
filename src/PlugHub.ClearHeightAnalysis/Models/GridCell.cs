namespace PlugHub.ClearHeightAnalysis.Models
{
    public sealed class GridCell
    {
        public GridCell(string number, Rect2d bounds, string levelName, double levelElevationMillimeters)
        {
            Number = number;
            Bounds = bounds;
            LevelName = levelName;
            LevelElevationMillimeters = levelElevationMillimeters;
        }

        public string Number { get; }
        public Rect2d Bounds { get; }
        public string LevelName { get; }
        public double LevelElevationMillimeters { get; }
    }
}
