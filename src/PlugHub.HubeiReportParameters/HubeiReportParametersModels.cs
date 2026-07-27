using System.Collections.Generic;

namespace PlugHub.HubeiReportParameters
{
    public enum HubeiReportScope
    {
        Global,
        TotalPlan,
        Monolithic,
        MiniReport
    }

    public enum HubeiReportSource
    {
        Hifc,
        Mini
    }

    public enum HubeiParameterType
    {
        Text,
        Integer,
        Number,
        YesNo
    }

    public sealed class HubeiReportParameterDefinition
    {
        public string PsetName { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string IfcTypeName { get; set; } = string.Empty;

        public HubeiParameterType ParameterType { get; set; }

        public IReadOnlyCollection<HubeiReportScope> Scopes { get; set; } = new HubeiReportScope[0];

        public HubeiReportSource Source { get; set; }
    }

    public sealed class HubeiReportDefaults
    {
        public string TextValue { get; set; } = "其他";

        public string NumberValue { get; set; } = "0";

        public bool YesNoValue { get; set; }
    }

    public sealed class HubeiReportSelection
    {
        public bool IncludeGlobal { get; set; }

        public bool IncludeTotalPlan { get; set; }

        public bool IncludeMonolithic { get; set; }

        public bool IncludeMiniReport { get; set; }

        public HubeiReportDefaults Defaults { get; set; } = new HubeiReportDefaults();

        public bool HasAnyScope => IncludeGlobal || IncludeTotalPlan || IncludeMonolithic || IncludeMiniReport;
    }

    public sealed class HubeiReportResult
    {
        public int RemovedCount { get; set; }

        public int AddedCount { get; set; }

        public int DefaultValueCount { get; set; }

        public List<string> SkippedDefinitions { get; } = new List<string>();
    }
}
