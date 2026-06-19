using DAL;
using MES.Comm;
using MES.Manager;
using MES.SetModel;
using PropertyChanged;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace MES.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public static class MonitorHelper
    {
        public static SiemensS7Instrument siemens = new SiemensS7Instrument();
    }
}