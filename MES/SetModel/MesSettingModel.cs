using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MES.SetModel
{
    [Serializable]
    [AddINotifyPropertyChangedInterface]
    public class MesSettingModel
    {
        public ObservableCollection<MesSetting> ListGroup { get; set; } = new ObservableCollection<MesSetting>();
    }
    [Serializable]
    [AddINotifyPropertyChangedInterface]
    public class MesSetting
    {
        /// <summary>
        /// 工站号，用于配置多个工站
        /// </summary>
        public int StationNumber { get; set; }
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
        public string OPID { get; set; } = "";
        /// <summary>
        /// 用于验证是否能调用该接口，需MES提供
        /// </summary>
        public string Token { get; set; } = "";
        /// <summary>
        /// 固资SN
        /// </summary>
        public string FixSN { get; set; } = "";
        /// <summary>
        /// 模号
        /// </summary>
        public string Mold { get; set; } = "";
        /// <summary>
        /// 物料码数量
        /// </summary>
        public int MaterialCount { get; set; }
        /// <summary>
        /// 分钉箱数量
        /// </summary>
        public int BoxCount { get; set; } = 0;
        /// <summary>
        /// 批追料数量
        /// </summary>
        public int BatchMaterialCount { get; set; } = 0;

        /// <summary>
        /// 批追料名称
        /// </summary>
        public string BatchMaterialName { get; set; } = "";

        /// <summary>
        /// 本地图片存储时间(天)
        /// </summary>
        public int PictureSaveTime { get; set; } = 0;
        public string RegexRule { get; set; } = "";
        
        /// <summary>
        /// 图片文件位置
        /// </summary>
        public string PictureFilePath { get; set; } = "";
        /// <summary>
        /// 已上传图片文件位置
        /// </summary>
        public string PictureUploadedFilePath { get; set; } = "";
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
        /// <summary>
        /// 状态上传间隔时间（s）
        /// </summary>
        public int StatusUploadTime { get; set; }
        /// <summary>
        /// 报警上传间隔时间（s）
        /// </summary>
        public int AlarmUploadTime { get; set; }
        ///// <summary>
        ///// 设定参数上传间隔时间（min）
        ///// </summary>
        //public int SetParaUploadTime { get; set; }

        /// <summary>
        /// 产品码长度，用于OP1010等工位扫码自动进站判断是否为产品码
        /// </summary>
        public int SNCodeLen { get; set; }
        /// <summary>
        /// 扫码FeedingCheck码长度，用于OP3030等工位扫码FeedingCheck判断是否为需要的码
        /// </summary>
        public int FeedingSNCodeLen { get; set; }
        /// <summary>
        /// 扫码FeedingCheck时的条码规则
        /// </summary>
        public string CodeRule { get; set; } = "";
        /// <summary>
        /// 精追料扫码数量
        /// </summary>
        public int ScanMaterialCount { get; set; }
        /// <summary>
        /// 是否为NG上下线工位
        /// </summary>
        public string IsNgCheckInStation { get; set; } = "0";
        /// <summary>
        /// 是否为选垫工位
        /// </summary>
        public string IsSelectStation { get; set; } = "0";

        /// <summary>
        /// 相机序号，一台工控机存在多个上传图片工站时使用
        /// </summary>
        public string CCDNumber { get; set; } = "0";
        /// <summary>
        /// 是否直接给PLC条码信息及扫码完成信号
        /// </summary>
        public string IsGivePlcCode { get; set; } = "0";
        /// <summary>
        /// 是否为胶水工位（进站检查胶水有无上料）
        /// </summary>
        public string IsGlueStation { get; set; } = "0";
        /// <summary>
        /// 是否为手动工位
        /// </summary>
        public string IsMauaStation { get; set; } = "0";
        /// <summary>
        /// 是否为核对载具码工位(转线工位核对空托盘)
        /// </summary>
        public string IsCarryCheckStation { get; set; } = "0";

    }


}
