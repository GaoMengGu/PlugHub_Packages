using System.Collections.Generic;
using PlugHub.Contracts.Features;
using PlugHub.Contracts.Modules;

namespace PlugHub.DuctPreferredJunction
{
    public sealed class DuctPreferredJunctionModule : IPlugHubModule
    {
        public ModuleDescriptor Describe()
        {
            return new ModuleDescriptor
            {
                Id = "plughub.modules.duct-preferred-junction",
                Name = "机电风管",
                Description = "风管连接偏好工具。",
                State = ModuleState.Enabled,
                Order = 300,
                Tags = new[] { "mep", "duct", "revit-api" },
                Features = new List<FeatureDescriptor>
                {
                    new FeatureDescriptor
                    {
                        Id = "plughub.modules.duct-preferred-junction.switch",
                        ModuleId = "plughub.modules.duct-preferred-junction",
                        Name = "风管接头切换",
                        Description = "在已选或点选风管的类型上切换 Tee/Tap 首选连接类型。",
                        Category = "mep",
                        Group = "机电风管",
                        Tags = new[] { "mep", "duct", "frequent" },
                        Order = 310,
                        DefaultState = FeatureState.Visible,
                        ButtonSize = "large",
                        CommandKey = "plughub.modules.duct-preferred-junction.switch",
                        CommandAssembly = "dist/PlugHub.DuctPreferredJunction.dll",
                        CommandType = "PlugHub.DuctPreferredJunction.DuctPreferredJunctionSwitcherCommand"
                    }
                }
            };
        }

        public void Initialize(IModuleContext context) { }

        public void Shutdown() { }
    }
}
