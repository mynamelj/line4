using MES.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MES.MesModel.Request
{
    public class CarrierCheckModel : BaseModel
    {
        /// <summary>
        /// 通讯事件名称 
        /// </summary>
        public string EventID { get; set; } = "";
        /// <summary>
        /// 验证能否调用接口
        /// </summary>
        public string Token { get; set; }
        /// <summary>
        /// 固资SN
        /// </summary>
        public string FixSN { get; set; }
        /// <summary>
        /// 载具或是TrayID，用于载具绑定
        /// </summary>
        public string CarrierID { get; set; }
    }
    public static class CarrierCheckModelHelper
    {
        public static CarrierCheckModel GetCarrierCheckModel(this string CarrierID, int number)
        {
            string api = SetHelper.ApiSetting.ListGroup[number].CarrierCheck;
            var apis = api.Split('/');

            CarrierCheckModel model = new CarrierCheckModel()
            {
                Line = SetHelper.MesSetting.ListGroup[number].Line,
                MachineID = SetHelper.MesSetting.ListGroup[number].MachineID,
                StationID = SetHelper.MesSetting.ListGroup[number].StationID,
                FixSN = SetHelper.MesSetting.ListGroup[number].FixSN,
                Token = SetHelper.MesSetting.ListGroup[number].Token,
                OPID = SetHelper.Opid[number].Id,
                CarrierID = CarrierID,
                EventID = apis[apis.Length - 1]
            };
            return model;
        }
    }
}
