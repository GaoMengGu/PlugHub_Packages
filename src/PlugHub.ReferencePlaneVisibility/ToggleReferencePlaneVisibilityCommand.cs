using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PlugHub.ReferencePlaneVisibility
{
    [Transaction(TransactionMode.Manual)]
    public sealed class ToggleReferencePlaneVisibilityCommand : IExternalCommand
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

            var referencePlaneCategoryId = new ElementId(BuiltInCategory.OST_CLines);
            if (!view.CanCategoryBeHidden(referencePlaneCategoryId))
            {
                message = "当前视图不支持隐藏参照平面类别。";
                return Result.Failed;
            }

            try
            {
                var shouldHide = !view.GetCategoryHidden(referencePlaneCategoryId);
                using (var transaction = new Transaction(document, shouldHide ? "隐藏参照平面" : "显示参照平面"))
                {
                    transaction.Start();
                    view.SetCategoryHidden(referencePlaneCategoryId, shouldHide);
                    transaction.Commit();
                }

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
