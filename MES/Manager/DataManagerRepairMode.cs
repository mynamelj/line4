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
            { "OP2020PressFit1", false },
            { "OP2020PressFit2", false },
            { "OP2030PressFit", false }
        };

        private string GetStationKeyByNumber(string number)
        {
            var stations = SetHelper.StationNumber.numberGroups;
            if (!int.TryParse(number, out int stationNumber)
                || stationNumber <= 0 || stationNumber > stations.Count)
            {
                return null;
            }

            // 工位顺序以当前配置为准：OP2030PressFit 在另一台工控机上也是第1工位。
            string stationName = stations[stationNumber - 1]?.Name;
            return stationName != null && repairModeStatus.ContainsKey(stationName) ? stationName : null;
        }

        public void TriggerRepairMode(bool target, string Number)
        {
            bool isOP3040 = int.TryParse(Number, out int stationNumber)
                && stationNumber > 0
                && stationNumber <= SetHelper.StationNumber.numberGroups.Count
                && SetHelper.StationNumber.numberGroups[stationNumber - 1]?.Name
                    ?.IndexOf("OP3040", StringComparison.OrdinalIgnoreCase) >= 0;
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
