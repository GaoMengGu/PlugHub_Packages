using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitApplication = Autodesk.Revit.ApplicationServices.Application;

namespace PlugHub.FamilyFileSaver
{
    [Transaction(TransactionMode.Manual)]
    public sealed class SaveFamilyFilesCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument? uiDocument = commandData?.Application?.ActiveUIDocument;
            Document? document = uiDocument?.Document;
            if (uiDocument == null || document == null)
            {
                message = "未找到当前 Revit 文档。";
                return Result.Failed;
            }

            try
            {
                List<FamilyItem> families = CollectFamilies(document);
                if (families.Count == 0)
                {
                    TaskDialog.Show("保存族文件", "当前项目中未找到可保存的族。");
                    return Result.Succeeded;
                }

                FamilySelectionWindow window = new FamilySelectionWindow(families);
                IntPtr revitHandle = uiDocument.Application.MainWindowHandle;
                if (revitHandle != IntPtr.Zero)
                {
                    System.Windows.Interop.WindowInteropHelper helper =
                        new System.Windows.Interop.WindowInteropHelper(window);
                    helper.Owner = revitHandle;
                }

                bool? result = window.ShowDialog();
                if (result == true)
                {
                    List<FamilyItem> selectedFamilies = window.SelectedFamilies;
                    if (selectedFamilies.Count == 0)
                    {
                        TaskDialog.Show("保存族文件", "未选择任何族。");
                        return Result.Succeeded;
                    }

                    using (FolderBrowserDialog destDialog = new FolderBrowserDialog())
                    {
                        destDialog.Description = "选择保存族文件的目标文件夹";
                        destDialog.ShowNewFolderButton = true;
                        if (destDialog.ShowDialog() != DialogResult.OK)
                        {
                            return Result.Cancelled;
                        }
                        string saveFolder = destDialog.SelectedPath;

                        RevitApplication app = document.Application;
                        SaveFamilies(app, selectedFamilies, saveFolder);
                    }
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private static List<FamilyItem> CollectFamilies(Document document)
        {
            List<FamilyItem> items = new List<FamilyItem>();

            var familyInstances = new FilteredElementCollector(document)
                .OfClass(typeof(FamilyInstance))
                .ToElements()
                .Cast<FamilyInstance>();

            var familyGroups = familyInstances
                .Where(fi => fi.Symbol != null && fi.Symbol.Family != null)
                .GroupBy(fi => fi.Symbol.Family.Id)
                .Where(g => g.Key != ElementId.InvalidElementId);

            foreach (var group in familyGroups)
            {
                ElementId familyId = group.Key;
                Family family = group.First().Symbol.Family;
                if (family == null)
                    continue;

                if (family.FamilyPlacementType == FamilyPlacementType.Invalid)
                    continue;

                Category? category = family.FamilyCategory;
                string categoryName = category != null ? category.Name : "未分类";

                items.Add(new FamilyItem
                {
                    Id = familyId,
                    Name = family.Name,
                    Category = categoryName,
                    FamilyObject = family,
                    InstanceCount = group.Count()
                });
            }

            return items.OrderBy(f => f.Category).ThenBy(f => f.Name).ToList();
        }

        private static void SaveFamilies(RevitApplication app, List<FamilyItem> families, string saveFolder)
        {
            int successCount = 0;
            int failCount = 0;
            List<string> errors = new List<string>();

            foreach (FamilyItem item in families)
            {
                Document? familyDoc = null;
                try
                {
                    Family? family = item.FamilyObject;
                    if (family == null)
                    {
                        failCount++;
                        errors.Add(item.Name + ": 族对象为空");
                        continue;
                    }

                    familyDoc = family.Document.EditFamily(family);
                    if (familyDoc == null)
                    {
                        failCount++;
                        errors.Add(item.Name + ": 无法打开族文档");
                        continue;
                    }

                    string categoryFolder = Path.Combine(saveFolder, GetSafeFileName(item.Category));
                    if (!Directory.Exists(categoryFolder))
                    {
                        Directory.CreateDirectory(categoryFolder);
                    }

                    string safeFileName = GetSafeFileName(item.Name) + ".rfa";
                    string destPath = Path.Combine(categoryFolder, safeFileName);

                    int counter = 1;
                    while (File.Exists(destPath))
                    {
                        destPath = Path.Combine(categoryFolder, GetSafeFileName(item.Name) + "_" + counter + ".rfa");
                        counter++;
                    }

                    SaveAsOptions saveOptions = new SaveAsOptions();
                    saveOptions.OverwriteExistingFile = false;

                    familyDoc.SaveAs(destPath, saveOptions);
                    successCount++;
                }
                catch (Exception ex)
                {
                    failCount++;
                    errors.Add(item.Name + ": " + ex.Message);
                }
                finally
                {
                    if (familyDoc != null)
                    {
                        try { familyDoc.Close(false); } catch { }
                    }
                }
            }

            string resultMsg = "保存完成！\n成功: " + successCount + " 个族\n失败: " + failCount + " 个族";
            if (errors.Count > 0)
            {
                string errorDetail = string.Join("\n", errors.Take(10));
                if (errors.Count > 10)
                    errorDetail += "\n... 还有 " + (errors.Count - 10) + " 个错误";
                resultMsg += "\n\n失败详情:\n" + errorDetail;
            }

            TaskDialog.Show("保存族文件", resultMsg);
        }

        private static string GetSafeFileName(string name)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            char[] chars = name.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
            return new string(chars);
        }
    }
}
