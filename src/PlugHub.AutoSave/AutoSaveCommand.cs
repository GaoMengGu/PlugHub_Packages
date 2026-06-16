using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PlugHub.AutoSave
{
    /// <summary>
    /// 手动触发自动保存的命令。
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    public sealed class AutoSaveCommand : IExternalCommand
    {
        private static AutoSaveService _service;

        public static void SetService(AutoSaveService service)
        {
            _service = service;
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            if (_service == null)
            {
                message = "自动保存服务未初始化。";
                return Result.Failed;
            }

            try
            {
                _service.ManualSave();
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
