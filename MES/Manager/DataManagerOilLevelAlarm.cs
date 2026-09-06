using MES.Comm;
using MES.View;
using System.Windows;

namespace MES.Manager
{
    public partial class DataManager
    {
        private static OilLevelAlarmWindow oilLevelAlarmWindow;

        public void TriggerOilLevelAlarm(bool target,string product)
        {
            SetHelper.ListPLCMessage.ShowInfoQueue(target ? $"液位低报警：{product}油位低" : $"油位报警解除：{product}");
            Application.Current.Dispatcher.BeginInvoke(() =>
            {
                if (target)
                {
                    if (oilLevelAlarmWindow == null)
                    {
                        oilLevelAlarmWindow = new OilLevelAlarmWindow();
                        oilLevelAlarmWindow.Closed += (s, e) => oilLevelAlarmWindow = null;
                        oilLevelAlarmWindow.Show();
                    }
                }
                else
                {
                    oilLevelAlarmWindow?.Close();
                    oilLevelAlarmWindow = null;
                }
            });
        }
    }
}
