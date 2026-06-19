using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MES.Comm
{
    public enum EQRunStatus
    {
        /// <summary>
        /// 运行
        /// </summary>
        Run = 1,
        /// <summary>
        /// 停机
        /// </summary>
        Stop = 2,
        /// <summary>
        /// 保养
        /// </summary>
        Maintain = 3,
        /// <summary>
        /// 等待
        /// </summary>
        Idle=4,
        /// <summary>
        /// 休息吃饭
        /// </summary>
        Standby=5,
    }
}
