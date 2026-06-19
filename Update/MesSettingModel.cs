using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MES.SetModel
{
    public class MesSettingModel
    {
        /// <summary>
        /// 线别代码
        /// </summary>
        public string Line { get; set; }
        /// <summary>
        /// 大机台段别（同MES Station）
        /// </summary>
        public string StationID { get; set; }
        /// <summary>
        /// 机台编号
        /// </summary>
        public string MachineID { get; set; }
        /// <summary>
        /// 操作人员ID
        /// </summary>
        public string OPID { get; set; }
        /// <summary>
        /// 用于验证是否能调用该接口，需MES提供
        /// </summary>
        public string Token { get; set; }
        /// <summary>
        /// 固资SN
        /// </summary>
        public string FixSN { get; set; }
        /// <summary>
        /// 模号
        /// </summary>
        public string Mold { get; set; }
        /// <summary>
        /// 物料码数量
        /// </summary>
        public int MaterialCount { get; set; }
        /// <summary>
        /// 图片文件位置
        /// </summary>
        public string PictureFilePath { get; set; } = "D:\\Image\\";
        /// <summary>
        /// 图片文件位置
        /// </summary>
        public string PictureUploadedFilePath { get; set; } = "D:\\ImageUploaded\\";
        /// <summary>
        /// 映射盘地址
        /// </summary>
        public string MappingDiskPath { get; set; }

        /// <summary>
        /// 是否模拟接口
        /// </summary>
        public bool IsSimulate { get; set; } = false;
        ///// <summary>
        ///// NG图片文件位置
        ///// </summary>
        //public string NGFilePath { get; set; }
    }
}
