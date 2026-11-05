using System;

namespace PlugHub.ClearHeightAnalysis.Revit
{
    public sealed class V3AnalysisBatchReader : IAnalysisBatchReader
    {
        public int SchemaVersion=>3;
        public Guid ManifestSchemaGuid=>new Guid("D83FE899-7A89-45F3-95A6-0CC310D2E9E3");
        public Guid ChunkSchemaGuid=>new Guid("E55A5B48-7D4B-493F-BFD8-C549661086B7");
        public string ManifestSchemaName=>"PlugHubClearHeightBatchManifestV3";
        public string ChunkSchemaName=>"PlugHubClearHeightBatchChunkV3";
        public bool IsLegacyReadOnly=>false;
    }
}
