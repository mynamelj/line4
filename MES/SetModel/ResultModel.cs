using MES.Manager;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MES.SetModel
{
    [AddINotifyPropertyChangedInterface]
    public class ResultModel
    {
        public string Line { get; set; }
        public string StationID { get; set; }
        public string MachineID { get; set; }

        /// <summary>
        /// 进站结果
        /// </summary>
        public string Result1 { get; set; }
        /// <summary>
        /// 进站SN
        /// </summary>
        public string CheckInSN { get; set; }
        /// <summary>
        /// 材料Link结果
        /// </summary>
        public string Result2 { get; set; }
        public Visibility LinkVis { get; set; }
        public string MiddleText { get; set; }
        /// <summary>
        /// 物料SN
        /// </summary>
        public string MaterialSN { get; set; }
        /// <summary>
        /// 出站结果
        /// </summary>
        public string Result3 { get; set; }
        /// <summary>
        /// 出站SN
        /// </summary>
        public string CheckOutSN { get; set; }
    }
}
