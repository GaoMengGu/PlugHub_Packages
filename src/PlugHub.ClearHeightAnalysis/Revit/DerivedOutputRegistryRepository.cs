#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.ExtensibleStorage;
using PlugHub.ClearHeightAnalysis.Core.Models;

namespace PlugHub.ClearHeightAnalysis.Revit
{
    public sealed class DerivedOutputRegistryRepository
    {
        private static readonly Guid SchemaGuid=new Guid("57621009-C230-4779-B5E2-7723C2750C4C");
        private const string BatchIdField="BatchId";private const string OutputIdsField="OutputIds";
        private const string OutputTypesField="OutputTypes";private const string CreatedFields="CreatedUtc";
        private const string ViewIdsField="ViewIds";private const string FilePathsField="FilePaths";private const string ElementIdsField="ElementIds";

        public IReadOnlyList<DerivedOutputRecord> Load(Document document,string batchId)
        {
            if(document==null)throw new ArgumentNullException(nameof(document));
            DataStorage? storage=Find(document,batchId);if(storage==null)return Array.Empty<DerivedOutputRecord>();
            Entity entity=storage.GetEntity(EnsureSchema());
            IList<string> ids=entity.Get<IList<string>>(OutputIdsField);IList<string> types=entity.Get<IList<string>>(OutputTypesField);
            IList<string> created=entity.Get<IList<string>>(CreatedFields);IList<string> views=entity.Get<IList<string>>(ViewIdsField);
            IList<string> paths=entity.Get<IList<string>>(FilePathsField);IList<string> elements=entity.Get<IList<string>>(ElementIdsField);
            if(new[]{types.Count,created.Count,views.Count,paths.Count,elements.Count}.Any(count=>count!=ids.Count))throw new InvalidOperationException("净高成果注册表字段数量不一致。");
            var result=new List<DerivedOutputRecord>();
            for(int index=0;index<ids.Count;index++)
            {
                if(!Enum.TryParse(types[index],out DerivedOutputType type))throw new InvalidOperationException("净高成果注册表包含未知类型。");
                DateTime time=DateTime.Parse(created[index],CultureInfo.InvariantCulture,DateTimeStyles.RoundtripKind).ToUniversalTime();
                int? view=string.IsNullOrWhiteSpace(views[index])?(int?)null:int.Parse(views[index],CultureInfo.InvariantCulture);
                int[] elementIds=string.IsNullOrWhiteSpace(elements[index])?Array.Empty<int>():elements[index].Split(',').Select(value=>int.Parse(value,CultureInfo.InvariantCulture)).ToArray();
                result.Add(new DerivedOutputRecord(ids[index],type,time,view,string.IsNullOrWhiteSpace(paths[index])?null:paths[index],elementIds));
            }
            return result.AsReadOnly();
        }

        public void Replace(Document document,string batchId,IEnumerable<DerivedOutputRecord> outputs)
        {
            if(document==null)throw new ArgumentNullException(nameof(document));if(string.IsNullOrWhiteSpace(batchId))throw new ArgumentException("批次标识不能为空。",nameof(batchId));
            List<DerivedOutputRecord> values=(outputs??throw new ArgumentNullException(nameof(outputs))).ToList();
            DataStorage? storage=Find(document,batchId);
            if(values.Count==0){if(storage!=null)document.Delete(storage.Id);return;}
            if(storage==null)storage=DataStorage.Create(document);
            Schema schema=EnsureSchema();var entity=new Entity(schema);entity.Set(BatchIdField,batchId);
            entity.Set<IList<string>>(OutputIdsField,values.Select(item=>item.OutputId).ToList());
            entity.Set<IList<string>>(OutputTypesField,values.Select(item=>item.OutputType.ToString()).ToList());
            entity.Set<IList<string>>(CreatedFields,values.Select(item=>item.CreatedAtUtc.ToString("O",CultureInfo.InvariantCulture)).ToList());
            entity.Set<IList<string>>(ViewIdsField,values.Select(item=>item.ViewId?.ToString(CultureInfo.InvariantCulture)??string.Empty).ToList());
            entity.Set<IList<string>>(FilePathsField,values.Select(item=>item.FilePath??string.Empty).ToList());
            entity.Set<IList<string>>(ElementIdsField,values.Select(item=>string.Join(",",item.ElementIds)).ToList());storage.SetEntity(entity);
        }

        private static DataStorage? Find(Document document,string batchId)=>new FilteredElementCollector(document).OfClass(typeof(DataStorage)).Cast<DataStorage>()
            .FirstOrDefault(storage=>{Entity entity=storage.GetEntity(EnsureSchema());return entity.IsValid()&&entity.Get<string>(BatchIdField)==batchId;});
        private static Schema EnsureSchema()
        {
            Schema? schema=Schema.Lookup(SchemaGuid);if(schema!=null)return schema;var builder=new SchemaBuilder(SchemaGuid);builder.SetSchemaName("PlugHubClearHeightDerivedOutputRegistryV3");
            builder.SetReadAccessLevel(AccessLevel.Public);builder.SetWriteAccessLevel(AccessLevel.Public);builder.AddSimpleField(BatchIdField,typeof(string));
            builder.AddArrayField(OutputIdsField,typeof(string));builder.AddArrayField(OutputTypesField,typeof(string));builder.AddArrayField(CreatedFields,typeof(string));
            builder.AddArrayField(ViewIdsField,typeof(string));builder.AddArrayField(FilePathsField,typeof(string));builder.AddArrayField(ElementIdsField,typeof(string));return builder.Finish();
        }
    }
}
