using MES.Comm;
using MES.View;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MES.Manager
{

    public partial class DataManager
    {
        private readonly Dictionary<string, bool> repairModeStatus = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            { "2030BearingPress1", false },
            { "2030BearingPress2", false },
            { "2020BearingPress", false }
        };

        private string GetStationKeyByNumber(string number)
        {
            switch (number?.Trim())
            {
                case "1":
                    return "2030BearingPress1";
                case "2":
                    return "2030BearingPress2";
                case "3":
                    return "2020BearingPress";
                default:
                    return null;
            }
        }

        public void TriggerRepairMode(bool target, string Number)
        {
            bool isOP3040 = int.TryParse(Number, out int stationNumber)
                && stationNumber > 0
                && stationNumber <= SetHelper.StationNumber.numberGroups.Count
                && SetHelper.StationNumber.numberGroups[stationNumber - 1].Name
                    .IndexOf("OP3040", StringComparison.OrdinalIgnoreCase) >= 0;
            if (isOP3040)
            {
                SetHelper.IsOP3040RepairMode = target;
            }
            else
            {
                string stationKey = GetStationKeyByNumber(Number);
                if (!string.IsNullOrEmpty(stationKey))
                {
                    repairModeStatus[stationKey] = target;
                }
                SetHelper.IsRepairMode = repairModeStatus.Values.Any(v => v);
            }
            SetHelper.ListPLCMessage.ShowInfoQueue($"工位[{Number}] 返修模式: {(target ? "开" : "关")}");

            // 返修状态统一由MessageView显示，避免悬浮窗口与状态文字重叠。
        }
    }
}
