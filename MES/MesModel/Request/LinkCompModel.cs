using MES.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shell;

namespace MES.MesModel.Request
{
    public class LinkCompModel : BaseModel
    {
        /// <summary>
        /// Link_Comp
        /// </summary>
        public string EventID { get; set; } = "";
        /// <summary>
        /// 产出产品SN、产出TrayID
        /// </summary>
        public string SN { get; set; }
        /// <summary>
        /// 组装材料BarCode或是上料TrayID
        /// </summary>
        public CompList[] CompList { get; set; }

    }

    public class CompList
    {
        /// <summary>
        /// 材料SN，材料PN,材料TrayID等
        /// </summary>
        public string CompID { get; set; }
        /// <summary>
        /// 材料使用数量
        /// </summary>
        public int Qty { get; set; }
    }


    public static class LinkCompModelHelper
    {
        public static LinkCompModel GetLinkCompModel(this string COMPID, string SN, int iNumber)
        {
            List<CompList> compLists = new List<CompList>();
            //foreach (var item in COMPID)
            //{
            compLists.Add(new CompList()
            {
                CompID = COMPID,
                Qty = 1
            });
            //}
            string api = SetHelper.ApiSetting.ListGroup[iNumber].LinkCompApi;
            var apis = api.Split('/');
            LinkCompModel model = new LinkCompModel()
            {
                Line = SetHelper.MesSetting.ListGroup[iNumber].Line,
                MachineID = SetHelper.MesSetting.ListGroup[iNumber].MachineID,
                StationID = SetHelper.MesSetting.ListGroup[iNumber].StationID,
                OPID = SetHelper.MesSetting.ListGroup[iNumber].OPID,
                SN = SN,
                CompList = compLists.ToArray(),
                EventID = apis[apis.Length - 1]//SetHelper.ApiSetting.ListGroup[iNumber].LinkCompApi,
            };

            return model;
        }
    }
}
