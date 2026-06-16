using System;
using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PlugHub.AutoSave
{
    /// <summary>
    /// 打开自动保存设置窗口的命令。
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public sealed class AutoSaveSettingsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiApp = commandData?.Application;
            if (uiApp == null)
            {
                message = "未找到 Revit 应用实例。";
                return Result.Failed;
            }

            try
            {
                var settings = AutoSaveSettings.Load();
                var service = new AutoSaveService(uiApp, settings);
                var window = new AutoSaveSettingsWindow(settings, service, uiApp);
                window.ShowDialog();
                service.Dispose();
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
