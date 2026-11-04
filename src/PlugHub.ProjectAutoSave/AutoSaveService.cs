using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;

namespace PlugHub.ProjectAutoSave
{
    public static class AutoSaveService
    {
        private static readonly Dictionary<string, DateTime> LastSaveTimes = new Dictionary<string, DateTime>();
        private static UIControlledApplication? _application;
        private static bool _isHooked;
        private static bool _isSaving;

        public static void Start(UIControlledApplication application)
        {
            if (application == null)
            {
                throw new ArgumentNullException(nameof(application));
            }

            if (_isHooked && !ReferenceEquals(_application, application))
            {
                Stop();
            }

            _application = application;
            if (!_isHooked)
            {
                application.Idling += OnIdling;
                _isHooked = true;
            }
        }

        public static void Stop()
        {
            if (_application != null && _isHooked)
            {
                _application.Idling -= OnIdling;
            }

            _application = null;
            _isHooked = false;
            _isSaving = false;
            LastSaveTimes.Clear();
        }

        private static void OnIdling(object? sender, IdlingEventArgs args)
        {
            if (_isSaving)
            {
                return;
            }

            AutoSaveSettings settings = AutoSaveSettings.Load();
            if (!settings.IsEnabled)
            {
                return;
            }

            UIDocument? uiDocument = (sender as UIApplication)?.ActiveUIDocument;
            Document? document = uiDocument?.Document;
            if (document == null || !ShouldSaveDocument(document, settings))
            {
                return;
            }

            string documentKey = GetDocumentKey(document);

            _isSaving = true;
            try
            {
                SaveDocument(document);
                LastSaveTimes[documentKey] = DateTime.UtcNow;
                if (settings.ShowNotification)
                {
                    TaskDialog.Show("自动保存", "当前文档已自动保存。");
                }
            }
            catch (Exception ex)
            {
                if (settings.ShowNotification)
                {
                    TaskDialog.Show("自动保存", "自动保存失败：" + ex.Message);
                }
            }
            finally
            {
                _isSaving = false;
            }
        }

        private static bool ShouldSaveDocument(Document document, AutoSaveSettings settings)
        {
            if (document.IsFamilyDocument || document.IsReadOnly || document.IsModifiable)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(document.PathName))
            {
                return false;
            }

            string documentKey = GetDocumentKey(document);
            DateTime now = DateTime.UtcNow;
            if (!LastSaveTimes.TryGetValue(documentKey, out DateTime lastSaveTime))
            {
                LastSaveTimes[documentKey] = now;
                return false;
            }

            if (!document.IsModified)
            {
                return false;
            }

            return now - lastSaveTime >= TimeSpan.FromMinutes(settings.IntervalMinutes);
        }

        private static void SaveDocument(Document document)
        {
            document.Save();
        }

        private static string GetDocumentKey(Document document)
        {
            return document.PathName ?? document.Title;
        }
    }
}
