namespace MES.MesModel.Response
{
    public class CarrierCheckResponse
    {
        /// <summary>
        /// 返回处理完成的EventID
        /// </summary>
        public string EventID { get; set; }
        /// <summary>
        /// 事件返回结果
        /// </summary>
        public string Result { get; set; }
        /// <summary>
        /// 事件返回结果信息ID或是描述
        /// </summary>
        public object MSG { get; set; }
        /// <summary>
        /// CarrierID
        /// </summary>
        public string CarrierID { get; set; }

        public DCInfo[] DC_Info { get; set; }
    }

    public class DCInfo
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
}
