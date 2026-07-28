using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using PlugHub.ClearHeightAnalysis.Core.Models;
using PlugHub.ClearHeightAnalysis.Core.Persistence;

namespace PlugHub.ClearHeightAnalysis.Revit
{
    public enum StoredBatchStatus { Available, Corrupt, UnsupportedVersion }

    public sealed class StoredBatchInfo
    {
        public StoredBatchInfo(string batchId, DateTime createdAtUtc, string levelName, int gridCount,
            int problemCount, StoredBatchStatus status, string statusMessage)
        {
            BatchId=batchId; CreatedAtUtc=createdAtUtc; LevelName=levelName; GridCount=gridCount;
            ProblemCount=problemCount; Status=status; StatusMessage=statusMessage;
        }
        public string BatchId { get; }
        public DateTime CreatedAtUtc { get; }
        public string LevelName { get; }
        public int GridCount { get; }
        public int ProblemCount { get; }
        public StoredBatchStatus Status { get; }
        public string StatusMessage { get; }
        public string DisplayName => CreatedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm") + "  " +
            LevelName + "  " + GridCount + "格  " + (Status == StoredBatchStatus.Available ? "可用" : StatusMessage);
    }

    public sealed class AnalysisBatchRepository
    {
        private static readonly Guid ManifestSchemaGuid = new Guid("BA26FB44-C324-47AA-8F4E-53AF8C4108A1");
        private static readonly Guid ChunkSchemaGuid = new Guid("8B9DF0AA-B751-4974-932D-3C8D409B12D2");

        private const string BatchIdField = "BatchId";
        private const string VersionField = "SchemaVersion";
        private const string CreatedField = "CreatedUtc";
        private const string LevelField = "LevelName";
        private const string GridCountField = "GridCount";
        private const string ProblemCountField = "ProblemCount";
        private const string ChunkCountField = "ChunkCount";
        private const string LengthField = "UncompressedLength";
        private const string HashField = "Sha256";
        private const string ChunkSequence = "ChunkSequence";
        private const string PayloadField = "Payload";

        public void Save(Document document, AnalysisBatch batch)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            AnalysisBatchEnvelope envelope = AnalysisBatchSerializer.Serialize(batch);
            Schema manifestSchema = EnsureManifestSchema();
            Schema chunkSchema = EnsureChunkSchema();
            using (var transaction = new Transaction(document, "保存净高分析批次"))
            {
                transaction.Start();
                DeleteStorageElements(document, batch.BatchId);
                DataStorage manifest = DataStorage.Create(document);
                var entity = new Entity(manifestSchema);
                entity.Set(BatchIdField, batch.BatchId);
                entity.Set(VersionField, envelope.SchemaVersion);
                entity.Set(CreatedField, batch.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture));
                entity.Set(LevelField, batch.RunData.Request.SelectedLevel?.Name ?? string.Empty);
                entity.Set(GridCountField, batch.RunData.Cells.Count);
                entity.Set(ProblemCountField, batch.RunData.Summary.Results.Count(result => result.Status != CellStatus.Passed));
                entity.Set(ChunkCountField, envelope.Chunks.Count);
                entity.Set(LengthField, envelope.UncompressedLength.ToString(CultureInfo.InvariantCulture));
                entity.Set(HashField, envelope.Sha256);
                manifest.SetEntity(entity);
                foreach (AnalysisBatchChunk chunk in envelope.Chunks)
                {
                    DataStorage storage = DataStorage.Create(document);
                    var chunkEntity = new Entity(chunkSchema);
                    chunkEntity.Set(BatchIdField, batch.BatchId);
                    chunkEntity.Set(ChunkSequence, chunk.Sequence);
                    chunkEntity.Set(PayloadField, chunk.Payload);
                    storage.SetEntity(chunkEntity);
                }
                transaction.Commit();
            }
        }

        public IReadOnlyList<StoredBatchInfo> List(Document document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            Schema schema = EnsureManifestSchema();
            var result = new List<StoredBatchInfo>();
            foreach (DataStorage storage in CollectStorage(document))
            {
                Entity entity = storage.GetEntity(schema);
                if (!entity.IsValid()) continue;
                string id = entity.Get<string>(BatchIdField);
                DateTime.TryParse(entity.Get<string>(CreatedField), CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out DateTime created);
                StoredBatchStatus status = StoredBatchStatus.Available;
                string message = string.Empty;
                int version = entity.Get<int>(VersionField);
                if (version != AnalysisBatch.CurrentSchemaVersion)
                {
                    status = StoredBatchStatus.UnsupportedVersion;
                    message = "不支持的批次版本 " + version + "。";
                }
                else
                {
                    try { Load(document, id); }
                    catch (AnalysisBatchVersionException ex) { status=StoredBatchStatus.UnsupportedVersion; message=ex.Message; }
                    catch (AnalysisBatchCorruptException ex) { status=StoredBatchStatus.Corrupt; message=ex.Message; }
                    catch (Exception ex) { status=StoredBatchStatus.Corrupt; message="批次数据损坏："+ex.Message; }
                }
                result.Add(new StoredBatchInfo(id, created.ToUniversalTime(), entity.Get<string>(LevelField),
                    entity.Get<int>(GridCountField), entity.Get<int>(ProblemCountField), status, message));
            }
            return result.OrderByDescending(item => item.CreatedAtUtc).ToList().AsReadOnly();
        }

        public AnalysisBatch Load(Document document, string batchId)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (string.IsNullOrWhiteSpace(batchId)) throw new ArgumentException("批次标识不能为空。", nameof(batchId));
            Schema manifestSchema = EnsureManifestSchema();
            Schema chunkSchema = EnsureChunkSchema();
            Entity manifest = CollectStorage(document).Select(storage => storage.GetEntity(manifestSchema))
                .FirstOrDefault(entity => entity.IsValid() && entity.Get<string>(BatchIdField) == batchId);
            if (manifest == null || !manifest.IsValid()) throw new KeyNotFoundException("未找到净高分析批次 " + batchId + "。");
            int chunkCount = manifest.Get<int>(ChunkCountField);
            List<AnalysisBatchChunk> chunks = CollectStorage(document).Select(storage => storage.GetEntity(chunkSchema))
                .Where(entity => entity.IsValid() && entity.Get<string>(BatchIdField) == batchId)
                .Select(entity => new AnalysisBatchChunk(entity.Get<int>(ChunkSequence), entity.Get<string>(PayloadField)))
                .OrderBy(chunk => chunk.Sequence).ToList();
            if (chunks.Count != chunkCount) throw new AnalysisBatchCorruptException("批次分块数量不一致。");
            if (!long.TryParse(manifest.Get<string>(LengthField), NumberStyles.None, CultureInfo.InvariantCulture, out long length))
                throw new AnalysisBatchCorruptException("批次长度清单无效。");
            var envelope = new AnalysisBatchEnvelope(manifest.Get<int>(VersionField), batchId, length,
                manifest.Get<string>(HashField), chunks);
            return AnalysisBatchSerializer.Deserialize(envelope);
        }

        public void DeleteBatchData(Document document, string batchId)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            using (var transaction = new Transaction(document, "删除净高分析批次数据"))
            {
                transaction.Start();
                DeleteStorageElements(document, batchId);
                transaction.Commit();
            }
        }

        private static void DeleteStorageElements(Document document, string batchId)
        {
            Schema manifest = EnsureManifestSchema();
            Schema chunk = EnsureChunkSchema();
            List<ElementId> ids = CollectStorage(document).Where(storage =>
            {
                Entity first = storage.GetEntity(manifest);
                if (first.IsValid() && first.Get<string>(BatchIdField) == batchId) return true;
                Entity second = storage.GetEntity(chunk);
                return second.IsValid() && second.Get<string>(BatchIdField) == batchId;
            }).Select(storage => storage.Id).ToList();
            if (ids.Count > 0) document.Delete(ids);
        }

        private static IEnumerable<DataStorage> CollectStorage(Document document) =>
            new FilteredElementCollector(document).OfClass(typeof(DataStorage)).Cast<DataStorage>();

        private static Schema EnsureManifestSchema()
        {
            Schema existing = Schema.Lookup(ManifestSchemaGuid);
            if (existing != null) return existing;
            var builder = BaseBuilder(ManifestSchemaGuid, "PlugHubClearHeightBatchManifestV2");
            builder.AddSimpleField(BatchIdField, typeof(string)); builder.AddSimpleField(VersionField, typeof(int));
            builder.AddSimpleField(CreatedField, typeof(string)); builder.AddSimpleField(LevelField, typeof(string));
            builder.AddSimpleField(GridCountField, typeof(int)); builder.AddSimpleField(ProblemCountField, typeof(int));
            builder.AddSimpleField(ChunkCountField, typeof(int)); builder.AddSimpleField(LengthField, typeof(string));
            builder.AddSimpleField(HashField, typeof(string));
            return builder.Finish();
        }

        private static Schema EnsureChunkSchema()
        {
            Schema existing = Schema.Lookup(ChunkSchemaGuid);
            if (existing != null) return existing;
            var builder = BaseBuilder(ChunkSchemaGuid, "PlugHubClearHeightBatchChunkV2");
            builder.AddSimpleField(BatchIdField, typeof(string)); builder.AddSimpleField(ChunkSequence, typeof(int));
            builder.AddSimpleField(PayloadField, typeof(string));
            return builder.Finish();
        }

        private static SchemaBuilder BaseBuilder(Guid guid, string name)
        {
            var builder = new SchemaBuilder(guid); builder.SetSchemaName(name);
            builder.SetReadAccessLevel(AccessLevel.Public); builder.SetWriteAccessLevel(AccessLevel.Public); return builder;
        }
    }
}
