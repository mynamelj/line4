using MES.Manager;

namespace MES.MesModel.Request
{
    public class QuerySNModel : BaseModel
    {
        /// <summary>
        /// 通讯事件名称 
        /// </summary>
        public string EventID { get; set; } = "Query_Weight";
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

    public static class QuerySNModelModelHelper
    {
        public static QuerySNModel GetQuerySNModel(this string SN, int number)
        {
            QuerySNModel model = new QuerySNModel()
            {
                Line = SetHelper.MesSetting.ListGroup[number].Line,
                MachineID = SetHelper.MesSetting.ListGroup[number].MachineID,
                StationID = SetHelper.MesSetting.ListGroup[number].StationID,
                FixSN = SetHelper.MesSetting.ListGroup[number].FixSN,
                Token = SetHelper.MesSetting.ListGroup[number].Token,
                OPID = "",
                SN = SN,
            };
            return model;
        }
    }

    
}
