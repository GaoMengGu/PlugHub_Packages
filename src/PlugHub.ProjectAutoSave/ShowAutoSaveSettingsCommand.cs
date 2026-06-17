using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PlugHub.ProjectAutoSave
{
    [Transaction(TransactionMode.Manual)]
    public sealed class ShowAutoSaveSettingsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication? uiApplication = commandData?.Application;
            if (uiApplication == null)
            {
                message = "未找到 Revit 应用程序。";
                return Result.Failed;
            }

            try
            {
                AutoSaveSettings settings = AutoSaveSettings.Load();
                AutoSaveService.ApplySettings(uiApplication, settings);

                AutoSaveSettingsWindow window = new AutoSaveSettingsWindow(settings);
                IntPtr revitHandle = uiApplication.MainWindowHandle;
                if (revitHandle != IntPtr.Zero)
                {
                    System.Windows.Interop.WindowInteropHelper helper =
                        new System.Windows.Interop.WindowInteropHelper(window);
                    helper.Owner = revitHandle;
                }

                bool? result = window.ShowDialog();
                if (result == true)
                {
                    window.Settings.Save();
                    AutoSaveService.ApplySettings(uiApplication, window.Settings);
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
