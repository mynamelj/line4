using MES.Manager;
using MES.ViewModel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace MES.MesModel.Request
{

    public class EQAlarmModel : BaseModel
    {
        /// <summary>
        /// Alarm
        /// </summary>
        public string EventID { get; set; } = "Alarm";

        /// <summary>
        /// 报警ID
        /// </summary>
        public string AlarmID { get; set; }
        /// <summary>
        /// 机台重置恢复的时间
        /// </summary>
        public string ResetTime { get; set; }
    }

    public static class AlarmModelHelper
    {
        public static EQAlarmModel GetAlarmModel(this string alarmID, int number)
        {
            EQAlarmModel model = new EQAlarmModel()
            {
                Line = SetHelper.MesSetting.ListGroup[number].Line,
                MachineID = SetHelper.MesSetting.ListGroup[number].MachineID,
                StationID = SetHelper.MesSetting.ListGroup[number].StationID,
                OPID = SetHelper.Opid[number].Id,
                ResetTime = DateTime.Now.ToString(),
                AlarmID = alarmID.ToString(),
            };
            return model;
        }
    }

    //public class EQAlarmModel : BaseModel
    //{
    //    /// <summary>
    //    /// Alarm
    //    /// </summary>
    //    public string EventID { get; set; } = "";
    //    /// <summary>
    //    /// 设备状态
    //    /// </summary>
    //    public string Status { get; set; }
    //    public List<AlarmIDList> AlarmIDList { get; set; }
    //}

    //public class AlarmIDList
    //{
    //    public string SendTime { get; set; }
    //    public string ResetTime { get; set; }
    //    public string AlarmID { get; set; }
    //}

    //public static class AlarmModelHelper
    //{
    //    public static EQAlarmModel GetAlarmModel(this int status, int number, List<AlarmIDList> alarmIDLists)
    //    {
    //        string api = SetHelper.ApiSetting.ListGroup[number].AlarmApi;
    //        var apis = api.Split('/');

    //        EQAlarmModel model = new EQAlarmModel()
    //        {
    //            Line = SetHelper.MesSetting.ListGroup[number].Line,
    //            MachineID = SetHelper.MesSetting.ListGroup[number].MachineID,
    //            StationID = SetHelper.MesSetting.ListGroup[number].StationID,
    //            OPID = SetHelper.Opid[number].Id,
    //            Status = status.ToString(),
    //            AlarmIDList = alarmIDLists,
    //            EventID = apis[apis.Length - 1],
    //        };
    //        return model;
    //    }
    //}
}
