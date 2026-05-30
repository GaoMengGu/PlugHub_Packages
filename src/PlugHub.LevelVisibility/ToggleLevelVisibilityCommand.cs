using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PlugHub.LevelVisibility
{
    [Transaction(TransactionMode.Manual)]
    public sealed class ToggleLevelVisibilityCommand : IExternalCommand
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

            var levelCategoryId = new ElementId(BuiltInCategory.OST_Levels);
            if (!view.CanCategoryBeHidden(levelCategoryId))
            {
                message = "当前视图不支持隐藏标高类别。";
                return Result.Failed;
            }

            try
            {
                var shouldHide = !view.GetCategoryHidden(levelCategoryId);
                using (var transaction = new Transaction(document, shouldHide ? "隐藏标高" : "显示标高"))
                {
                    transaction.Start();
                    view.SetCategoryHidden(levelCategoryId, shouldHide);
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
