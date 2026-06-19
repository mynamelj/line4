using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.RightsManagement;
using System.Text;
using System.Threading.Tasks;

namespace MES.MesModel.Response
{
    public class CodeSoftPrintResponse
    {
        /// <summary>
        /// 返回处理的状态代码 200
        /// </summary>
        public string StatusCode { get; set; }
        /// <summary>
        /// 事件返回结果 打印OK/报错
        /// </summary>
        public string ErrorMessage { get; set; }
        /// <summary>
        /// true
        /// </summary>
        public string IsSuccess { get; set; }
    }
}
