using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB;

namespace PlugHub.HubeiReportParameters
{
    public static class RevitCategoryCatalog
    {
        private static readonly IReadOnlyDictionary<string, BuiltInCategory> Categories =
            new Dictionary<string, BuiltInCategory>(StringComparer.Ordinal)
            {
                ["项目信息"] = BuiltInCategory.OST_ProjectInformation,
                ["场地"] = BuiltInCategory.OST_Site,
                ["标高"] = BuiltInCategory.OST_Levels,
                ["轴网"] = BuiltInCategory.OST_Grids,
                ["墙"] = BuiltInCategory.OST_Walls,
                ["门"] = BuiltInCategory.OST_Doors,
                ["窗"] = BuiltInCategory.OST_Windows,
                ["楼板"] = BuiltInCategory.OST_Floors,
                ["屋顶"] = BuiltInCategory.OST_Roofs,
                ["天花板"] = BuiltInCategory.OST_Ceilings,
                ["楼梯"] = BuiltInCategory.OST_Stairs,
                ["栏杆"] = BuiltInCategory.OST_Railings,
                ["房间"] = BuiltInCategory.OST_Rooms,
                ["空间"] = BuiltInCategory.OST_MEPSpaces,
                ["面积"] = BuiltInCategory.OST_Areas,
                ["结构柱"] = BuiltInCategory.OST_StructuralColumns,
                ["结构框架"] = BuiltInCategory.OST_StructuralFraming,
                ["结构基础"] = BuiltInCategory.OST_StructuralFoundation,
                ["结构墙"] = BuiltInCategory.OST_Walls,
                ["结构楼板"] = BuiltInCategory.OST_Floors,
                ["桁架"] = BuiltInCategory.OST_Truss,
                ["支撑"] = BuiltInCategory.OST_StructuralFraming,
                ["风管"] = BuiltInCategory.OST_DuctCurves,
                ["风管管件"] = BuiltInCategory.OST_DuctFitting,
                ["风管附件"] = BuiltInCategory.OST_DuctAccessory,
                ["风口"] = BuiltInCategory.OST_DuctTerminal,
                ["机械设备"] = BuiltInCategory.OST_MechanicalEquipment,
                ["风管保温"] = BuiltInCategory.OST_DuctInsulations,
                ["管道"] = BuiltInCategory.OST_PipeCurves,
                ["管道管件"] = BuiltInCategory.OST_PipeFitting,
                ["管道附件"] = BuiltInCategory.OST_PipeAccessory,
                ["卫浴装置"] = BuiltInCategory.OST_PlumbingFixtures,
                ["喷头"] = BuiltInCategory.OST_Sprinklers,
                ["管道保温"] = BuiltInCategory.OST_PipeInsulations,
                ["电缆桥架"] = BuiltInCategory.OST_CableTray,
                ["电缆桥架配件"] = BuiltInCategory.OST_CableTrayFitting,
                ["线管"] = BuiltInCategory.OST_Conduit,
                ["线管配件"] = BuiltInCategory.OST_ConduitFitting,
                ["电气设备"] = BuiltInCategory.OST_ElectricalEquipment,
                ["照明设备"] = BuiltInCategory.OST_LightingFixtures,
                ["通信设备"] = BuiltInCategory.OST_CommunicationDevices
            };

        public static IReadOnlyCollection<BuiltInCategory> Parse(string value, int rowNumber)
        {
            string[] names = (value ?? string.Empty).Split(',').Select(name => name.Trim()).Where(name => name.Length > 0).ToArray();
            if (names.Length == 0)
            {
                throw new InvalidOperationException("第 " + rowNumber + " 行的 Revit构件不能为空。");
            }

            var result = new List<BuiltInCategory>();
            foreach (string name in names)
            {
                if (!Categories.TryGetValue(name, out BuiltInCategory category))
                {
                    throw new InvalidOperationException("第 " + rowNumber + " 行的 Revit构件不受支持：" + name + "。");
                }

                if (!result.Contains(category))
                {
                    result.Add(category);
                }
            }

            return result;
        }
    }
}
