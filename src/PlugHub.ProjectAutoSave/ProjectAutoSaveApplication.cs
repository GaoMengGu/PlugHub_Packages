using Autodesk.Revit.UI;

namespace PlugHub.ProjectAutoSave
{
    public sealed class ProjectAutoSaveApplication : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            AutoSaveService.Start(application);
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            AutoSaveService.Stop();
            return Result.Succeeded;
        }
    }
}
