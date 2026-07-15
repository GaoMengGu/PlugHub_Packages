#nullable enable
using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using PlugHub.ClearHeightAnalysis.Core.Models;

namespace PlugHub.ClearHeightAnalysis.Revit
{
    public static class OutputTagService
    {
        private static readonly Guid SchemaGuid = new Guid("49C650DC-46F8-48DF-A77B-2F5628F552B3");
        private const string BatchId = "BatchId";
        private const string OutputId = "OutputId";
        private const string OutputType = "OutputType";
        private const string RegionId = "RegionId";

        public static void Tag(Element element, string batchId, string outputId, DerivedOutputType outputType, string regionId = "")
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            Schema schema = EnsureSchema();
            var entity = new Entity(schema);
            entity.Set(BatchId, batchId); entity.Set(OutputId, outputId);
            entity.Set(OutputType, outputType.ToString()); entity.Set(RegionId, regionId ?? string.Empty);
            element.SetEntity(entity);
        }

        public static bool Matches(Element element, string batchId, string? outputId = null)
        {
            if (element == null) return false;
            Entity entity = element.GetEntity(EnsureSchema());
            return entity.IsValid() && entity.Get<string>(BatchId) == batchId &&
                   (string.IsNullOrWhiteSpace(outputId) || entity.Get<string>(OutputId) == outputId);
        }

        private static Schema EnsureSchema()
        {
            Schema schema = Schema.Lookup(SchemaGuid);
            if (schema != null) return schema;
            var builder = new SchemaBuilder(SchemaGuid); builder.SetSchemaName("PlugHubClearHeightDerivedOutputV2");
            builder.SetReadAccessLevel(AccessLevel.Public); builder.SetWriteAccessLevel(AccessLevel.Public);
            builder.AddSimpleField(BatchId, typeof(string)); builder.AddSimpleField(OutputId, typeof(string));
            builder.AddSimpleField(OutputType, typeof(string)); builder.AddSimpleField(RegionId, typeof(string));
            return builder.Finish();
        }
    }
}
