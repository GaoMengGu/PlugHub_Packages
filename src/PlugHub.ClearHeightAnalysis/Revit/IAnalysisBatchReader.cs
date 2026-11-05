using System;

namespace PlugHub.ClearHeightAnalysis.Revit
{
    public interface IAnalysisBatchReader
    {
        int SchemaVersion { get; }
        Guid ManifestSchemaGuid { get; }
        Guid ChunkSchemaGuid { get; }
        string ManifestSchemaName { get; }
        string ChunkSchemaName { get; }
        bool IsLegacyReadOnly { get; }
    }
}
