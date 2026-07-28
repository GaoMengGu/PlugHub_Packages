using System.Collections.Generic;
using PlugHub.Contracts.Features;
using PlugHub.Contracts.Modules;

namespace PlugHub.ClearHeightAnalysis
{
    public sealed class ClearHeightAnalysisModule : IPlugHubModule
    {
        public ModuleDescriptor Describe()
        {
            return new ModuleDescriptor
            {
                Id = "plughub.modules.clear-height-analysis",
                Name = "土建工具",
                Description = "按建筑外轮廓网格分析结构和机电构件控制下的项目净高。",
                State = ModuleState.Enabled,
                Order = 130,
                Tags = new[] { "civil", "architecture", "structural", "mep", "clear-height", "heatmap", "revit-api" },
                Features = new List<FeatureDescriptor>
                {
                    new FeatureDescriptor
                    {
                        Id = "plughub.modules.clear-height-analysis.analyze",
                        ModuleId = "plughub.modules.clear-height-analysis",
                        Name = "净高分析",
                        Description = "按可调网格投影结构和机电构件，生成净高热力图和低净高结果表。",
                        Category = "civil",
                        Group = "土建工具",
                        Tags = new[] { "civil", "architecture", "structural", "mep", "clear-height", "heatmap" },
                        Order = 130,
                        DefaultState = FeatureState.Visible,
                        ButtonSize = "large",
                        CommandKey = "plughub.modules.clear-height-analysis.analyze",
                        CommandAssembly = "dist/PlugHub.ClearHeightAnalysis.dll",
                        CommandType = "PlugHub.ClearHeightAnalysis.ClearHeightAnalysisCommand"
                    }
                }
            };
        }

        public void Initialize(IModuleContext context) { }

        public void Shutdown() { }
    }
}
