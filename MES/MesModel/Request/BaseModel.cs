using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MES.MesModel.Request
{
    public class BaseModel
    {
        /// <summary>
        /// 线别代码
        /// </summary>
        public string Line { get; set; }
        /// <summary>
        /// 大机台段别（同MES Station）
        /// </summary>
        public string StationID { get; set; }
        /// <summary>
        /// 机台编号
        /// </summary>
        public string MachineID { get; set; }
        /// <summary>
        /// 操作人员ID
        /// </summary>
        public string OPID { get; set; }
        /// <summary>
        /// 机台触发事件的时间
        /// </summary>
        public string SendTime { get; set; } = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
    }
}
