using System.Collections.Generic;
using PlugHub.Contracts.Features;
using PlugHub.Contracts.Modules;

namespace PlugHub.ReferencePlaneVisibility
{
    public sealed class ReferencePlaneVisibilityModule : IPlugHubModule
    {
        public ModuleDescriptor Describe()
        {
            return new ModuleDescriptor
            {
                Id = "plughub.modules.reference-plane-visibility",
                Name = "视图工具",
                Description = "当前视图参照平面显示控制工具。",
                State = ModuleState.Enabled,
                Order = 240,
                Tags = new[] { "view", "reference-plane", "visibility", "revit-api" },
                Features = new List<FeatureDescriptor>
                {
                    new FeatureDescriptor
                    {
                        Id = "plughub.modules.reference-plane-visibility.toggle",
                        ModuleId = "plughub.modules.reference-plane-visibility",
                        Name = "参照平面显隐",
                        Description = "在当前视图中切换参照平面类别的可见性。",
                        Category = "view",
                        Group = "视图工具",
                        Tags = new[] { "view", "reference-plane", "visibility", "frequent" },
                        Order = 240,
                        DefaultState = FeatureState.Visible,
                        ButtonSize = "large",
                        CommandKey = "plughub.modules.reference-plane-visibility.toggle",
                        CommandAssembly = "dist/PlugHub.ReferencePlaneVisibility.dll",
                        CommandType = "PlugHub.ReferencePlaneVisibility.ToggleReferencePlaneVisibilityCommand"
                    }
                }
            };
        }

        public void Initialize(IModuleContext context) { }

        public void Shutdown() { }
    }
}
