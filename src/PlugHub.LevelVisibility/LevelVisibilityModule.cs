using System.Collections.Generic;
using PlugHub.Contracts.Features;
using PlugHub.Contracts.Modules;

namespace PlugHub.LevelVisibility
{
    public sealed class LevelVisibilityModule : IPlugHubModule
    {
        public ModuleDescriptor Describe()
        {
            return new ModuleDescriptor
            {
                Id = "plughub.modules.level-visibility",
                Name = "视图工具",
                Description = "当前视图标高显示控制工具。",
                State = ModuleState.Enabled,
                Order = 220,
                Tags = new[] { "view", "level", "visibility", "revit-api" },
                Features = new List<FeatureDescriptor>
                {
                    new FeatureDescriptor
                    {
                        Id = "plughub.modules.level-visibility.toggle",
                        ModuleId = "plughub.modules.level-visibility",
                        Name = "标高显隐",
                        Description = "在当前视图中切换标高类别的可见性。",
                        Category = "view",
                        Group = "视图工具",
                        Tags = new[] { "view", "level", "visibility", "frequent" },
                        Order = 220,
                        DefaultState = FeatureState.Visible,
                        ButtonSize = "large",
                        CommandKey = "plughub.modules.level-visibility.toggle",
                        CommandAssembly = "dist/PlugHub.LevelVisibility.dll",
                        CommandType = "PlugHub.LevelVisibility.ToggleLevelVisibilityCommand"
                    }
                }
            };
        }

        public void Initialize(IModuleContext context) { }

        public void Shutdown() { }
    }
}
