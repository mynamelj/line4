using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MES.MesModel.Response
{
    public class SNCheckINResponse
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
        /// 事件返回结果信息ID或是描述
        /// </summary>
        public object MSG { get; set; }
        /// <summary>
        /// 产品ID
        /// </summary>
        public string SN { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string CarrierID { get; set; }
    }
}
