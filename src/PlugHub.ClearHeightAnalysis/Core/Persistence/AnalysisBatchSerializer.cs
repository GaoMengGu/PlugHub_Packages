#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;
using System.Text;
using PlugHub.ClearHeightAnalysis.Core.Geometry;
using PlugHub.ClearHeightAnalysis.Core.Models;

namespace PlugHub.ClearHeightAnalysis.Core.Persistence
{
    public sealed class AnalysisBatchCorruptException : Exception
    {
        public AnalysisBatchCorruptException(string message) : base(message) { }
        public AnalysisBatchCorruptException(string message, Exception inner) : base(message, inner) { }
    }

    public sealed class AnalysisBatchVersionException : Exception
    {
        public AnalysisBatchVersionException(string message) : base(message) { }
    }

    public sealed class AnalysisBatchChunk
    {
        public AnalysisBatchChunk(int sequence, string payload)
        {
            if (sequence < 0) throw new ArgumentOutOfRangeException(nameof(sequence));
            Sequence = sequence;
            Payload = string.IsNullOrEmpty(payload) ? throw new ArgumentException("分块不能为空。", nameof(payload)) : payload;
        }
        public int Sequence { get; }
        public string Payload { get; }
    }

    public sealed class AnalysisBatchEnvelope
    {
        public AnalysisBatchEnvelope(int schemaVersion, string batchId, long uncompressedLength, string sha256, IEnumerable<AnalysisBatchChunk> chunks)
        {
            SchemaVersion = schemaVersion;
            BatchId = batchId ?? throw new ArgumentNullException(nameof(batchId));
            UncompressedLength = uncompressedLength;
            Sha256 = sha256 ?? throw new ArgumentNullException(nameof(sha256));
            Chunks = (chunks ?? throw new ArgumentNullException(nameof(chunks))).ToList().AsReadOnly();
        }
        public int SchemaVersion { get; }
        public string BatchId { get; }
        public long UncompressedLength { get; }
        public string Sha256 { get; }
        public IReadOnlyList<AnalysisBatchChunk> Chunks { get; }
    }

    public static class AnalysisBatchSerializer
    {
        public const int MaximumChunkCharacters = 256 * 1024;

        public static AnalysisBatchEnvelope Serialize(AnalysisBatch batch, int maximumChunkCharacters = MaximumChunkCharacters)
        {
            if (batch == null) throw new ArgumentNullException(nameof(batch));
            if (batch.SchemaVersion != AnalysisBatch.CurrentSchemaVersion)
                throw new AnalysisBatchVersionException("不支持写入净高分析批次版本 " + batch.SchemaVersion + "。");
            if (maximumChunkCharacters <= 0 || maximumChunkCharacters > MaximumChunkCharacters)
                throw new ArgumentOutOfRangeException(nameof(maximumChunkCharacters));

            byte[] json = SerializeJson(BatchDto.From(batch));
            string hash = Hash(json);
            string payload = Convert.ToBase64String(Compress(json));
            var chunks = new List<AnalysisBatchChunk>();
            for (int offset = 0, sequence = 0; offset < payload.Length; offset += maximumChunkCharacters, sequence++)
            {
                int length = Math.Min(maximumChunkCharacters, payload.Length - offset);
                chunks.Add(new AnalysisBatchChunk(sequence, payload.Substring(offset, length)));
            }
            return new AnalysisBatchEnvelope(batch.SchemaVersion, batch.BatchId, json.LongLength, hash, chunks);
        }

        public static AnalysisBatch Deserialize(AnalysisBatchEnvelope envelope)
        {
            if (envelope == null) throw new ArgumentNullException(nameof(envelope));
            if (envelope.SchemaVersion != AnalysisBatch.CurrentSchemaVersion)
                throw new AnalysisBatchVersionException("不支持的净高分析批次版本 " + envelope.SchemaVersion + "。");
            if (envelope.Chunks.Count == 0) throw new AnalysisBatchCorruptException("批次没有数据分块。");
            for (int index = 0; index < envelope.Chunks.Count; index++)
            {
                if (envelope.Chunks[index].Sequence != index)
                    throw new AnalysisBatchCorruptException("批次分块序号不连续。");
                if (envelope.Chunks[index].Payload.Length > MaximumChunkCharacters)
                    throw new AnalysisBatchCorruptException("批次分块超过允许大小。");
            }

            try
            {
                string base64 = string.Concat(envelope.Chunks.Select(chunk => chunk.Payload));
                byte[] json = Decompress(Convert.FromBase64String(base64));
                if (json.LongLength != envelope.UncompressedLength)
                    throw new AnalysisBatchCorruptException("批次原始长度校验失败。");
                if (!string.Equals(Hash(json), envelope.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new AnalysisBatchCorruptException("批次 SHA-256 校验失败。");
                BatchDto? dto = DeserializeJson<BatchDto>(json);
                if (dto == null) throw new AnalysisBatchCorruptException("批次内容为空。");
                AnalysisBatch batch = dto.ToModel();
                if (batch.SchemaVersion != envelope.SchemaVersion || !string.Equals(batch.BatchId, envelope.BatchId, StringComparison.Ordinal))
                    throw new AnalysisBatchCorruptException("批次清单与内容不一致。");
                return batch;
            }
            catch (AnalysisBatchCorruptException) { throw; }
            catch (AnalysisBatchVersionException) { throw; }
            catch (Exception ex) { throw new AnalysisBatchCorruptException("批次数据无法解码。", ex); }
        }

        private static byte[] SerializeJson<T>(T value)
        {
            using (var stream = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(T)).WriteObject(stream, value);
                return stream.ToArray();
            }
        }

        private static T? DeserializeJson<T>(byte[] bytes) where T : class
        {
            using (var stream = new MemoryStream(bytes))
                return new DataContractJsonSerializer(typeof(T)).ReadObject(stream) as T;
        }

        private static byte[] Compress(byte[] bytes)
        {
            using (var output = new MemoryStream())
            {
                using (var gzip = new GZipStream(output, CompressionMode.Compress, true)) gzip.Write(bytes, 0, bytes.Length);
                return output.ToArray();
            }
        }

        private static byte[] Decompress(byte[] bytes)
        {
            using (var input = new MemoryStream(bytes))
            using (var gzip = new GZipStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                gzip.CopyTo(output);
                return output.ToArray();
            }
        }

        private static string Hash(byte[] bytes)
        {
            using (SHA256 sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
        }

        [DataContract]
        private sealed class BatchDto
        {
            [DataMember(Order = 1)] public int Version;
            [DataMember(Order = 2)] public string Id = string.Empty;
            [DataMember(Order = 3)] public string Created = string.Empty;
            [DataMember(Order = 4)] public string DocumentKey = string.Empty;
            [DataMember(Order = 5)] public string DocumentTitle = string.Empty;
            [DataMember(Order = 6)] public RequestDto Request = new RequestDto();
            [DataMember(Order = 7)] public List<RegionDto> Boundary = new List<RegionDto>();
            [DataMember(Order = 8)] public List<CellDto> Cells = new List<CellDto>();
            [DataMember(Order = 9)] public List<ObstacleDto> Obstacles = new List<ObstacleDto>();
            [DataMember(Order = 10)] public SettingsDto Settings = new SettingsDto();
            [DataMember(Order = 11)] public List<ResultDto> Results = new List<ResultDto>();
            [DataMember(Order = 12)] public long CandidateVisits;
            [DataMember(Order = 13)] public List<OutputDto> Outputs = new List<OutputDto>();

            public static BatchDto From(AnalysisBatch batch) => new BatchDto
            {
                Version = batch.SchemaVersion, Id = batch.BatchId, Created = batch.CreatedAtUtc.ToString("O", CultureInfo.InvariantCulture),
                DocumentKey = batch.DocumentKey, DocumentTitle = batch.DocumentTitle,
                Request = RequestDto.From(batch.RunData.Request),
                Boundary = batch.RunData.Boundary.Regions.Select(RegionDto.From).ToList(),
                Cells = batch.RunData.Cells.Select(CellDto.From).ToList(),
                Obstacles = batch.RunData.Obstacles.Select(ObstacleDto.From).ToList(),
                Settings = SettingsDto.From(batch.RunData.Settings),
                Results = batch.RunData.Summary.Results.Select(ResultDto.From).ToList(),
                CandidateVisits = batch.RunData.Summary.CandidateVisitCount,
                Outputs = batch.Outputs.Select(OutputDto.From).ToList()
            };

            public AnalysisBatch ToModel()
            {
                if (Version != AnalysisBatch.CurrentSchemaVersion) throw new AnalysisBatchVersionException("批次内容版本不受支持。");
                AnalysisRequest request = Request.ToModel();
                AnalysisBoundary boundary = new AnalysisBoundary(Boundary.Select(item => item.ToModel()));
                List<GridCellData> cells = Cells.Select(item => item.ToModel()).ToList();
                var byNumber = cells.ToDictionary(cell => cell.Number, StringComparer.Ordinal);
                List<ObstacleSnapshot> obstacles = Obstacles.Select(item => item.ToModel()).ToList();
                CoreAnalysisSettings settings = Settings.ToModel();
                List<CellAnalysisResult> results = Results.Select(item => item.ToModel(byNumber)).ToList();
                var run = new AnalysisRunData(request, boundary, cells, obstacles, settings, new CoreAnalysisSummary(results, CandidateVisits));
                DateTime created = DateTime.Parse(Created, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToUniversalTime();
                return new AnalysisBatch(Version, Id, created, DocumentKey, DocumentTitle, run, Outputs.Select(item => item.ToModel()));
            }
        }

        [DataContract] private sealed class RequestDto
        {
            [DataMember(Order=1)] public List<LevelDto> Levels = new List<LevelDto>();
            [DataMember(Order=2)] public List<SourceDto> Sources = new List<SourceDto>();
            [DataMember(Order=3)] public string SelectedLevelId = string.Empty;
            [DataMember(Order=4)] public int BoundaryMode;
            [DataMember(Order=5)] public double Grid;
            [DataMember(Order=6)] public double Threshold;
            [DataMember(Order=7)] public double FinishOffset;
            [DataMember(Order=8)] public double SearchHeight;
            [DataMember(Order=9)] public double MinimumPipe;
            [DataMember(Order=10)] public bool IncludeCeilings;
            [DataMember(Order=11)] public bool IncludeMep;
            public static RequestDto From(AnalysisRequest value) => new RequestDto { Levels=value.Levels.Select(LevelDto.From).ToList(), Sources=value.SourceModels.Select(SourceDto.From).ToList(), SelectedLevelId=value.SelectedLevel?.UniqueId ?? string.Empty, BoundaryMode=(int)value.BoundaryMode, Grid=value.GridSizeMillimeters, Threshold=value.ClearHeightThresholdMillimeters, FinishOffset=value.FinishFloorOffsetMillimeters, SearchHeight=value.SearchHeightMillimeters, MinimumPipe=value.MinimumPipeDiameterMillimeters, IncludeCeilings=value.IncludeCeilings, IncludeMep=value.IncludeMep };
            public AnalysisRequest ToModel() { var levels=Levels.Select(x=>x.ToModel()).ToList(); var sources=Sources.Select(x=>x.ToModel()).ToList(); var value=new AnalysisRequest(levels,sources) { BoundaryMode=(AnalysisBoundaryMode)BoundaryMode, GridSizeMillimeters=Grid, ClearHeightThresholdMillimeters=Threshold, FinishFloorOffsetMillimeters=FinishOffset, SearchHeightMillimeters=SearchHeight, MinimumPipeDiameterMillimeters=MinimumPipe, IncludeCeilings=IncludeCeilings, IncludeMep=IncludeMep }; value.SelectedLevel=levels.FirstOrDefault(x=>x.UniqueId==SelectedLevelId); return value; }
        }
        [DataContract] private sealed class LevelDto { [DataMember(Order=1)] public string Id=string.Empty; [DataMember(Order=2)] public string Name=string.Empty; [DataMember(Order=3)] public double Elevation; public static LevelDto From(AnalysisLevelChoice x)=>new LevelDto{Id=x.UniqueId,Name=x.Name,Elevation=x.ElevationMillimeters}; public AnalysisLevelChoice ToModel()=>new AnalysisLevelChoice(Id,Name,Elevation); }
        [DataContract] private sealed class SourceDto { [DataMember(Order=1)] public string Key=string.Empty; [DataMember(Order=2)] public string Name=string.Empty; [DataMember(Order=3)] public bool Host; [DataMember(Order=4)] public bool Loaded; [DataMember(Order=5)] public bool Selected; public static SourceDto From(SourceModelChoice x)=>new SourceDto{Key=x.Key,Name=x.DisplayName,Host=x.IsHost,Loaded=x.IsLoaded,Selected=x.IsSelected}; public SourceModelChoice ToModel()=>new SourceModelChoice(Key,Name,Host,Loaded,Selected); }
        [DataContract] private sealed class PointDto { [DataMember(Order=1)] public double X; [DataMember(Order=2)] public double Y; public static PointDto From(Point2d p)=>new PointDto{X=p.X,Y=p.Y}; public Point2d ToModel()=>new Point2d(X,Y); }
        [DataContract] private sealed class LoopDto { [DataMember(Order=1)] public List<PointDto> Points=new List<PointDto>(); public static LoopDto From(PolygonLoop2d x)=>new LoopDto{Points=x.Points.Select(PointDto.From).ToList()}; public PolygonLoop2d ToModel()=>new PolygonLoop2d(Points.Select(p=>p.ToModel())); }
        [DataContract] private sealed class RegionDto { [DataMember(Order=1)] public LoopDto Outer=new LoopDto(); [DataMember(Order=2)] public List<LoopDto> Holes=new List<LoopDto>(); public static RegionDto From(BoundaryRegion x)=>new RegionDto{Outer=LoopDto.From(x.Outer),Holes=x.Holes.Select(LoopDto.From).ToList()}; public BoundaryRegion ToModel()=>new BoundaryRegion(Outer.ToModel(),Holes.Select(x=>x.ToModel())); }
        [DataContract] private sealed class CellDto
        {
            [DataMember(Order=1)] public string Number=string.Empty; [DataMember(Order=2)] public int Column; [DataMember(Order=3)] public int Row; [DataMember(Order=4)] public double MinX; [DataMember(Order=5)] public double MinY; [DataMember(Order=6)] public double MaxX; [DataMember(Order=7)] public double MaxY; [DataMember(Order=8)] public string LevelId=string.Empty; [DataMember(Order=9)] public string LevelName=string.Empty; [DataMember(Order=10)] public double LevelElevation; [DataMember(Order=11)] public List<int> RegionIndexes=new List<int>();
            public static CellDto From(GridCellData x)=>new CellDto{Number=x.Number,Column=x.Column,Row=x.Row,MinX=x.MinX,MinY=x.MinY,MaxX=x.MaxX,MaxY=x.MaxY,LevelId=x.LevelUniqueId,LevelName=x.LevelName,LevelElevation=x.LevelElevationMillimeters,RegionIndexes=x.BoundaryRegionIndexes.ToList()};
            public GridCellData ToModel()=>new GridCellData(Number,Column,Row,MinX,MinY,MaxX,MaxY,LevelId,LevelName,LevelElevation,RegionIndexes);
        }
        [DataContract] private sealed class PlaneDto { [DataMember(Order=1)] public double A; [DataMember(Order=2)] public double B; [DataMember(Order=3)] public double C; public static PlaneDto From(ElevationPlane x)=>new PlaneDto{A=x.A,B=x.B,C=x.C}; public ElevationPlane ToModel()=>new ElevationPlane(A,B,C); }
        [DataContract] private sealed class ObstacleDto
        {
            [DataMember(Order=1)] public string Key=string.Empty; [DataMember(Order=2)] public string Name=string.Empty; [DataMember(Order=3)] public string Category=string.Empty; [DataMember(Order=4)] public string Source=string.Empty; [DataMember(Order=5)] public string? Link; [DataMember(Order=6)] public int Kind; [DataMember(Order=7)] public LoopDto Footprint=new LoopDto(); [DataMember(Order=8)] public PlaneDto Plane=new PlaneDto(); [DataMember(Order=9)] public double Bottom; [DataMember(Order=10)] public double Top; [DataMember(Order=11)] public int Confidence;
            public static ObstacleDto From(ObstacleSnapshot x)=>new ObstacleDto{Key=x.Key,Name=x.DisplayName,Category=x.CategoryName,Source=x.SourceModelName,Link=x.LinkInstanceKey,Kind=(int)x.Kind,Footprint=LoopDto.From(x.Footprint),Plane=PlaneDto.From(x.BottomPlane),Bottom=x.BottomElevationMillimeters,Top=x.TopElevationMillimeters,Confidence=(int)x.Confidence};
            public ObstacleSnapshot ToModel()=>new ObstacleSnapshot(Key,Name,Category,Source,Link,(ObstacleKind)Kind,Footprint.ToModel(),Plane.ToModel(),Bottom,Top,(GeometryConfidence)Confidence);
        }
        [DataContract] private sealed class SettingsDto { [DataMember(Order=1)] public double Level; [DataMember(Order=2)] public double Finish; [DataMember(Order=3)] public double Search; [DataMember(Order=4)] public double Threshold; public static SettingsDto From(CoreAnalysisSettings x)=>new SettingsDto{Level=x.LevelElevationMillimeters,Finish=x.FinishFloorOffsetMillimeters,Search=x.SearchHeightMillimeters,Threshold=x.ClearHeightThresholdMillimeters}; public CoreAnalysisSettings ToModel()=>new CoreAnalysisSettings(Level,Finish,Search,Threshold); }
        [DataContract] private sealed class ResultDto
        {
            [DataMember(Order=1)] public string Cell=string.Empty; [DataMember(Order=2)] public double? Height; [DataMember(Order=3)] public double Threshold; [DataMember(Order=4)] public int Status; [DataMember(Order=5)] public string? Obstacle; [DataMember(Order=6)] public int? Confidence;
            public static ResultDto From(CellAnalysisResult x)=>new ResultDto{Cell=x.Cell.Number,Height=x.ClearHeightMillimeters,Threshold=x.ThresholdMillimeters,Status=(int)x.Status,Obstacle=x.ControllingObstacleKey,Confidence=x.Confidence.HasValue?(int)x.Confidence.Value:(int?)null};
            public CellAnalysisResult ToModel(IReadOnlyDictionary<string,GridCellData> cells)=>new CellAnalysisResult(cells[Cell],Height,Threshold,(CellStatus)Status,Obstacle,Confidence.HasValue?(GeometryConfidence)Confidence.Value:(GeometryConfidence?)null);
        }
        [DataContract] private sealed class OutputDto
        {
            [DataMember(Order=1)] public string Id=string.Empty; [DataMember(Order=2)] public int Type; [DataMember(Order=3)] public string Created=string.Empty; [DataMember(Order=4)] public int? ViewId; [DataMember(Order=5)] public string? FilePath; [DataMember(Order=6)] public List<int> ElementIds=new List<int>();
            public static OutputDto From(DerivedOutputRecord x)=>new OutputDto{Id=x.OutputId,Type=(int)x.OutputType,Created=x.CreatedAtUtc.ToString("O",CultureInfo.InvariantCulture),ViewId=x.ViewId,FilePath=x.FilePath,ElementIds=x.ElementIds.ToList()};
            public DerivedOutputRecord ToModel()=>new DerivedOutputRecord(Id,(DerivedOutputType)Type,DateTime.Parse(Created,CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind).ToUniversalTime(),ViewId,FilePath,ElementIds);
        }
    }
}
