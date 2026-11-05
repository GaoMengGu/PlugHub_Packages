using System;
using System.Collections.Generic;
using System.Linq;
using PlugHub.ClearHeightAnalysis.Core.Models;

namespace PlugHub.ClearHeightAnalysis.Core.Services
{
    public static class AnalysisRequestValidator
    {
        public static IReadOnlyList<string> Validate(AnalysisRequest request)
        {
            if (request == null)
            {
                return new[] { "分析请求不能为空。" };
            }

            var errors = new List<string>();
            if (request.SelectedLevel == null)
                errors.Add("请选择真实 Revit 楼层。");

            List<SourceModelChoice> selected = request.SourceModels.Where(source => source.IsSelected).ToList();
            if (selected.Count == 0)
                errors.Add("请至少选择一个参与分析的模型。");
            if (selected.Any(source => !source.IsLoaded))
                errors.Add("所选链接模型包含未加载项。");
            if (request.BoundaryMode == AnalysisBoundaryMode.AutomaticHostFloors &&
                !selected.Any(source => source.IsHost))
                errors.Add("自动识别当前楼层楼板需要选择当前模型。");

            if (request.GridSizeMillimeters < 100 || request.GridSizeMillimeters > 5000)
                errors.Add("网格尺寸需为 100-5000mm。");
            if (request.ClearHeightThresholdMillimeters < 1000 || request.ClearHeightThresholdMillimeters > 10000)
                errors.Add("净高阈值需为 1000-10000mm。");
            if (request.FinishFloorOffsetMillimeters < 0 || request.FinishFloorOffsetMillimeters > 1000)
                errors.Add("完成面偏移需为 0-1000mm。");
            if (request.SearchHeightMillimeters < 1000 || request.SearchHeightMillimeters > 20000)
                errors.Add("搜索高度需为 1000-20000mm。");
            if (request.MinimumPipeDiameterMillimeters < 0 || request.MinimumPipeDiameterMillimeters > 2000)
                errors.Add("最小管径需为 0-2000mm。");

            return errors.AsReadOnly();
        }
    }
}
