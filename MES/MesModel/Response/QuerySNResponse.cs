namespace MES.MesModel.Response
{
    public class QuerySNResponse
    {
        /// <summary>
        /// 返回处理完成的EventID
        /// </summary>
        public string EventID { get; set; }
        /// <summary>
        /// 事件返回结果 PASS/FAIL
        /// </summary>
        public string Result { get; set; }
        /// <summary>
        /// 事件返回结果信息ID或是描述
        /// </summary>
        public object MSG { get; set; }
    }

}
