using MES.SetModel;

// Only configuration and message storage are replaced; no WPF startup, PLC or MES requests run.
namespace MES.Manager
{
    public static class SetHelper
    {
        public static StationNumberModel StationNumber = new StationNumberModel();
        public static bool IsRepairMode;
        public static bool IsOP3040RepairMode;
        public static TestMessages ListScanMessage = new TestMessages();
        public static TestMessages ListPLCMessage = new TestMessages();
    }

    public sealed class TestMessages
    {
        public void ShowInfoQueue(string message) { }
    }

    public partial class DataManager
    {
        public bool IsStationInRepair(string stationName)
        {
            return repairModeStatus.TryGetValue(stationName, out bool enabled) && enabled;
        }
    }
}

namespace PropertyChanged
{
    public sealed class AddINotifyPropertyChangedInterfaceAttribute : System.Attribute { }
}
namespace MES.Comm { }
namespace MES.View { }
namespace System.Windows { }
