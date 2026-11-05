using System;

namespace PlugHub.ClearHeightAnalysis.Revit
{
    public sealed class V2AnalysisBatchReader : IAnalysisBatchReader
    {
        public int SchemaVersion=>2;
        public Guid ManifestSchemaGuid=>new Guid("BA26FB44-C324-47AA-8F4E-53AF8C4108A1");
        public Guid ChunkSchemaGuid=>new Guid("8B9DF0AA-B751-4974-932D-3C8D409B12D2");
        public string ManifestSchemaName=>"PlugHubClearHeightBatchManifestV2";
        public string ChunkSchemaName=>"PlugHubClearHeightBatchChunkV2";
        public bool IsLegacyReadOnly=>true;
    }
}
