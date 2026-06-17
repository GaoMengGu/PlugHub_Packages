using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using PlugHub.ClearHeightAnalysis.Models;
using PlugHub.ClearHeightAnalysis.Services;
using PlugHub.ClearHeightAnalysis.UI;

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
                ClearHeightAnalysisWindow window = new ClearHeightAnalysisWindow();
                IntPtr revitHandle = uiApplication.MainWindowHandle;
                if (revitHandle != IntPtr.Zero)
                {
                    var helper = new System.Windows.Interop.WindowInteropHelper(window);
                    helper.Owner = revitHandle;
                }

                bool? dialogResult = window.ShowDialog();
                if (dialogResult != true)
                {
                    return Result.Cancelled;
                }

                AnalysisSettings settings = window.Settings;
                var outlineProvider = new BuildingOutlineProvider();
                IReadOnlyList<Rect2d> masks = outlineProvider.GetOutlineMasks(uiDocument, document, settings);
                if (masks.Count == 0)
                {
                    message = "未能识别建筑外轮廓，请选择楼板或使用当前视图裁剪框。";
                    return Result.Failed;
                }

                Rect2d boundary = outlineProvider.GetBoundary(masks);
                List<GridCell> cells = GridBuilder.BuildInsideRectangles(boundary, masks, settings.GridSizeMillimeters, settings.LevelName, settings.LevelElevationMillimeters).ToList();
                if (cells.Count == 0)
                {
                    message = "分析范围内没有生成任何网格。";
                    return Result.Failed;
                }

                var obstacleCollector = new ObstacleCollector();
                IReadOnlyList<ObstacleProjection> obstacles = obstacleCollector.Collect(document, settings);
                if (obstacles.Count == 0)
                {
                    message = "没有收集到参与净高分析的结构、建筑或机电构件。";
                    return Result.Failed;
                }

                List<ClearHeightResult> results = cells
                    .Select(cell => ClearHeightCalculator.Calculate(cell, ObstacleProjectionMapper.FindCoveringObstacles(cell, obstacles), settings))
                    .ToList();

                using (var transaction = new Transaction(document, "生成净高分析热力图"))
                {
                    transaction.Start();
                    var cleanupService = new ResultCleanupService();
                    cleanupService.DeleteExistingResults(document);
                    var heatmapRenderer = new HeatmapRenderer();
                    heatmapRenderer.Render(document, document.ActiveView, results, settings, Guid.NewGuid().ToString("N"));
                    transaction.Commit();
                }

                AnalysisResultWindow resultWindow = new AnalysisResultWindow(results);
                if (revitHandle != IntPtr.Zero)
                {
                    var helper = new System.Windows.Interop.WindowInteropHelper(resultWindow);
                    helper.Owner = revitHandle;
                }

                resultWindow.ShowDialog();
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
