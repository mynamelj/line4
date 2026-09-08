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
        private static RepairModeIndicatorWindow repairModeIndicatorWindow;

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
            string stationKey = GetStationKeyByNumber(Number);
            if (!string.IsNullOrEmpty(stationKey))
            {
                repairModeStatus[stationKey] = target;
            }

            // 只要有任意一个工位处于返修模式，全局即为返修模式；全为 false 时才退出
            bool anyRepair = repairModeStatus.Values.Any(v => v);
            SetHelper.IsRepairMode = anyRepair;

            SetHelper.ListPLCMessage.ShowInfoQueue($"工位[{(stationKey ?? Number)}] 返修模式: {(target ? "开" : "关")}，整线返修状态: {(anyRepair ? "开" : "关")}");

            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                if (anyRepair)
                {
                    if (repairModeIndicatorWindow == null)
                    {
                        repairModeIndicatorWindow = new RepairModeIndicatorWindow();
                        repairModeIndicatorWindow.Closed += (s, e) => repairModeIndicatorWindow = null;
                        repairModeIndicatorWindow.Show();
                    }
                }
                else
                {
                    repairModeIndicatorWindow?.Close();
                    repairModeIndicatorWindow = null;
                }
            });
        }
    }
}
