using System;
using System.Collections.Generic;
using System.Linq;

namespace PlugHub.HubeiReportParameters
{
    public sealed class HubeiReportScheduleSource
    {
        public HubeiReportScheduleSource(string propertySetName, string propertyName, string parameterName, int categoryId, string categoryName, bool isProjectInformation)
        {
            PropertySetName = propertySetName;
            PropertyName = propertyName;
            ParameterName = parameterName;
            CategoryId = categoryId;
            CategoryName = categoryName;
            IsProjectInformation = isProjectInformation;
        }

        public string PropertySetName { get; }

        public string PropertyName { get; }

        public string ParameterName { get; }

        public int CategoryId { get; }

        public string CategoryName { get; }

        public bool IsProjectInformation { get; }
    }

    public sealed class HubeiReportScheduleCategory
    {
        public HubeiReportScheduleCategory(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }

        public string Name { get; }
    }

    public sealed class HubeiReportScheduleField
    {
        public HubeiReportScheduleField(string parameterName, string heading)
        {
            ParameterName = parameterName;
            Heading = heading;
        }

        public string ParameterName { get; }

        public string Heading { get; }
    }

    public sealed class HubeiReportSchedulePlan
    {
        public HubeiReportSchedulePlan(string name, IReadOnlyList<HubeiReportScheduleCategory> categories, IReadOnlyList<HubeiReportScheduleField> fields)
        {
            Name = name;
            Categories = categories;
            Fields = fields;
        }

        public string Name { get; }

        public IReadOnlyList<HubeiReportScheduleCategory> Categories { get; }

        public IReadOnlyList<HubeiReportScheduleField> Fields { get; }
    }

    public static class HubeiReportSchedulePlanner
    {
        public static IReadOnlyList<HubeiReportSchedulePlan> Build(IEnumerable<HubeiReportScheduleSource> sources)
        {
            if (sources == null)
            {
                throw new ArgumentNullException(nameof(sources));
            }

            return sources
                .Where(source => source != null && !source.IsProjectInformation)
                .GroupBy(source => source.PropertySetName, StringComparer.Ordinal)
                .Select(group => new HubeiReportSchedulePlan(
                    group.Key,
                    group.GroupBy(source => source.CategoryId)
                        .Select(categoryGroup => categoryGroup.First())
                        .Select(source => new HubeiReportScheduleCategory(source.CategoryId, source.CategoryName))
                        .ToArray(),
                    group.GroupBy(source => source.ParameterName, StringComparer.Ordinal)
                        .Select(fieldGroup => fieldGroup.First())
                        .Select(source => new HubeiReportScheduleField(source.ParameterName, source.PropertyName))
                        .ToArray()))
                .Where(plan => plan.Categories.Count > 0 && plan.Fields.Count > 0)
                .OrderBy(plan => plan.Name, StringComparer.Ordinal)
                .ToArray();
        }
    }
}
