using System.Collections.Generic;
using PlugHub.Contracts.Features;
using PlugHub.Contracts.Modules;

namespace PlugHub.FamilyFileSaver
{
    public sealed class FamilyFileSaverModule : IPlugHubModule
    {
        public ModuleDescriptor Describe()
        {
            return new ModuleDescriptor
            {
                Id = "plughub.modules.family-file-saver",
                Name = "族文件保存",
                Description = "读取当前项目所有族，筛选后批量保存到指定文件夹。",
                State = ModuleState.Enabled,
                Order = 500,
                Tags = new[] { "family", "save", "export", "revit-api" },
                Features = new List<FeatureDescriptor>
                {
                    new FeatureDescriptor
                    {
                        Id = "plughub.modules.family-file-saver.save",
                        ModuleId = "plughub.modules.family-file-saver",
                        Name = "保存族文件",
                        Description = "读取当前项目所有族，排除系统族，弹出选择窗口批量保存到指定文件夹。",
                        Category = "family",
                        Group = "族文件保存",
                        Tags = new[] { "family", "save", "export" },
                        Order = 510,
                        DefaultState = FeatureState.Visible,
                        ButtonSize = "large",
                        CommandKey = "plughub.modules.family-file-saver.save",
                        CommandAssembly = "dist/PlugHub.FamilyFileSaver.dll",
                        CommandType = "PlugHub.FamilyFileSaver.SaveFamilyFilesCommand"
                    }
                }
            };
        }

        public void Initialize(IModuleContext context) { }

        public void Shutdown() { }
    }
}
