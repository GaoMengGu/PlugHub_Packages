using System.Collections.Generic;
using PlugHub.Contracts.Features;
using PlugHub.Contracts.Modules;

namespace PlugHub.HubeiReportParameters
{
    public sealed class HubeiReportParametersModule : IPlugHubModule
    {
        public ModuleDescriptor Describe()
        {
            return new ModuleDescriptor
            {
                Id = "plughub.modules.hubei-report-parameters",
                Name = "土建工具",
                Description = "按湖北报规清单批量创建总图、单体、全局共享参数，可用最小报建限定参数范围。",
                State = ModuleState.Enabled,
                Order = 395,
                Tags = new[] { "civil", "hubei", "report", "planning", "parameter", "shared-parameter", "revit-api" },
                Features = new List<FeatureDescriptor>
                {
                    new FeatureDescriptor
                    {
                        Id = "plughub.modules.hubei-report-parameters.sync",
                        ModuleId = "plughub.modules.hubei-report-parameters",
                        Name = "湖北报规参数",
                        Description = "按总图、单体、全局分类选择属性，可用最小报建限定参数范围，并在当前项目中创建对应共享参数和默认值。",
                        Category = "civil",
                        Group = "土建工具",
                        Tags = new[] { "civil", "hubei", "report", "planning", "parameter", "shared-parameter" },
                        Order = 395,
                        DefaultState = FeatureState.Visible,
                        ButtonSize = "large",
                        CommandKey = "plughub.modules.hubei-report-parameters.sync",
                        CommandAssembly = "dist/PlugHub.HubeiReportParameters.dll",
                        CommandType = "PlugHub.HubeiReportParameters.SyncHubeiReportParametersCommand"
                    }
                }
            };
        }

        public void Initialize(IModuleContext context) { }

        public void Shutdown() { }
    }
}
