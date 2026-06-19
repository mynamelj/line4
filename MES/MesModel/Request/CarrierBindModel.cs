using MES.Manager;

namespace MES.MesModel.Request
{
    public class CarrierBindModel : BaseModel
    {
        /// <summary>
        /// 通讯事件名称 
        /// </summary>
        public string EventID { get; set; } = "SN_CarrierBind";
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
        /// <summary>
        /// UNBIND/Bind
        /// </summary>
        public string BindType { get; set; }
        /// <summary>
        /// 载具或是TrayID，用于载具绑定
        /// </summary>
        public string CarrierID { get; set; }
        /// <summary>
        /// 载具穴位
        /// </summary>
        public string ACPoint { get; set; } = "1";
    }

    public static class CarrierBindModelHelper
    {
        public static CarrierBindModel GetCarrierBindModel(this string CarrierID, int number, EnumBindType BindType, string SN = "")
        {
            CarrierBindModel model = new CarrierBindModel()
            {
                Line = SetHelper.MesSetting.ListGroup[number].Line,
                MachineID = SetHelper.MesSetting.ListGroup[number].MachineID,
                StationID = SetHelper.MesSetting.ListGroup[number].StationID,
                FixSN = SetHelper.MesSetting.ListGroup[number].FixSN,
                Token = SetHelper.MesSetting.ListGroup[number].Token,
                OPID = "",
                BindType = BindType.ToString(),
                SN = SN,
                CarrierID = CarrierID,
            };
            return model;
        }
    }

    public enum EnumBindType
    {
        UNBIND,
        Bind,
    }
}
