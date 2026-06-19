using MES.Manager;
using MES.ViewModel;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MES.MesModel.Request
{
    public class GlueOffLineModel : BaseModel
    {
        /// <summary>
        /// 通讯事件名称 
        /// </summary>
        public string EventID { get; set; } = "CompSN_OffLine";
        /// <summary>
        /// 
        /// </summary>
        public string Token { get; set; }
        /// <summary>
        /// 留空
        /// </summary>
        public string OffType { get; set; }
        /// <summary>
        /// 固资SN
        /// </summary>
        public string FixSN { get; set; }
        /// <summary>
        /// 扫码编号
        /// </summary>
        public string SN { get; set; }
        /// <summary>
        /// 语言留空
        /// </summary>
        public string Language { get; set; }
    }

    public static class GlueOffLineModelModelHelper
    {
        public static GlueOffLineModel GetGlueOffLineModel(this MaterailOnOffModel material, string opID)
        {
            GlueOffLineModel model = new GlueOffLineModel()
            {
                SN = material.GlueCode,
                OffType = "",
                Line = SetHelper.MesSetting.ListGroup[0].Line,
                StationID = SetHelper.MesSetting.ListGroup[0].StationID,
                MachineID = SetHelper.MesSetting.ListGroup[0].MachineID,
                OPID = opID == "" ? "1" : opID,
                Token = SetHelper.MesSetting.ListGroup[0].Token,
                FixSN = "CompSN_OffLine",
            };
            return model;
        }
    }
}
