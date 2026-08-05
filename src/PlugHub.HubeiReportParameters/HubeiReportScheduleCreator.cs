using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace PlugHub.HubeiReportParameters
{
    public static class HubeiReportScheduleCreator
    {
        public static int Recreate(Document document, IReadOnlyCollection<HubeiReportTemplateRow> rows)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            IReadOnlyList<HubeiReportSchedulePlan> plans = HubeiReportSchedulePlanner.Build(CreateSources(rows));
            if (plans.Count == 0)
            {
                return 0;
            }

            using (var transaction = new Transaction(document, "创建湖北报规属性集明细表"))
            {
                transaction.Start();
                DeleteExistingSchedules(document, plans.Select(plan => plan.Name));
                foreach (HubeiReportSchedulePlan plan in plans)
                {
                    CreateSchedule(document, plan);
                }

                transaction.Commit();
            }

            return plans.Count;
        }

        private static IEnumerable<HubeiReportScheduleSource> CreateSources(IEnumerable<HubeiReportTemplateRow> rows)
        {
            if (rows == null)
            {
                yield break;
            }

            foreach (HubeiReportTemplateRow row in rows)
            {
                foreach (Category category in row.RevitCategories)
                {
                    int categoryId = category.Id.IntegerValue;
                    yield return new HubeiReportScheduleSource(
                        row.PropertySetName,
                        row.Name,
                        row.RevitParameterName,
                        categoryId,
                        category.Name,
                        categoryId == (int)BuiltInCategory.OST_ProjectInformation);
                }
            }
        }

        private static void DeleteExistingSchedules(Document document, IEnumerable<string> scheduleNames)
        {
            HashSet<string> names = new HashSet<string>(scheduleNames, StringComparer.Ordinal);
            ElementId[] existingIds = new FilteredElementCollector(document)
                .OfClass(typeof(ViewSchedule))
                .Cast<ViewSchedule>()
                .Where(schedule => !schedule.IsTemplate && names.Contains(schedule.Name))
                .Select(schedule => schedule.Id)
                .ToArray();
            foreach (ElementId id in existingIds)
            {
                document.Delete(id);
            }
        }

        private static void CreateSchedule(Document document, HubeiReportSchedulePlan plan)
        {
            ElementId scheduleCategoryId = plan.Categories.Count == 1
                ? new ElementId(plan.Categories[0].Id)
                : ElementId.InvalidElementId;
            if (plan.Categories.Count == 1 && !ViewSchedule.GetValidCategoriesForSchedule().Contains(scheduleCategoryId))
            {
                throw new InvalidOperationException("Revit 类别不支持创建明细表：" + plan.Categories[0].Name + "。");
            }

            ViewSchedule schedule = CreateViewSchedule(document, scheduleCategoryId, plan);
            schedule.Name = plan.Name;
            ScheduleDefinition definition = schedule.Definition;
            IList<SchedulableField> schedulableFields = definition.GetSchedulableFields();
            ScheduleField firstField = null;
            foreach (HubeiReportScheduleField fieldPlan in plan.Fields)
            {
                SchedulableField schedulableField = schedulableFields.FirstOrDefault(
                    candidate => string.Equals(candidate.GetName(document), fieldPlan.ParameterName, StringComparison.Ordinal));
                if (schedulableField == null)
                {
                    throw new InvalidOperationException("属性集 " + plan.Name + " 的参数无法加入明细表：" + fieldPlan.ParameterName + "。");
                }

                ScheduleField field = definition.AddField(schedulableField);
                field.ColumnHeading = fieldPlan.Heading;
                if (firstField == null)
                {
                    firstField = field;
                }
            }

            if (plan.Categories.Count > 1 && firstField != null)
            {
                definition.AddFilter(new ScheduleFilter(firstField.FieldId, ScheduleFilterType.HasParameter));
            }
        }

        private static ViewSchedule CreateViewSchedule(Document document, ElementId scheduleCategoryId, HubeiReportSchedulePlan plan)
        {
            if (plan.Categories.Count != 1 || scheduleCategoryId.IntegerValue != (int)BuiltInCategory.OST_Areas)
            {
                return ViewSchedule.CreateSchedule(document, scheduleCategoryId);
            }

            AreaScheme areaScheme = new FilteredElementCollector(document)
                .OfClass(typeof(AreaScheme))
                .Cast<AreaScheme>()
                .OrderBy(scheme => scheme.Name, StringComparer.Ordinal)
                .FirstOrDefault();
            if (areaScheme == null)
            {
                throw new InvalidOperationException("属性集 " + plan.Name + " 使用面积类别，但当前项目中没有面积方案。");
            }

            return ViewSchedule.CreateSchedule(document, scheduleCategoryId, areaScheme.Id);
        }
    }
}
