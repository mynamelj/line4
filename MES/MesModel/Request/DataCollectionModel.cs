using MES.Manager;

namespace MES.MesModel.Request
{
    public class DataCollectionModel : BaseModel
    {
        /// <summary>
        /// 通讯事件名称 
        /// </summary>
        public string EventID { get; set; } = "Data_Collection";
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
        /// 载具编号
        /// </summary>
        public string CarrierID { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DCData[] DC_Info { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public CompList[] CompList { get; set; }
    }

    public class DCData
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

    public static class DataCollectionModelHelper
    {
        public static DataCollectionModel GetDataCollectionModel(this string SN, DCData[] dCData, CompList[] compList, int number, string CarrierID = "")
        {
            DataCollectionModel model = new DataCollectionModel()
            {
                Line = SetHelper.MesSetting.ListGroup[number].Line,
                MachineID = SetHelper.MesSetting.ListGroup[number].MachineID,
                StationID = SetHelper.MesSetting.ListGroup[number].StationID,
                FixSN = SetHelper.MesSetting.ListGroup[number].FixSN,
                Token = SetHelper.MesSetting.ListGroup[number].Token,
                OPID = "",
                SN = SN,
                CarrierID = CarrierID,
                CompList = compList,
                DC_Info = dCData,
            };
            return model;
        }
    }


}
