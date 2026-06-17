using System.Collections.Generic;
using PlugHub.Contracts.Features;
using PlugHub.Contracts.Modules;

namespace PlugHub.ProjectAutoSave
{
    public sealed class ProjectAutoSaveModule : IPlugHubModule
    {
        public ModuleDescriptor Describe()
        {
            return new ModuleDescriptor
            {
                Id = "plughub.modules.project-auto-save",
                Name = "小工具",
                Description = "按自定义分钟间隔自动保存当前 Revit 项目文件。",
                State = ModuleState.Enabled,
                Order = 120,
                Tags = new[] { "project", "save", "auto-save", "settings", "revit-api" },
                Features = new List<FeatureDescriptor>
                {
                    new FeatureDescriptor
                    {
                        Id = "plughub.modules.project-auto-save.settings",
                        ModuleId = "plughub.modules.project-auto-save",
                        Name = "自动保存",
                        Description = "设置本次 Revit 会话内项目文件自动保存开关、分钟间隔和保存后弹窗提示。",
                        Category = "utility",
                        Group = "小工具",
                        Tags = new[] { "project", "save", "auto-save", "settings" },
                        Order = 120,
                        DefaultState = FeatureState.Visible,
                        ButtonSize = "large",
                        CommandKey = "plughub.modules.project-auto-save.settings",
                        CommandAssembly = "dist/PlugHub.ProjectAutoSave.dll",
                        CommandType = "PlugHub.ProjectAutoSave.ShowAutoSaveSettingsCommand"
                    }
                }
            };
        }

        public void Initialize(IModuleContext context) { }

        public void Shutdown() { }
    }
}
