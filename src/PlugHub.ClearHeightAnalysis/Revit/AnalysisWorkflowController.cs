#nullable enable
using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
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
                var settings=new V2AnalysisWindow(RevitAnalysisContextBuilder.GetLevelChoices(document),RevitAnalysisContextBuilder.GetSourceChoices(document));
                SetOwner(settings,application.MainWindowHandle);
                if(settings.ShowDialog()!=true)return Result.Cancelled;
                AnalysisRunData runData=new RevitAnalysisRunner().Run(uiDocument,settings.Request);
                string documentKey=document.ProjectInformation?.UniqueId??(!string.IsNullOrWhiteSpace(document.PathName)?document.PathName:document.Title);
                AnalysisBatch batch=AnalysisBatch.Create(documentKey,document.Title,runData);
                var repository=new AnalysisBatchRepository();
                repository.Save(document,batch);
                var results=new V2ResultWindow(uiDocument,repository,batch);
                SetOwner(results,application.MainWindowHandle);
                results.ShowDialog();
                return Result.Succeeded;
            }
            catch(Autodesk.Revit.Exceptions.OperationCanceledException){return Result.Cancelled;}
            catch(Exception ex){message=ex.Message;return Result.Failed;}
        }

        private static void SetOwner(System.Windows.Window window,IntPtr handle)
        { if(handle!=IntPtr.Zero)new System.Windows.Interop.WindowInteropHelper(window).Owner=handle; }
    }
}
