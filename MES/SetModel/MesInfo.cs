using MES.ViewModel;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace MES.SetModel
{
    [AddINotifyPropertyChangedInterface]
    public class MesInfo
    {
        public string CarrierID { get; set; } = "";
        public string SNCode { get; set; } = "";
        public ObservableCollection<MaterialInfo> MaterialCode { get; set; } = new ObservableCollection<MaterialInfo>();

        public string LinkCompResult { get; set; } = "";
        public Brush LinkCompColor { get; set; } = Brushes.Green;
        public string LinkCompMsg { get; set; } = "";

        public string CheckInResult { get; set; } = "";
        public Brush CheckInColor { get; set; } = Brushes.Green;
        public string CheckInMsg { get; set; } = "";
        public string CheckOutResult { get; set; } = "";
        public string CheckOutMsg { get; set; } = "";
        public Brush CheckOutColor { get; set; } = Brushes.Green;

    }
}
