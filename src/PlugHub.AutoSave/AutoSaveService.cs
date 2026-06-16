using System;
using System.Threading;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace PlugHub.AutoSave
{
    /// <summary>
    /// 定时自动保存服务，通过 Timer 定期检查并保存。
    /// </summary>
    public sealed class AutoSaveService : IDisposable
    {
        private readonly UIApplication _uiApp;
        private readonly AutoSaveSettings _settings;
        private Timer _timer;
        private DateTime _lastSaveTime;
        private bool _disposed;

        public AutoSaveService(UIApplication uiApp, AutoSaveSettings settings)
        {
            _uiApp = uiApp ?? throw new ArgumentNullException(nameof(uiApp));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public void Start()
        {
            Stop();
            _lastSaveTime = DateTime.Now;
            _timer = new Timer(CheckAutoSave, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        public void Stop()
        {
            _timer?.Dispose();
            _timer = null;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Stop();
                _disposed = true;
            }
        }

        private void CheckAutoSave(object state)
        {
            if (!_settings.IsEnabled)
                return;

            var elapsed = DateTime.Now - _lastSaveTime;
            if (elapsed.TotalMinutes >= _settings.IntervalMinutes)
            {
                TrySave();
            }
        }

        public void TrySave()
        {
            try
            {
                var doc = _uiApp.ActiveUIDocument?.Document;
                if (doc == null)
                    return;

                if (!doc.IsModified)
                    return;

                // 只保存项目文件，不保存族文件
                if (doc.IsFamilyDocument)
                    return;

                // 需要有效的保存路径
                string path = doc.PathName;
                if (string.IsNullOrEmpty(path))
                    return;

                var saveOptions = new SaveAsOptions
                {
                    OverwriteExistingFile = true,
                    Compact = false
                };

                doc.SaveAs(path, saveOptions);
                _lastSaveTime = DateTime.Now;

                if (_settings.ShowNotification)
                {
                    ShowNotification($"自动保存成功 {DateTime.Now:HH:mm:ss}");
                }
            }
            catch (Exception ex)
            {
                if (_settings.ShowNotification)
                {
                    ShowNotification($"自动保存失败: {ex.Message}");
                }
            }
        }

        public void ManualSave()
        {
            TrySave();
        }

        private void ShowNotification(string message)
        {
            try
            {
                var doc = _uiApp.ActiveUIDocument?.Document;
                if (doc != null)
                {
                    var td = new TaskDialog("自动保存");
                    td.MainContent = message;
                    td.MainInstruction = "PlugHub AutoSave";
                    td.Show();
                }
            }
            catch { /* 静默失败 */ }
        }
    }
}
