using MES.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace MES.MesModel.Request
{
    public class GetParaModel : BaseModel
    {
        /// <summary>
        /// 通讯事件名称 
        /// </summary>
        public string EventID { get; set; } = "";
        /// <summary>
        /// 验证能否调用接口
        /// </summary>
        public string Token { get; set; }
        /// <summary>
        /// 固资SN
        /// </summary>
        public string FixSN { get; set; }

        public Para_Info[] Para_Info { get; set; }
    }
    public class Para_Info
    {
        /// <summary>
        /// 参数版本
        /// </summary>
        public string ParaREV { get; set; }

        public ParaList[] ParaList { get; set; }
    }
    public class ParaList
    {
        /// <summary>
        /// 参数项目
        /// </summary>
        public string ParaItem { get; set; }
        /// <summary>
        /// 值，没有放空
        /// </summary>
        public string Value { get; set; }
        /// <summary>
        /// 上限，没有放空
        /// </summary>
        public string Up { get; set; }
        /// <summary>
        /// 下限，没有放空
        /// </summary>
        public string Down { get; set; }
    }

    public static class GetParaHelper
    {
        public static GetParaModel GetGetParaModel(this List<Para_Info> para_Infos, int number)
        {
            string api = SetHelper.ApiSetting.ListGroup[number].GetPara;
            var apis = api.Split('/');

            GetParaModel model = new GetParaModel()
            {
                Line = SetHelper.MesSetting.ListGroup[number].Line,
                MachineID = SetHelper.MesSetting.ListGroup[number].MachineID,
                StationID = SetHelper.MesSetting.ListGroup[number].StationID,
                OPID = SetHelper.Opid[number].Id,
                FixSN = SetHelper.MesSetting.ListGroup[number].FixSN.Obj2String(),
                Token = SetHelper.MesSetting.ListGroup[number].Token.Obj2String(),
                Para_Info = para_Infos.ToArray(),
                EventID = apis[apis.Length - 1]//SetHelper.ApiSetting.ListGroup[number].GetPara
            };
            return model;
        }
    }
}
