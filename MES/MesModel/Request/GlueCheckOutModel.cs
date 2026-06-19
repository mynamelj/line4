using MES.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MES.MesModel.Request
{
    public class GlueCheckOutModel : BaseModel
    {
        /// <summary>
        /// 通讯事件名称 
        /// </summary>
        public string EventID { get; set; } = "";
        /// <summary>
        /// 
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

    public static class GlueCheckOutModelModelHelper
    {
        public static GlueCheckOutModel GetGlueCheckOutModel(this string SN, string No, int iNumber)
        {
            string api = SetHelper.ApiSetting.ListGroup[iNumber].GlueCheckOut;
            var apis = api.Split('/');
            GlueCheckOutModel model = new GlueCheckOutModel()
            {
                Line = SetHelper.MesSetting.ListGroup[iNumber].Line,
                StationID = SetHelper.MesSetting.ListGroup[iNumber].StationID,
                MachineID = SetHelper.MesSetting.ListGroup[iNumber].MachineID,
                OPID = No,
                Token = SetHelper.MesSetting.ListGroup[iNumber].Token,
                FixSN = SetHelper.MesSetting.ListGroup[iNumber].Token,
                SN = SN,
                EventID = apis[apis.Length - 1]// SetHelper.ApiSetting.ListGroup[iNumber].GlueCheckOut,
            };
            return model;
        }
    }

    public static class GlueShortageModelModelHelper
    {
        public static GlueCheckOutModel GetGlueShortageModel(this int iNumber)
        {
            string api = SetHelper.ApiSetting.ListGroup[iNumber].GlueCheckOut;
            var apis = api.Split('/');
            GlueCheckOutModel model = new GlueCheckOutModel()
            {
                Line = SetHelper.MesSetting.ListGroup[iNumber].Line,
                StationID = SetHelper.MesSetting.ListGroup[iNumber].StationID,
                MachineID = SetHelper.MesSetting.ListGroup[iNumber].MachineID,
                OPID = "1",
                Token = SetHelper.MesSetting.ListGroup[iNumber].Token,
                FixSN = "Glue_Shortage",
                SN = "",
                EventID = "Glue_Shortage"// SetHelper.ApiSetting.ListGroup[iNumber].GlueCheckOut,
            };
            return model;
        }
    }
}
