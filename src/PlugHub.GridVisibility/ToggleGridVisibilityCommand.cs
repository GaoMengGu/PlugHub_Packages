using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PlugHub.GridVisibility
{
    [Transaction(TransactionMode.Manual)]
    public sealed class ToggleGridVisibilityCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiDocument = commandData?.Application?.ActiveUIDocument;
            var document = uiDocument?.Document;
            if (document == null)
            {
                message = "未找到当前 Revit 文档。";
                return Result.Failed;
            }

            var view = document.ActiveView;
            if (view == null)
            {
                message = "未找到当前视图。";
                return Result.Failed;
            }

            var gridCategoryId = new ElementId(BuiltInCategory.OST_Grids);
            if (!view.CanCategoryBeHidden(gridCategoryId))
            {
                message = "当前视图不支持隐藏轴网类别。";
                return Result.Failed;
            }

            try
            {
                var shouldHide = !view.GetCategoryHidden(gridCategoryId);
                using (var transaction = new Transaction(document, shouldHide ? "隐藏轴网" : "显示轴网"))
                {
                    transaction.Start();
                    view.SetCategoryHidden(gridCategoryId, shouldHide);
                    transaction.Commit();
                }

                TaskDialog.Show("轴网显隐切换", shouldHide ? "已隐藏当前视图中的轴网。" : "已显示当前视图中的轴网。");
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
