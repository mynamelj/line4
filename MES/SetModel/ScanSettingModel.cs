using PropertyChanged;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MES.SetModel
{
    [AddINotifyPropertyChangedInterface]
    public class ScanSettingModel
    {
        public string ID { get; set; } = Guid.NewGuid().ToString();

        public string IP { get; set; }
        public int Port { get; set; }
        public int Com { get; set; }
        public string BaudRate { get; set; }
        public string Parity { get; set; }
        public string DataBits { get; set; }
        public string StopBits { get; set; }

        public string EndLine { get; set; } = "\r\n";
        public int TestTime { get; set; } = 3000;
        public string OnCmd { get; set; }
        public bool OnHex { get; set; } = false;
        public string OffCmd { get; set; }
        public bool OffHex { get; set; } = false;
    }
}
