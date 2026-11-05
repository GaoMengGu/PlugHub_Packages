using System;

namespace PlugHub.ClearHeightAnalysis.Core.Models
{
    public sealed class AnalysisLevelChoice
    {
        public AnalysisLevelChoice(string uniqueId, string name, double elevationMillimeters)
        {
            UniqueId = string.IsNullOrWhiteSpace(uniqueId)
                ? throw new ArgumentException("楼层唯一标识不能为空。", nameof(uniqueId))
                : uniqueId;
            Name = string.IsNullOrWhiteSpace(name) ? uniqueId : name;
            ElevationMillimeters = elevationMillimeters;
        }

        public string UniqueId { get; }
        public string Name { get; }
        public double ElevationMillimeters { get; }
        public string DisplayName => Name + "  (" + ElevationMillimeters.ToString("0") + " mm)";
    }
}
