using MES.Manager;
using MES.SetModel;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;

namespace MES.MesModel.Request
{
    /// <summary>
    /// 20250328新增批次号下料接口
    /// </summary>
    public class CompSNOfflineModel : BaseModel
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
        /// 
        /// </summary>
        public string Offtype { get; set; } = "";
        /// <summary>
        /// 扫码编号
        /// </summary>
        public string SN { get; set; }
    }
    public static class CompSNOfflineHelper
    {
        public static CompSNOfflineModel GetCompSNOffline(this string sn, int number, int lightNumber)
        {
            CompSNOfflineModel model = new CompSNOfflineModel()
            {
                Line = SetHelper.MesSetting.ListGroup[number].Line,
                MachineID = SetHelper.MesSetting.ListGroup[number].MachineID,
                StationID = SetHelper.MesSetting.ListGroup[number].StationID,
                FixSN = "CompSN_Offline",// SetHelper.ApiSetting.ListGroup[number].CompSNOffline,
                Token = SetHelper.MesSetting.ListGroup[number].Token,
                OPID = lightNumber < 1 ? 1.ToString() : lightNumber.ToString(), //SetHelper.Opid[number].Id,
                SN = sn,
                EventID = "CompSN_Offline"// SetHelper.ApiSetting.ListGroup[number].CompSNOffline,
            };

            return model;
        }
    }
}
