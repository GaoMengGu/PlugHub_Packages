using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using PlugHub.ClearHeightAnalysis.Core.Geometry;
using PlugHub.ClearHeightAnalysis.Core.Models;
using PlugHub.ClearHeightAnalysis.Core.Persistence;
using Xunit;

namespace PlugHub.ClearHeightAnalysis.Tests.Core
{
    public sealed class AnalysisBatchSerializerTests
    {
        [Fact]
        public void RoundTripPreservesAnalysisButDoesNotEmbedMutableOutputRegistry()
        {
            AnalysisBatch source = CreateBatch();

            AnalysisBatchEnvelope envelope = AnalysisBatchSerializer.Serialize(source, 256);
            AnalysisBatch restored = AnalysisBatchSerializer.Deserialize(envelope);

            Assert.Equal(3, AnalysisBatch.CurrentSchemaVersion);
            Assert.Equal(AnalysisBatch.CurrentSchemaVersion, restored.SchemaVersion);
            Assert.Equal(source.BatchId, restored.BatchId);
            Assert.Equal("document-key", restored.DocumentKey);
            Assert.Equal("示例项目", restored.DocumentTitle);
            Assert.Equal("view-1", restored.Context.ViewUniqueId);
            Assert.Equal("楼层平面", restored.Context.ViewType);
            Assert.Equal(2, restored.Context.Sources.Count);
            Assert.Equal(1234, restored.Context.Sources[1].Transform.OffsetX);
            Assert.Single(restored.RunData.Boundary.Regions);
            Assert.Single(restored.RunData.Boundary.Regions[0].Holes);
            Assert.Equal(2, restored.RunData.Cells.Count);
            Assert.NotEmpty(restored.RunData.Cells[0].EffectiveRegions);
            Assert.True(restored.RunData.Cells[0].EffectiveAreaSquareMillimeters > 0);
            Assert.Single(restored.RunData.Obstacles);
            Assert.IsType<LinearObstacle>(restored.RunData.Obstacles[0].Geometry);
            Assert.Equal(125, ((LinearObstacle)restored.RunData.Obstacles[0].Geometry).HalfWidthMillimeters);
            Assert.Equal("host:obstacle-1", restored.RunData.Summary.Results[0].ControllingObstacleKey);
            Assert.Equal(GeometryConfidence.Exact, restored.RunData.Summary.Results[0].Confidence);
            Assert.Equal(17, restored.RunData.Summary.CandidateVisitCount);
            Assert.Empty(restored.Outputs);
        }

        [Fact]
        public void SerializeSplitsPayloadIntoOrderedBoundedChunks()
        {
            AnalysisBatchEnvelope envelope = AnalysisBatchSerializer.Serialize(CreateBatch(), 64);

            Assert.True(envelope.Chunks.Count > 1);
            Assert.Equal(Enumerable.Range(0, envelope.Chunks.Count), envelope.Chunks.Select(chunk => chunk.Sequence));
            Assert.All(envelope.Chunks, chunk => Assert.InRange(chunk.Payload.Length, 1, 64));
        }

        [Fact]
        public void DeserializeRejectsMissingOrReorderedChunks()
        {
            AnalysisBatchEnvelope source = AnalysisBatchSerializer.Serialize(CreateBatch(), 64);
            var missing = new AnalysisBatchEnvelope(source.SchemaVersion, source.BatchId, source.UncompressedLength, source.Sha256, source.Chunks.Skip(1));
            var reordered = new AnalysisBatchEnvelope(source.SchemaVersion, source.BatchId, source.UncompressedLength, source.Sha256, source.Chunks.Reverse());

            Assert.Throws<AnalysisBatchCorruptException>(() => AnalysisBatchSerializer.Deserialize(missing));
            Assert.Throws<AnalysisBatchCorruptException>(() => AnalysisBatchSerializer.Deserialize(reordered));
        }

        [Fact]
        public void DeserializeRejectsHashAndLengthMismatch()
        {
            AnalysisBatchEnvelope source = AnalysisBatchSerializer.Serialize(CreateBatch(), 256);
            var badHash = new AnalysisBatchEnvelope(source.SchemaVersion, source.BatchId, source.UncompressedLength, new string('0', 64), source.Chunks);
            var badLength = new AnalysisBatchEnvelope(source.SchemaVersion, source.BatchId, source.UncompressedLength + 1, source.Sha256, source.Chunks);

            Assert.Throws<AnalysisBatchCorruptException>(() => AnalysisBatchSerializer.Deserialize(badHash));
            Assert.Throws<AnalysisBatchCorruptException>(() => AnalysisBatchSerializer.Deserialize(badLength));
        }

        [Fact]
        public void DeserializeRejectsUnsupportedSchemaVersion()
        {
            AnalysisBatchEnvelope source = AnalysisBatchSerializer.Serialize(CreateBatch(), 256);
            var future = new AnalysisBatchEnvelope(AnalysisBatch.CurrentSchemaVersion + 1, source.BatchId, source.UncompressedLength, source.Sha256, source.Chunks);

            Assert.Throws<AnalysisBatchVersionException>(() => AnalysisBatchSerializer.Deserialize(future));
        }

        [Fact]
        public void DeserializeV2MarksBatchLegacyReadOnlyAndRestoresRectangleCells()
        {
            AnalysisBatchEnvelope current = AnalysisBatchSerializer.Serialize(CreateBatch(), 4096);
            byte[] currentJson = Decompress(Convert.FromBase64String(
                string.Concat(current.Chunks.Select(chunk => chunk.Payload))));
            string legacyText = Encoding.UTF8.GetString(currentJson)
                .Replace("\"Version\":3", "\"Version\":2");
            byte[] legacyJson = Encoding.UTF8.GetBytes(legacyText);
            byte[] compressed = Compress(legacyJson);
            string payload = Convert.ToBase64String(compressed);
            var envelope = new AnalysisBatchEnvelope(2, current.BatchId, legacyJson.LongLength,
                Hash(legacyJson), new[] { new AnalysisBatchChunk(0, payload) });

            AnalysisBatch restored = AnalysisBatchSerializer.Deserialize(envelope);

            Assert.True(restored.IsLegacyReadOnly);
            Assert.False(restored.CanGenerateModelOutputs);
            Assert.Equal(2, restored.SchemaVersion);
            Assert.All(restored.RunData.Cells, cell => Assert.Equal(1, cell.EffectiveAreaRatio, 6));
            Assert.Throws<InvalidOperationException>(() => restored.WithOutputs(Array.Empty<DerivedOutputRecord>()));
        }

        private static byte[] Compress(byte[] bytes)
        {
            using var output = new MemoryStream();
            using (var gzip = new GZipStream(output, CompressionMode.Compress, true))
                gzip.Write(bytes, 0, bytes.Length);
            return output.ToArray();
        }

        private static byte[] Decompress(byte[] bytes)
        {
            using var input = new MemoryStream(bytes);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);
            return output.ToArray();
        }

        private static string Hash(byte[] bytes)
        {
            using SHA256 sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", string.Empty).ToLowerInvariant();
        }

        private static AnalysisBatch CreateBatch()
        {
            var level = new AnalysisLevelChoice("level-1", "一层", 0);
            var host = new SourceModelChoice("host", "当前模型", true, true, true);
            var request = new AnalysisRequest(new[] { level }, new[] { host })
            {
                SelectedLevel = level,
                BoundaryMode = AnalysisBoundaryMode.AutomaticHostFloors,
                GridSizeMillimeters = 1000,
                ClearHeightThresholdMillimeters = 3000,
                FinishFloorOffsetMillimeters = 50,
                SearchHeightMillimeters = 6000,
                MinimumPipeDiameterMillimeters = 75,
                IncludeCeilings = true,
                IncludeMep = true
            };
            var outer = Rectangle(0, 0, 2000, 2000);
            var hole = Rectangle(500, 500, 750, 750);
            var boundary = new AnalysisBoundary(new[] { new BoundaryRegion(outer, new[] { hole }) });
            var first = new GridCellData("一层-000001", 0, 0, 0, 0, 1000, 1000, "level-1", "一层", 0, new[] { 0 });
            var second = new GridCellData("一层-000002", 1, 0, 1000, 0, 2000, 1000, "level-1", "一层", 0, new[] { 0 });
            var obstacle = new ObstacleSnapshot(
                "host:obstacle-1", "风管 1", "风管", "当前模型", null,
                new LinearObstacle(new Point2d(0, 500), new Point2d(2000, 500), 125, 2850, 2950, 3200),
                GeometryConfidence.Exact);
            var settings = new CoreAnalysisSettings(0, 50, 6000, 3000);
            var results = new[]
            {
                new CellAnalysisResult(first, 2800, 3000, CellStatus.Insufficient, obstacle.Key, GeometryConfidence.Exact),
                new CellAnalysisResult(second, null, 3000, CellStatus.Unknown, null, null)
            };
            var runData = new AnalysisRunData(request, boundary, new[] { first, second }, new[] { obstacle }, settings, new CoreAnalysisSummary(results, 17));
            var output = new DerivedOutputRecord("output-1", DerivedOutputType.LightweightDrawing, new DateTime(2026, 7, 15, 12, 0, 0, DateTimeKind.Utc), 42, null, new[] { 101, 102 });
            var context = new AnalysisContextRecord("view-1", "一层平面", "楼层平面", "level-1", "一层", 0, new[]
            {
                new AnalysisSourceContextRecord("host", "当前模型", true, Transform3d.Identity),
                new AnalysisSourceContextRecord("link:instance-1", "链接实例 1", false,
                    new Transform3d(1, 0, 0, 1234, 0, 1, 0, 5678, 0, 0, 1, 0))
            });
            return new AnalysisBatch(
                AnalysisBatch.CurrentSchemaVersion,
                "batch-1",
                new DateTime(2026, 7, 15, 10, 0, 0, DateTimeKind.Utc),
                "document-key",
                "示例项目",
                runData,
                context,
                new[] { output });
        }

        private static PolygonLoop2d Rectangle(double minX, double minY, double maxX, double maxY)
        {
            return new PolygonLoop2d(new[]
            {
                new Point2d(minX, minY), new Point2d(maxX, minY),
                new Point2d(maxX, maxY), new Point2d(minX, maxY)
            });
        }
    }
}
