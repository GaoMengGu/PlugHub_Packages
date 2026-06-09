using System.Collections.Generic;
using PlugHub.Contracts.Features;
using PlugHub.Contracts.Modules;

namespace PlugHub.FamilyMaterialParameters
{
    public sealed class FamilyMaterialParametersModule : IPlugHubModule
    {
        public ModuleDescriptor Describe()
        {
            return new ModuleDescriptor
            {
                Id = "plughub.modules.family-material-parameters",
                Name = "族工具",
                Description = "族文件材质参数批处理工具。",
                State = ModuleState.Enabled,
                Order = 400,
                Tags = new[] { "family", "material", "revit-api" },
                Features = new List<FeatureDescriptor>
                {
                    new FeatureDescriptor
                    {
                        Id = "plughub.modules.family-material-parameters.batch-add-material",
                        ModuleId = "plughub.modules.family-material-parameters",
                        Name = "批量材质",
                        Description = "批量打开族文件，添加材质参数并关联实体材质参数。",
                        Category = "family",
                        Group = "族工具",
                        Tags = new[] { "family", "material", "batch" },
                        Order = 410,
                        DefaultState = FeatureState.Visible,
                        ButtonSize = "large",
                        CommandKey = "plughub.modules.family-material-parameters.batch-add-material",
                        CommandAssembly = "dist/PlugHub.FamilyMaterialParameters.dll",
                        CommandType = "PlugHub.FamilyMaterialParameters.BatchAddMaterialParameterCommand"
                    }
                }
            };
        }

        public void Initialize(IModuleContext context) { }

        public void Shutdown() { }
    }
}
