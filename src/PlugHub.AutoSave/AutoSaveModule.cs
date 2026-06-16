using System.Collections.Generic;
using PlugHub.Contracts.Features;
using PlugHub.Contracts.Modules;

namespace PlugHub.AutoSave
{
    public sealed class AutoSaveModule : IPlugHubModule
    {
        public ModuleDescriptor Describe()
        {
            return new ModuleDescriptor
            {
                Id = "plughub.modules.auto-save",
                Name = "自动保存",
                Description = "定时自动保存 Revit 项目文件，支持自定义间隔和提醒。",
                State = ModuleState.Enabled,
                Order = 100,
                Tags = new[] { "save", "auto", "productivity", "revit-api" },
                Features = new List<FeatureDescriptor>
                {
                    new FeatureDescriptor
                    {
                        Id = "plughub.modules.auto-save.manual-save",
                        ModuleId = "plughub.modules.auto-save",
                        Name = "立即保存",
                        Description = "手动触发一次自动保存。",
                        Category = "save",
                        Group = "自动保存",
                        Tags = new[] { "save", "manual" },
                        Order = 100,
                        DefaultState = FeatureState.Visible,
                        ButtonSize = "large",
                        CommandKey = "plughub.modules.auto-save.manual-save",
                        CommandAssembly = "dist/PlugHub.AutoSave.dll",
                        CommandType = "PlugHub.AutoSave.AutoSaveCommand"
                    },
                    new FeatureDescriptor
                    {
                        Id = "plughub.modules.auto-save.settings",
                        ModuleId = "plughub.modules.auto-save",
                        Name = "保存设置",
                        Description = "打开自动保存设置窗口，配置保存间隔和提醒。",
                        Category = "save",
                        Group = "自动保存",
                        Tags = new[] { "save", "settings" },
                        Order = 110,
                        DefaultState = FeatureState.Visible,
                        ButtonSize = "large",
                        CommandKey = "plughub.modules.auto-save.settings",
                        CommandAssembly = "dist/PlugHub.AutoSave.dll",
                        CommandType = "PlugHub.AutoSave.AutoSaveSettingsCommand"
                    }
                }
            };
        }

        public void Initialize(IModuleContext context) { }

        public void Shutdown() { }
    }
}
