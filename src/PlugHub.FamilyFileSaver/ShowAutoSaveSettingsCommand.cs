using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PlugHub.FamilyFileSaver
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
                    TaskDialog.Show("自动保存设置", window.Settings.IsEnabled
                        ? $"自动保存已启用，间隔 {window.Settings.IntervalMinutes} 分钟。"
                        : "自动保存已关闭。");
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
