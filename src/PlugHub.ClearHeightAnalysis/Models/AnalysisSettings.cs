namespace PlugHub.ClearHeightAnalysis.Models
{
    public sealed class AnalysisSettings
    {
        public const double DefaultGridSizeMillimeters = 1000;
        public const double DefaultClearHeightThresholdMillimeters = 3000;

        public string LevelName { get; set; } = string.Empty;
        public double LevelElevationMillimeters { get; set; }
        public double GridSizeMillimeters { get; set; } = DefaultGridSizeMillimeters;
        public double ClearHeightThresholdMillimeters { get; set; } = DefaultClearHeightThresholdMillimeters;
        public double FinishFloorOffsetMillimeters { get; set; }
        public double SearchHeightMillimeters { get; set; } = 6000;
        public bool UseActiveViewCropAsFallback { get; set; } = true;
        public bool IncludeCurrentModel { get; set; } = true;
        public bool IncludeLinkedModels { get; set; } = true;
        public bool IncludeCeilings { get; set; } = true;
        public bool IncludeMep { get; set; } = true;
        public double MinimumPipeDiameterMillimeters { get; set; } = 50;
        public bool RenderOnlyProblemCells { get; set; } = true;
    }
}
