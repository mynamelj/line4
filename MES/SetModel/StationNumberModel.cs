using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MES.SetModel
{
    [AddINotifyPropertyChangedInterface]
    public class StationNumberModel
    {
        public ObservableCollection<NumberGroup> numberGroups { get; set; }=new ObservableCollection<NumberGroup>() { };

    }
    [AddINotifyPropertyChangedInterface]
    public class NumberGroup
    {
        /// <summary>
        /// 序号
        /// </summary>
        public int Number { get; set; }
        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; }
    }
}
