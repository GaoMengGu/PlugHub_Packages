using System;

// Test substitutes cover managed lifecycle behavior; production also builds against Revit 2020 references.
namespace Autodesk.Revit.DB
{
    public sealed class Document
    {
        public bool IsFamilyDocument { get; set; }
        public bool IsReadOnly { get; set; }
        public bool IsModifiable { get; set; }
        public bool IsModified { get; set; } = true;
        public string PathName { get; set; } = "project.rvt";
        public string Title { get; set; } = "Project";
        public int Saves { get; private set; }
        public Action? OnSave { get; set; }
        public void Save() { OnSave?.Invoke(); Saves++; }
    }
}

namespace Autodesk.Revit.UI.Events
{
    public sealed class IdlingEventArgs : EventArgs { }
}

namespace Autodesk.Revit.UI
{
    public enum Result { Succeeded, Failed, Cancelled }
    public interface IExternalApplication
    {
        Result OnStartup(UIControlledApplication application);
        Result OnShutdown(UIControlledApplication application);
    }
    public sealed class UIControlledApplication
    {
        public event EventHandler<Events.IdlingEventArgs>? Idling;
        public int Subscribers => Idling?.GetInvocationList().Length ?? 0;
        public void Raise(object? sender) => Idling?.Invoke(sender, new Events.IdlingEventArgs());
    }
    public sealed class UIApplication
    {
        public UIDocument? ActiveUIDocument { get; set; }
    }
    public sealed class UIDocument
    {
        public DB.Document Document { get; set; } = new DB.Document();
    }
    public static class TaskDialog
    {
        public static int Notifications;
        public static void Show(string title, string message) => Notifications++;
    }
}

namespace PlugHub.ProjectAutoSave
{
    // Replace only disk settings, so tests never touch the user's existing XML.
    public sealed class AutoSaveSettings
    {
        public static AutoSaveSettings Stored = new AutoSaveSettings();
        public bool IsEnabled { get; set; }
        public int IntervalMinutes { get; set; } = 10;
        public bool ShowNotification { get; set; }
        public static AutoSaveSettings Load() => Stored;
    }
}
