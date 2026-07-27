using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PlugHub.HubeiReportParameters
{
    [Transaction(TransactionMode.Manual)]
    public sealed class SyncHubeiReportParametersCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (commandData == null || commandData.Application?.ActiveUIDocument == null)
            {
                message = "未收到 Revit 命令上下文。";
                return Result.Failed;
            }

            try
            {
                HubeiReportSelection selection = ShowSelectionDialog();
                if (selection == null)
                {
                    return Result.Cancelled;
                }

                if (!selection.HasAnyScope)
                {
                    TaskDialog.Show("湖北报规参数", "请至少选择一个分类。\n\n可选分类：总图、单体、全局、最小报建。");
                    return Result.Cancelled;
                }

                IReadOnlyList<HubeiReportParameterDefinition> definitions = HubeiReportCatalog.GetDefinitions(selection);
                if (definitions.Count == 0)
                {
                    TaskDialog.Show("湖北报规参数", "所选分类没有可创建的属性。");
                    return Result.Cancelled;
                }

                Document document = commandData.Application.ActiveUIDocument.Document;
                HubeiReportResult result = ApplyDefinitions(document, definitions, selection.Defaults);
                ShowResult(result, definitions.Count);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private static HubeiReportSelection ShowSelectionDialog()
        {
            using (var form = new HubeiReportSelectionForm())
            {
                return form.ShowDialog() == DialogResult.OK ? form.Selection : null;
            }
        }

        private static HubeiReportResult ApplyDefinitions(Document document, IReadOnlyList<HubeiReportParameterDefinition> definitions, HubeiReportDefaults defaults)
        {
            var result = new HubeiReportResult();
            string sharedFilePath = Path.Combine(Path.GetTempPath(), "PlugHub.HubeiReportParameters.shared");
            File.WriteAllText(sharedFilePath, CreateSharedParameterTemplate());

            string originalSharedParameterPath = document.Application.SharedParametersFilename;
            document.Application.SharedParametersFilename = sharedFilePath;

            try
            {
                DefinitionFile definitionFile = document.Application.OpenSharedParameterFile();
                if (definitionFile == null)
                {
                    throw new InvalidOperationException("无法打开共享参数文件。");
                }

                DefinitionGroup group = GetOrCreateGroup(definitionFile, "PlugHub_HubeiReport");

                using (Transaction transaction = new Transaction(document, "湖北报规共享参数同步"))
                {
                    transaction.Start();

                    foreach (HubeiReportParameterDefinition definition in definitions)
                    {
                        RemoveExistingBinding(document, definition.Name, out bool removed);
                        if (removed)
                        {
                            result.RemovedCount++;
                        }

                        ExternalDefinition externalDefinition = GetOrCreateDefinition(group, definition.Name, MapParameterType(definition.ParameterType));
                        CategorySet categories = GetBindingCategories(document, definition);
                        if (IsCategorySetEmpty(categories))
                        {
                            result.SkippedDefinitions.Add(definition.Name);
                            continue;
                        }

                        InstanceBinding binding = document.Application.Create.NewInstanceBinding(categories);
                        if (!document.ParameterBindings.Insert(externalDefinition, binding, BuiltInParameterGroup.PG_DATA))
                        {
                            document.ParameterBindings.ReInsert(externalDefinition, binding, BuiltInParameterGroup.PG_DATA);
                        }

                        result.AddedCount++;
                    }

                    transaction.Commit();
                }

                using (Transaction transaction = new Transaction(document, "湖北报规默认值填充"))
                {
                    transaction.Start();

                    foreach (HubeiReportParameterDefinition definition in definitions)
                    {
                        result.DefaultValueCount += FillDefaultValues(document, definition, defaults);
                    }

                    transaction.Commit();
                }
            }
            finally
            {
                document.Application.SharedParametersFilename = originalSharedParameterPath;
            }

            return result;
        }

        private static string CreateSharedParameterTemplate()
        {
            return "# This is a Revit shared parameter file.\r\n" +
                   "# Do not edit manually.\r\n" +
                   "*META\tVERSION\tMINVERSION\r\n" +
                   "META\t2\t1\r\n" +
                   "*GROUP\tID\tNAME\r\n" +
                   "*PARAM\tGUID\tNAME\tDATATYPE\tDATACATEGORY\tGROUP\tVISIBLE\tDESCRIPTION\tUSERMODIFIABLE\tHIDEWHENNOVALUE\r\n";
        }

        private static DefinitionGroup GetOrCreateGroup(DefinitionFile definitionFile, string groupName)
        {
            DefinitionGroup group = definitionFile.Groups.get_Item(groupName);
            return group ?? definitionFile.Groups.Create(groupName);
        }

        private static ExternalDefinition GetOrCreateDefinition(DefinitionGroup group, string name, ParameterType parameterType)
        {
            Definition existing = group.Definitions.get_Item(name);
            if (existing is ExternalDefinition externalDefinition)
            {
                return externalDefinition;
            }

            return (ExternalDefinition)group.Definitions.Create(new ExternalDefinitionCreationOptions(name, parameterType)
            {
                Visible = true
            });
        }

        private static void RemoveExistingBinding(Document document, string name, out bool removed)
        {
            removed = false;
            var iterator = document.ParameterBindings.ForwardIterator();
            while (iterator.MoveNext())
            {
                if (iterator.Key != null && string.Equals(iterator.Key.Name, name, StringComparison.Ordinal))
                {
                    removed = document.ParameterBindings.Remove(iterator.Key);
                    return;
                }
            }
        }

        private static CategorySet GetBindingCategories(Document document, HubeiReportParameterDefinition definition)
        {
            CategorySet categorySet = document.Application.Create.NewCategorySet();

            if (definition.Scopes.Contains(HubeiReportScope.Global))
            {
                AddCategory(document, categorySet, BuiltInCategory.OST_ProjectInformation);
            }

            if (definition.Scopes.Contains(HubeiReportScope.TotalPlan))
            {
                AddCategory(document, categorySet, BuiltInCategory.OST_Site);
                AddCategory(document, categorySet, BuiltInCategory.OST_GenericModel);
            }

            if (definition.Scopes.Contains(HubeiReportScope.Monolithic))
            {
                AddCategory(document, categorySet, BuiltInCategory.OST_Levels);
                AddCategory(document, categorySet, BuiltInCategory.OST_Rooms);
                AddCategory(document, categorySet, BuiltInCategory.OST_Areas);
                AddCategory(document, categorySet, BuiltInCategory.OST_MEPSpaces);
                AddCategory(document, categorySet, BuiltInCategory.OST_Floors);
                AddCategory(document, categorySet, BuiltInCategory.OST_Walls);
                AddCategory(document, categorySet, BuiltInCategory.OST_Roofs);
                AddCategory(document, categorySet, BuiltInCategory.OST_Columns);
                AddCategory(document, categorySet, BuiltInCategory.OST_StructuralColumns);
                AddCategory(document, categorySet, BuiltInCategory.OST_StructuralFraming);
                AddCategory(document, categorySet, BuiltInCategory.OST_Ceilings);
            }

            return categorySet;
        }

        private static bool IsCategorySetEmpty(CategorySet categorySet)
        {
            foreach (Category _ in categorySet)
            {
                return false;
            }

            return true;
        }

        private static void AddCategory(Document document, CategorySet categorySet, BuiltInCategory builtInCategory)
        {
            Category category = document.Settings.Categories.get_Item(builtInCategory);
            if (category != null && !categorySet.Contains(category))
            {
                categorySet.Insert(category);
            }
        }

        private static ParameterType MapParameterType(HubeiParameterType parameterType)
        {
            switch (parameterType)
            {
                case HubeiParameterType.Integer:
                    return ParameterType.Integer;
                case HubeiParameterType.Number:
                    return ParameterType.Number;
                case HubeiParameterType.YesNo:
                    return ParameterType.YesNo;
                case HubeiParameterType.Text:
                default:
                    return ParameterType.Text;
            }
        }

        private static int FillDefaultValues(Document document, HubeiReportParameterDefinition definition, HubeiReportDefaults defaults)
        {
            int updatedCount = 0;
            IEnumerable<Element> elements = CollectTargetElements(document, definition);

            foreach (Element element in elements)
            {
                Parameter parameter = element.LookupParameter(definition.Name);
                if (parameter == null || parameter.IsReadOnly)
                {
                    continue;
                }

                if (TrySetDefault(parameter, definition.ParameterType, defaults))
                {
                    updatedCount++;
                }
            }

            return updatedCount;
        }

        private static IEnumerable<Element> CollectTargetElements(Document document, HubeiReportParameterDefinition definition)
        {
            if (definition.Scopes.Contains(HubeiReportScope.Global))
            {
                yield return document.ProjectInformation;
                yield break;
            }

            var targetCategoryIds = new HashSet<int>();
            if (definition.Scopes.Contains(HubeiReportScope.TotalPlan))
            {
                targetCategoryIds.Add((int)BuiltInCategory.OST_Site);
                targetCategoryIds.Add((int)BuiltInCategory.OST_GenericModel);
            }

            if (definition.Scopes.Contains(HubeiReportScope.Monolithic))
            {
                targetCategoryIds.UnionWith(new[]
                {
                    (int)BuiltInCategory.OST_Levels,
                    (int)BuiltInCategory.OST_Rooms,
                    (int)BuiltInCategory.OST_Areas,
                    (int)BuiltInCategory.OST_MEPSpaces,
                    (int)BuiltInCategory.OST_Floors,
                    (int)BuiltInCategory.OST_Walls,
                    (int)BuiltInCategory.OST_Roofs,
                    (int)BuiltInCategory.OST_Columns,
                    (int)BuiltInCategory.OST_StructuralColumns,
                    (int)BuiltInCategory.OST_StructuralFraming,
                    (int)BuiltInCategory.OST_Ceilings
                });
            }

            foreach (Element element in new FilteredElementCollector(document).WhereElementIsNotElementType())
            {
                if (element.Category != null && targetCategoryIds.Contains(element.Category.Id.IntegerValue))
                {
                    yield return element;
                }
            }
        }

        private static bool TrySetDefault(Parameter parameter, HubeiParameterType parameterType, HubeiReportDefaults defaults)
        {
            switch (parameter.StorageType)
            {
                case StorageType.String:
                    return parameter.Set(defaults.TextValue ?? string.Empty);
                case StorageType.Integer:
                    if (parameterType == HubeiParameterType.YesNo)
                    {
                        return parameter.Set(defaults.YesNoValue ? 1 : 0);
                    }

                    if (int.TryParse(defaults.NumberValue, out int integerValue))
                    {
                        return parameter.Set(integerValue);
                    }

                    return false;
                case StorageType.Double:
                    if (double.TryParse(defaults.NumberValue, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                    {
                        return parameter.Set(value);
                    }

                    if (double.TryParse(defaults.NumberValue, out value))
                    {
                        return parameter.Set(value);
                    }

                    return false;
                default:
                    return false;
            }
        }

        private static void ShowResult(HubeiReportResult result, int totalDefinitions)
        {
            string message = string.Format(
                "处理完成。\n创建: {0}\n删除同名参数: {1}\n默认值写入: {2}\n总计: {3}",
                result.AddedCount,
                result.RemovedCount,
                result.DefaultValueCount,
                totalDefinitions);

            if (result.SkippedDefinitions.Count > 0)
            {
                message += "\n\n未绑定分类的属性:\n" + string.Join("\n", result.SkippedDefinitions);
            }

            TaskDialog.Show("湖北报规参数", message);
        }
    }
}
