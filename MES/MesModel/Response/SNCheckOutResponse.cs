using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MES.MesModel.Response
{
    public class SNCheckOutResponse
    {
        /// <summary>
        /// 返回处理完成的EventID
        /// </summary>
        public string EventID { get; set; }
        /// <summary>
        /// 事件返回结果 PASS/FAIL
        /// </summary>
        public string Result { get; set; }
        /// <summary>
        /// 事件返回结果信息ID或是描述
        /// </summary>
        public object Msg { get; set; }
        /// <summary>
        /// 给机台反馈停机命令 PASS：继续执行；STOP：停机；Alarm:报警不停机提示MES 回复MSG 内容；
        /// </summary>
        public string Need_Work { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public SN_InfoItem[] SN_Info { get; set; }
    }

    public class SN_InfoItem
    {
        /// <summary>
        /// 产出产品SN、产出TrayID
        /// </summary>
        public string SN { get; set; }
        /// <summary>
        /// 产品结果：PASS,FAIL
        /// </summary>
        public string SNResult { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Msg_ID { get; set; }
    }
}
