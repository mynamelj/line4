using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MES.MesModel.Request
{
    public class FileUploadModel : BaseModel
    {
        /// <summary>
        /// 
        /// </summary>
        public string EventID { get; set; } = "SN_FileUpload";
        /// <summary>
        /// SN信息列表
        /// </summary>
        public SNList[] SNList { get; set; }
  
    }

    public class SNList
    {
        /// <summary>
        /// 产品码
        /// </summary>
        public string SN { get; set; }
        /// <summary>
        /// 文件信息列表
        /// </summary>
        public FileInfos[] FileInfo { get; set; }

    }

    public class FileInfos
    {
        /// <summary>
        /// 文件名 yyyyMMdd_StationID_Result_MMdd_HHmmss_MachineID_穴位_Original/Detection_SN_该SN的第几张图片
        /// </summary>
        public string FileName { get; set; }
        /// <summary>
        /// 文件存放路径 W:\Line前两位\yyyyMM\dd\Line后两位\StationID\MachineID\(OK或NG)
        /// </summary>
        public string FilePath { get; set; }
    }
}
