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

namespace MES.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class GlueMaterialOnOffViewModel
    {
        public GlueMaterialOnOffViewModel()
        {
            if (SetHelper.MesSetting.ListGroup.Count > 0)
            {

                for (int i = 0; i < SetHelper.MesSetting.ListGroup[0].BoxCount; i++)
                {
                    BoxNoList.Add((i + 1).ToString());
                }
                //for (int i = 0; i < SetHelper.MesSetting.ListGroup[0].BatchMaterialCount; i++)
                //{
                //    LocationList.Add((i + 1).ToString());
                //}
                for (int i = 0; i < SetHelper.StationNumber.numberGroups.Count; i++)
                {
                    if (SetHelper.MesSetting.ListGroup[i].IsGlueStation == "1")
                    {
                        LocationList.Add(SetHelper.StationNumber.numberGroups[i].Name);
                    }
                }
            }

            SetHelper.scanManager.OnScanOpenBox += ScanManager_OnScanOpenBox;
            GlueOnLineList = SetHelper.GlueOnLineList;
            UpdateRestTime();
        }

        /// <summary>
        /// 刷新胶水剩余时间，每分钟刷新一次
        /// </summary>
        private void UpdateRestTime()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    foreach (var item in GlueOnLineList)
                    {
                        double time = (DateTime.Now - Convert.ToDateTime(item.DateTime)).TotalSeconds;
                        item.RestDateTimeNow = Convert.ToInt32((Convert.ToDouble(item.RestDateTime) - time));

                        if (item.RestDateTimeNow < 0)
                        {
                            item.IsOverTime = "Yes";
                        }

                    }
                    Thread.Sleep(60 * 1000);
                }
            });
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
        public ObservableCollection<string> LocationList { get; set; } = new ObservableCollection<string>() { };
        /// <summary>
        /// 选中的料箱号
        /// </summary>
        public string BoxNo { get; set; } = "";
        public string LocationNo { get; set; } = "";
        public int LocationIndex { get; set; } = 0;
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
        public ObservableCollection<MaterailOnOffModel> GlueOnLineList { get; set; } = new ObservableCollection<MaterailOnOffModel>();

        /// <summary>
        /// 上料
        /// </summary>
        public ICommand MaterialOnCommand => new RelayCommand(async () =>
        {
            if (string.IsNullOrEmpty(GlueCode))
            {
                ErrorMsg = "胶水码不能为空！";
                Color = Brushes.Red;
                return;
            }

            if (string.IsNullOrEmpty(LocationNo))
            {
                ErrorMsg = "工位号不能为空！";
                Color = Brushes.Red;
                return;
            }

            if (GlueOnLineList.Any(x => x.GlueCode == GlueCode))
            {
                ErrorMsg = "请勿重复上料！";
                Color = Brushes.Red;
                return;
            }

            ErrorMsg = $"{GlueCode}:上料正在请求MES中...";
            Color = Brushes.Green;
            //var response = await SetHelper.mesManager.GlueOnLine(GlueCode.GetGlueCheckOutModel(LocationNo));

            //int iNumber= LocationList.IndexOf(LocationNo); 20250417改为由名称匹配获取
            int iNumber = SetHelper.StationNumber.numberGroups.Select(it => it.Name).ToList().IndexOf(LocationNo);
            //1代表上线，2代表下线
            var response = await SetHelper.mesManager.GlueOnOrOffLine(GlueCode.GetGlueCheckOutModel(1.ToString(), iNumber), iNumber);

            //var response = (true,"code1#123");//测试用
            if (response.Item1)
            {
                //string openMsg = "";
                //if (BoxNo != "")//如果选择了箱号那么就开箱
                //{
                //    string tagName = "开箱" + BoxNo;
                //    bool result = SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, tagName, true);
                //    openMsg = $"\r\n料箱{BoxNo}开箱{(result ? "成功" : "失败")}！";
                //}

                ErrorMsg = $"{GlueCode}:胶水上料成功！\r\nMES返回的信息:{response.Item2}";
                Color = Brushes.Green;

                var ReturnMsg = response.Item2.Split('#');
                string snCode = GlueCode;//胶水SN
                //string Batch = "";
                string dateTime = System.DateTime.Now.ToString();//扫码时间
                string stationName = LocationNo;//工位号
                string restTime = ReturnMsg[ReturnMsg.Length - 1];//最后一位//胶水剩余时间（分钟）2025/4/6Mes人员修改为秒

                //var detail = GlueCode.Split('|').ToArray();
                //if (detail.Length > 7)
                //{
                //    Mcode = detail[0].Obj2String();
                //    DateTime = detail[1].Obj2String();
                //    Batch = detail[6].Obj2String();
                //}
                GlueOnLineList.Add(new MaterailOnOffModel() { GlueCode = snCode, DateTime = dateTime, LocationNo = stationName, RestDateTime = restTime, RestDateTimeNow = Convert.ToInt32(restTime) });
                //GlueOnLineList.Add(new MaterailOnOffModel() { GlueCode = GlueCode, BoxNo = BoxNo, MaterialName = MaterialName, MaterialCode = Mcode, Batch = Batch ,DateTime = DateTime,LocationNo=LocationNo });
                SetHelper.SaveSys(GlueOnLineList, SetHelper.gluepath);
                SetHelper.GlueOnLineList = GlueOnLineList;
            }
            else
            {
                ErrorMsg = $" {GlueCode} :胶水上料失败！\r\n\r\n MES返回的信息:{response.Item2}";
                Color = Brushes.Red;
            }
            GlueCode = "";
            MaterialName = "";
            BoxNo = "";
        });

        /// <summary>
        /// 下料
        /// </summary>
        public ICommand MaterialOffLineCommand => new RelayCommand<MaterailOnOffModel>(async (s) =>
        {
            int iNumber = LocationList.IndexOf(s.LocationNo);
            //int iNumber = SetHelper.StationNumber.numberGroups.Select(it => it.Name).ToList().IndexOf(LocationNo);

            var result = MessageBox.Show($"胶水\r\n{s.GlueCode}\r\n是否确认下料??", "下料确认", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.No)
            {
                return;
            }


            if (string.IsNullOrEmpty(s.GlueCode))
            {
                ErrorMsg = $"下料失败！\r\n\r\n胶水码为空";
                Color = Brushes.Red;
                return;
            }

            ErrorMsg = $"{s.GlueCode}:下料正在请求MES中...";
            Color = Brushes.Green;
            //1=上线，2=下线
            var response = await SetHelper.mesManager.GlueOnOrOffLine(s.GlueCode.GetGlueCheckOutModel(2.ToString(), iNumber), iNumber);

            //var response = (true, "code1#123");//测试用

            //var response = await SetHelper.mesManager.GlueOffLine(s.GetGlueOffLineModel(s.LocationNo));

            if (response.Item1)
            {
                //string openMsg = "";
                //if (s.BoxNo != "")//如果有箱号那么就开箱
                //{
                //    string tagName = "开箱" + s.BoxNo;
                //    bool result = SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, tagName, true);
                //    openMsg = $"\r\n料箱{s.BoxNo}开箱{(result ? "成功" : "失败")}！";
                //}

                ErrorMsg = $"{s.GlueCode}:下料成功 ！\r\n\r\n MES返回信息: {response.Item2}";
                Color = Brushes.Green;
                GlueOnLineList.Remove(s);
                //SaveSys(GlueOnLineList);
                SetHelper.SaveSys(GlueOnLineList, SetHelper.gluepath);
                SetHelper.GlueOnLineList = GlueOnLineList;
            }
            else
            {
                ErrorMsg = $"{s.GlueCode}:下料失败！\r\n\r\n MES返回信息: {response.Item2}";
                Color = Brushes.Red;
            }
        });

        public ICommand MaterialDeleteCommand => new RelayCommand<MaterailOnOffModel>((material) =>
        {
            try
            {
                if (SetHelper.IsAdmin)
                {
                    var msgResult = MessageBox.Show($"是否强制删除:\r\n物料名称：胶水\r\n料号：{material.GlueCode}", "", MessageBoxButton.YesNo);
                    if (msgResult == MessageBoxResult.Yes)
                    {
                        GlueOnLineList.Remove(material);
                        SetHelper.SaveSys(GlueOnLineList, SetHelper.gluepath);
                        SetHelper.GlueOnLineList = GlueOnLineList;
                        //SaveSys(GlueOnLineList);
                        SetHelper.ListPLCMessage.ShowInfoQueue($"料号：{material.GlueCode} 物料名称：胶水 已强制删除");
                    }
                }
                else
                {
                    MessageBox.Show("权限不足");
                }
            }
            catch (Exception ex)
            {
                SetHelper.ListPLCMessage.ShowInfoQueue(ex.ToString(), false, "ex");
            }
        });
        //public void SaveSys(ObservableCollection<MaterailOnOffModel> GlueOnLineList)
        //{
        //    string materialjson = JSON.ToJsonFormat(GlueOnLineList);
        //    File.WriteAllText(SetHelper.gluepath, materialjson);
        //}

        //public static ObservableCollection<MaterailOnOffModel> ReadSys()
        //{
        //    lock (SetHelper.gluepath)
        //    {
        //        string json = File.ReadAllText(SetHelper.gluepath);
        //        ObservableCollection<MaterailOnOffModel> strings = JSON.FromJson<ObservableCollection<MaterailOnOffModel>>(json);
        //        return strings ?? new ObservableCollection<MaterailOnOffModel>();
        //    }
        //}

    }

    [AddINotifyPropertyChangedInterface]
    public class MaterailOnOffModel
    {
        /// <summary>
        ///胶水码 
        /// </summary>
        public string GlueCode { get; set; }
        /// <summary>
        /// 物料名称
        /// </summary>
       // public string MaterialName { get; set; }
        /// <summary>
        /// 扫码时间
        /// </summary>
        public string DateTime { get; set; }
        /// <summary>
        /// 扫码时剩余时间
        /// </summary>
        public string RestDateTime { get; set; }

        /// <summary>
        /// 实时剩余时间
        /// </summary>
        public int RestDateTimeNow { get; set; }

        /// <summary>
        /// 是否超时
        /// </summary>
        public string IsOverTime { get; set; } = "No";
        /// <summary>
        /// 零件编码
        /// </summary>
        // public string MaterialCode { get; set; }
        /// <summary>
        /// 批次
        /// </summary>
        // public string Batch { get; set; }
        /// <summary>
        /// 料箱号
        /// </summary>
        //public string BoxNo { get; set; }

        /// <summary>
        /// 工位号
        /// </summary>
        public string LocationNo { get; set; }
        /// <summary>
        /// MES返回的信息
        /// </summary>
        public string MesMsg { get; set; }
    }
}
