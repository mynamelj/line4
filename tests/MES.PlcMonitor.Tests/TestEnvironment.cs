using MES.SetModel;

// Run monitor logic with in-memory configuration and PLC responses, without hardware access.
namespace MES.Manager
{
    public static class SetHelper
    {
        public static PLCSettingModel PLCSetting = new PLCSettingModel();
        public static TestPlc siemens = new TestPlc();
    }

    public sealed class TestPlc
    {
        public Func<PLCGroupName, string, object> Read;

        public bool ReadItem(PLCGroupName group, string tag, ref object value)
        {
            value = Read?.Invoke(group, tag);
            return value != null;
        }

    }
}

namespace PropertyChanged
{
    public sealed class AddINotifyPropertyChangedInterfaceAttribute : System.Attribute { }
}
