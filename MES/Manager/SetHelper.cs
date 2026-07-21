using DAL;
using MES.Comm;
using MES.SetModel;
using MES.ViewModel;
using PropertyChanged;
using S7.Net.Types;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using DateTime = System.DateTime;

namespace MES.Manager
{
    [AddINotifyPropertyChangedInterface]
    public static class SetHelper
    {
        public static string mainpath = Path.Combine(Environment.CurrentDirectory, "configs");
        public static string mespath = "";
        public static string apipath = "";
        public static string simapipath = "";
        public static string scanpath = "";
        public static string plcpath = "";
        public static string alarmpath = "";

        //public static string gluepath = "";
        public static string stationNumberPath = "";

        public static string opidPath = "";
        public static string openBoxCodePath = "";
        public static string lightConfigPath = "";

        public static string materialpath = Path.Combine(mainpath, "material.json");
        public static string gluepath = Path.Combine(mainpath, "glue.json");
        public static string userpath = Path.Combine(mainpath, "user.json");
        public static string productpath = Path.Combine(mainpath, "product.json");

        public static bool IsMsgWindowOpen = false;
        public static bool IsMsgGreenWindowOpen = false;
        public static string realapipath = "";
        public static string logmainpath = Environment.CurrentDirectory + "\\log\\";

        /// <summary>
        /// MES设置
        /// </summary>
        public static MesSettingModel MesSetting = new MesSettingModel();

        public static ApiSettingModel ApiSetting = new ApiSettingModel();

        /// <summary>
        /// 扫描枪配置
        /// </summary>
        public static List<ScanSettingModel> ScanSetting = new List<ScanSettingModel>();

        public static PLCSettingModel PLCSetting = new PLCSettingModel();

        /// <summary>
        /// 用户
        /// </summary>
        public static List<UserModel> Users { get; set; } = new List<UserModel>();

        /// <summary>
        /// 工位集合
        /// </summary>
        public static StationNumberModel StationNumber = new StationNumberModel();

        public static LightConfigModel LightConfig = new LightConfigModel();
        public static OPID[] Opid;

        /// <summary>
        /// 开箱匹配的码，预留8个工站，每个工站4个开箱
        /// </summary>
        public static List<List<OpenBox>> OpenBoxes;

        public static SiemensS7Instrument siemens = new SiemensS7Instrument();
        public static MesManager mesManager = new MesManager();
        public static ScanManager scanManager = new ScanManager();
        public static DataManager dataManager = new DataManager();
        public static ResultModel[] resultModel;

        //对应2020增加扫码枪对应工站
        public static string NowScanStaion = "";

        /// <summary>
        /// 报警信息
        /// </summary>
        public static List<AlarmData> alarmDatas { get; set; } = new List<AlarmData>();

        public static string CCDImagepath = "";

        public static string[] NowMaterialCode;
        public static string Password = Extension.ReadConfig("Password");
        public static string UserName = Extension.ReadConfig("UserName");

        public static string NowProductCode = "";
        public static string NowUser = "";

        //public static int MaterialCount = 0;
        public static bool[] IsOpen;

        public static bool[] IsRestart;
        public static bool IsAdmin = false;
        public static bool IsFirstStart = true;

        public static ObservableCollection<MaterailOnOffModel> GlueOnLineList = new ObservableCollection<MaterailOnOffModel>();
        public static ObservableCollection<MaterailModel> MaterailOnLineList = new ObservableCollection<MaterailModel>();

        public static ObservableCollection<ProductTypeModel> products = new ObservableCollection<ProductTypeModel>();
        public static ProductTypeModel NowProduct = new ProductTypeModel();

        public static DateTime DateStart = new DateTime();
        public static DateTime DateEnd = new DateTime();

        public static bool StartOk = false;

        public static ProductTypeModel GetProductType(int ProductType)
        {
            ProductTypeModel productType = products.FirstOrDefault(x => x.ProductID == ProductType);
            return productType ?? new ProductTypeModel { ProductID = 1, ProductName="D02" };
        }

        /// 把产品型号名称改成产品型号ID
        public static ProductTypeModel GetProductName(string ProductName)
        {
            ProductTypeModel productType = products.FirstOrDefault(x => x.ProductName == ProductName);
            return productType;
        }

        public static bool InitializedSetting(int ProductType = 1)
        {
            try
            {
                Users = ReadSys<List<UserModel>>(userpath, "用户");

                #region 根据产品型号更改配置文件路径

                products = ReadSys<ObservableCollection<ProductTypeModel>>(productpath) ?? new ObservableCollection<ProductTypeModel>();

                NowProduct = GetProductType(ProductType) ?? new ProductTypeModel();
                ListPLCMessage.ShowInfoQueue("当前产品型号" + NowProduct.ProductName);
                if (string.IsNullOrEmpty(NowProduct.ProductName))
                {
                    return false;
                }
                SetFilePath(NowProduct.ProductName);

                #endregion 根据产品型号更改配置文件路径

                alarmDatas = ReadSys<List<AlarmData>>(alarmpath, "报警信息");

                MesSetting = ReadSys<MesSettingModel>(mespath, "MES");

                List<OPID> lsOpid = ReadSys<List<OPID>>(opidPath, "操作人员工号");
                if (lsOpid == null)
                {
                    lsOpid = new List<OPID>();
                    lsOpid.AddRange(new OPID[] { new OPID() { Id = "" }, new OPID() { Id = "" }, new OPID() { Id = "" }, new OPID() { Id = "" } });
                }

                OpenBoxes = ReadSys<List<List<OpenBox>>>(openBoxCodePath, "开箱匹配码");
                if (OpenBoxes == null)
                {
                    OpenBoxes = new List<List<OpenBox>>();
                    for (int i = 0; i < 4; i++)
                    {
                        OpenBoxes.Add(new List<OpenBox> { new OpenBox() { CheckCode = "" }, new OpenBox() { CheckCode = "" }, new OpenBox() { CheckCode = "" }, new OpenBox() { CheckCode = "" } });
                    }
                }

                ApiSetting = ReadSys<ApiSettingModel>(apipath, "API");
                realapipath = apipath;

                StationNumber = ReadSys<StationNumberModel>(stationNumberPath, "工站");
                IsOpen = new bool[StationNumber.numberGroups.Count];
                //扫码窗口数量与工位数相同
                IsRestart = new bool[StationNumber.numberGroups.Count];
                NowMaterialCode = new string[StationNumber.numberGroups.Count];
                resultModel = new ResultModel[StationNumber.numberGroups.Count];
                Opid = new OPID[StationNumber.numberGroups.Count];

                for (int i = 0; i < StationNumber.numberGroups.Count; i++)
                {
                    resultModel[i] = new ResultModel();
                    resultModel[i].Line = MesSetting.ListGroup[i].Line;
                    resultModel[i].MachineID = MesSetting.ListGroup[i].MachineID;
                    resultModel[i].StationID = MesSetting.ListGroup[i].StationID;

                    if (SetHelper.MesSetting.ListGroup[i].ScanMaterialCount == 0 && SetHelper.MesSetting.ListGroup[i].IsCarryCheckStation != "1")
                    {
                        //没精追料也不是核对载具码工位
                        SetHelper.resultModel[i].LinkVis = Visibility.Collapsed;
                    }
                    else
                    {
                        SetHelper.resultModel[i].LinkVis = Visibility.Visible;
                    }
                    //优先显示LinkComp的字
                    if (SetHelper.MesSetting.ListGroup[i].ScanMaterialCount != 0)
                    {
                        SetHelper.resultModel[i].MiddleText = "材料Link：";
                    }
                    else if (SetHelper.MesSetting.ListGroup[i].IsCarryCheckStation == "1")
                    {
                        SetHelper.resultModel[i].MiddleText = "核对载具码：";
                    }
                    //resultModel[i].LinkVis = MesSetting.ListGroup[i].ScanMaterialCount == 0 ? Visibility.Collapsed : Visibility.Visible;

                    Opid[i] = new OPID();

                    Opid[i].Id = lsOpid[i] == null ? "" : lsOpid[i].Id;
                }

                GlueOnLineList = ReadSys<ObservableCollection<MaterailOnOffModel>>(gluepath, "胶水");

                MaterailOnLineList = ReadSys<ObservableCollection<MaterailModel>>(materialpath, "批追料");

                ScanSetting = ReadSys<List<ScanSettingModel>>(scanpath, "扫码枪");

                PLCSetting = ReadSys<PLCSettingModel>(plcpath, "PLC");

                bool scanResult = scanManager.InitializeScan();
                ListScanMessage.ShowInfoQueue("扫码枪初始化" + (scanResult ? "成功" : "失败"));

                bool plcResult = dataManager.InitialzePLC();
                ListPLCMessage.ShowInfoQueue("PLC初始化" + (plcResult ? "成功" : "失败"));

                StartOk = true;
                return true;
            }
            catch (Exception ex)
            {
                ListMesMessage.ShowInfoQueue("软件初始化异常" + ex.ToString());
            }
            return false;
        }

        #region 配置相关

        public static T LoadConfig<T>(string configID, ProductTypeModel productType, string Name = "") where T : class, new()
        {
            try
            {
                string path = "";
                if (productType == null)
                {
                    path = Path.Combine(mainpath, configID);//未区分机型之前的软件配置路径
                }
                else
                {
                    SetFilePath(productType.ProductName);
                    path = Path.Combine(mainpath, productType.ProductName, configID);
                }

                T TConfig = ReadSys<T>(path, Name);

                return TConfig;
            }
            catch (Exception ex)
            {
                ListMesMessage.ShowInfoQueue(ex.ToString());
                return new T();
            }
        }

        public static void SaveConfig<T>(T config, string configID, ProductTypeModel productType, string Name = "") where T : class, new()
        {
            try
            {
                string path = "";
                if (productType == null)
                {
                    path = Path.Combine(mainpath, configID);//未区分机型之前的软件配置路径
                }
                else
                {
                    SetFilePath(productType.ProductName);
                    path = Path.Combine(mainpath, productType.ProductName, configID);
                }

                SaveSys(config, path, Name);

                ListMesMessage.ShowInfoQueue($"{Name}配置保存成功");
            }
            catch (Exception ex)
            {
                ListMesMessage.ShowInfoQueue(ex.ToString());
            }
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <param name="Name"></param>
        /// <param name="ProductName"></param>
        /// <returns></returns>
        public static T ReadSys<T>(string path, string Name = "") where T : class, new()
        {
            try
            {
                string json = File.ReadAllText(path);
                T strings = JSON.FromJson<T>(json);

                if (Name != "")
                {
                    if (strings == null)
                    {
                        ListPLCMessage.ShowInfoQueue($"{Name}未配置");
                    }
                    else
                    {
                        ListPLCMessage.ShowInfoQueue($"{Name}配置加载完成");
                    }
                }
                return strings ?? new T();
            }
            catch (Exception ex)
            {
                ListMesMessage.ShowInfoQueue(ex.ToString());
            }

            return new T();
        }

        public static void SaveSys<T>(T Model, string path, string Name = "") where T : class, new()
        {
            try
            {
                string strings = JSON.ToJsonFormat(Model);

                File.WriteAllText(path, strings);

                if (Name != "")
                {
                    ListPLCMessage.ShowInfoQueue($"{Name}配置修改完成");
                }
            }
            catch (Exception ex)
            {
                ListMesMessage.ShowInfoQueue(ex.ToString());
            }
        }

        public const string mes = "sys.json";
        public const string api = "api.json";
        public const string apisim = "apisim.json";
        public const string scan = "scan.json";
        public const string plc = "plc.json";
        public const string alarm = "alarm.json";

        //public const string glue = "glue.json";
        public const string stationNumber = "stationNumber.json";

        public const string opid = "opid.json";
        public const string openBoxCode = "openBoxCode.json";
        public const string lightConfig = "lightConfig.json";

        public static void SetFilePath(string ProductName)
        {
            if (string.IsNullOrEmpty(ProductName))
            {
            }

            #region 根据产品型号加载配置文件路径

            mespath = System.IO.Path.Combine(mainpath, ProductName, mes);
            apipath = System.IO.Path.Combine(mainpath, ProductName, api);
            simapipath = System.IO.Path.Combine(mainpath, ProductName, apisim);
            scanpath = System.IO.Path.Combine(mainpath, ProductName, scan);
            plcpath = System.IO.Path.Combine(mainpath, ProductName, plc);
            alarmpath = System.IO.Path.Combine(mainpath, ProductName, alarm);
            //gluepath = System.IO.Path.Combine(mainpath, ProductName, glue);
            stationNumberPath = System.IO.Path.Combine(mainpath, ProductName, stationNumber);
            opidPath = System.IO.Path.Combine(mainpath, ProductName, opid);
            openBoxCodePath = System.IO.Path.Combine(mainpath, ProductName, openBoxCode);
            lightConfigPath = System.IO.Path.Combine(mainpath, ProductName, lightConfig);
            List<string> pathList = new List<string>() { mespath, apipath, simapipath, scanpath, plcpath, alarmpath, materialpath, gluepath, stationNumberPath, opidPath, openBoxCodePath, lightConfigPath, productpath, userpath };
            List<string> directoryList = new List<string>() { logmainpath };
            CheckDirectory(pathList, directoryList);

            #endregion 根据产品型号加载配置文件路径
        }

        public static void CheckDirectory(List<string> pathList, List<string> directoryList)
        {
            foreach (var item in pathList)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    System.IO.FileInfo fileInfo = new System.IO.FileInfo(item);
                    if (!fileInfo.Exists)
                    {
                        fileInfo.Directory.Create();
                        fileInfo.Create().Dispose();
                    }
                }
            }

            foreach (var item in directoryList)
            {
                if (!string.IsNullOrEmpty(item))
                {
                    System.IO.DirectoryInfo directoryInfo = new System.IO.DirectoryInfo(item);
                    if (!directoryInfo.Exists)
                    {
                        directoryInfo.Create();
                    }
                }
            }
        }

        #endregion 配置相关

        public static ConcurrentQueue<MessageModel> ListPLCMessage { get; set; } = new ConcurrentQueue<MessageModel>();
        public static ConcurrentQueue<MessageModel> ListMesMessage { get; set; } = new ConcurrentQueue<MessageModel>();
        public static ConcurrentQueue<MessageModel> ListScanMessage { get; set; } = new ConcurrentQueue<MessageModel>();
        public static ConcurrentQueue<MessageModel> ListOEEMessage { get; set; } = new ConcurrentQueue<MessageModel>();
        public static ConcurrentQueue<MessageModel> ListOtherMessage { get; set; } = new ConcurrentQueue<MessageModel>();
        private static object lockobj = new object();

        public static void ShowInfoQueue(this ConcurrentQueue<MessageModel> ListMessage, string msg, bool isShow = true, string name = "log", bool result = false)
        {
            lock (lockobj)
            {
                if (!Directory.Exists(logmainpath)) Directory.CreateDirectory(logmainpath);
                File.AppendAllText($"{logmainpath}{name}-{System.DateTime.Now.ToString("yyyy-MM-dd")}.txt", $"{System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss fff")}：{msg}\r\n");
                if (isShow)
                {
                    MessageModel messageModel = new MessageModel();
                    if (ListMessage.Count >= 100)
                    {
                        ListMessage.TryDequeue(out messageModel);
                    }

                    var msgModel = new MessageModel() { Info = msg };
                    if (msg.Contains("产品进站结果：False") || msg.Contains("产品出站结果：False") || msg.Contains("产品LinkComp结果：False") || msg.Contains("与设定SN码长度"))
                    {
                        msgModel.FailBrush = Brushes.Red;
                    }
                    msgModel.FailBrush = result ? Brushes.Red : Brushes.Black;
                    ListMessage.Enqueue(msgModel);
                }
            }
        }

        public static void CheckDirectory()
        {
            Create(mainpath, mespath);
            Create(mainpath, alarmpath);
            Create(mainpath, plcpath);
            Create(mainpath, scanpath);
            Create(mainpath, apipath);
            Create(mainpath, simapipath);
            Create(mainpath, materialpath);
            Create(mainpath, gluepath);
            Create(mainpath, userpath);
            Create(logmainpath);

            void Create(string directoryPath, string filePath = "")
            {
                if (!Directory.Exists(directoryPath))
                {
                    Directory.CreateDirectory(directoryPath);
                }
                if (filePath != "" && !File.Exists(filePath))
                {
                    FileStream fs = File.Create(filePath);
                    fs.Dispose();
                }
            }
        }

        public static DataItem GetDataItem(PLCGroupName groupName, PLCTagItem tagItem)
        {
            try
            {
                DataItem item = siemens.DicDataItems[groupName.ToString()][tagItem.ToString()];
                if (item != null)
                    return item;
            }
            catch (Exception ex)
            {
                ListPLCMessage.ShowInfoQueue(ex.ToString());
            }
            ListPLCMessage.ShowInfoQueue($"未找到标签--{groupName}--{tagItem}");
            return null;
        }
    }
}