using System;
using System.Collections.Generic;
using System.Linq;

namespace PlugHub.HubeiReportParameters
{
    internal sealed class HubeiReportScheduleSource
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

    internal sealed class HubeiReportScheduleCategory
    {
        public HubeiReportScheduleCategory(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public int Id { get; }

        public string Name { get; }
    }

    internal sealed class HubeiReportScheduleField
    {
        public HubeiReportScheduleField(string parameterName, string heading)
        {
            ParameterName = parameterName;
            Heading = heading;
        }

        public string ParameterName { get; }

        public string Heading { get; }
    }

    internal sealed class HubeiReportSchedulePlan
    {
        public HubeiReportSchedulePlan(string name, IReadOnlyList<HubeiReportScheduleCategory> categories, IReadOnlyList<HubeiReportScheduleField> fields, string filterParameterName)
        {
            Name = name;
            Categories = categories;
            Fields = fields;
            FilterParameterName = filterParameterName;
        }

        public string Name { get; }

        public IReadOnlyList<HubeiReportScheduleCategory> Categories { get; }

        public IReadOnlyList<HubeiReportScheduleField> Fields { get; }

        public string FilterParameterName { get; }
    }

    internal static class HubeiReportSchedulePlanner
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
                .Select(CreatePlan)
                .Where(plan => plan.Categories.Count > 0 && plan.Fields.Count > 0)
                .OrderBy(plan => plan.Name, StringComparer.Ordinal)
                .ToArray();
        }

        private static HubeiReportSchedulePlan CreatePlan(IGrouping<string, HubeiReportScheduleSource> group)
        {
            HubeiReportScheduleSource[] sources = group.ToArray();
            HubeiReportScheduleCategory[] categories = sources
                .GroupBy(source => source.CategoryId)
                .Select(categoryGroup => categoryGroup.First())
                .Select(source => new HubeiReportScheduleCategory(source.CategoryId, source.CategoryName))
                .ToArray();
            HubeiReportScheduleField[] fields = sources
                .GroupBy(source => source.ParameterName, StringComparer.Ordinal)
                .Select(fieldGroup => fieldGroup.First())
                .Select(source => new HubeiReportScheduleField(source.ParameterName, source.PropertyName))
                .ToArray();
            string filterParameterName = sources
                .GroupBy(source => source.ParameterName, StringComparer.Ordinal)
                .Where(fieldGroup => fieldGroup.Select(source => source.CategoryId).Distinct().Count() == categories.Length)
                .Select(fieldGroup => fieldGroup.Key)
                .FirstOrDefault();
            if (categories.Length > 1 && string.IsNullOrEmpty(filterParameterName))
            {
                throw new InvalidOperationException("属性集 " + group.Key + " 绑定多个 Revit 类别，但没有一个字段覆盖全部类别，无法创建准确的多类别明细表。");
            }

            return new HubeiReportSchedulePlan(group.Key, categories, fields, filterParameterName ?? string.Empty);
        }
    }
}
