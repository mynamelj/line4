using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MES.SetModel
{
    [Serializable]
    [AddINotifyPropertyChangedInterface]
    public class LightConfigModel
    {
        public ObservableCollection<LightConfig> ListGroup { get; set; } = new ObservableCollection<LightConfig>();
    }
    [Serializable]
    [AddINotifyPropertyChangedInterface]
    public class LightConfig
    {
        public int LightNumber { get; set; }
        public string LightCode { get; set; } = "";
    }
}
