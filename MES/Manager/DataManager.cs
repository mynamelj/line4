using MES.Comm;
using MES.MesModel.Request;
using MES.MesModel.Response;
using MES.SetModel;
using MES.View;
using MES.ViewModel;
using Newtonsoft.Json.Linq;
using S7.Net.Types;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media.Media3D;
using static MES.Extension;
using DateTime = System.DateTime;


namespace MES.Manager
{

    public delegate void RecItemDelegate(MesInfo data);

    public delegate void ChangeParamsDelegate(ProductTypeModel product);

    public delegate void MaterialOffline(string code);



    public partial class DataManager
    {
        public ChangeParamsDelegate ChangeParamsAction;
        public MaterialOffline MaterialOfflineAction;
        public ListenPLC listenPLC = new ListenPLC();
        public static bool RepairFlag = false;//维修标志位
        public static int specialRepairFlag = 0;//维修标志位
        public static int RepairStation = 0;//维修站志位
        private List<string> repairDataList=new List<string>();
        public DataManager()
        {
            SetHelper.siemens.OnDataChange += Siemens_OnDataChange;
            GetStatusInfo();
            PictureUploadAsync();
            string baseDirectory=AppDomain.CurrentDomain.BaseDirectory;
            string filePath = Path.Combine(baseDirectory, "misc.json");
            if (File.Exists(filePath))
            {
                try
                {
                    string jsonStr = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                    JObject jo = JObject.Parse(jsonStr);
                    repairDataList = jo["返修读取数据"]?.ToObject<List<string>>() ?? repairDataList;
                }
                catch (Exception ex)
                {
                    SetHelper.ListOEEMessage.ShowInfoQueue(ex.Message);
                }
            }


        }

        private bool isFirstStart = true;

        public event RecItemDelegate RecItemEvent;

        public bool InitialzePLC()
        {
            bool plcResult = SetHelper.siemens.InitailzePLC();
            status = new int[SetHelper.StationNumber.numberGroups.Count];
            ScanSuccess = new bool[SetHelper.StationNumber.numberGroups.Count];
            //UploadStatusStart();
            //UploadAlarms();
            DeleteLocalPicture();
            DeleteLocalLog();
            DeleteLocalMesLog();
            if (isFirstStart)
            {
                GetGlueShortage();
                isFirstStart = false;
            }
            int count = SetHelper.StationNumber.numberGroups.Count;
            LinkCompRunning = new bool[count];
            LinkCompLocks = Enumerable.Range(0, count).Select(_ => new object()).ToArray();
            return plcResult;
        }

        /// <summary>
        /// 设备运行状态
        /// </summary>
        private int[] status;
        private CancellationTokenSource timeoutStatus = new CancellationTokenSource();
        private CancellationTokenSource HeatingfinishedSource = new CancellationTokenSource();
        private CancellationTokenSource HeatingcheckSource = new CancellationTokenSource();
        private CancellationTokenSource HeatingfinishedSource2 = new CancellationTokenSource();
        private CancellationTokenSource HeatingcheckSource2 = new CancellationTokenSource();
        private int flag = 0;
        public async void Siemens_OnDataChange(string TagName, int Address, int Bit, object TagValue)
        {
            if (!TagName.Contains("心跳") && !TagName.Contains("设备运行状态") && Address > 0)
            {
                SetHelper.ListPLCMessage.ShowInfoQueue($"{TagName}-{Address}.{Bit}-{TagValue}");
            }
            if (Address < 0)
            {
                SetHelper.ListPLCMessage.ShowInfoQueue($"手动触发 {TagName} {TagValue}");//模拟触发
            }
            if (Address == 0)
            {
                //1010工位没有前置工站，所以扫码触发PLC
                SetHelper.ListPLCMessage.ShowInfoQueue($"扫码触发 {TagName} {TagValue}");//目前OP1010使用
            }
            PLCTagItem item = TagName.String2PLCTagItem();
            //获取标签末尾的数字
            Match match = Regex.Match(TagName, @"(\d+)$");
            //默认是1
            string Number = "1";
            if (match.Success)
            {
                Number = match.Value;
            }

            Thread.Sleep(100);
            string ItemName = "";
            if (TagName.LastIndexOf('_') > 0)
            {
                ItemName = TagName.Remove(TagName.LastIndexOf('_'));
            }
            else
            {
                ItemName = TagName;
            }



            switch (ItemName)
            {
                case "心跳":
                    // 统一的只执行一次的初始化入口
                    //  持续跳动的心跳反馈 
                    if (Number == "1")
                    {
                        int iNumber = Convert.ToInt32(Number) - 1;
                        string stationName = SetHelper.StationNumber.numberGroups[iNumber].Name;
                        //心跳一个工控机只留一个
                        SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "心跳_" + Number, (bool)TagValue);

                        if ((bool)TagValue)
                        {
                            MessageViewModel.PlcHeartStatic = "★☆★☆★☆";
                        }
                        else
                        {
                            MessageViewModel.PlcHeartStatic = "☆★☆★☆★";
                        }
                    }
                    break;

                case "设备运行状态":
                    _ = StatusUpLoad(Number, Convert.ToInt32(TagValue));
                    break;

                case "上传报警启动":
                    if ((int)TagValue == 1)
                    {
                        AlarmUpload(Number);
                    }
                    break;

                case "产品型号获取":
                    if ((bool)TagValue)
                    {
                        await GetProductFromMES(Number.Obj2Int());
                    }
                    break;

                case "产品型号切换":
                    if (TagValue.Obj2Int() != 0)
                    {
                        ChangeProductType(TagValue.Obj2Int(), Number.Obj2Int());
                    }
                    break;

                case "获取重量":
                    GetWeight(Number);
                    break;

                case "产品进站启动":

                    if ((bool)TagValue)
                    {
                        //OP1010工位PLC给进站信号触发弹窗提示扫码
                        int iNumber = Convert.ToInt32(Number) - 1;
                        string stationName = SetHelper.StationNumber.numberGroups[iNumber].Name;
                        //4130不再扫码
                        if ((stationName.ToUpper().Contains("OP1010")|| stationName.ToUpper().Contains("OP3040")||
                            (stationName.ToUpper().Contains("OP4030")||stationName.ToUpper().Contains("OP2020")|| 
                            stationName.ToUpper().Contains("OP2030") || stationName.ToUpper().Contains("1NG_IO")) && Address != 0)) //0为扫码触发
                        {
                            await System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                            {
                                if (IsOP1010ViewOpen)
                                {
                                    op1010View.Close();
                                }
                                SetHelper.NowScanStaion = stationName;
                                op1010View = new OP1010View("等待扫描SN码进站", $"{stationName}工位请扫码进站");
                                op1010View.Show();
                                IsOP1010ViewOpen = true;
                            });
                        }
                        else //非1010工位正常进站 &&  OP1010扫完码后用这个进站
                        {
                            ProductCheckIn(Number);
                        }
                    }
                    else
                    {
                        int iNumber = Convert.ToInt32(Number) - 1;
                        if (SetHelper.StationNumber.numberGroups.Count() > iNumber)
                        {
                            string stationName = SetHelper.StationNumber.numberGroups[iNumber].Name;
                            if (stationName.ToUpper().Contains("OP1010") 
                                || stationName.ToUpper().Contains("OP4030") || stationName.ToUpper().Contains("OP2020") 
                                || stationName.ToUpper().Contains("OP2030") || stationName.ToUpper().Contains("NG_IO"))
                            {
                                
                                await Application.Current.Dispatcher.BeginInvoke(() =>
                                {
                                    SetHelper.NowScanStaion = "";
                                    if (IsOP1010ViewOpen)
                                    {
                                        
                                        op1010View.Close();
                                        IsOP1010ViewOpen = false;
                                    }
                                });
                            }
                        }
                    }
                    break;

                case "产品出站启动":
                    if ((bool)TagValue)
                    {
                        await ProductCheckOutAsync(Number);
                    }
                    break;
                case "检查材料合法性启动":
                    if ((bool)TagValue)
                    {
                        string materialCode = "";
                        object obj = new object();
                        int iNumber = Convert.ToInt32(Number) - 1;
                        string stationName = SetHelper.StationNumber.numberGroups[iNumber].Name;

                        if (SetHelper.siemens.ReadItem(PLCGroupName.ReadGroup, "扫描材料码_" + Number, ref obj))
                        {
                            materialCode = obj.Obj2String();
                            SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读到材料码为{materialCode}");
                        }
                        else
                        {
                            SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读材料码失败");
                        }

                       await  FeedingCheckAsync(Number, materialCode);
                    }
                    break;

                case "核对载具码启动":
                    if ((bool)TagValue)
                    {
                        await CarrierCheckAsync(Number);
                    }
                    break;

                case "设定参数上报启动":
                    if ((bool)TagValue)
                    {
                        UploadGetPara(Number);
                    }
                    break;

                case "OEE上报启动":
                    if ((bool)TagValue)
                    {
                       await  OEEDataCollectionAsync(Number);
                    }
                    break;

                case "打印信号发送启动":
                    if ((bool)TagValue)
                    {
                        UploadPrint(Number);
                    }
                    break;

                case "解绑启动":
                case "返修下线":
                    if ((bool)TagValue)
                    {
                        await  SNCarrierBindAsync(EnumBindType.UNBIND, Number);
                    }
                    break;

                case "绑定启动":
                case "返修上线":
                    if ((bool)TagValue)
                    {
                        await SNCarrierBindAsync(EnumBindType.Bind, Number);
                    }
                    break;

                case "数据上传启动":
                    if ((bool)TagValue)
                    {
                        await  DataCollectoinAsync(Number);
                    }
                    break;

                case "扫描材料码启动":
                    if ((bool)TagValue)
                    {
                        string stationName = SetHelper.StationNumber.numberGroups[int.Parse(Number) - 1].Name;
                        if (stationName.Contains("OP3045") && Number == "3")//3045-3需要先弹窗扫码，再进站，所以在此处加了弹窗
                        {
                            ScanCheckIn(Number);
                        }
                        else if (stationName.Contains("OP2020") || stationName.Contains("OP2030") || stationName.Contains("OP2055"))
                        {
                            int iNumber = Convert.ToInt32(Number) - 1;
                            await System.Windows.Application.Current.Dispatcher.BeginInvoke(() =>
                            {
                                if (IsOP1010ViewOpen)
                                {
                                    op1010View.Close();
                                }

                                SetHelper.NowScanStaion = stationName;
                                op1010View = new OP1010View("等待扫描材料码", $"{stationName}工位请扫描材料码");
                                op1010View.Show();
                                IsOP1010ViewOpen = true;
                            });
                        }
                        else
                        {
                           await LinkCompStart(Number);
                        }
                    }
                    else
                    {
                        string stationName = SetHelper.StationNumber.numberGroups[int.Parse(Number) - 1].Name;
                        if (stationName.Contains("OP2020") || stationName.Contains("OP2030") || stationName.Contains("OP2055"))
                        {
                            int iNo = Convert.ToInt32(Number) - 1;
                            if (SetHelper.StationNumber.numberGroups.Count() > iNo)
                            {
                                await Application.Current.Dispatcher.BeginInvoke(() =>
                                {
                                    if (IsOP1010ViewOpen)
                                    {
                                        op1010View.Close();
                                        IsOP1010ViewOpen = false;
                                    }
                                });
                            }
                        }
                        else
                        {
                            int iNumber = Convert.ToInt32(Number) - 1;
                            if (SetHelper.IsRestart.Length > iNumber)
                            {
                                try
                                {
                                    //关闭该工位弹窗，1秒后可再次触发弹窗
                                    SetHelper.IsRestart[iNumber] = true;
                                    Thread.Sleep(1000);
                                    SetHelper.IsRestart[iNumber] = false;
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show($"关闭弹窗失败，{ex.Message}");
                                }
                            }
                        }
                    }
                    break;

                default:
                    SetHelper.ListPLCMessage.ShowInfoQueue($"未找到PLC触发信号标签--{TagName}");
                    break;
            }
        }

        
    }
}

