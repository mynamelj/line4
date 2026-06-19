using MES.Manager;

namespace MES.MesModel.Request
{
    /// <summary>
    /// 20250328新增批次号上料接口
    /// </summary>
    public class CompSNChangeModel : BaseModel
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
    public static class CompSNChangeModelHelper
    {
        public static CompSNChangeModel GetCompSNChangeModel(this string sn, int number)
        {
            CompSNChangeModel model = new CompSNChangeModel()
            {
                Line = SetHelper.MesSetting.ListGroup[number].Line,
                MachineID = SetHelper.MesSetting.ListGroup[number].MachineID,
                StationID = SetHelper.MesSetting.ListGroup[number].StationID,
                FixSN = "CompSN_ChangeModel",
                Token = SetHelper.MesSetting.ListGroup[number].Token,
                OPID = "1",
                SN = sn,
                //这个EventID MES要求写死
                EventID = "CompSN_ChangeModel"
            };

            return model;
        }
    }
}
