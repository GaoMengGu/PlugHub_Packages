using System;

namespace PlugHub.ClearHeightAnalysis.Core.Services
{
    public static class ResultViewPolicy
    {
        public static bool IsSupported(string viewType)
        {
            return string.Equals(viewType,"FloorPlan",StringComparison.Ordinal)||
                   string.Equals(viewType,"EngineeringPlan",StringComparison.Ordinal)||
                   string.Equals(viewType,"AreaPlan",StringComparison.Ordinal);
        }
        public static void EnsureSupported(string viewType)
        {
            if(!IsSupported(viewType))throw new InvalidOperationException("净高模型成果仅支持楼层平面、结构平面和面积平面。");
        }
    }
}
