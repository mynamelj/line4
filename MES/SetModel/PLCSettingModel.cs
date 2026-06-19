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
    public class PLCSettingModel
    {
        public string IPAddress { get; set; }
        public int Port { get; set; }
        public int ReadDB { get; set; } = 6500;
        public int WriteDB { get; set; } = 6501;
        public ObservableCollection<PLCGroup> ListGroup { get; set; } = new ObservableCollection<PLCGroup>();
    }

    [Serializable]
    [AddINotifyPropertyChangedInterface]
    public class PLCGroup
    {
        public string GroupName { get; set; }
        public string GroupType { get; set; }
        public int RefreshTime { get; set; } = 200;
        public string GroupDesc { get; set; }
        public ObservableCollection<PLCTag> ListTag { get; set; } = new ObservableCollection<PLCTag>();
    }

    [Serializable]
    [AddINotifyPropertyChangedInterface]
    public class PLCTag
    {
        public string TagName { get; set; } = "";
        public int TagDbArea { get; set; } = 320;
        public string TagAddress { get; set; }
        public string DataType { get; set; }
        public string TagDesc { get; set; }
        public int TagValue { get; set; } = 0;
        public double UpLimit { get; set; } = 100;
        public double LowLimit { get; set; } = -100;
        public object CurrentValue { get; set; }
        public string BackColor { get; set; } = "White";
    }

    public enum PLCGroupName
    {
        /// <summary>
        /// 触发组
        /// </summary>
        TriggerGroup,//触发组

        /// <summary>
        /// 写入组
        /// </summary>
        WriteGroup,//写入组

        /// <summary>
        /// 一般读取组
        /// </summary>
        ReadGroup,//一般读取组

        /// <summary>
        /// 下线数据读取组
        /// </summary>
        CheckOutGroup,//下线数据读取组

        /// <summary>
        /// oee数据
        /// </summary>
        OeeGroup,//oee数据

        /// <summary>
        /// 设定参数上报组
        /// </summary>
        GetParaGroup, //设定参数上报组

        /// <summary>
        /// 报警组
        /// </summary>
        AlarmGroup,

        /// <summary>
        /// 配方组
        /// </summary>
        FormulaGroup,

        /// <summary>
        /// 数据组
        /// </summary>
        DataCollectionGroup,
    }
}