using MES.Comm;
using MES.Manager;
using MES.MesModel.Request;
using MES.SetModel;
using MES.View;
using PropertyChanged;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace MES.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class MaterialOnOffViewModel : ProductChangeBase
    {
        public ICommand SelectionChangedCommand { get; }
        public ICommand MaterialChangedCommand { get; }

        private string[] UploadsuccessCode = new string[] { "", "", "", "", "", "", "", "", "", "" };

        public MaterialOnOffViewModel()
        {
            MessageManager.Subscribe(OnMessageReceived);
            StatusManager.Subscribe(OnStatusReceived);
            SelectionChangedCommand = new RelayCommand<int>(OnSelectionChanged);
            MaterialChangedCommand = new RelayCommand<string>(OnMaterialChanged);

            if (SetHelper.MesSetting.ListGroup.Count > 0)
            {
                //默认显示为第一个工站的信息
                for (int i = 0; i < SetHelper.MesSetting.ListGroup[0].BoxCount; i++)
                {
                    //BoxNoList.Add((i + 1).ToString());
                    string code = SetHelper.OpenBoxes[0][i].CheckCode;

                    OpenBoxes.Add(new OpenBoxModel() { CheckCode = code, BoxName = "开箱" + (i + 1) });
                }


                for (int i = 0; i < SetHelper.StationNumber.numberGroups.Count; i++)
                {
                    LocationList.Add(SetHelper.StationNumber.numberGroups[i].Name);
                }

                var materialName = new ObservableCollection<string>();
                var names = SetHelper.MesSetting.ListGroup[0].BatchMaterialName.Split(',', '，');
                foreach (var item in names)
                {
                    materialName.Add(item);
                }
                MaterialNameList = materialName;

                foreach (var item in SetHelper.MesSetting.ListGroup)
                {
                    if (item.IsSelectStation == "1")
                    {
                        //string json = File.ReadAllText(SetHelper.lightConfigPath);
                        //SetHelper.LightConfig.ListGroup = JSON.FromJson<ObservableCollection<LightConfig>>(json);
                        SetHelper.LightConfig.ListGroup = SetHelper.LoadConfig<ObservableCollection<LightConfig>>(SetHelper.lightConfig, SetHelper.NowProduct);
                    }
                }

                if (MaterialNameList.Count > 0)
                {
                    if (SetHelper.MesSetting.ListGroup[0].IsSelectStation == "1")
                    {
                        IsSelectStation = Visibility.Visible;
                    }
                    else
                    {
                        IsSelectStation = Visibility.Hidden;
                        LightNumber = 0;
                    }
                }
                else
                {
                    IsSelectStation = Visibility.Hidden;
                    LightNumber = 0;
                }
            }

            SetHelper.scanManager.OnScanOpenBox += ScanManager_OnScanOpenBox; ;
            GlueOnLineList = SetHelper.MaterailOnLineList;
            SelectMesSetting = SetHelper.MesSetting;

            SetHelper.dataManager.MaterialOfflineAction += (Code) =>
            {
                var data = GlueOnLineList.FirstOrDefault(x => x.GlueCode == Code);
                if (data != null)
                {
                    data.GlueCode = "";
                    SetHelper.SaveSys(GlueOnLineList, SetHelper.materialpath);
                }
            };
        }

        public override void GetParams(ProductTypeModel productType)
        {
            SelectMesSetting = SetHelper.MesSetting;
            // var mesSetting = SetHelper.LoadConfig<MesSetting>(SetHelper.mes, productType);
            var materialName = new ObservableCollection<string>();
            var locationName = new ObservableCollection<string>();
            var names = SetHelper.MesSetting.ListGroup[0].BatchMaterialName.Split(',', '，');
            foreach (var item in names)
            {
                materialName.Add(item);
            }

            for (int i = 0; i < SetHelper.StationNumber.numberGroups.Count; i++)
            {
                locationName.Add(SetHelper.StationNumber.numberGroups[i].Name);
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                MaterialNameList.Clear();
                MaterialNameList = materialName;
                LocationList.Clear();
                LocationList = locationName;
                OnSelectionChanged(0);
            });

            foreach (var item in SetHelper.MesSetting.ListGroup)
            {
                if (item.IsSelectStation == "1")
                {
                    SetHelper.LightConfig.ListGroup = SetHelper.LoadConfig<ObservableCollection<LightConfig>>(SetHelper.lightConfig, SetHelper.NowProduct);
                }
            }

            if (MaterialNameList.Count > 0)
            {
                if (SetHelper.MesSetting.ListGroup[0].IsSelectStation == "1")
                {
                    IsSelectStation = Visibility.Visible;
                }
                else
                {
                    IsSelectStation = Visibility.Hidden;
                    LightNumber = 0;
                }
            }
            else
            {
                IsSelectStation = Visibility.Hidden;
                LightNumber = 0;
            }
        }

        public MesSettingModel SelectMesSetting { get; set; } = new MesSettingModel();

        private async void OnMessageReceived(object message)
        {
            var lst = message as ObservableCollection<MaterailModel>;
            for (int i = 0; i < lst.Count; i++)
            {
                if (lst[i].GlueCode == "" && GlueOnLineList[i].GlueCode != "")
                {
                    (bool, string) responseOff = (false, "");
                    //触发下料
                    await Task.Run
                    (async () =>
                    {
                        responseOff = await SetHelper.mesManager.CompSNOffline(GlueOnLineList[i].GlueCode.GetCompSNOffline(i, GlueOnLineList[i].LightNumber), i);
                    });
                    if (responseOff.Item1 == true)
                    {
                        GlueOnLineList[i].GlueCode = "";
                        SetHelper.ListMesMessage.ShowInfoQueue($"出站MES反馈{lst[i].MaterialName} {lst[i].GlueCode} 已用完, 批次码自动下料成功,请扫码上料");
                    }
                    else
                    {
                        SetHelper.ListMesMessage.ShowInfoQueue($"出站MES反馈{lst[i].MaterialName} {lst[i].GlueCode} 已用完, 批次码自动下料失败\r\n\r\nMES返回信息:{responseOff.Item2}");
                    }
                }
            }
            //GlueOnLineList = lst;
            //SaveSys(GlueOnLineList);
            SetHelper.SaveSys(GlueOnLineList, SetHelper.materialpath);
        }

        private void OnStatusReceived(object message)
        {
            var lst = message as int[];
            for (int i = 0; i < lst.Length; i++)
            {
                if (lst[i] == 1)
                {
                    //收到工位自动信号 复位对应的开箱按钮
                    UploadsuccessCode[i] = "";
                    int iNumber = LocationList.IndexOf(LocationNo);
                    if (iNumber == i)
                    {
                        //如果当前选择工位与收到运行信号的工位一致，则复位开箱信号，否则在切换工位时进行复位
                        foreach (var item in OpenBoxes)
                        {
                            item.IsOpenEnable = false;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 工位号下拉框变化
        /// </summary>
        /// <param name="selectedItem"></param>
        private void OnSelectionChanged(int selectedItem)
        {
            if (selectedItem < 0) return;

            LocationIndexStatic = selectedItem;
            //物料名称下拉框变化
            var materialName = new ObservableCollection<string>();
            var names = SelectMesSetting.ListGroup[selectedItem].BatchMaterialName.Split(',', '，');
            foreach (var item in names)
            {
                materialName.Add(item);
            }
            MaterialNameList = materialName;

            GlueOnLineList = new ObservableCollection<MaterailModel>(SetHelper.MaterailOnLineList.Where(x => LocationList.Contains(x.LocationNo)).ToList());

            //开箱按钮变化
            OpenBoxes.Clear();
            //var openButton = new ObservableCollection<OpenBoxModel>();

            for (int i = 0; i < SelectMesSetting.ListGroup[selectedItem].BoxCount; i++)
            {
                BoxNoList.Add((i + 1).ToString());
                string configCodes = SetHelper.OpenBoxes[selectedItem][i].CheckCode;
                //如果上传成功的码中包含当前配置的开箱码，则按钮使能
                var codes = configCodes.Split(',', '，');
                bool isEnable = false;
                foreach (var code in codes)
                {
                    if (UploadsuccessCode[selectedItem].Contains(code) && !string.IsNullOrEmpty(code))
                    {
                        isEnable = true;
                    }
                }
                OpenBoxes.Add(new OpenBoxModel() { CheckCode = configCodes, BoxName = "开箱" + (i + 1), IsOpenEnable = isEnable });
            }
        }

        /// <summary>
        /// 物料名称下拉框变化
        /// </summary>
        /// <param name="selectedItem"></param>
        private void OnMaterialChanged(string selectedItem)
        {
            if (SetHelper.MesSetting.ListGroup[LocationIndexStatic].IsSelectStation == "1" && !string.IsNullOrEmpty(selectedItem) && selectedItem.Contains("垫片"))
            {
                int lightNum = -1;
                if (SetHelper.LightConfig.ListGroup != null)
                {
                    int index = SetHelper.LightConfig.ListGroup.ToList().FindIndex(it => it.LightCode == selectedItem);
                    if (index != -1)
                    {
                        lightNum = SetHelper.LightConfig.ListGroup[index].LightNumber;
                    }
                }
                LightNumber = lightNum;
            }
            else
            {
                LightNumber = 0;
            }

            //if (GlueOnLineList.Count > 0 && selectedItem != "")
            //{
            //    MaterailModel material = GlueOnLineList.FirstOrDefault(x => x.MaterialName == selectedItem);
            //    if (material != null)
            //    {
            //        BoxNo = material.BoxNo;
            //    }
            //}
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

        public ObservableCollection<string> MaterialNameList { get; set; } = new ObservableCollection<string>() { };
        public string MaterialNameNo { get; set; } = "";

        /// <summary>
        /// 物料一次使用数量
        /// </summary>
        //public List<int> UseCountOnce { get; set; } = new List<int>() {1,2,3,4,5,6,7,8,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30 };
        public int UseCountOnceSelect { get; set; } = 1;
        /// <summary>
        /// 灯号
        /// </summary>
        public int LightNumber { get; set; } = 0;
        /// <summary>
        /// 是否选垫工位 1=隐藏 2=显示
        /// </summary>
        public Visibility IsSelectStation { get; set; } = Visibility.Hidden;
        /// <summary>
        /// 选中的料箱号
        /// </summary>
        public string BoxNo { get; set; } = "";
        public string LocationNo { get; set; } = "";
        public static int LocationIndexStatic = 0;
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
        public ObservableCollection<MaterailModel> GlueOnLineList { get; set; } = new ObservableCollection<MaterailModel>();
        public ObservableCollection<OpenBoxModel> OpenBoxes { get; set; } = new ObservableCollection<OpenBoxModel>();

        /// <summary>
        /// 上料
        /// </summary>
        public ICommand MaterialOnCommand => new RelayCommand(async () =>
        {
            if (string.IsNullOrEmpty(GlueCode))
            {
                ErrorMsg = "物料码不能为空！";
                Color = Brushes.Red;
                return;
            }

            if (string.IsNullOrEmpty(LocationNo))
            {
                ErrorMsg = "工位号不能为空！";
                Color = Brushes.Red;
                return;
            }

            if (string.IsNullOrEmpty(MaterialNameNo))
            {
                ErrorMsg = "物料名称不能为空！";
                Color = Brushes.Red;
                return;
            }

            //var result = MessageBox.Show($"{MaterialNameNo}\r\n{GlueCode}\r\n是否确认上料??\r\n当前料箱号:{(BoxNo == "" ? "无" : BoxNo)}", "上料确认", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            //if (result == MessageBoxResult.No)
            //{
            //    return;
            //}

            //不卡控重复物料，可重复上
            //if (GlueOnLineList.Any(x => x.GlueCode == GlueCode))
            //{
            //    ErrorMsg = "物料码重复，请勿重复上料！";
            //    Color = Brushes.Red;
            //    return;
            //}

            int iNumber = LocationList.IndexOf(LocationNo);
            LocationIndexStatic = iNumber;
            //上料前先下料，如果存在名称相同，工位相同，但物料码不同的则触发下料
            if (GlueOnLineList.Any(x => (x.MaterialName == MaterialNameNo && x.LocationNo == LocationNo) && (!string.IsNullOrEmpty(x.GlueCode))))
            {
                var model = GlueOnLineList.Where(x => x.MaterialName == MaterialNameNo && x.LocationNo == LocationNo).FirstOrDefault();
                ErrorMsg = $"{model.GlueCode}:下料正在请求MES中...";
                var responseOff = await SetHelper.mesManager.CompSNOffline(model.GlueCode.GetCompSNOffline(iNumber, model.LightNumber), iNumber);
                //var responseOff = (true, "下料成功");//测试用
                if (!responseOff.Item1)
                {
                    ErrorMsg = $"旧物料 {model.GlueCode}:下料失败！{DateTime.Now}\r\n\r\nMES返回信息: {responseOff.Item2}";
                    Color = Brushes.Red;
                    return;
                }
                else
                {
                    string code = model.GlueCode;

                    foreach (var item in GlueOnLineList)
                    {
                        if (item.GlueCode == code)
                        {
                            item.GlueCode = "";
                            item.MesMsg = responseOff.Item2;
                        }
                    }
                }
                SetHelper.SaveSys(GlueOnLineList, SetHelper.materialpath, "批追物料");
            }
            //下料后保存
            //SaveSys(GlueOnLineList);



            ErrorMsg = $"{GlueCode}:上料正在请求MES中...";
            Color = Brushes.Green;

            var light = LightNumber < 1 ? 1 : LightNumber;
            var response = await SetHelper.mesManager.CompSNCheckout(GlueCode.GetCompSNCheckout(iNumber, light), iNumber);
            if (response.Item1)
            {
                ErrorMsg = $"{GlueCode}:{MaterialNameNo}上料成功！{DateTime.Now}\r\n\r\nMES返回信息: {response.Item2}";
                Color = Brushes.Green;

                string dateTime = System.DateTime.Now.ToString();//扫码时间
                //如果工位号和物料号已存在，则替换掉旧的
                var items = GlueOnLineList.Where(a => a.MaterialName == MaterialNameNo && a.LocationNo == LocationNo).ToList();

                if (items.Count > 0)
                {
                    foreach (var item in items)
                    {
                        GlueOnLineList.Remove(item);
                        GlueOnLineList.Add(new MaterailModel() { MaterialName = MaterialNameNo, BoxNo = BoxNo, DateTime = dateTime, LightNumber = LightNumber, GlueCode = GlueCode, LocationNo = LocationNo, MesMsg = response.Item2, UseCountOnce = UseCountOnceSelect });
                    }
                }
                else
                {
                    GlueOnLineList.Add(new MaterailModel() { MaterialName = MaterialNameNo, BoxNo = BoxNo, DateTime = dateTime, LightNumber = LightNumber, GlueCode = GlueCode, LocationNo = LocationNo, MesMsg = response.Item2, UseCountOnce = UseCountOnceSelect });
                }

                SetHelper.SaveSys(GlueOnLineList, SetHelper.materialpath, "批追物料");

                UploadsuccessCode[iNumber] = GlueCode;
                foreach (var item in OpenBoxes)
                {
                    var codes = item.CheckCode.Split(',', '，');
                    foreach (var code in codes)
                    {
                        if (GlueCode.Contains(code))
                        {
                            item.IsOpenEnable = true;
                        }
                    }
                }

                //写入开箱信号
                //if (BoxNo != "无" || BoxNo != "")
                //{
                SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, $"{MaterialNameNo}开箱_{(iNumber + 1)}", true);
                //}

            }
            else
            {
                ErrorMsg = $"{GlueCode}:上料失败！{DateTime.Now}\r\n\r\nMES返回信息: {response.Item2}";
                Color = Brushes.Red;
            }
            GlueCode = "";
            MaterialName = "";
            BoxNo = "";
            SetHelper.MaterailOnLineList = GlueOnLineList;
        });

        public ICommand LightConfigCommand => new RelayCommand(() =>
        {
            var gapNames = MaterialNameList.Where(it => it.Contains("垫片")).ToList();
            List<LightConfig> configs = new List<LightConfig>();
            if (SetHelper.LightConfig.ListGroup != null)
            {
                foreach (var item in gapNames)
                {
                    int index = SetHelper.LightConfig.ListGroup.ToList().FindIndex(it => it.LightCode == item);
                    if (index != -1)
                    {
                        configs.Add(SetHelper.LightConfig.ListGroup[index]);
                    }
                    else
                    {
                        configs.Add(new LightConfig()
                        {
                            LightCode = item,
                            LightNumber = -1,
                        });
                    }
                }
            }

            LightConfigView view = new LightConfigView(configs);
            view.ShowDialog();

        });


        public ICommand MaterialOffLineCommand => new RelayCommand<MaterailModel>(async (s) =>
        {
            int iNumber = LocationList.IndexOf(s.LocationNo);

            var result = MessageBox.Show($"{s.MaterialName}\r\n{s.GlueCode}\r\n是否确认下料??", "下料确认", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.No)
            {
                return;
            }

            ErrorMsg = $"{s.GlueCode}:下料正在请求MES中...";
            var responseOff = await SetHelper.mesManager.CompSNOffline(s.GlueCode.GetCompSNOffline(iNumber, s.LightNumber), iNumber);
            //var responseOff = (true, "下料成功");//测试用
            if (!responseOff.Item1)
            {
                ErrorMsg = $"物料 {s.GlueCode}:下料失败！{DateTime.Now}\r\n\r\nMES返回信息: {responseOff.Item2}";
                Color = Brushes.Red;
                return;
            }
            else
            {
                ErrorMsg = $"物料 {s.GlueCode}:下料成功！{DateTime.Now}\r\n\r\nMES返回信息: {responseOff.Item2}";
                Color = Brushes.Green;

                string code = s.GlueCode;


                foreach (var item in GlueOnLineList)
                {
                    if (item.GlueCode == code)
                    {
                        item.GlueCode = "";
                        item.MesMsg = responseOff.Item2;
                    }
                }
            }

            //下料后保存
            //SaveSys(GlueOnLineList);
            SetHelper.SaveSys(GlueOnLineList, SetHelper.materialpath, "批追物料");
            SetHelper.MaterailOnLineList = GlueOnLineList;

        });


        public ICommand MaterialDeleteCommand => new RelayCommand<MaterailModel>((material) =>
        {
            try
            {
                if (SetHelper.IsAdmin)
                {
                    var msgResult = MessageBox.Show($"是否强制删除:\r\n物料名称：{material.MaterialName}\r\n料号：{material.GlueCode}", "", MessageBoxButton.YesNo);
                    if (msgResult == MessageBoxResult.Yes)
                    {
                        GlueOnLineList.Remove(material);

                        SetHelper.SaveSys(GlueOnLineList, SetHelper.materialpath, "批追物料");

                        SetHelper.ListPLCMessage.ShowInfoQueue($"料号：{material.GlueCode} 物料名称：{material.MaterialName} 已强制删除");
                        SetHelper.MaterailOnLineList = GlueOnLineList;

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

        public ICommand CopyCommand => new RelayCommand<MaterailModel>((material) =>
        {
            try
            {
                if (SetHelper.IsAdmin)
                {
                    new CopyToView(material).ShowDialog();
                }
                else
                {
                    MessageBox.Show("权限不足");
                }
            }
            catch (Exception ex)
            {

            }
        });

    }
    [AddINotifyPropertyChangedInterface]
    public class MaterailModel
    {
        /// <summary>
        ///胶水码 
        /// </summary>
        public string GlueCode { get; set; }
        /// <summary>
        /// 物料一次使用数量
        /// </summary>
        public int UseCountOnce { get; set; }
        /// <summary>
        /// 所属箱号
        /// </summary>
        public string BoxNo { get; set; }
        /// <summary>
        /// 物料名称
        /// </summary>
        public string MaterialName { get; set; }
        /// <summary>
        /// 扫码时间
        /// </summary>
        public string DateTime { get; set; }
        /// <summary>
        /// 工位号
        /// </summary>
        public string LocationNo { get; set; }
        /// <summary>
        /// MES返回的信息
        /// </summary>
        public string MesMsg { get; set; }
        /// <summary>
        /// 灯号
        /// </summary>
        public int LightNumber { get; set; }
    }

    [AddINotifyPropertyChangedInterface]
    public class OpenBoxModel
    {
        public ICommand OpenBoxCommand { get; }
        public ICommand SaveCommand { get; }

        public OpenBoxModel()
        {
            OpenBoxCommand = new RelayCommand<object>(OpenBox);
            SaveCommand = new RelayCommand<object>(SaveCode);
            //string[] spiteCode = code.Split(',', '，');
        }

        private void SaveCode(object name)
        {
            if (SetHelper.IsAdmin)
            {
                Match match = Regex.Match(name.ToString(), @"(\d+)$");
                //默认是1
                string Number = "1";
                if (match.Success)
                {
                    Number = match.Value;
                }
                //开箱序号
                int iNumber = Convert.ToInt32(Number) - 1;
                SetHelper.OpenBoxes[MaterialOnOffViewModel.LocationIndexStatic][iNumber].CheckCode = CheckCode;
                string json = JSON.ToJsonFormat(SetHelper.OpenBoxes);
                File.WriteAllText(SetHelper.openBoxCodePath, json);
                MessageBox.Show("保存成功，重启生效");
            }
            else
            {
                MessageBox.Show("权限不足");
            }
        }
        private void OpenBox(object i)
        {
            int stationNumber = (int)i + 1;
            string stationName = SetHelper.StationNumber.numberGroups[(int)i].Name;
            //给PLC发送开箱信号
            string varName = BoxName + "_" + stationNumber.ToString();
            bool result = SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, varName, true);
            MessageBox.Show($"{stationName} 向PLC写开箱信号 {varName} {true},{(result ? "成功" : "失败")}");
            SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 向PLC写开箱信号 {varName} {true},{(result ? "成功" : "失败")}");

        }
        public string CheckCode { get; set; }
        public string BoxName { get; set; }
        public bool IsOpenEnable { get; set; }

    }

}
