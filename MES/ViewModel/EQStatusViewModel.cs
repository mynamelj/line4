using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MES.Comm;
using MES.Manager;
using MES.MesModel.Request;
using MES.SetModel;
using static MES.Manager.ScanManager;

namespace MES.ViewModel
{
    public class EQStatusViewModel
    {

        //public EQStatusViewModel()
        //{
        //    if (SetHelper.BoxCodeInfos != null && SetHelper.BoxCodeInfos.Count > 0)
        //    {
        //        BoxCodeInfo = new ObservableCollection<OpenBoxSettingModel>(SetHelper.BoxCodeInfos);
        //    }
        //    else
        //    {
        //        for (int i = 1; i <= SetHelper.MesSetting.BoxCount; i++)
        //        {
        //            BoxCodeInfo.Add(new OpenBoxSettingModel() { Index = i, Code = "" });
        //        }
        //    }
        //    SetHelper.scanManager.OnScanOpenBox += EQStatusViewModel_OnScanOpenBox;
        //}

        //private void EQStatusViewModel_OnScanOpenBox(string ScanCode)
        //{
        //    CodeCompare(ScanCode);
        //}

        //public ObservableCollection<OpenBoxSettingModel> BoxCodeInfo { get; set; } = new ObservableCollection<OpenBoxSettingModel>();
        //public string BoxCode { get; set; }
        //public string MaterialCode { get; set; }

        //public ICommand SaveCommand => new RelayCommand(() =>
        //{
        //    string codejson = JSON.ToJsonFormat(BoxCodeInfo);
        //    File.WriteAllText(SetHelper.boxcodepath, codejson);
        //});

        ////发送PLC信号
        ////条码对比
        //public void CodeCompare(string code)
        //{
        //    var CodeInfo = BoxCodeInfo.FirstOrDefault(x => x.Code == code);
        //    if (CodeInfo != null)
        //    {
        //        string tagName = "开箱" + CodeInfo.Index;
        //        PLCTagItem pLCTag = (PLCTagItem)Enum.Parse(typeof(PLCTagItem), tagName);
        //        bool result = SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, pLCTag, true);
        //        SetHelper.ListPLCMessage.ShowInfoQueue($"PC{tagName}写true{(result ? "成功" : "失败")}");
        //        MessageBox.Show($"分钉箱{CodeInfo.Index}已开箱");
        //    }
        //    else
        //    {
        //        MessageBox.Show("未找到匹配的分钉箱");
        //    }
        //}
    }
}
