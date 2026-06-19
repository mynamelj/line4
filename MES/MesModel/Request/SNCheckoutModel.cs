using MES.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MES.MesModel.Request
{
    public class SNCheckoutModel : BaseModel
    {
        /// <summary>
        /// SN_CheckOut
        /// </summary>
        public string EventID { get; set; } = "";
        /// <summary>
        /// 用于验证是否能调用该接口，需MES提供
        /// </summary>
        public string Token { get; set; }
        /// <summary>
        /// 模号
        /// </summary>
        public string Mold { get; set; }
        /// <summary>
        /// 固资SN
        /// </summary>
        public string FixSN { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public SNInfo[] SNInfo { get; set; }
        /// <summary>
        /// 载具SN
        /// </summary>
        public string CarrierID { get; set; }
        /// <summary>
        /// 综合使用数量
        /// </summary>
        public string Qty { get; set; } = "";

    }

    public class SNInfo
    {
        /// <summary>
        /// 产出产品SN、产出TrayID
        /// </summary>
        public string SN { get; set; }
        /// <summary>
        /// PASS/FAIL
        /// </summary>
        public string Result { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DC_Info[] DC_Info { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public CompList[] CompList { get; set; }
    }

    public class DC_Info
    {
        /// <summary>
        /// 测试项目
        /// </summary>
        public string Item { get; set; }
        /// <summary>
        /// 测试值
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// PASS/FAIL
        /// </summary>
        public string Result { get; set; }
    }

    public static class SNCheckOutHelper
    {
        public static SNCheckoutModel GetSNCheckoutModel(this List<DC_Info> infos, List<CompList> compLists, string SN, string carryID, int number, int outchannel)
        {

            //List<CompList> compLists = new List<CompList>();
            //compLists.Add(new CompList() { CompID = "", Qty = 0 });

            var result = infos.FirstOrDefault(x => x.Result.ToUpper() == "FAIL");

            List<SNInfo> sNInfos = new List<SNInfo>()
            {
                new SNInfo()
                {
                    CompList = compLists.ToArray(),
                    DC_Info = infos.ToArray(),
                    Result = result == null ? "PASS" : "FAIL",
                    SN = SN
                }
            };
            string api = SetHelper.ApiSetting.ListGroup[number].CheckOutApi;
            var apis = api.Split('/');

            SNCheckoutModel model = new SNCheckoutModel()
            {
                Line = SetHelper.MesSetting.ListGroup[number].Line,
                MachineID = outchannel == 0 ? SetHelper.MesSetting.ListGroup[number].MachineID : SetHelper.MesSetting.ListGroup[number].MachineID + "#" + outchannel.ToString(),
                StationID = SetHelper.MesSetting.ListGroup[number].StationID,
                OPID = SetHelper.Opid[number].Id,
                CarrierID = carryID,
                FixSN = SetHelper.MesSetting.ListGroup[number].FixSN.Obj2String(),
                Mold = SetHelper.MesSetting.ListGroup[number].Mold.Obj2String(),
                Token = SetHelper.MesSetting.ListGroup[number].Token.Obj2String(),
                SNInfo = sNInfos.ToArray(),
                Qty = "",
                EventID = apis[apis.Length - 1]//SetHelper.ApiSetting.ListGroup[number].CheckOutApi,
            };
            return model;
        }
    }
}
