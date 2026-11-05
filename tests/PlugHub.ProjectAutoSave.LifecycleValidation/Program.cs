using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Text.Json;
using Document = Autodesk.Revit.DB.Document;
using Autodesk.Revit.UI;
using PlugHub.ProjectAutoSave;

internal static class Program
{
    private static readonly Dictionary<string, DateTime> SaveTimes =
        (Dictionary<string, DateTime>)typeof(AutoSaveService)
            .GetField("LastSaveTimes", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;

    private static void Main()
    {
        Lifecycle();
        Payload();
        Console.WriteLine("Auto-save lifecycle and all 9 package payloads passed.");
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Lifecycle()
    {
        var entry = new ProjectAutoSaveApplication();
        var host = new UIControlledApplication();
        var ui = new UIApplication();
        try
        {
            Require(entry.OnStartup(host) == Result.Succeeded, "Startup failed without an active document.");
            entry.OnStartup(host);
            Require(host.Subscribers == 1, "Repeated start must not duplicate the event.");
            host.Raise(ui);
            host.Raise(null);
            var document = new Document();
            ui.ActiveUIDocument = new UIDocument { Document = document };
            host.Raise(ui);
            Require(document.Saves == 0 && host.Subscribers == 1, "Disabled settings must not save or remove the lifecycle hook.");

            AutoSaveSettings.Stored = new AutoSaveSettings { IsEnabled = true, IntervalMinutes = 1 };
            host.Raise(ui);
            Require(document.Saves == 0, "First idle must establish an interval, not save immediately.");
            SaveTimes[document.PathName] = DateTime.UtcNow.AddMinutes(-2);
            host.Raise(ui);
            Require(document.Saves == 1, "Enabling existing settings must work without opening the command.");
            host.Raise(ui);
            Require(document.Saves == 1, "Consecutive idles must honor the interval.");

            SaveTimes[document.PathName] = DateTime.UtcNow.AddMinutes(-2);
            AutoSaveSettings.Stored.IsEnabled = false;
            host.Raise(ui);
            Require(document.Saves == 1 && host.Subscribers == 1, "Disabling must stop saves and allow re-enabling.");
            AutoSaveSettings.Stored.IsEnabled = true;
            host.Raise(ui);
            Require(document.Saves == 2, "Re-enabling must resume without a new subscription.");

            foreach (var blocked in new[] {
                new Document { IsFamilyDocument = true }, new Document { IsReadOnly = true },
                new Document { IsModifiable = true }, new Document { PathName = "" },
                new Document { IsModified = false } })
            {
                SaveTimes[blocked.PathName] = DateTime.UtcNow.AddMinutes(-2);
                ui.ActiveUIDocument.Document = blocked;
                host.Raise(ui);
                Require(blocked.Saves == 0, "Protected or unchanged documents must not save.");
            }

            ui.ActiveUIDocument.Document = document;
            SaveTimes[document.PathName] = DateTime.UtcNow.AddMinutes(-2);
            document.OnSave = () => throw new IOException("Simulated save failure");
            AutoSaveSettings.Stored.ShowNotification = true;
            host.Raise(ui);
            Require(document.Saves == 2 && TaskDialog.Notifications == 1, "Save failures must be isolated and reported.");
            document.OnSave = () => host.Raise(ui);
            host.Raise(ui);
            Require(document.Saves == 3, "Save guard must recover after failure and prevent recursive saving.");
            Require(entry.OnShutdown(host) == Result.Succeeded && host.Subscribers == 0 && SaveTimes.Count == 0,
                "Shutdown must release the hook and session timing.");
            host.Raise(ui);
            Require(document.Saves == 3, "Shutdown must stop all callbacks.");

            entry.OnStartup(host);
            host.Raise(ui);
            Require(document.Saves == 3 && SaveTimes.ContainsKey(document.PathName), "New session must honor stored enabled settings with a fresh interval.");
            var otherHost = new UIControlledApplication();
            entry.OnStartup(otherHost);
            Require(host.Subscribers == 0 && otherHost.Subscribers == 1, "Changing host must release the previous hook.");
            entry.OnShutdown(otherHost);
            entry.OnShutdown(otherHost);
            Require(otherHost.Subscribers == 0, "Shutdown must be idempotent.");
        }
        finally { AutoSaveService.Stop(); }
    }

    private static void Payload()
    {
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root != null && !File.Exists(Path.Combine(root.FullName, "packages.json"))) root = root.Parent;
        Require(root != null, "Package root not found.");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(root!.FullName, "packages.json")));
        var modules = manifest.RootElement.GetProperty("modules").EnumerateArray().ToArray();
        Require(modules.Length == 9, "All nine migrated payloads must be checked.");
        foreach (var module in modules)
        {
            string relative = module.GetProperty("assembly").GetString()!;
            string path = Path.GetFullPath(Path.Combine(root.FullName, relative));
            Require(!Path.IsPathRooted(relative) && path.StartsWith(root.FullName + Path.DirectorySeparatorChar), "Assembly must stay package-relative.");
            using var stream = File.OpenRead(path);
            using var pe = new PEReader(stream);
            var metadata = pe.GetMetadataReader();
            Require(metadata.AssemblyReferences.Any(handle => metadata.GetString(metadata.GetAssemblyReference(handle).Name) == "PlugHub.Contracts"), "Missing Contracts reference: " + relative);
            foreach (var handle in metadata.AssemblyReferences)
            {
                string dependency = metadata.GetString(metadata.GetAssemblyReference(handle).Name);
                bool providedByHost = dependency == "PlugHub.Contracts" || dependency == "mscorlib" || dependency == "System"
                    || dependency.StartsWith("System.", StringComparison.Ordinal) || dependency == "WindowsBase"
                    || dependency == "PresentationCore" || dependency == "PresentationFramework"
                    || dependency == "RevitAPI" || dependency == "RevitAPIUI";
                Require(providedByHost || File.Exists(Path.Combine(Path.GetDirectoryName(path)!, dependency + ".dll")), "Missing private dependency: " + dependency);
            }
            Require(metadata.GetAssemblyDefinition().GetCustomAttributes().Any(handle => {
                var attribute = metadata.GetCustomAttribute(handle);
                if (attribute.Constructor.Kind != HandleKind.MemberReference) return false;
                var constructor = metadata.GetMemberReference((MemberReferenceHandle)attribute.Constructor);
                if (constructor.Parent.Kind != HandleKind.TypeReference) return false;
                var owner = metadata.GetTypeReference((TypeReferenceHandle)constructor.Parent);
                if (metadata.GetString(owner.Name) != "TargetFrameworkAttribute") return false;
                var value = metadata.GetBlobReader(attribute.Value);
                return value.ReadUInt16() == 1 && value.ReadSerializedString() == ".NETFramework,Version=v4.8";
            }), "Payload must target net48: " + relative);
            foreach (var handle in metadata.MemberReferences)
            {
                var member = metadata.GetMemberReference(handle);
                if (member.Parent.Kind != HandleKind.TypeReference) continue;
                var owner = metadata.GetTypeReference((TypeReferenceHandle)member.Parent);
                string ns = metadata.GetString(owner.Namespace);
                string name = metadata.GetString(member.Name);
                Require(!ns.StartsWith("PlugHub.Contracts", StringComparison.Ordinal) || (name != "set_State" && name != "set_DefaultState"), "Old state contract remains in " + relative);
            }
            foreach (var feature in module.GetProperty("features").EnumerateArray())
                RequireEntry(metadata, feature.GetProperty("commandType").GetString()!, "IExternalCommand");
            if (module.TryGetProperty("autoStart", out var autoStart) && autoStart.GetBoolean())
                RequireEntry(metadata, module.GetProperty("startupType").GetString()!, "IExternalApplication");
        }
    }

    private static void RequireEntry(MetadataReader metadata, string typeName, string contract)
    {
        var type = metadata.TypeDefinitions.Select(metadata.GetTypeDefinition).Single(item =>
            metadata.GetString(item.Namespace) + "." + metadata.GetString(item.Name) == typeName);
        Require((type.Attributes & TypeAttributes.Public) != 0 && (type.Attributes & TypeAttributes.Abstract) == 0, "Entry must be public and concrete: " + typeName);
        Require(type.GetInterfaceImplementations().Any(handle => {
            var implemented = metadata.GetInterfaceImplementation(handle).Interface;
            if (implemented.Kind != HandleKind.TypeReference) return false;
            var reference = metadata.GetTypeReference((TypeReferenceHandle)implemented);
            return metadata.GetString(reference.Namespace) == "Autodesk.Revit.UI" && metadata.GetString(reference.Name) == contract;
        }), "Missing " + contract + ": " + typeName);
        Require(type.GetMethods().Select(metadata.GetMethodDefinition).Any(method => {
            var signature = metadata.GetBlobReader(method.Signature);
            signature.ReadSignatureHeader();
            int parameterCount = signature.ReadCompressedInteger();
            return metadata.GetString(method.Name) == ".ctor" && (method.Attributes & MethodAttributes.Public) != 0 && parameterCount == 0;
        }), "Entry must have a public parameterless constructor: " + typeName);
    }
}
