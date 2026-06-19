using MES.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MES.MesModel.Request
{
    public class OEEModel : BaseModel
    {
        /// <summary>
        /// SN_CheckOut
        /// </summary>
        public string EventID { get; set; } = "OEEDataCollection";
        /// <summary>
        /// 用于验证是否能调用该接口，需MES提供
        /// </summary>
        public string Token { get; set; }
        /// <summary>
        /// 模号
        /// </summary>
        //public string Mold { get; set; }
        /// <summary>
        /// 固资SN
        /// </summary>
        public string FixSN { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public OeeInfo[] DC_Info { get; set; }
        /// <summary>
        /// SN
        /// </summary>
        public string SN { get; set; }
        /// <summary>
        /// 载具SN
        /// </summary>
        public string CarrierID { get; set; }

        public string Result { get; set; } = "PASS";
    }
    public class OeeInfo
    {
        /// <summary>
        /// 测试项目
        /// </summary>
        public string Item { get; set; }
        /// <summary>
        /// 测试值
        /// </summary>
        public string Value { get; set; }

    }
    public static class OEEHelper
    {
        public static OEEModel GetOEEModel(this List<OeeInfo> infos, string carryID,int iNumber)
        {
            OEEModel model = new OEEModel()
            {
                Line = SetHelper.MesSetting.ListGroup[iNumber].Line,
                MachineID = SetHelper.MesSetting.ListGroup[iNumber].MachineID,
                StationID = SetHelper.MesSetting.ListGroup[iNumber].StationID,
                OPID = SetHelper.MesSetting.ListGroup[iNumber].OPID.Obj2String(),
                SN = "",
                CarrierID = carryID,
                FixSN = SetHelper.MesSetting.ListGroup[iNumber].FixSN.Obj2String(),
                Token = SetHelper.MesSetting.ListGroup[iNumber].Token.Obj2String(),
                DC_Info = infos.ToArray(),
            };
            return model;
        }
    }
}
