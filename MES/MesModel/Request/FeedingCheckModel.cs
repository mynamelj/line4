using MES.Manager;
using MES.SetModel;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;

namespace MES.MesModel.Request
{
    public class FeedingCheckModel : BaseModel
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
    }
    public static class FeedingCheckHelper
    {
        public static FeedingCheckModel GetFeedingCheck(this string sn, int number)
        {
            string api = SetHelper.ApiSetting.ListGroup[number].FeedingCheck;
            var apis = api.Split('/');

            FeedingCheckModel model = new FeedingCheckModel()
            {
                Line = SetHelper.MesSetting.ListGroup[number].Line,
                MachineID = SetHelper.MesSetting.ListGroup[number].MachineID,
                StationID = SetHelper.MesSetting.ListGroup[number].StationID,
                FixSN = SetHelper.MesSetting.ListGroup[number].FixSN,
                Token = SetHelper.MesSetting.ListGroup[number].Token,
                OPID = SetHelper.Opid[number].Id,
                SN = sn,
                EventID = apis[apis.Length - 1]// SetHelper.ApiSetting.ListGroup[number].FeedingCheck,
            };

            return model;
        }
    }
}
