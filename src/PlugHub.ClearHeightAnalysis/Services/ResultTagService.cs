using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;

namespace PlugHub.ClearHeightAnalysis.Services
{
    public static class ResultTagService
    {
        private static readonly Guid SchemaGuid = new Guid("4F45C95B-487B-4A18-842B-2EE37683860A");
        private const string BatchFieldName = "BatchId";

        public static Schema Schema => EnsureSchema();

        public static void TagResult(Element element, string batchId)
        {
            Schema schema = EnsureSchema();
            var entity = new Entity(schema);
            entity.Set(BatchFieldName, batchId);
            element.SetEntity(entity);
        }

        public static bool IsTaggedResult(Element element)
        {
            Entity entity = element.GetEntity(EnsureSchema());
            return entity.IsValid() && !string.IsNullOrWhiteSpace(entity.Get<string>(BatchFieldName));
        }

        private static Schema EnsureSchema()
        {
            Schema schema = Schema.Lookup(SchemaGuid);
            if (schema != null)
            {
                return schema;
            }

            var builder = new SchemaBuilder(SchemaGuid);
            builder.SetSchemaName("PlugHubClearHeightAnalysisResult");
            builder.SetReadAccessLevel(AccessLevel.Public);
            builder.SetWriteAccessLevel(AccessLevel.Public);
            builder.AddSimpleField(BatchFieldName, typeof(string));
            return builder.Finish();
        }
    }
}
