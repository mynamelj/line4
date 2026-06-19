using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MES.Comm
{
    public enum PLCTagItem
    {
        设备运行状态_1,
        设备报警,
        设备报警_1,
        报警_1,
        PLC进站流程ID,
        载具码,
        PLC出站流程ID,
        PC进站流程ID,
        PC出站流程ID,
        进站结果,
        出站结果,
        产品SN,
        开箱1,
        开箱2,
        产品出站启动,
        产品进站启动,
        扫描材料码启动,
        设备运行状态,
        扫描材料码完成,
        操作权限,
        按钮时间,
        DEFAULT,
    }
}
