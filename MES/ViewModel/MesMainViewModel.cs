using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MES.Comm;
using MES.Manager;
using MES.MesModel.Request;
using MES.MesModel.Response;
using MES.Service;
using MES.SetModel;
using MES.View;
using PropertyChanged;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using static MES.Extension;

namespace MES.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public partial class MesMainViewModel :  ProductChangeBase
    {
        public ICommand SelectionChangedCommand { get; }
        private DispatcherTimer timer;
        private readonly IWindowService _windowService;

        public MesMainViewModel(IWindowService windowService)
        {
            SelectionChangedCommand = new RelayCommand<int>(OnSelectionChanged);
            stationNumber = SetHelper.StationNumber;
            foreach (var item in stationNumber.numberGroups)
            {
                SelectNumber.Add(item.Name);
            }
            _windowService = windowService;
            mesSettingmodel = SetHelper.MesSetting;
            apiSettingmodel = SetHelper.ApiSetting;
            OnSelectionChanged(0);
        }

        object lockobj = new object();
        public override void GetParams(ProductTypeModel product)
        {
            ObservableCollection<string> Numbers = new ObservableCollection<string>();

            mesSettingmodel = SetHelper.LoadConfig<MesSettingModel>(SetHelper.mes, product);
            apiSettingmodel = SetHelper.LoadConfig<ApiSettingModel>(SetHelper.api, product);

            mesSetting = mesSettingmodel.ListGroup[selectNumber];
            apiSetting = apiSettingmodel.ListGroup[selectNumber];

            stationNumber = SetHelper.LoadConfig<StationNumberModel>(SetHelper.stationNumber, product);
            foreach (var item in stationNumber.numberGroups)
            {
                Numbers.Add(item.Name);
            }
            SelectNumber = Numbers;
            SelectId = 0;




        }

        private int selectNumber = 0;
        //private void Timer_Tick(object sender, EventArgs e)
        //{
        //    IsSimulate = false;
        //}
        private void OnSelectionChanged(int selectedItem)
        {
            if (selectedItem < 0) return;
            selectNumber = selectedItem;
            //将参数及接口参数更新为对应序号的

            apiSetting = apiSettingmodel.ListGroup.Count() > 0 ? apiSettingmodel.ListGroup[selectedItem] : new ApiSetting();

            mesSetting = mesSettingmodel.ListGroup.Count() > 0 ? mesSettingmodel.ListGroup[selectedItem] : new MesSetting();

        }

        //绑定到主页面的对象
        public MesSettingModel mesSettingmodel { get; set; } = new MesSettingModel();
        public MesSetting mesSetting { get; set; } = new MesSetting();
        public ApiSettingModel apiSettingmodel { get; set; } = new ApiSettingModel();
        public ApiSetting apiSetting { get; set; } = new ApiSetting();
        public StationNumberModel stationNumber { get; set; } = new StationNumberModel();


        //选择工站号
        public ObservableCollection<string> SelectNumber { get; set; } = new ObservableCollection<string>();
        public string SelectText { get; set; }
        public int SelectId { get; set; } = 0;
        public static bool IsSimulate { get; set; }
        public string ButtonText { get; set; } = "MES已开启";

        public ICommand ClickCommand => new RelayCommand(() =>
        {
            if (SetHelper.IsAdmin)
            {
                try
                {
                    //保存到Json中
                    var api = DataExtend.Clone(apiSetting);
                    apiSettingmodel.ListGroup[selectNumber] = api;
                    //string apijson = JSON.ToJsonFormat(SetHelper.ApiSetting);
                    //File.WriteAllText(SetHelper.realapipath, apijson);
                    SetHelper.SaveConfig(apiSettingmodel, SetHelper.api, ProductType);
                }
                catch
                {
                    System.Windows.Forms.MessageBox.Show("Apijson保存失败，请先选择要保存的工站");
                    return;
                }

                //SetHelper.StationNumber = stationNumber;
                //string stationJson=JSON.ToJsonFormat(stationNumber);
                //File.WriteAllText(SetHelper.stationNumberPath, stationJson);

                try
                {
                    mesSetting.PictureUploadedFilePath = CompletePath(mesSetting.PictureUploadedFilePath);
                    mesSetting.PictureFilePath = CompletePath(mesSetting.PictureFilePath);
                    mesSetting.MappingDiskPath = CompletePath(mesSetting.MappingDiskPath);
                    var mes = DataExtend.Clone(mesSetting);
                    mesSettingmodel.ListGroup[selectNumber] = mes;
                    //string mesjson = JSON.ToJsonFormat(SetHelper.MesSetting);
                    //File.WriteAllText(SetHelper.mespath, mesjson);
                    SetHelper.SaveConfig(mesSettingmodel, SetHelper.mes, ProductType);

                }
                catch
                {
                    System.Windows.Forms.MessageBox.Show("Mesjson保存失败，请先选择要保存的工站");
                    return;
                }


                SetHelper.ListMesMessage.ShowInfoQueue("配置已应用");

                System.Windows.Forms.MessageBox.Show("保存成功,重启软件生效");
            }
            else
            {
                OnSelectionChanged(SelectId);//重新载入当前工站配置，界面上不暂存已修改内容
                System.Windows.Forms.MessageBox.Show("权限不足,已编辑内容无法保存");
            }



            string CompletePath(string path)
            {
                if (!path.EndsWith("\\"))
                {
                    path = path + "\\";
                }

                return path;
            }
        });



        /// <summary>
        /// 日志
        /// </summary>

        public ICommand GetFilePathCommand => new RelayCommand(() =>
        {

            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "请选择文件夹";
            folderBrowserDialog.ShowNewFolderButton = true;
            folderBrowserDialog.SelectedPath = "C:\\";

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                mesSetting.PictureFilePath = folderBrowserDialog.SelectedPath;
            }
        });

        public ICommand GetMappingDiskPathCommand => new RelayCommand(() =>
        {

            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "请选择文件夹";
            folderBrowserDialog.ShowNewFolderButton = true;
            folderBrowserDialog.SelectedPath = "C:\\";

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                mesSetting.MappingDiskPath = folderBrowserDialog.SelectedPath;
            }
        });

        public ICommand GetUploadedFilePathCommand => new RelayCommand(() =>
        {

            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.Description = "请选择文件夹";
            folderBrowserDialog.ShowNewFolderButton = true;
            folderBrowserDialog.SelectedPath = "C:\\";

            if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
            {
                mesSetting.PictureUploadedFilePath = folderBrowserDialog.SelectedPath;
            }
        });


        #region mes接口
        public async Task FileUploadAsync(string SN, PictureType type, int count)
        {
            //DateTime dtNow = DateTime.Now;
            //List<string> strings = new List<string>() { "OK", "NG" };

            //foreach (var item in strings)
            //{
            //    string netPath = $"{mesSetting.MappingDiskPath}{mesSetting.Line.Substring(0, 2)}\\{dtNow.ToString("yyyyMM")}\\{dtNow.ToString("dd")}\\Line{mesSetting.Line.Substring(mesSetting.Line.Length - 2, 2)}\\{mesSetting.StationID}\\{mesSetting.MachineID}\\{item}";
            //    string path = $"{mesSetting.PictureFilePath}\\{item}\\";
            //    if (!Directory.Exists(path))
            //    {
            //        Directory.CreateDirectory(path);
            //    }
            //    if (!Directory.Exists(netPath))
            //    {
            //        Directory.CreateDirectory(netPath);
            //    }

            //    var fileList = Directory.GetFiles(path).Where(x => x.Contains(".jpg")).ToList();
            //    foreach (var picture in fileList)
            //    {
            //        List<FileInfos> files = new List<FileInfos>();
            //        files.Add(new FileInfos()
            //        {
            //            //yyyyMMdd_StationID_Result_MMdd_HHmmss_MachineID_穴位_Original/Detection_SN_该SN的第几张图片
            //            FileName = $"{dtNow.ToString("yyyyMMdd")}_{mesSetting.StationID}_{item}_{SN}_{dtNow.ToString("MMdd")}_{dtNow.ToString("HHmmss")}_{mesSetting.MachineID}_08_{type.ToString()}_{count}",
            //            //W:\Line前两位\yyyyMM\dd\Line后两位\StationID\MachineID\(OK或NG)
            //            FilePath = netPath,
            //        });
            //        List<SNList> snlists = new List<SNList>();
            //        snlists.Add(new SNList()
            //        {
            //            SN = SN,
            //            FileInfo = files.ToArray()
            //        });
            //        FileUploadModel model = new FileUploadModel()
            //        {
            //            Line = mesSetting.Line,
            //            MachineID = mesSetting.MachineID,
            //            OPID = mesSetting.OPID,
            //            StationID = mesSetting.StationID,
            //            SNList = snlists.ToArray()
            //        };

            //        (bool, string) response = await SetHelper.mesManager.FileUpLoad(model);

            //        SetHelper.ListMesMessage.ShowInfoQueue($"图片上传结果：{response.Item1},消息：{response.Item2}");
            //    }
            //}

            SetHelper.dataManager.PictureUploadAsync();
        }

        public async Task SNCheckIn(string SN)
        {
            SNCheckINModel model = new SNCheckINModel()
            {
                Line = mesSetting.Line,
                MachineID = mesSetting.MachineID,
                StationID = mesSetting.StationID,
                FixSN = mesSetting.FixSN,
                Token = mesSetting.Token,
                OPID = mesSetting.OPID,
                SN = SN,
                CarrierID = SN,
            };

            (bool, string, string, string, string) reuslt = await SetHelper.mesManager.CheckIn(model, 0);
            SetHelper.ListMesMessage.ShowInfoQueue($"产品进站结果：{reuslt.Item1},消息：{reuslt.Item2}");
        }

        public async Task LinkComp(List<string> CompID, string SN)
        {
            List<CompList> compLists = new List<CompList>();
            foreach (var item in CompID)
            {
                compLists.Add(new CompList()
                {
                    CompID = item,
                    Qty = 1
                });
            }

            LinkCompModel model = new LinkCompModel()
            {
                Line = mesSetting.Line,
                MachineID = mesSetting.MachineID,
                StationID = mesSetting.StationID,
                OPID = mesSetting.OPID,
                SN = SN,
                CompList = compLists.ToArray(),
            };

            (bool, string) reuslt = await SetHelper.mesManager.LinkComp(model, 0);
            SetHelper.ListMesMessage.ShowInfoQueue($"产品LinkComp结果：{reuslt.Item1},消息：{reuslt.Item2}");
        }

        public async Task CheckOut(string SN)
        {
            List<CompList> compLists = new List<CompList>();
            compLists.Add(new CompList());

            List<DC_Info> dcInfo = new List<DC_Info>();
            dcInfo.Add(new DC_Info()
            {
                Item = "1111",
                Result = "PASS",
                Value = "23",
            });

            List<SNInfo> sNInfos = new List<SNInfo>();
            SNInfo info = new SNInfo()
            {
                CompList = compLists.ToArray(),
                DC_Info = dcInfo.ToArray(),
                Result = "PASS",
                SN = SN
            };

            SNCheckoutModel model = new SNCheckoutModel()
            {
                Line = mesSetting.Line,
                MachineID = mesSetting.MachineID,
                StationID = mesSetting.StationID,
                OPID = mesSetting.OPID,
                CarrierID = "",
                FixSN = mesSetting.FixSN,
                Mold = mesSetting.Mold,
                Token = mesSetting.Token,
                SNInfo = sNInfos.ToArray()
            };

            (bool, string, SN_InfoItem[], string) reuslt = await SetHelper.mesManager.CheckOut(model, 0);
            SetHelper.ListMesMessage.ShowInfoQueue($"产品出站结果：{reuslt.Item1},消息：{reuslt.Item2}");
        }

        public async Task Alarm()
        {
            EQAlarmModel model = new EQAlarmModel()
            {
                Line = mesSetting.Line,
                MachineID = mesSetting.MachineID,
                StationID = mesSetting.StationID,
                OPID = mesSetting.OPID,
                // ResetTime = DateTime.Now.ToString(),
            };

            //(bool, string) reuslt = await SetHelper.mesManager.EQAlarm(model);
            //SetHelper.ListMesMessage.ShowInfoQueue($"设备报警结果：{reuslt.Item1},消息：{reuslt.Item2}");
        }

        public async Task Status()
        {
            EQStatusModel model = new EQStatusModel()
            {
                Line = mesSetting.Line,
                MachineID = mesSetting.MachineID,
                StationID = mesSetting.StationID,
                OPID = mesSetting.OPID,
                FixSN = mesSetting.FixSN,
                Token = mesSetting.Token,
                STATUS = "",
            };

            //(bool, string) reuslt = await SetHelper.mesManager.EQStatus(model);
            //SetHelper.ListMesMessage.ShowInfoQueue($"
            //结果：{reuslt.Item1},消息：{reuslt.Item2}");
        }


        #endregion

        public System.Windows.Media.Brush ButtonColor { get; set; }
        #region 接口测试 20250402增加模拟SN码功能
        //public ICommand IsMesOpenCommand => new RelayCommand(() =>
        //{
        //    if()
        //});

        public ICommand Command0 => new RelayCommand(async () =>
        {
            _ = Task.Run(() =>
            {
                if (IsSimulate)
                {
                    IsSimulate = false;
                    ButtonText = "MES已开启";
                    ButtonColor = System.Windows.Media.Brushes.Green;
                    SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "MES屏蔽_1", false);

                }
                else
                {
                    if (SetHelper.IsAdmin)
                    {
                        IsSimulate = true;
                        ButtonText = "MES已屏蔽";
                        ButtonColor = System.Windows.Media.Brushes.Red;
                        SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "MES屏蔽_1", true);
                    }
                    else
                    {
                        System.Windows.Forms.MessageBox.Show("权限不足");
                    }
                }
            });

        });
        public ICommand Command1 => new RelayCommand(async () =>
        {
            if (IsSimulate)
            {
                _ = Task.Run(() =>
                 {
                     SetHelper.dataManager.Siemens_OnDataChange("产品进站启动" + "_" + (SelectId + 1).ToString(), -1, 0, true);
                     System.Windows.Forms.MessageBox.Show(SelectText + "产品进站启动" + "_" + (SelectId + 1).ToString() + "\r\n" + "模拟触发成功");
                 });

            }
        });

        public ICommand Command8 => new RelayCommand(async () =>
        {
            if (IsSimulate)
            {
                _ = Task.Run(() =>
                {
                    SetHelper.dataManager.Siemens_OnDataChange("扫描材料码启动" + "_" + (SelectId + 1).ToString(), -1, 0, true);
                    System.Windows.Forms.MessageBox.Show(SelectText + "扫描材料码启动" + "_" + (SelectId + 1).ToString() + "\r\n" + "模拟触发成功");
                });

            }
        });

        public ICommand Command2 => new RelayCommand(async () =>
        {
            if (IsSimulate)
            {
                _ = Task.Run(() =>
                {
                    SetHelper.dataManager.Siemens_OnDataChange("产品出站启动" + "_" + (SelectId + 1).ToString(), -1, 0, true);
                    System.Windows.Forms.MessageBox.Show(SelectText + "产品出站启动" + "_" + (SelectId + 1).ToString() + "\r\n" + "模拟触发成功");
                });
            }
        });

        public ICommand Command3 => new RelayCommand(async () =>
        {
            if (IsSimulate)
            {
                _ = Task.Run(() =>
                  {
                      SetHelper.dataManager.Siemens_OnDataChange("扫描材料码启动" + "_" + (SelectId + 1).ToString(), -1, 0, true);
                      //MessageBox.Show(SelectText + "扫描材料码启动" + "_" + (SelectId + 1).ToString() + "\r\n" + "模拟触发成功");
                  });
            }
        });

        public ICommand Command4 => new RelayCommand(async () =>
        {
            if (IsSimulate)
            {
                await Task.Run(() =>
                {
                    SetHelper.dataManager.Siemens_OnDataChange("扫描材料码启动" + "_" + (SelectId + 1).ToString(), -1, 0, false);
                    //MessageBox.Show(SelectText + "扫描材料码关闭" + "_" + (SelectId + 1).ToString() + "\r\n" + "模拟触发成功");
                });
            }
        });

        public ICommand Command5 => new RelayCommand(async () =>
        {
            if (IsSimulate)
            {
                await Task.Run(() =>
                {
                    SetHelper.dataManager.Siemens_OnDataChange("核对载具码启动" + "_" + (SelectId + 1).ToString(), -1, 0, true);
                    System.Windows.Forms.MessageBox.Show(SelectText + "核对载具码启动" + "_" + (SelectId + 1).ToString() + "\r\n" + "模拟触发成功");
                });
            }
        });

        public ICommand Command6 => new RelayCommand(async () =>
        {
            if (IsSimulate)
            {
               await Task.Run(() =>
                {
                    SetHelper.dataManager.Siemens_OnDataChange("设定参数上报启动" + "_" + (SelectId + 1).ToString(), -1, 0, true);
                    System.Windows.Forms.MessageBox.Show(SelectText + "设定参数上报启动" + "_" + (SelectId + 1).ToString() + "\r\n" + "模拟触发成功");
                });
            }
        });

        public ICommand Command7 => new RelayCommand(async () =>
        {
            if (IsSimulate)
            {
                var x = Task.Run(() =>
                {
                    SetHelper.dataManager.Siemens_OnDataChange("OEE上报启动" + "_" + (SelectId + 1).ToString(), -1, 0, true);
                    System.Windows.Forms.MessageBox.Show(SelectText + "OEE上报启动" + "_" + (SelectId + 1).ToString() + "\r\n" + "模拟触发成功");
                });
            }
        });

        //public ICommand Command8 => new RelayCommand(async () =>
        //{
        //    SetHelper.dataManager.Siemens_OnDataChange(PLCTagItem.产品出站启动.ToString(), 2020, true);
        //});
        public ICommand Command9 => new RelayCommand(async () =>
        {
            SetHelper.dataManager.Siemens_OnDataChange("设备运行状态_1", 2020, 1, 1);
        });
        public ICommand Command10 => new RelayCommand(async () =>
        {
            SetHelper.dataManager.GetWeight("1");
        });




        [RelayCommand]
        private void OpenTestWindow()
        {

            _windowService.Show<TestView>();
        }

        [RelayCommand]
        private void OpenMiscWindow()
        {
            _windowService.ShowDialog<MiscView>();
        }


        public ICommand Command11 => new RelayCommand(async () =>
        {
            try
            {
                double time = (SetHelper.DateEnd - SetHelper.DateStart).TotalSeconds;
                object data = new object();
                List<OeeInfo> dcInfoList = new List<OeeInfo>();
                dcInfoList.Add(new OeeInfo() { Item = "Cycle_Time", Value = 100.Obj2String() });
                dcInfoList.Add(new OeeInfo() { Item = "Eq_Active_Rate", Value = "100%" });
                dcInfoList.Add(new OeeInfo() { Item = "Takt_Time", Value = 100.Obj2String() });
                dcInfoList.Add(new OeeInfo() { Item = "TossingCOLLECTION", Value = "0" });
                //(bool, string) reuslt = await SetHelper.mesManager.EQOEE(dcInfoList.GetOEEModel("CSF-E29-1-DZRT-008"));
                //SetHelper.ListMesMessage.ShowInfoQueue($"OEE上传结果：{reuslt.Item1},消息：{reuslt.Item2}");

            }
            catch (Exception ex)
            {
                SetHelper.ListMesMessage.ShowInfoQueue(ex.ToString());
            }
        });
        #endregion
    }
}
