using System.Collections.Generic;
using PlugHub.Contracts.Features;
using PlugHub.Contracts.Modules;

namespace PlugHub.GridVisibility
{
    public sealed class GridVisibilityModule : IPlugHubModule
    {
        public ModuleDescriptor Describe()
        {
            return new ModuleDescriptor
            {
                Id = "plughub.modules.grid-visibility",
                Name = "视图工具",
                Description = "当前视图轴网显示控制工具。",
                State = ModuleState.Enabled,
                Order = 200,
                Tags = new[] { "view", "grid", "visibility", "revit-api" },
                Features = new List<FeatureDescriptor>
                {
                    new FeatureDescriptor
                    {
                        Id = "plughub.modules.grid-visibility.toggle",
                        ModuleId = "plughub.modules.grid-visibility",
                        Name = "显隐",
                        Description = "在当前视图中切换轴网类别的可见性。",
                        Category = "view",
                        Group = "视图工具",
                        Tags = new[] { "view", "grid", "visibility", "frequent" },
                        Order = 210,
                        DefaultState = FeatureState.Visible,
                        ButtonSize = "large",
                        CommandKey = "plughub.modules.grid-visibility.toggle",
                        CommandAssembly = "dist/PlugHub.GridVisibility.dll",
                        CommandType = "PlugHub.GridVisibility.ToggleGridVisibilityCommand"
                    }
                }
            };
        }

        public void Initialize(IModuleContext context) { }

        public void Shutdown() { }
    }
}
