namespace PlugHub.ClearHeightAnalysis.Models
{
    public sealed class AnalysisLevel
    {
        public AnalysisLevel(string name, double elevationMillimeters)
        {
            Name = name;
            ElevationMillimeters = elevationMillimeters;
        }

        public string Name { get; }
        public double ElevationMillimeters { get; }
    }
}
