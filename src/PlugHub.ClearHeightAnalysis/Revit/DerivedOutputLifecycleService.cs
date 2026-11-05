#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autodesk.Revit.DB;
using PlugHub.ClearHeightAnalysis.Core.Models;
using PlugHub.ClearHeightAnalysis.Core.Services;

namespace PlugHub.ClearHeightAnalysis.Revit
{
    public sealed class DerivedOutputLifecycleService
    {
        private readonly DerivedOutputRegistryRepository _registry=new DerivedOutputRegistryRepository();
        public IReadOnlyList<DerivedOutputRecord> List(Document document,string batchId)=>_registry.Load(document,batchId);

        public DerivedOutputRecord ReplaceModel(Document document,AnalysisBatch batch,string transactionName,Func<string,DerivedOutputRecord> build)
        {
            if(document==null)throw new ArgumentNullException(nameof(document));if(batch==null)throw new ArgumentNullException(nameof(batch));if(build==null)throw new ArgumentNullException(nameof(build));
            using(var group=new TransactionGroup(document,transactionName))
            {
                group.Start();
                try
                {
                    DerivedOutputRecord created;
                    using(var transaction=new Transaction(document,transactionName)){transaction.Start();created=build(Guid.NewGuid().ToString("N"));DerivedOutputTransition transition=DerivedOutputLifecyclePlanner.Replace(_registry.Load(document,batch.BatchId),created);DeleteExisting(document,batch.BatchId,transition.OutputsToDelete);_registry.Replace(document,batch.BatchId,transition.NextOutputs);transaction.Commit();}
                    group.Assimilate();return created;
                }catch{group.RollBack();throw;}
            }
        }

        public DerivedOutputRecord ReplaceFile(Document document,AnalysisBatch batch,DerivedOutputType type,string finalPath,byte[] content,Func<string,string,string,DerivedOutputRecord>? buildModel=null)
        {
            if(string.IsNullOrWhiteSpace(finalPath))throw new ArgumentException("成果路径不能为空。",nameof(finalPath));string fullPath=Path.GetFullPath(finalPath);string? directory=Path.GetDirectoryName(fullPath);if(!string.IsNullOrWhiteSpace(directory))Directory.CreateDirectory(directory);
            string extension=Path.GetExtension(fullPath);string temporary=fullPath+"."+Guid.NewGuid().ToString("N")+".tmp"+extension;string backup=fullPath+"."+Guid.NewGuid().ToString("N")+".bak";File.WriteAllBytes(temporary,content);bool hadExisting=File.Exists(fullPath);
            using(var group=new TransactionGroup(document,"替换净高分析文件成果"))
            {
                group.Start();
                try
                {
                    DerivedOutputRecord created;
                    using(var transaction=new Transaction(document,"更新净高分析成果注册表"))
                    {
                        transaction.Start();string outputId=Guid.NewGuid().ToString("N");created=buildModel!=null?buildModel(temporary,fullPath,outputId):new DerivedOutputRecord(outputId,type,DateTime.UtcNow,null,fullPath,Array.Empty<int>());
                        DerivedOutputTransition transition=DerivedOutputLifecyclePlanner.Replace(_registry.Load(document,batch.BatchId),created);DeleteExisting(document,batch.BatchId,transition.OutputsToDelete);_registry.Replace(document,batch.BatchId,transition.NextOutputs);transaction.Commit();
                    }
                    if(hadExisting)File.Move(fullPath,backup);File.Move(temporary,fullPath);
                    if(created.OutputType==DerivedOutputType.RasterHeatmap&&created.ElementIds.Count>0)
                    {
                        using(var reload=new Transaction(document,"重载净高分析 PNG")){reload.Start();ImageType? imageType=document.GetElement(new ElementId(created.ElementIds[0])) as ImageType;if(imageType==null)throw new InvalidOperationException("无法定位净高分析图像类型。");using(var options=new ImageTypeOptions(fullPath,false))imageType.ReloadFrom(options);reload.Commit();}
                    }
                    group.Assimilate();TryDelete(backup);return created;
                }
                catch
                {
                    if(group.GetStatus()==TransactionStatus.Started)group.RollBack();TryDelete(temporary);if(File.Exists(backup)){TryDelete(fullPath);File.Move(backup,fullPath);}else if(!hadExisting)TryDelete(fullPath);throw;
                }
            }
        }

        public void Delete(Document document,string batchId,string outputId)
        {
            using(var group=new TransactionGroup(document,"删除净高分析成果")){group.Start();try{using(var transaction=new Transaction(document,"删除净高分析成果")){transaction.Start();DerivedOutputTransition transition=DerivedOutputLifecyclePlanner.Delete(_registry.Load(document,batchId),outputId);DeleteExisting(document,batchId,transition.OutputsToDelete);_registry.Replace(document,batchId,transition.NextOutputs);transaction.Commit();}group.Assimilate();}catch{group.RollBack();throw;}}
        }
        public void DeleteAll(Document document,string batchId)
        {
            using(var group=new TransactionGroup(document,"删除净高分析全部成果")){group.Start();try{using(var transaction=new Transaction(document,"删除净高分析全部成果")){transaction.Start();IReadOnlyList<DerivedOutputRecord> outputs=_registry.Load(document,batchId);DeleteExisting(document,batchId,outputs);_registry.Replace(document,batchId,Array.Empty<DerivedOutputRecord>());transaction.Commit();}group.Assimilate();}catch{group.RollBack();throw;}}
        }
        private static void DeleteExisting(Document document,string batchId,IEnumerable<DerivedOutputRecord> outputs)
        {
            var ids=new HashSet<ElementId>();foreach(DerivedOutputRecord output in outputs)foreach(int value in output.ElementIds){ElementId id=new ElementId(value);Element? element=document.GetElement(id);if(element!=null&&OutputTagService.Matches(element,batchId,output.OutputId))ids.Add(id);}
            if(ids.Count>0)document.Delete(ids.ToList());
        }
        private static void TryDelete(string path){try{if(File.Exists(path))File.Delete(path);}catch(IOException){}catch(UnauthorizedAccessException){}}
    }
}
