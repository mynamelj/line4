using MES.Manager;
using MES.SetModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MES.MesModel.Request
{
    public class SNCheckINModel : BaseModel
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
        /// 扫码编号
        /// </summary>
        public string SN { get; set; }
        /// <summary>
        /// 载具或是TrayID，用于载具绑定
        /// </summary>
        public string CarrierID { get; set; }
    }

    public static class CheckInModelHelper
    {
        public static SNCheckINModel GetCheckInModel(this string CarrierID, int number,string snCode,bool isDownLine)
        {
            string api = SetHelper.ApiSetting.ListGroup[number].CheckINApi;
            var apis= api.Split('/');

            SNCheckINModel model = new SNCheckINModel()
            {
                Line = SetHelper.MesSetting.ListGroup[number].Line,
                MachineID = SetHelper.MesSetting.ListGroup[number].MachineID,
                StationID = SetHelper.MesSetting.ListGroup[number].StationID,
                FixSN = SetHelper.MesSetting.ListGroup[number].FixSN,
                Token = SetHelper.MesSetting.ListGroup[number].Token,
                OPID = SetHelper.Opid[number].Id,
                SN = isDownLine==true?CarrierID:snCode,//下线将载具码放到SN栏位
                CarrierID = isDownLine==true?"":CarrierID,//下线载具码栏位为空
                EventID = apis[apis.Length-1]
            };
            return model;
        }
    }
}
