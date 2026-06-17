using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using PlugHub.ClearHeightAnalysis.Services;

namespace PlugHub.ClearHeightAnalysis
{
    [Transaction(TransactionMode.Manual)]
    public sealed class ClearHeightAnalysisCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApplication = commandData?.Application;
            UIDocument uiDocument = uiApplication?.ActiveUIDocument;
            Document document = uiDocument?.Document;
            if (uiApplication == null || uiDocument == null || document == null)
            {
                message = "未找到当前 Revit 文档。";
                return Result.Failed;
            }

            try
            {
                var outlineProvider = new BuildingOutlineProvider();
                var heatmapRenderer = new HeatmapRenderer();
                string mapperName = nameof(ObstacleProjectionMapper);
                if (outlineProvider == null || heatmapRenderer == null || string.IsNullOrWhiteSpace(mapperName))
                {
                    message = "净高分析服务初始化失败。";
                    return Result.Failed;
                }

                TaskDialog.Show("净高分析", "净高分析模块已注册，核心分析流程将在后续任务接入。当前版本不会修改模型。");
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
