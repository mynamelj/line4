using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MES.MesModel.Response
{
    public class EQAlarmResponse
    {
        /// <summary>
        /// 返回处理完成的EventID
        /// </summary>
        public string EventID { get; set; }
        /// <summary>
        /// 事件返回结果
        /// </summary>
        public string Result { get; set; }
        /// <summary>
        /// AlarmID上传MES成功 PASS/FAIL
        /// </summary>
        public object Msg { get; set; }
        /// <summary>
        /// 事件返回结果信息ID或是描述
        /// </summary>
        public string AlarmID { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Need_Work { get; set; }
    }
}


