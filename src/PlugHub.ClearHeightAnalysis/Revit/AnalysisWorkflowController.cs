#nullable enable
using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;
using System.Collections.Generic;
using PlugHub.ClearHeightAnalysis.Core.Geometry;
using PlugHub.ClearHeightAnalysis.Core.Models;
using PlugHub.ClearHeightAnalysis.UI;

namespace PlugHub.ClearHeightAnalysis.Revit
{
    public sealed class AnalysisWorkflowController
    {
        public Result Execute(ExternalCommandData commandData, ref string message)
        {
            UIApplication? application=commandData?.Application;
            UIDocument? uiDocument=application?.ActiveUIDocument;
            Document? document=uiDocument?.Document;
            if(application==null||uiDocument==null||document==null){message="未找到当前 Revit 文档。";return Result.Failed;}
            try
            {
                var repository=new AnalysisBatchRepository();
                IReadOnlyList<StoredBatchInfo> stored=repository.List(document);
                var settings=new V2AnalysisWindow(RevitAnalysisContextBuilder.GetLevelChoices(document),RevitAnalysisContextBuilder.GetSourceChoices(document),
                    stored.Any(item=>item.Status==StoredBatchStatus.Available));
                SetOwner(settings,application.MainWindowHandle);
                if(settings.ShowDialog()!=true)return Result.Cancelled;
                if(settings.OpenHistoryRequested)
                {
                    StoredBatchInfo latest=stored.First(item=>item.Status==StoredBatchStatus.Available);
                    ShowResults(uiDocument,application,repository,repository.Load(document,latest.BatchId));
                    return Result.Succeeded;
                }
                AnalysisRunData runData=new RevitAnalysisRunner().Run(uiDocument,settings.Request);
                string documentKey=document.ProjectInformation?.UniqueId??(!string.IsNullOrWhiteSpace(document.PathName)?document.PathName:document.Title);
                AnalysisContextRecord context=CreateContext(document,uiDocument.ActiveView,runData);
                AnalysisBatch batch=AnalysisBatch.Create(documentKey,document.Title,runData,context);
                repository.Save(document,batch);
                ShowResults(uiDocument,application,repository,batch);
                return Result.Succeeded;
            }
            catch(Autodesk.Revit.Exceptions.OperationCanceledException){return Result.Cancelled;}
            catch(Exception ex){message=ex.Message;return Result.Failed;}
        }

        private static void SetOwner(System.Windows.Window window,IntPtr handle)
        { if(handle!=IntPtr.Zero)new System.Windows.Interop.WindowInteropHelper(window).Owner=handle; }

        private static void ShowResults(UIDocument uiDocument,UIApplication application,AnalysisBatchRepository repository,AnalysisBatch batch)
        {
            var results=new V2ResultWindow(uiDocument,repository,batch);
            SetOwner(results,application.MainWindowHandle);
            results.ShowDialog();
        }

        private static AnalysisContextRecord CreateContext(Document document,View view,AnalysisRunData runData)
        {
            RevitAnalysisContext revit=RevitAnalysisContextBuilder.Build(document,runData.Request);
            return new AnalysisContextRecord(view.UniqueId,view.Name,view.ViewType.ToString(),revit.Level.UniqueId,revit.Level.Name,
                Services.UnitConversion.FeetToMillimeters(revit.Level.Elevation),revit.Sources.Select(source=>
                    new AnalysisSourceContextRecord(source.Key,
                        source.IsHost?"当前模型："+source.Document.Title:"链接实例："+source.LinkInstance!.Name,
                        source.IsHost,ToCoreTransform(source.Transform))));
        }

        private static Transform3d ToCoreTransform(Transform transform)
        {
            XYZ x=transform.BasisX,y=transform.BasisY,z=transform.BasisZ,o=transform.Origin;
            return new Transform3d(x.X,y.X,z.X,Services.UnitConversion.FeetToMillimeters(o.X),
                x.Y,y.Y,z.Y,Services.UnitConversion.FeetToMillimeters(o.Y),
                x.Z,y.Z,z.Z,Services.UnitConversion.FeetToMillimeters(o.Z));
        }
    }
}
