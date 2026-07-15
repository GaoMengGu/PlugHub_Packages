using System;
using System.Collections.Generic;
using System.Linq;

namespace PlugHub.ClearHeightAnalysis.Core.Models
{
    public sealed class AnalysisBatch
    {
        public const int CurrentSchemaVersion = 2;

        public AnalysisBatch(
            int schemaVersion,
            string batchId,
            DateTime createdAtUtc,
            string documentKey,
            string documentTitle,
            AnalysisRunData runData,
            IEnumerable<DerivedOutputRecord> outputs)
        {
            if (schemaVersion <= 0) throw new ArgumentOutOfRangeException(nameof(schemaVersion));
            if (string.IsNullOrWhiteSpace(batchId)) throw new ArgumentException("批次标识不能为空。", nameof(batchId));
            if (createdAtUtc.Kind != DateTimeKind.Utc) throw new ArgumentException("批次时间必须使用 UTC。", nameof(createdAtUtc));
            if (string.IsNullOrWhiteSpace(documentKey)) throw new ArgumentException("文档标识不能为空。", nameof(documentKey));
            SchemaVersion = schemaVersion;
            BatchId = batchId;
            CreatedAtUtc = createdAtUtc;
            DocumentKey = documentKey;
            DocumentTitle = string.IsNullOrWhiteSpace(documentTitle) ? documentKey : documentTitle;
            RunData = runData ?? throw new ArgumentNullException(nameof(runData));
            Outputs = (outputs ?? throw new ArgumentNullException(nameof(outputs))).ToList().AsReadOnly();
        }

        public int SchemaVersion { get; }
        public string BatchId { get; }
        public DateTime CreatedAtUtc { get; }
        public string DocumentKey { get; }
        public string DocumentTitle { get; }
        public AnalysisRunData RunData { get; }
        public IReadOnlyList<DerivedOutputRecord> Outputs { get; }

        public static AnalysisBatch Create(string documentKey, string documentTitle, AnalysisRunData runData)
        {
            return new AnalysisBatch(CurrentSchemaVersion, Guid.NewGuid().ToString("N"), DateTime.UtcNow,
                documentKey, documentTitle, runData, Array.Empty<DerivedOutputRecord>());
        }

        public AnalysisBatch WithOutputs(IEnumerable<DerivedOutputRecord> outputs)
        {
            return new AnalysisBatch(SchemaVersion, BatchId, CreatedAtUtc, DocumentKey, DocumentTitle, RunData, outputs);
        }
    }
}
