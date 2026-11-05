#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace PlugHub.ClearHeightAnalysis.Core.Models
{
    public enum DerivedOutputType
    {
        LightweightDrawing,
        AreaColorPlan,
        RasterHeatmap,
        Csv
    }

    public sealed class DerivedOutputRecord
    {
        public DerivedOutputRecord(
            string outputId,
            DerivedOutputType outputType,
            DateTime createdAtUtc,
            int? viewId,
            string? filePath,
            IEnumerable<int> elementIds)
        {
            OutputId = string.IsNullOrWhiteSpace(outputId)
                ? throw new ArgumentException("成果标识不能为空。", nameof(outputId))
                : outputId;
            if (createdAtUtc.Kind != DateTimeKind.Utc)
                throw new ArgumentException("成果时间必须使用 UTC。", nameof(createdAtUtc));
            OutputType = outputType;
            CreatedAtUtc = createdAtUtc;
            ViewId = viewId;
            FilePath = filePath;
            ElementIds = (elementIds ?? throw new ArgumentNullException(nameof(elementIds)))
                .Distinct().ToList().AsReadOnly();
        }

        public string OutputId { get; }
        public DerivedOutputType OutputType { get; }
        public DateTime CreatedAtUtc { get; }
        public int? ViewId { get; }
        public string? FilePath { get; }
        public IReadOnlyList<int> ElementIds { get; }
    }
}
