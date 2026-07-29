using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace PlugHub.HubeiReportParameters
{
    public static class RevitCategoryCatalog
    {
        private static readonly IReadOnlyDictionary<string, BuiltInCategory> Aliases =
            new Dictionary<string, BuiltInCategory>(StringComparer.Ordinal)
            {
                ["结构柱"] = BuiltInCategory.OST_StructuralColumns,
                ["结构框架"] = BuiltInCategory.OST_StructuralFraming,
                ["结构基础"] = BuiltInCategory.OST_StructuralFoundation,
                ["结构墙"] = BuiltInCategory.OST_Walls,
                ["结构楼板"] = BuiltInCategory.OST_Floors,
                ["桁架"] = BuiltInCategory.OST_Truss,
                ["支撑"] = BuiltInCategory.OST_StructuralFraming,
                ["风管"] = BuiltInCategory.OST_DuctCurves,
                ["风管管件"] = BuiltInCategory.OST_DuctFitting,
                ["风管附件"] = BuiltInCategory.OST_DuctAccessory,
                ["风口"] = BuiltInCategory.OST_DuctTerminal,
                ["机械设备"] = BuiltInCategory.OST_MechanicalEquipment,
                ["风管保温"] = BuiltInCategory.OST_DuctInsulations,
                ["管道"] = BuiltInCategory.OST_PipeCurves,
                ["管道管件"] = BuiltInCategory.OST_PipeFitting,
                ["管道附件"] = BuiltInCategory.OST_PipeAccessory,
                ["卫浴装置"] = BuiltInCategory.OST_PlumbingFixtures,
                ["喷头"] = BuiltInCategory.OST_Sprinklers,
                ["管道保温"] = BuiltInCategory.OST_PipeInsulations,
                ["电缆桥架"] = BuiltInCategory.OST_CableTray,
                ["电缆桥架配件"] = BuiltInCategory.OST_CableTrayFitting,
                ["线管"] = BuiltInCategory.OST_Conduit,
                ["线管配件"] = BuiltInCategory.OST_ConduitFitting,
                ["电气设备"] = BuiltInCategory.OST_ElectricalEquipment,
                ["照明设备"] = BuiltInCategory.OST_LightingFixtures,
                ["通信设备"] = BuiltInCategory.OST_CommunicationDevices
            };

        public static IReadOnlyCollection<string> ParseNames(string value, int rowNumber)
        {
            string[] names = (value ?? string.Empty).Split(',').Select(name => name.Trim()).Where(name => name.Length > 0).ToArray();
            if (names.Length == 0)
            {
                throw new InvalidOperationException("第 " + rowNumber + " 行的 Revit类别不能为空。");
            }

            return names.Distinct(StringComparer.Ordinal).ToArray();
        }

        public static void Resolve(Document document, IReadOnlyCollection<HubeiReportTemplateRow> rows)
        {
            if (document == null)
            {
                throw new InvalidOperationException("未收到 Revit 项目，无法解析 Revit类别。");
            }

            Dictionary<string, Category> categories = document.Settings.Categories
                .Cast<Category>()
                .Where(category => category != null && category.AllowsBoundParameters)
                .GroupBy(category => category.Name, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

            foreach (HubeiReportTemplateRow row in rows)
            {
                var resolved = new List<Category>();
                foreach (string name in row.RevitCategoryNames)
                {
                    Category category = ResolveCategory(document, categories, name);
                    if (category == null)
                    {
                        throw new InvalidOperationException("第 " + row.RowNumber + " 行的 Revit类别无法在当前项目中解析：" + name + "。请使用当前 Revit 显示的类别名称。");
                    }

                    if (resolved.All(item => item.Id.IntegerValue != category.Id.IntegerValue))
                    {
                        resolved.Add(category);
                    }
                }

                if (resolved.Count == 0)
                {
                    throw new InvalidOperationException("第 " + row.RowNumber + " 行的 Revit类别不能为空。");
                }

                row.RevitCategories = resolved;
            }
        }

        private static Category ResolveCategory(Document document, IReadOnlyDictionary<string, Category> categories, string name)
        {
            if (categories.TryGetValue(name, out Category category))
            {
                return category;
            }

            if (Aliases.TryGetValue(name, out BuiltInCategory builtInCategory))
            {
                Category aliasCategory = document.Settings.Categories.get_Item(builtInCategory);
                if (aliasCategory != null && aliasCategory.AllowsBoundParameters)
                {
                    return aliasCategory;
                }
            }

            return null;
        }
    }
}
