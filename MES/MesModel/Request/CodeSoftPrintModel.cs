using MES.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MES.MesModel.Request
{
    public class CodeSoftPrintModel
    {
        /// <summary>
        /// 线别代码
        /// </summary>
        public string Line { get; set; }
        /// <summary>
        /// 机台站别
        /// </summary>
        public string Station { get; set; }
        /// <summary>
        /// 机台编号
        /// </summary>
        public string MachineID { get; set; }
        /// <summary>
        /// 扫码编号
        /// </summary>
        public string SN { get; set; }
    }

    public static class CodeSoftPrintModelHelper
    {
        public static CodeSoftPrintModel GetCodeSoftPrintModel(this string SN,int number)
        {
            CodeSoftPrintModel model = new CodeSoftPrintModel()
            {
                Line = SetHelper.MesSetting.ListGroup[number].Line,
                Station = SetHelper.MesSetting.ListGroup[number].StationID,
                MachineID= SetHelper.MesSetting.ListGroup[number].MachineID,
                SN= SN,
            };
            return model;
        }
    }
}
