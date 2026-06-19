using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace MES.MesModel.Response
{
    class FileUploadResponse
    {
        /// <summary>
        /// 状态码 200/404/500
        /// </summary>
        public string StatusCode { get; set; }
        /// <summary>
        /// 通讯异常信息
        /// </summary>
        public object ErrorMessage { get; set; }
        /// <summary>
        /// 执行是否成功
        /// </summary>
        public bool IsSuccess { get; set; }
        /// <summary>
        /// 数据区
        /// </summary>
        public Data Data { get; set; }
    }

    public class Data
    {
        /// <summary>
        /// SN列表
        /// </summary>
        public SNList[] SNList { get; set; }
    }
    public class SNList
    {
        /// <summary>
        /// SN
        /// </summary>
        public string SN { get; set; }
        /// <summary>
        /// 文件列表
        /// </summary>
        public FileInfo[] FileInfo { get; set; }
    }

    public class FileInfo
    {
        /// <summary>
        /// 文件名
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// 处理结果
        /// </summary>
        public string Msg { get; set; }
    }


}
