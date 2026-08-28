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

        public void TriggerRepairMode(bool target)
        {
            if(target)
            {
                SetHelper.IsRepairMode = true;
                SetHelper.ListPLCMessage.ShowInfoQueue($"返修模式开");
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    if (repairModeIndicatorWindow == null)
                    {
                        repairModeIndicatorWindow = new RepairModeIndicatorWindow();
                        repairModeIndicatorWindow.Closed += (s, e) => repairModeIndicatorWindow = null;
                        repairModeIndicatorWindow.Show();
                    }
                });
            }
            else
            {
                SetHelper.IsRepairMode = false;
                SetHelper.ListPLCMessage.ShowInfoQueue($"返修模式关");
                Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    repairModeIndicatorWindow?.Close();
                    repairModeIndicatorWindow = null;
                });
            }
        }

    }
}
