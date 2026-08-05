using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace PlugHub.HubeiReportParameters
{
    public sealed class HubeiReportTemplateRow
    {
        public int RowNumber { get; set; }

        public string PropertySetName { get; set; } = string.Empty;

        public string BindingKind { get; set; } = string.Empty;

        public string IfcEntityName { get; set; } = string.Empty;

        public IReadOnlyCollection<string> RevitCategoryNames { get; set; } = new string[0];

        public IReadOnlyCollection<Category> RevitCategories { get; set; } = new Category[0];

        public string Name { get; set; } = string.Empty;

        public string RevitParameterName { get; set; } = string.Empty;

        public string IfcDataType { get; set; } = string.Empty;

        public ParameterType RevitParameterType { get; set; }

        public string DefaultValue { get; set; } = string.Empty;

        public string ActualValue { get; set; } = string.Empty;

        public bool IsInstanceBinding => BindingKind == "I";
    }

    public sealed class HubeiReportTemplate
    {
        public string FilePath { get; set; } = string.Empty;

        public IReadOnlyList<HubeiReportTemplateRow> Rows { get; set; } = new HubeiReportTemplateRow[0];
    }

    public sealed class HubeiReportSelection
    {
        public string TemplatePath { get; set; } = string.Empty;

        public bool RemoveExistingParameters { get; set; }

        public bool WriteActualValues { get; set; }

        public bool ExportHifcMappingFile { get; set; }

        public bool CreatePropertySetSchedules { get; set; }
    }

    public sealed class HubeiReportResult
    {
        public int CreatedCount { get; set; }

        public int UpdatedBindingCount { get; set; }

        public int RemovedCount { get; set; }

        public int ActualValueCount { get; set; }

        public int DefaultValueCount { get; set; }

        public int SkippedValueCount { get; set; }

        public int CreatedScheduleCount { get; set; }
    }
}
