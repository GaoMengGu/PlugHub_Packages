namespace PlugHub.ClearHeightAnalysis.Models
{
    public sealed class ObstacleProjection
    {
        public ObstacleProjection(string elementKey, string elementName, string categoryName, string sourceModelName, bool isFromLink, Rect2d bounds, double bottomElevationMillimeters)
        {
            ElementKey = elementKey;
            ElementName = elementName;
            CategoryName = categoryName;
            SourceModelName = sourceModelName;
            IsFromLink = isFromLink;
            Bounds = bounds;
            BottomElevationMillimeters = bottomElevationMillimeters;
        }

        public string ElementKey { get; }
        public string ElementName { get; }
        public string CategoryName { get; }
        public string SourceModelName { get; }
        public bool IsFromLink { get; }
        public Rect2d Bounds { get; }
        public double BottomElevationMillimeters { get; }
    }
}
