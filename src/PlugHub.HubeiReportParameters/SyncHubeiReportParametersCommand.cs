using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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

                Document document = commandData.Application.ActiveUIDocument.Document;
                EnsureSavedProject(document);
                HubeiReportTemplate template = HubeiReportTemplateReader.Read(selection.TemplatePath);
                HubeiReportTemplateReader.PrepareForDocument(template, document);
                if (!ConfirmMergedParameters(template))
                {
                    return Result.Cancelled;
                }

                IReadOnlyList<HubeiReportTemplateRow> definitions = HubeiReportTemplateReader.MergeParameterRows(template.Rows);
                HubeiReportResult result = ApplyDefinitions(document, definitions, template.Rows, selection.RemoveExistingParameters);
                string hifcPath = WriteHifcFile(document, template.Rows);
                ShowResult(result, definitions.Count, hifcPath);
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

        private static bool ConfirmMergedParameters(HubeiReportTemplate template)
        {
            string[] duplicateNames = template.Rows.GroupBy(row => row.RevitParameterName, StringComparer.Ordinal)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToArray();
            if (duplicateNames.Length == 0)
            {
                return true;
            }

            string message = "以下同名参数将合并其 Revit类别绑定：\n" + string.Join("、", duplicateNames) + "\n\n是否继续？";
            return TaskDialog.Show("湖北报规参数", message, TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No) == TaskDialogResult.Yes;
        }

        private static void EnsureSavedProject(Document document)
        {
            if (document == null || string.IsNullOrWhiteSpace(document.PathName))
            {
                throw new InvalidOperationException("请先保存当前 Revit 项目，以便生成 项目名-HIFC.txt。");
            }
        }

        private static HubeiReportResult ApplyDefinitions(Document document, IReadOnlyList<HubeiReportTemplateRow> definitions, IReadOnlyList<HubeiReportTemplateRow> valueRows, bool removeExistingParameters)
        {
            var result = new HubeiReportResult();
            string sharedFilePath = Path.Combine(Path.GetTempPath(), "PlugHub.HubeiReportParameters.shared");
            File.WriteAllText(sharedFilePath, CreateSharedParameterTemplate(), new UTF8Encoding(false));

            string originalSharedParameterPath = document.Application.SharedParametersFilename;
            document.Application.SharedParametersFilename = sharedFilePath;
            try
            {
                DefinitionFile definitionFile = document.Application.OpenSharedParameterFile();
                if (definitionFile == null)
                {
                    throw new InvalidOperationException("无法打开共享参数文件。");
                }

                DefinitionGroup group = definitionFile.Groups.get_Item("PlugHub_HubeiReport") ?? definitionFile.Groups.Create("PlugHub_HubeiReport");
                using (var transaction = new Transaction(document, "湖北报规模板参数同步"))
                {
                    transaction.Start();
                    foreach (HubeiReportTemplateRow definition in definitions)
                    {
                        ApplyDefinition(document, group, definition, removeExistingParameters, result);
                    }

                    transaction.Commit();
                }

                using (var transaction = new Transaction(document, "湖北报规模板参数赋值"))
                {
                    transaction.Start();
                    foreach (HubeiReportTemplateRow definition in valueRows)
                    {
                        FillValues(document, definition, result);
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

        private static void ApplyDefinition(Document document, DefinitionGroup group, HubeiReportTemplateRow definition, bool removeExistingParameters, HubeiReportResult result)
        {
            Definition existingDefinition = FindExistingDefinition(document, definition.RevitParameterName);
            if (existingDefinition != null && removeExistingParameters)
            {
                if (!document.ParameterBindings.Remove(existingDefinition))
                {
                    throw new InvalidOperationException("无法清除同名参数：" + definition.RevitParameterName + "。");
                }

                result.RemovedCount++;
                existingDefinition = null;
            }

            ElementBinding existingBinding = existingDefinition == null ? null : document.ParameterBindings.get_Item(existingDefinition) as ElementBinding;
            CategorySet categories = GetBindingCategories(document, definition.RevitCategories, existingBinding);
            if (existingDefinition != null)
            {
                EnsureCompatibleExistingDefinition(existingDefinition, existingBinding, definition);
                ReInsertBinding(document, existingDefinition, categories, definition.IsInstanceBinding);
                result.UpdatedBindingCount++;
                return;
            }

            ExternalDefinition externalDefinition = GetOrCreateDefinition(group, definition.RevitParameterName, definition.RevitParameterType);
            InsertBinding(document, externalDefinition, categories, definition.IsInstanceBinding);
            result.CreatedCount++;
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

        private static ExternalDefinition GetOrCreateDefinition(DefinitionGroup group, string name, ParameterType parameterType)
        {
            Definition existing = group.Definitions.get_Item(name);
            if (existing is ExternalDefinition externalDefinition)
            {
                if (externalDefinition.ParameterType != parameterType)
                {
                    throw new InvalidOperationException("共享参数 " + name + " 的属性类型与模板不一致。");
                }

                return externalDefinition;
            }

            return (ExternalDefinition)group.Definitions.Create(new ExternalDefinitionCreationOptions(name, parameterType) { Visible = true });
        }

        private static Definition FindExistingDefinition(Document document, string name)
        {
            DefinitionBindingMapIterator iterator = document.ParameterBindings.ForwardIterator();
            while (iterator.MoveNext())
            {
                if (iterator.Key != null && string.Equals(iterator.Key.Name, name, StringComparison.Ordinal))
                {
                    return iterator.Key;
                }
            }

            return null;
        }

        private static void EnsureCompatibleExistingDefinition(Definition existingDefinition, ElementBinding existingBinding, HubeiReportTemplateRow definition)
        {
            if (existingBinding == null)
            {
                throw new InvalidOperationException("同名参数 " + definition.RevitParameterName + " 的绑定类型不受支持。请勾选清除当前项目同名参数后重试。");
            }

            if (existingDefinition.ParameterType != definition.RevitParameterType)
            {
                throw new InvalidOperationException("同名参数 " + definition.RevitParameterName + " 的属性类型与模板不一致。请勾选清除当前项目同名参数后重试。");
            }

            bool isInstanceBinding = existingBinding is InstanceBinding;
            if (isInstanceBinding != definition.IsInstanceBinding)
            {
                throw new InvalidOperationException("同名参数 " + definition.RevitParameterName + " 的 I/T 类型与模板不一致。请勾选清除当前项目同名参数后重试。");
            }
        }

        private static CategorySet GetBindingCategories(Document document, IReadOnlyCollection<Category> requestedCategories, ElementBinding existingBinding)
        {
            CategorySet categorySet = document.Application.Create.NewCategorySet();
            if (existingBinding != null)
            {
                foreach (Category category in existingBinding.Categories)
                {
                    categorySet.Insert(category);
                }
            }

            foreach (Category category in requestedCategories)
            {
                if (category == null || !category.AllowsBoundParameters)
                {
                    throw new InvalidOperationException("Revit 类别不支持共享参数绑定：" + (category == null ? "<null>" : category.Name) + "。");
                }

                if (!categorySet.Contains(category))
                {
                    categorySet.Insert(category);
                }
            }

            return categorySet;
        }

        private static void InsertBinding(Document document, Definition definition, CategorySet categories, bool isInstanceBinding)
        {
            ElementBinding binding = isInstanceBinding
                ? (ElementBinding)document.Application.Create.NewInstanceBinding(categories)
                : document.Application.Create.NewTypeBinding(categories);
            if (!document.ParameterBindings.Insert(definition, binding, BuiltInParameterGroup.PG_DATA))
            {
                throw new InvalidOperationException("无法创建共享参数绑定：" + definition.Name + "。");
            }
        }

        private static void ReInsertBinding(Document document, Definition definition, CategorySet categories, bool isInstanceBinding)
        {
            ElementBinding binding = isInstanceBinding
                ? (ElementBinding)document.Application.Create.NewInstanceBinding(categories)
                : document.Application.Create.NewTypeBinding(categories);
            if (!document.ParameterBindings.ReInsert(definition, binding, BuiltInParameterGroup.PG_DATA))
            {
                throw new InvalidOperationException("无法更新共享参数绑定：" + definition.Name + "。");
            }
        }

        private static void FillValues(Document document, HubeiReportTemplateRow definition, HubeiReportResult result)
        {
            bool hasActualValue = !string.IsNullOrWhiteSpace(definition.ActualValue);
            foreach (Element element in CollectTargetElements(document, definition))
            {
                Parameter parameter = element.LookupParameter(definition.RevitParameterName);
                if (parameter == null || parameter.IsReadOnly || !TrySetValue(parameter, definition.Value))
                {
                    result.SkippedValueCount++;
                    continue;
                }

                if (hasActualValue)
                {
                    result.ActualValueCount++;
                }
                else
                {
                    result.DefaultValueCount++;
                }
            }
        }

        private static IEnumerable<Element> CollectTargetElements(Document document, HubeiReportTemplateRow definition)
        {
            HashSet<int> categoryIds = new HashSet<int>(definition.RevitCategories.Select(category => category.Id.IntegerValue));
            if (definition.IsInstanceBinding && categoryIds.Remove((int)BuiltInCategory.OST_ProjectInformation))
            {
                yield return document.ProjectInformation;
            }

            FilteredElementCollector collector = new FilteredElementCollector(document);
            IEnumerable<Element> elements = definition.IsInstanceBinding
                ? collector.WhereElementIsNotElementType()
                : collector.WhereElementIsElementType();
            foreach (Element element in elements)
            {
                if (element.Category != null && categoryIds.Contains(element.Category.Id.IntegerValue))
                {
                    yield return element;
                }
            }
        }

        private static bool TrySetValue(Parameter parameter, string value)
        {
            switch (parameter.StorageType)
            {
                case StorageType.String:
                    return parameter.Set(value ?? string.Empty);
                case StorageType.Integer:
                    if (IsBooleanValue(value, out int booleanValue))
                    {
                        return parameter.Set(booleanValue);
                    }

                    return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int integerValue) && parameter.Set(integerValue);
                case StorageType.Double:
                    if (parameter.SetValueString(value))
                    {
                        return true;
                    }

                    return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double numberValue) && parameter.Set(numberValue);
                default:
                    return false;
            }
        }

        private static bool IsBooleanValue(string value, out int result)
        {
            if (string.Equals(value, "是", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1")
            {
                result = 1;
                return true;
            }

            if (string.Equals(value, "否", StringComparison.OrdinalIgnoreCase) || string.Equals(value, "false", StringComparison.OrdinalIgnoreCase) || value == "0")
            {
                result = 0;
                return true;
            }

            result = 0;
            return false;
        }

        private static string WriteHifcFile(Document document, IReadOnlyCollection<HubeiReportTemplateRow> rows)
        {
            string directory = Path.GetDirectoryName(document.PathName);
            string name = Path.GetFileNameWithoutExtension(document.PathName);
            string path = Path.Combine(directory, name + "-HIFC.txt");
            File.WriteAllText(path, HubeiReportTemplateWriter.BuildHifcText(rows), new UTF8Encoding(true));
            return path;
        }

        private static void ShowResult(HubeiReportResult result, int definitionCount, string hifcPath)
        {
            string message = string.Format(
                "处理完成。\n创建: {0}\n更新绑定: {1}\n清除同名参数: {2}\n真实数据写入: {3}\n默认值写入: {4}\n未写入: {5}\n参数总数: {6}\nHIFC 文件: {7}",
                result.CreatedCount,
                result.UpdatedBindingCount,
                result.RemovedCount,
                result.ActualValueCount,
                result.DefaultValueCount,
                result.SkippedValueCount,
                definitionCount,
                hifcPath);
            TaskDialog.Show("湖北报规参数", message);
        }
    }
}
