using MES.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MES.MesModel.Request
{
    public class EQStatusModel : BaseModel
    {
        /// <summary>
        /// Status
        /// </summary>
        public string EventID { get; set; } = "";
        /// <summary>
        /// 用于验证是否能调用该接口，需MES提供
        /// </summary>
        public string Token { get; set; }
        /// <summary>
        /// 固资SN
        /// </summary>
        public string FixSN { get; set; }
        /// <summary>
        /// 设备状态
        /// 1. Run  运行 2. Stop 停机 3. Maintain 保养 4. Idle 等待(物料或者载具) 5. Standby 休息/吃饭
        /// </summary>
        public string STATUS { get; set; }
    }

    public static class StatusModelHelper
    {
        public static EQStatusModel GetStatusModel(this string status, int number)
        {
            string api = SetHelper.ApiSetting.ListGroup[number].StatusApi;
            var apis = api.Split('/');

            EQStatusModel model = new EQStatusModel()
            {
                Line = SetHelper.MesSetting.ListGroup[number].Line,
                MachineID = SetHelper.MesSetting.ListGroup[number].MachineID,
                StationID = SetHelper.MesSetting.ListGroup[number].StationID,
                OPID = SetHelper.MesSetting.ListGroup[number].OPID,
                FixSN = SetHelper.MesSetting.ListGroup[number].FixSN,
                Token = SetHelper.MesSetting.ListGroup[number].Token,
                STATUS = status,
                EventID = apis[apis.Length - 1]// SetHelper.ApiSetting.ListGroup[number].StatusApi,
            };
            return model;
        }
    }
}
