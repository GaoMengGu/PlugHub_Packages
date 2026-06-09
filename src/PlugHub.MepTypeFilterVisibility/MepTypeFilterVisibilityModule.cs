using System.Collections.Generic;
using PlugHub.Contracts.Features;
using PlugHub.Contracts.Modules;

namespace PlugHub.MepTypeFilterVisibility
{
    public sealed class MepTypeFilterVisibilityModule : IPlugHubModule
    {
        public ModuleDescriptor Describe()
        {
            return new ModuleDescriptor
            {
                Id = "plughub.modules.mep-type-filter-visibility",
                Name = "机电工具",
                Description = "按选中风管、管道或桥架类型控制当前视图过滤器显示。",
                State = ModuleState.Enabled,
                Order = 330,
                Tags = new[] { "mep", "duct", "pipe", "cable-tray", "filter", "visibility", "revit-api" },
                Features = new List<FeatureDescriptor>
                {
                    new FeatureDescriptor
                    {
                        Id = "plughub.modules.mep-type-filter-visibility.apply",
                        ModuleId = "plughub.modules.mep-type-filter-visibility",
                        Name = "机电类型过滤显示",
                        Description = "框选风管、管道或桥架后，按系统类型或设备类型创建并切换当前视图过滤器。",
                        Category = "mep",
                        Group = "机电工具",
                        Tags = new[] { "mep", "duct", "pipe", "cable-tray", "filter", "visibility", "frequent" },
                        Order = 330,
                        DefaultState = FeatureState.Visible,
                        ButtonSize = "large",
                        CommandKey = "plughub.modules.mep-type-filter-visibility.apply",
                        CommandAssembly = "dist/PlugHub.MepTypeFilterVisibility.dll",
                        CommandType = "PlugHub.MepTypeFilterVisibility.ApplyMepTypeFilterVisibilityCommand"
                    }
                }
            };
        }

        public void Initialize(IModuleContext context) { }

        public void Shutdown() { }
    }
}
