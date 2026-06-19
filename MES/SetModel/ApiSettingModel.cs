using PropertyChanged;
using System.Collections.ObjectModel;

namespace MES.SetModel
{
    [Serializable]
    [AddINotifyPropertyChangedInterface]
    public class ApiSettingModel
    {
        public ObservableCollection<ApiSetting> ListGroup { get; set; } = new ObservableCollection<ApiSetting>();
    }
    [Serializable]
    [AddINotifyPropertyChangedInterface]
    public class ApiSetting
    {
        public ApiSetting Clone()
        {
            return (ApiSetting)this.MemberwiseClone();
        }
        /// <summary>
        /// 工站号，用于配置多个工站
        /// </summary>
        public int StationNumber { get; set; } = 0;
        /// <summary>
        /// 基础地址
        /// </summary>
        public string BaseUrl { get; set; } = "";
        /// <summary>
        /// 
        /// </summary>
        public string ChangeProductTypeApi { get; set; } = "";
        /// <summary>
        /// 
        /// </summary>
        public string FileUploadApi { get; set; } = "";
        /// <summary>
        /// 
        /// </summary>
        public string CheckINApi { get; set; } = "";
        /// <summary>
        /// 
        /// </summary>
        public string QuerySNApi { get; set; } = "StandAlone/QuerySN";
        /// <summary>
        /// 
        /// </summary>
        public string LinkCompApi { get; set; } = "";
        /// <summary>
        /// 
        /// </summary>
        public string DataCollectionApi { get; set; } = "StandAlone/Data_Collection";
        /// <summary>
        /// 
        /// </summary>
        public string CheckOutApi { get; set; } = "";
        /// <summary>
        /// 机台报警
        /// </summary>
        public string AlarmApi { get; set; } = "";
        /// <summary>
        /// 
        /// </summary>
        public string OEEApi { get; set; } = "";
        /// <summary>
        /// 机台状态
        /// </summary>
        public string StatusApi { get; set; } = "";
        /// <summary>
        /// 上料
        /// </summary>
        public string GlueCheckOut { get; set; } = "";
        /// <summary>
        /// 下料
        /// </summary>
        public string GlueOffLine { get; set; } = "";
        /// <summary>
        /// 检查材料合法性
        /// </summary>
        public string FeedingCheck { get; set; } = "";
        /// <summary>
        /// 核对载具或托盘
        /// </summary>
        public string CarrierCheck { get; set; } = "";
        /// <summary>
        /// 载具绑定/解绑
        /// </summary>
        public string CarrierBind { get; set; } = "StandAlone/SN_CarrierBind";
        /// <summary>
        /// 发送打印信号给打印机
        /// </summary>
        public string CodeSoftPrint { get; set; } = "";
        /// <summary>
        /// 设定参数上报
        /// </summary>
        public string GetPara { get; set; } = "";
        /// <summary>
        /// 物料码新上料
        /// </summary>
        public string CompSNCheckout { get; set; } = "";
        /// <summary>
        /// 物料码更换
        /// </summary>
        public string CompSNChange { get; set; } = "Glue/Glue_CheckOut";
        /// <summary>
        /// 物料码新下料
        /// </summary>
        public string CompSNOffline { get; set; } = "";
    }

}
