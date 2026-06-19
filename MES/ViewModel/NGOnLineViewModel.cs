using MES.Manager;
using MES.MesModel.Request;
using MES.SetModel;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.Metrics;
using System.Windows.Media;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MES.Comm;
using System.IO;
using System.Xml.Linq;
using System.Diagnostics.Eventing.Reader;

namespace MES.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class NGOnLineViewModel
    {
        public ICommand SelectionChangedCommand { get; }

        public NGOnLineViewModel()
        {
            //MessageManager.Subscribe(OnMessageReceived);
            SelectionChangedCommand = new RelayCommand<int>(OnSelectionChanged);
            //for (int i = 0; i < SetHelper.MesSetting.ListGroup[0].BoxCount; i++)
            //{
            //    BoxNoList.Add((i + 1).ToString());
            //}
            //for (int i = 0; i < SetHelper.MesSetting.ListGroup[0].BatchMaterialCount; i++)
            //{
            //    LocationList.Add((i + 1).ToString());
            //}
            for (int i = 0; i < SetHelper.StationNumber.numberGroups.Count; i++)
            {
                LocationList.Add(SetHelper.StationNumber.numberGroups[i].Name);
            }

            //var materialName = new List<string>();
            //var names = SetHelper.MesSetting.ListGroup[0].BatchMaterialName.Split(',', '，');
            //foreach (var item in names)
            //{
            //    materialName.Add(item);
            //}
            //MaterialNameList = materialName;

            SetHelper.scanManager.OnScanOpenBox += ScanManager_OnScanOpenBox; ;
            //GlueOnLineList = ReadSys();
        }
        //private void OnMessageReceived(object message)
        //{
        //    var lst= message as ObservableCollection<MaterailModel>;
        //    GlueOnLineList= lst;
        //}

        private void OnSelectionChanged(int selectedItem)
        {
            var materialName= new List<string>();
            var names=   SetHelper.MesSetting.ListGroup[selectedItem].BatchMaterialName.Split(',','，');
            foreach (var item in names)
            {
                materialName.Add(item);
            }
            MaterialNameList = materialName;
        }

            /// <summary>
            /// 扫码触发事件
            /// </summary>
            /// <param name="ScanCode"></param>
        private void ScanManager_OnScanOpenBox(string ScanCode)
        {
            GlueCode = ScanCode;
            BoxNo = "";
            ErrorMsg = "";
        }

        /// <summary>
        /// 料箱号集合
        /// </summary>
        public List<string> BoxNoList { get; set; } = new List<string>() { "" };
        public List<string> LocationList { get; set; } = new List<string>() { };

        public List<string> MaterialNameList { get; set; } = new List<string>() { };
        public string MaterialNameNo { get; set; } = "";
        /// <summary>
        /// 选中的料箱号
        /// </summary>
        public string BoxNo { get; set; } = "";
        public string LocationNo { get; set; } = "";
        /// <summary>
        /// 批追码
        /// </summary>
        public string GlueCode { get; set; } = "";
        /// <summary>
        /// 
        /// </summary>
        public string MaterialName { get; set; } = "";
        /// <summary>
        /// 上下料时的信息
        /// </summary>
        public string ErrorMsg { get; set; } = "";
        /// <summary>
        /// OK绿色 NG红色
        /// </summary>
        public Brush Color { get; set; } = Brushes.Green;
        /// <summary>
        /// 已上料的批追码
        /// </summary>
        public ObservableCollection<MaterailModel> GlueOnLineList { get; set; } = new ObservableCollection<MaterailModel>();

        /// <summary>
        /// 上料(方法重写)
        /// </summary>
        public ICommand MaterialOnCommand => new RelayCommand(async () =>
        {
            if (string.IsNullOrEmpty(GlueCode))
            {
                ErrorMsg = "上线条码不能为空！";
                Color = Brushes.Red;
                return;
            }

            if (string.IsNullOrEmpty(LocationNo))
            {
                ErrorMsg = "工位号不能为空！";
                Color = Brushes.Red;
                return;
            }

            //当前工站号
            int iNumber = LocationList.IndexOf(LocationNo);

            object obj = new object();
            //取得进站载具码
            string carryID = "";

            if (SetHelper.siemens.ReadItem(PLCGroupName.ReadGroup, "进站载具码_" + (iNumber + 1), ref obj))
            {
                carryID = obj.Obj2String();
                ErrorMsg = $"读到进站载具码为{carryID}";
            }
            else
            {
                ErrorMsg = $"读进站载具码失败\r\n\r\n请重新进站";
                Color = Brushes.Red;
                return;
            }

            if (carryID == "")
            {
                ErrorMsg = $"未读进站载具码失败\r\n\r\n请联系PLC确认";
                Color = Brushes.Red;
                return;
            }

            //发送校验
            var response = await SetHelper.mesManager.MaterialOn(carryID.GetCarrierCheckModel(iNumber), iNumber);
            if (response.Item1 && response.Item2 == GlueCode)
            {
                ErrorMsg = ErrorMsg + $"\r\n\r\n当前载具绑定壳体码 : {response.Item2}\r\n\r\n与上线条码一致";
                Color = Brushes.Green;
            }
            else
            {
                ErrorMsg = ErrorMsg + $"\r\n\r\n当前MES返回信息: {response.Item2} \r\n\r\n请确认与上线条码是否一致性";
                Color = Brushes.Red;
            }

        });

        ///// <summary>
        ///// 上料
        ///// </summary>
        //public ICommand MaterialOnCommand => new RelayCommand(async () =>
        //{
        //    if (string.IsNullOrEmpty(GlueCode))
        //    {
        //        ErrorMsg = "上线条码不能为空！";
        //        Color = Brushes.Red;
        //        return;
        //    }

        //    if (string.IsNullOrEmpty(LocationNo))
        //    {
        //        ErrorMsg = "工位号不能为空！";
        //        Color = Brushes.Red;
        //        return;
        //    }

        //    //ErrorMsg = $"{GlueCode}:NG上线正在请求MES中...";
        //    Color = Brushes.Green;

        //    //var response = await SetHelper.mesManager.GlueOnLine(GlueCode.GetGlueCheckOutModel(LocationNo));

        //    int iNumber= LocationList.IndexOf(LocationNo);

        //    //var response = await SetHelper.mesManager.CarrierCheck(GlueCode.GetCarrierCheckModel(iNumber),iNumber);

        //    //NG上线直接成功，并且将码写给PLC
        //    int result;
        //    string name = MaterialNameNo;
        //    result = 1;

        //    //if (response.Item1)
        //    //{
        //    //    ErrorMsg = $"{DateTime.Now} {GlueCode}:NG上线成功！\r\n\r\nMES返回的信息: {response.Item2}";
        //    //    Color = Brushes.Green;
        //    //    string name = MaterialNameNo;
        //    //    result = 1;
        //    //}
        //    //else
        //    //{
        //    //    ErrorMsg =$"{DateTime.Now} 条码:{GlueCode}  NG上线失败！\r\n\r\nMES返回的信息 {response.Item2}";
        //    //    Color = Brushes.Red;
        //    //    result = 2;
        //    //}

        //    bool bResult = SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "扫描材料码结果_" + (iNumber+1).ToString(), result);
        //    SetHelper.ListPLCMessage.ShowInfoQueue($"{LocationNo} 扫描材料码结果写{result},{(bResult ? "成功" : "失败")}");

        //    bool bResult1 = SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "产品SN_" + (iNumber + 1).ToString(), GlueCode);
        //    SetHelper.ListPLCMessage.ShowInfoQueue($"{LocationNo} 产品SN写{GlueCode},{(bResult1 ? "成功" : "失败")}");

        //    ErrorMsg = $"{DateTime.Now} SN: {GlueCode}:NG上线成功\r\n给PLC发送结果{(bResult ? "成功" : "失败")}\r\n给PLC发送产品SN{(bResult1 ? "成功" : "失败")}";
        //    if (bResult && bResult1)
        //    {
        //        Color = Brushes.Green;
        //    }
        //    else
        //    {
        //        Color = Brushes.Red;
        //    }
        //    GlueCode = "";
        //    MaterialName = "";
        //    BoxNo = "";


        //});


        //public void SaveSys(ObservableCollection<MaterailModel> GlueOnLineList)
        //{
        //    string materialjson = JSON.ToJsonFormat(GlueOnLineList);
        //    File.WriteAllText(SetHelper.materialpath, materialjson);
        //}

        //public ObservableCollection<MaterailModel> ReadSys()
        //{
        //    lock (SetHelper.materialpath)
        //    {
        //        string json = File.ReadAllText(SetHelper.materialpath);
        //        ObservableCollection<MaterailModel> strings = JSON.FromJson<ObservableCollection<MaterailModel>>(json);
        //        return strings ?? new ObservableCollection<MaterailModel>();
        //    }
        //}

    }


}
