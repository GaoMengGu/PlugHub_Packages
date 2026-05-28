using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitApplication = Autodesk.Revit.ApplicationServices.Application;

namespace PlugHub.FamilyMaterialParameters
{
    [Transaction(TransactionMode.Manual)]
    public sealed class BatchAddMaterialParameterCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (commandData == null)
            {
                message = "未收到 Revit 命令上下文。";
                return Result.Failed;
            }

            RevitApplication application = commandData.Application.Application;

            try
            {
                using (var openFileDialog = new OpenFileDialog())
                {
                    openFileDialog.Multiselect = true;
                    openFileDialog.Filter = "Revit Family Files (*.rfa)|*.rfa";
                    openFileDialog.Title = "选择族文件";

                    if (openFileDialog.ShowDialog() != DialogResult.OK)
                    {
                        return Result.Cancelled;
                    }

                    var familyFiles = openFileDialog.FileNames;
                    if (familyFiles.Length == 0)
                    {
                        message = "未选择任何族文件。";
                        return Result.Failed;
                    }

                    var result = ProcessFamilies(application, familyFiles, "材质");
                    ShowResult(result);
                    return Result.Succeeded;
                }
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private static BatchResult ProcessFamilies(RevitApplication application, IEnumerable<string> familyFiles, string parameterName)
        {
            var result = new BatchResult();

            foreach (var familyFile in familyFiles)
            {
                Document? familyDocument = null;
                try
                {
                    familyDocument = application.OpenDocumentFile(familyFile);

                    using (var transaction = new Transaction(familyDocument, "添加材质参数"))
                    {
                        transaction.Start();
                        AddMaterialParameterToFamily(familyDocument, parameterName);
                        transaction.Commit();
                    }

                    familyDocument.Save();
                    familyDocument.Close(false);
                    familyDocument = null;
                    result.SuccessCount++;
                }
                catch (Exception ex)
                {
                    result.FailCount++;
                    result.FailedFiles.Add(string.Format("{0}: {1}", Path.GetFileName(familyFile), ex.Message));

                    if (familyDocument != null)
                    {
                        try
                        {
                            familyDocument.Close(false);
                        }
                        catch
                        {
                        }
                    }
                }
            }

            return result;
        }

        private static void AddMaterialParameterToFamily(Document document, string parameterName)
        {
            var familyManager = document.FamilyManager;
            var existingParameter = FindParameter(familyManager, parameterName);
            if (existingParameter != null)
            {
                familyManager.RemoveParameter(existingParameter);
            }

            var materialParameter = familyManager.AddParameter(
                parameterName,
                BuiltInParameterGroup.PG_MATERIALS,
                ParameterType.Material,
                true);

            var concreteMaterialId = GetOrCreateConcreteMaterial(document);
            if (concreteMaterialId != ElementId.InvalidElementId)
            {
                familyManager.Set(materialParameter, concreteMaterialId);
            }

            var elementIds = new FilteredElementCollector(document)
                .WhereElementIsNotElementType()
                .ToElementIds();

            foreach (var elementId in elementIds)
            {
                var element = document.GetElement(elementId);
                if (element != null)
                {
                    BindElementToMaterialParameter(familyManager, element, materialParameter);
                }
            }
        }

        private static FamilyParameter? FindParameter(FamilyManager familyManager, string parameterName)
        {
            foreach (FamilyParameter parameter in familyManager.Parameters)
            {
                if (string.Equals(parameter.Definition.Name, parameterName, StringComparison.Ordinal))
                {
                    return parameter;
                }
            }

            return null;
        }

        private static void BindElementToMaterialParameter(FamilyManager familyManager, Element element, FamilyParameter materialParameter)
        {
            if (element is GenericForm form && !form.IsSolid)
            {
                return;
            }

            if (!(element is GenericForm) && !(element is FamilyInstance))
            {
                return;
            }

            try
            {
                var elementMaterialParameter = element.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);
                if (elementMaterialParameter != null)
                {
                    familyManager.AssociateElementParameterToFamilyParameter(elementMaterialParameter, materialParameter);
                }
            }
            catch
            {
            }
        }

        private static ElementId GetOrCreateConcreteMaterial(Document document)
        {
            foreach (Element material in new FilteredElementCollector(document).OfClass(typeof(Material)))
            {
                if (IsConcreteMaterial(material.Name))
                {
                    return material.Id;
                }
            }

            var newMaterialId = Material.Create(document, "混凝土");
            if (newMaterialId != ElementId.InvalidElementId)
            {
                return newMaterialId;
            }

            foreach (Element material in new FilteredElementCollector(document).OfClass(typeof(Material)))
            {
                return material.Id;
            }

            return ElementId.InvalidElementId;
        }

        private static bool IsConcreteMaterial(string name)
        {
            return string.Equals(name, "混凝土", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Concrete", StringComparison.OrdinalIgnoreCase)
                || name.IndexOf("混凝土", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Concrete", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static void ShowResult(BatchResult result)
        {
            var resultMessage = string.Format(
                "处理完成！\n成功: {0} 个文件\n失败: {1} 个文件",
                result.SuccessCount,
                result.FailCount);

            if (result.FailedFiles.Count > 0)
            {
                resultMessage += "\n\n失败文件详情:\n" + string.Join("\n", result.FailedFiles);
            }

            TaskDialog.Show("批量添加材质参数", resultMessage);
        }

        private sealed class BatchResult
        {
            public int SuccessCount { get; set; }
            public int FailCount { get; set; }
            public List<string> FailedFiles { get; } = new List<string>();
        }
    }
}
