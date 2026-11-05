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
    public enum StoredBatchStatus { Available, LegacyReadOnly, Corrupt, UnsupportedVersion }

    public sealed class StoredBatchInfo
    {
        public StoredBatchInfo(string batchId, DateTime createdAtUtc, string levelName, int gridCount,
            int problemCount, StoredBatchStatus status, string statusMessage, int schemaVersion=0)
        {
            BatchId=batchId; CreatedAtUtc=createdAtUtc; LevelName=levelName; GridCount=gridCount;
            ProblemCount=problemCount; Status=status; StatusMessage=statusMessage; SchemaVersion=schemaVersion;
        }
        public string BatchId { get; }
        public DateTime CreatedAtUtc { get; }
        public string LevelName { get; }
        public int GridCount { get; }
        public int ProblemCount { get; }
        public StoredBatchStatus Status { get; }
        public string StatusMessage { get; }
        public int SchemaVersion { get; }
        public bool CanOpen => Status==StoredBatchStatus.Available||Status==StoredBatchStatus.LegacyReadOnly;
        public string DisplayName => CreatedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm") + "  " +
            LevelName + "  " + GridCount + "格  " + (Status == StoredBatchStatus.Available ? "可用" : Status==StoredBatchStatus.LegacyReadOnly?"旧版只读":StatusMessage);
    }

    public sealed class AnalysisBatchRepository
    {
        private static readonly IAnalysisBatchReader[] Readers = { new V3AnalysisBatchReader(), new V2AnalysisBatchReader() };
        private readonly Dictionary<string, Tuple<StoredBatchStatus,string>> _loadDiagnostics =
            new Dictionary<string, Tuple<StoredBatchStatus,string>>(StringComparer.Ordinal);

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
            Save(document,batch,envelope);
        }

        public void Save(Document document,AnalysisBatch batch,AnalysisBatchEnvelope envelope)
        {
            if(document==null)throw new ArgumentNullException(nameof(document));if(batch==null)throw new ArgumentNullException(nameof(batch));if(envelope==null)throw new ArgumentNullException(nameof(envelope));
            if(envelope.SchemaVersion!=batch.SchemaVersion||!string.Equals(envelope.BatchId,batch.BatchId,StringComparison.Ordinal))throw new ArgumentException("批次正文与清单不一致。",nameof(envelope));
            IAnalysisBatchReader reader=Readers[0];
            Schema manifestSchema = EnsureManifestSchema(reader);
            Schema chunkSchema = EnsureChunkSchema(reader);
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
            var result = new List<StoredBatchInfo>();
            foreach(IAnalysisBatchReader reader in Readers)
            {
                Schema schema=EnsureManifestSchema(reader);
                foreach (DataStorage storage in CollectStorage(document))
                {
                    Entity entity=storage.GetEntity(schema);if(!entity.IsValid())continue;
                    string id=entity.Get<string>(BatchIdField);DateTime.TryParse(entity.Get<string>(CreatedField),CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind,out DateTime created);
                    int version=entity.Get<int>(VersionField);StoredBatchStatus status=version!=reader.SchemaVersion?StoredBatchStatus.UnsupportedVersion:reader.IsLegacyReadOnly?StoredBatchStatus.LegacyReadOnly:StoredBatchStatus.Available;
                    string message=status==StoredBatchStatus.UnsupportedVersion?"不支持的批次版本 "+version+"。":reader.IsLegacyReadOnly?"旧版矩形近似，只读兼容。":string.Empty;
                    if(_loadDiagnostics.TryGetValue(id,out Tuple<StoredBatchStatus,string> diagnostic)){status=diagnostic.Item1;message=diagnostic.Item2;}
                    result.Add(new StoredBatchInfo(id,created.ToUniversalTime(),entity.Get<string>(LevelField),entity.Get<int>(GridCountField),entity.Get<int>(ProblemCountField),status,message,version));
                }
            }
            return result.OrderByDescending(item => item.CreatedAtUtc).ToList().AsReadOnly();
        }

        public AnalysisBatch Load(Document document, string batchId)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));
            if (string.IsNullOrWhiteSpace(batchId)) throw new ArgumentException("批次标识不能为空。", nameof(batchId));
            IAnalysisBatchReader? reader=null; Entity? manifest=null; Schema? chunkSchema=null;
            foreach(IAnalysisBatchReader candidate in Readers)
            {
                Schema schema=EnsureManifestSchema(candidate);Entity found=CollectStorage(document).Select(storage=>storage.GetEntity(schema)).FirstOrDefault(entity=>entity.IsValid()&&entity.Get<string>(BatchIdField)==batchId);
                if(found!=null&&found.IsValid()){reader=candidate;manifest=found;chunkSchema=EnsureChunkSchema(candidate);break;}
            }
            if (manifest == null || !manifest.IsValid() || reader==null || chunkSchema==null) throw new KeyNotFoundException("未找到净高分析批次 " + batchId + "。");
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
            try
            {
                AnalysisBatch batch=AnalysisBatchSerializer.Deserialize(envelope);
                _loadDiagnostics.Remove(batchId);
                return batch;
            }
            catch(AnalysisBatchVersionException ex)
            {
                _loadDiagnostics[batchId]=Tuple.Create(StoredBatchStatus.UnsupportedVersion,ex.Message);
                throw;
            }
            catch(AnalysisBatchCorruptException ex)
            {
                _loadDiagnostics[batchId]=Tuple.Create(StoredBatchStatus.Corrupt,ex.Message);
                throw;
            }
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
            _loadDiagnostics.Remove(batchId);
        }

        private static void DeleteStorageElements(Document document, string batchId)
        {
            List<ElementId> ids = CollectStorage(document).Where(storage =>
            {
                foreach(IAnalysisBatchReader reader in Readers){Entity first=storage.GetEntity(EnsureManifestSchema(reader));if(first.IsValid()&&first.Get<string>(BatchIdField)==batchId)return true;Entity second=storage.GetEntity(EnsureChunkSchema(reader));if(second.IsValid()&&second.Get<string>(BatchIdField)==batchId)return true;}return false;
            }).Select(storage => storage.Id).ToList();
            if (ids.Count > 0) document.Delete(ids);
        }

        private static IEnumerable<DataStorage> CollectStorage(Document document) =>
            new FilteredElementCollector(document).OfClass(typeof(DataStorage)).Cast<DataStorage>();

        private static Schema EnsureManifestSchema(IAnalysisBatchReader reader)
        {
            Schema existing = Schema.Lookup(reader.ManifestSchemaGuid);
            if (existing != null) return existing;
            var builder = BaseBuilder(reader.ManifestSchemaGuid, reader.ManifestSchemaName);
            builder.AddSimpleField(BatchIdField, typeof(string)); builder.AddSimpleField(VersionField, typeof(int));
            builder.AddSimpleField(CreatedField, typeof(string)); builder.AddSimpleField(LevelField, typeof(string));
            builder.AddSimpleField(GridCountField, typeof(int)); builder.AddSimpleField(ProblemCountField, typeof(int));
            builder.AddSimpleField(ChunkCountField, typeof(int)); builder.AddSimpleField(LengthField, typeof(string));
            builder.AddSimpleField(HashField, typeof(string));
            return builder.Finish();
        }

        private static Schema EnsureChunkSchema(IAnalysisBatchReader reader)
        {
            Schema existing = Schema.Lookup(reader.ChunkSchemaGuid);
            if (existing != null) return existing;
            var builder = BaseBuilder(reader.ChunkSchemaGuid, reader.ChunkSchemaName);
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
