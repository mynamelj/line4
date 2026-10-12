using System;

namespace MES.Manager
{
    internal static class StationRules
    {
        // 啮合站已独立移出，仅输入轴、中间轴的压装站共用 SN 前缀分流。
        internal static bool IsInputOrIntermediatePressFitStation(string stationName)
        {
            return string.Equals(stationName, "OP2020PressFit1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(stationName, "OP2020PressFit2", StringComparison.OrdinalIgnoreCase);
        }
    }
}
