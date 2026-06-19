using MES.Manager;
using MES.SetModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MES.MesModel.Request
{
    public class ChangeProductTypeModel : BaseModel
    {
        /// <summary>
        /// 通讯事件名称 
        /// </summary>
        public string EventID { get; set; } = "";
    }

    public static class ChangeProductTypeModelHelper
    {
        public static ChangeProductTypeModel GetChangeProductTypeModel(this int number)
        {
            ChangeProductTypeModel model = new ChangeProductTypeModel()
            {
                Line = SetHelper.MesSetting.ListGroup[number].Line,
                MachineID = SetHelper.MesSetting.ListGroup[number].MachineID,
                StationID = SetHelper.MesSetting.ListGroup[number].StationID,
                OPID = SetHelper.Opid[number].Id,
                EventID = "Order_Get",
            };
            return model;
        }
    }
}
