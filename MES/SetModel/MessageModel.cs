using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MES.SetModel
{
    [AddINotifyPropertyChangedInterface]
    public class MessageModel
    {
        public DateTime DTNow { get; set; } = DateTime.Now;
        public string Info { get; set; }

        public Brush FailBrush { get; set; } = Brushes.Black;

    }
}
