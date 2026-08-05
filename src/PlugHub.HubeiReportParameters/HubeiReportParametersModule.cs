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
                Description = "按用户选择的 CSV 模板创建 Revit 共享参数、写入参数值并生成 HIFC 属性集映射文件。",
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
                        Description = "选择 CSV 模板后创建或更新当前项目共享参数，可选择默认值、真实数据或不赋值，并按需生成明细表和 HIFC 映射文件。",
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
