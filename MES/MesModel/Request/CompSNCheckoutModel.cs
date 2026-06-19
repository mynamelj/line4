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
    /// <summary>
    /// 20250328新增批次号上料接口
    /// </summary>
    public class CompSNCheckoutModel : BaseModel
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
    public static class CompSNCheckoutHelper
    {
        public static CompSNCheckoutModel GetCompSNCheckout(this string sn, int number,int lightNumber)
        {
            CompSNCheckoutModel model = new CompSNCheckoutModel()
            {
                Line = SetHelper.MesSetting.ListGroup[number].Line,
                MachineID = SetHelper.MesSetting.ListGroup[number].MachineID,
                StationID = SetHelper.MesSetting.ListGroup[number].StationID,
                FixSN = "CompSN_Checkout",//SetHelper.ApiSetting.ListGroup[number].CompSNCheckout,
                Token = SetHelper.MesSetting.ListGroup[number].Token,
                OPID = lightNumber.ToString(), //选垫工位灯号需要发送，其余默认发1 //SetHelper.Opid[number].Id,
                SN = sn,
                //这个EventID MES要求写死
                EventID = "CompSN_Checkout"
            };

            return model;
        }
    }
}
