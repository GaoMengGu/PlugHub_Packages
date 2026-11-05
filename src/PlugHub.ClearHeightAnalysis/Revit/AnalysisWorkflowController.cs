#nullable enable
using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;
using System.Collections.Generic;
using PlugHub.ClearHeightAnalysis.Core.Geometry;
using PlugHub.ClearHeightAnalysis.Core.Models;
using PlugHub.ClearHeightAnalysis.Core.Persistence;
using PlugHub.ClearHeightAnalysis.Core.Services;
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
                    stored.Any(item=>item.CanOpen));
                SetOwner(settings,application.MainWindowHandle);
                if(settings.ShowDialog()!=true)return Result.Cancelled;
                if(settings.OpenHistoryRequested)
                {
                    StoredBatchInfo latest=stored.First(item=>item.CanOpen);
                    ShowResults(uiDocument,application,repository,repository.Load(document,latest.BatchId));
                    return Result.Succeeded;
                }
                var runner=new RevitAnalysisRunner();
                RevitAnalysisPreparation preparation=runner.Prepare(uiDocument,settings.Request);
                if(!ConfirmScale(preparation.Scale))return Result.Cancelled;
                RevitAnalysisSnapshot snapshot=runner.Capture(preparation);
                string documentKey=document.ProjectInformation?.UniqueId??(!string.IsNullOrWhiteSpace(document.PathName)?document.PathName:document.Title);
                string documentTitle=document.Title;
                AnalysisContextRecord context=CreateContext(uiDocument.ActiveView,preparation.Context);
                var progressWindow=new AnalysisProgressWindow((token,progress)=>
                {
                    AnalysisRunData runData=runner.Compute(snapshot,token,progress);
                    token.ThrowIfCancellationRequested();
                    IReadOnlyList<RiskRegion> regions=RiskRegionBuilder.Build(runData.Summary.Results,false,token,progress);
                    AnalysisBatch batch=AnalysisBatch.Create(documentKey,documentTitle,runData,context);
                    AnalysisBatchEnvelope envelope=AnalysisBatchSerializer.Serialize(batch,token,progress);
                    return new PreparedAnalysisBatch(batch,envelope,regions);
                });
                SetOwner(progressWindow,application.MainWindowHandle);
                bool? completed=progressWindow.ShowDialog();
                if(completed!=true)
                {
                    if(progressWindow.Failure!=null)throw progressWindow.Failure;
                    return Result.Cancelled;
                }
                PreparedAnalysisBatch prepared=progressWindow.Result??throw new InvalidOperationException("净高分析未返回结果。");
                repository.Save(document,prepared.Batch,prepared.Envelope);
                ShowResults(uiDocument,application,repository,prepared.Batch,prepared.RiskRegions);
                return Result.Succeeded;
            }
            catch(Autodesk.Revit.Exceptions.OperationCanceledException){return Result.Cancelled;}
            catch(Exception ex){message=ex.Message;return Result.Failed;}
        }

        private static void SetOwner(System.Windows.Window window,IntPtr handle)
        { if(handle!=IntPtr.Zero)new System.Windows.Interop.WindowInteropHelper(window).Owner=handle; }

        private static void ShowResults(UIDocument uiDocument,UIApplication application,AnalysisBatchRepository repository,AnalysisBatch batch,IReadOnlyList<RiskRegion> regions=null)
        {
            var results=new V2ResultWindow(uiDocument,repository,batch,regions);
            SetOwner(results,application.MainWindowHandle);
            results.ShowDialog();
        }

        private static bool ConfirmScale(AnalysisScaleEstimate scale)
        {
            if(scale.Decision==AnalysisScaleDecision.Allow)return true;
            string level=scale.Decision==AnalysisScaleDecision.RequireConfirmation?"规模提示":"超大规模高级确认";
            TaskDialogResult result=TaskDialog.Show(level,
                "预计候选网格 "+scale.CandidateCellCount.ToString("N0")+" 个（"+scale.ColumnCount+" × "+scale.RowCount+"）。\n继续可能占用较多时间和内存。",
                TaskDialogCommonButtons.Yes|TaskDialogCommonButtons.No);
            return result==TaskDialogResult.Yes;
        }

        private static AnalysisContextRecord CreateContext(View view,RevitAnalysisContext revit)
        {
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
