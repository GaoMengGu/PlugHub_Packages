using System;
using System.Collections.Generic;
using System.Linq;
using PlugHub.ClearHeightAnalysis.Core.Geometry;

namespace PlugHub.ClearHeightAnalysis.Core.Models
{
    public sealed class AnalysisSourceContextRecord
    {
        public AnalysisSourceContextRecord(string key, string displayName, bool isHost, Transform3d transform)
        {
            Key = string.IsNullOrWhiteSpace(key) ? throw new ArgumentException("来源模型标识不能为空。", nameof(key)) : key;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? key : displayName;
            IsHost = isHost;
            Transform = transform;
        }
        public string Key { get; }
        public string DisplayName { get; }
        public bool IsHost { get; }
        public Transform3d Transform { get; }
    }

    public sealed class AnalysisContextRecord
    {
        public AnalysisContextRecord(string viewUniqueId, string viewName, string viewType,
            string levelUniqueId, string levelName, double levelElevationMillimeters,
            IEnumerable<AnalysisSourceContextRecord> sources)
        {
            ViewUniqueId = viewUniqueId ?? string.Empty;
            ViewName = viewName ?? string.Empty;
            ViewType = viewType ?? string.Empty;
            LevelUniqueId = string.IsNullOrWhiteSpace(levelUniqueId) ? throw new ArgumentException("楼层标识不能为空。", nameof(levelUniqueId)) : levelUniqueId;
            LevelName = string.IsNullOrWhiteSpace(levelName) ? levelUniqueId : levelName;
            LevelElevationMillimeters = levelElevationMillimeters;
            Sources = (sources ?? throw new ArgumentNullException(nameof(sources))).ToList().AsReadOnly();
            if (Sources.Count == 0) throw new ArgumentException("分析上下文至少需要一个来源模型。", nameof(sources));
        }
        public string ViewUniqueId { get; }
        public string ViewName { get; }
        public string ViewType { get; }
        public string LevelUniqueId { get; }
        public string LevelName { get; }
        public double LevelElevationMillimeters { get; }
        public IReadOnlyList<AnalysisSourceContextRecord> Sources { get; }

        public static AnalysisContextRecord FromRunData(AnalysisRunData runData)
        {
            AnalysisLevelChoice level = runData.Request.SelectedLevel ??
                throw new ArgumentException("分析请求缺少楼层。", nameof(runData));
            return new AnalysisContextRecord(string.Empty, string.Empty, string.Empty,
                level.UniqueId, level.Name, level.ElevationMillimeters,
                runData.Request.SourceModels.Where(source => source.IsSelected).Select(source =>
                    new AnalysisSourceContextRecord(source.Key, source.DisplayName, source.IsHost, Transform3d.Identity)));
        }
    }
}
