using System;
using System.Collections.Generic;
using System.Linq;
using PlugHub.HubeiReportParameters;

internal static class Program
{
    private static int Main()
    {
        try
        {
            IReadOnlyList<HubeiReportSchedulePlan> plans = HubeiReportSchedulePlanner.Build(new[]
            {
                new HubeiReportScheduleSource("Pset_申报信息属性集", "基点坐标X", "Pset_申报信息属性集_基点坐标X", -2003101, "项目信息", true),
                new HubeiReportScheduleSource("Pset_绿地信息属性集", "投影面积", "Pset_绿地信息属性集_投影面积", -2001260, "地形", false),
                new HubeiReportScheduleSource("Pset_绿地信息属性集", "投影面积", "Pset_绿地信息属性集_投影面积", -2001260, "地形", false),
                new HubeiReportScheduleSource("Pset_绿地信息属性集", "绿地类型", "Pset_绿地信息属性集_绿地类型", -2001260, "地形", false),
                new HubeiReportScheduleSource("Pset_混合属性集", "名称", "Pset_混合属性集_名称", -2001260, "地形", false),
                new HubeiReportScheduleSource("Pset_混合属性集", "名称", "Pset_混合属性集_名称", -2000011, "墙", false),
                new HubeiReportScheduleSource("Pset_混合属性集", "编码", "Pset_混合属性集_编码", -2000011, "墙", false),
                new HubeiReportScheduleSource("Pset_混合属性集", "编码", "Pset_混合属性集_编码", -2003101, "项目信息", true)
            });

            if (plans.Any(plan => plan.Name == "Pset_申报信息属性集"))
            {
                throw new InvalidOperationException("Project Information property sets must not create schedules.");
            }

            HubeiReportSchedulePlan green = plans.Single(plan => plan.Name == "Pset_绿地信息属性集");
            if (green.Categories.Count != 1 || green.Fields.Count != 2)
            {
                throw new InvalidOperationException("Schedule planning must deduplicate categories and fields.");
            }

            HubeiReportScheduleField projectedArea = green.Fields.Single(field => field.ParameterName == "Pset_绿地信息属性集_投影面积");
            if (projectedArea.Heading != "投影面积")
            {
                throw new InvalidOperationException("Schedule headings must use the template property name without the property-set prefix.");
            }

            HubeiReportSchedulePlan mixed = plans.Single(plan => plan.Name == "Pset_混合属性集");
            if (mixed.Categories.Count != 2 || mixed.Fields.Count != 2)
            {
                throw new InvalidOperationException("A property set must retain all non-project-information categories and fields.");
            }

            Console.WriteLine("Hubei report schedule planning validation passed.");
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception.Message);
            return 1;
        }
    }
}
