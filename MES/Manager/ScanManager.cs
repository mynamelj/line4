using Autofac;
using MES.MesModel.Request;
using MES.Service;
using MES.SetModel;
using MES.ViewModel;
using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using TsHardWare;
using static MES.Extension;

namespace MES.Manager
{
    public class ScanManager
    {
        /// <summary>
        /// 扫码枪初始化
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public static string ProductCode = "";

        private readonly IMiscService miscService;
        /// <summary>
        /// SN码
        /// </summary>
        public static string SNCode = "";

        private List<HardWare_ScanBar> scanBar = new List<HardWare_ScanBar>();




        public ScanManager()
        {
            miscService = CreateMiscService();
        }
        public bool InitializeScan()
        {
            try
            {

                int i = 0;
                foreach (var item in scanBar)
                {
                    if (item != null && item.IsConnect)
                    {
                        item.Close();
                        item.isRun = false;
                        item.OnReadBar -= ScanBar_OnReadBar;
                    }
                }
                scanBar.Clear();

                foreach (var item in SetHelper.ScanSetting)
                {
                    HardWare_ScanBar scan = null;

                    scan = new HardWare_ScanBar(item.Com, item.BaudRate.String2Int(), item.Parity.String2Parity(), item.DataBits.String2Int(), item.StopBits.String2StopBits()) { Name = "扫码枪" };

                    scan.EndLine = item.EndLine.Replace("\\r", "\r").Replace("\\n", "\n");

                    scan.ID = item.ID;

                    if (scan == null)
                    {
                        SetHelper.ListScanMessage.ShowInfoQueue("扫码枪初始化失败！");
                        return false;
                    }
                    scan.HardWareIndex = i;
                    scan.Index = i + 1;
                    //连接扫描枪
                    if (!scan.Connect())
                    {
                        SetHelper.ListScanMessage.ShowInfoQueue("扫码枪初始化失败！");
                        return false;
                    }
                    scan.OnReadBar += ScanBar_OnReadBar;
                    scan.isRun = true;
                    scan.StartReading();
                    scanBar.Add(scan);
                    i++;
                }
                
                SetHelper.ListScanMessage.ShowInfoQueue("扫码枪初始化成功！");
                return true;
            }
            catch (Exception ex)
            {
                SetHelper.ListScanMessage.ShowInfoQueue(ex.ToString());
                return false;
            }
        }

        public void StopScan()
        {
        }

        public delegate void ScanOpenBox(string ScanCode);

        public event ScanOpenBox OnScanOpenBox;


        public async void ScanBar_OnReadBar(List<string> listBar, int hardIndex)
        {
            string str = string.Join("", listBar);
            // 线外啮合站扫码直接进站，不依赖弹窗、SN前缀映射或PLC。
            if (MES.SpecialStations.Meshina.MeshinaRuntime.IsStation(hardIndex))
            {
                await MES.SpecialStations.Meshina.MeshinaRuntime.ScanAsync(hardIndex, str);
                return;
            }
            string stationName = "";
            OnScanOpenBox?.Invoke(str);//将各个页面的码更新

            SetHelper.ListScanMessage.ShowInfoQueue(stationName + " 扫描到条码为" + str.ToString());

            int stationIndex = SetHelper.isSpecialStation ? miscService.ResolveStationIndex(hardIndex, str) : hardIndex;
            if (stationIndex < 0)
            {
                return;
            }

            hardIndex = stationIndex;
            stationName = SetHelper.StationNumber.numberGroups[hardIndex].Name;

            SetHelper.ListScanMessage.ShowInfoQueue(stationName + " 1=>");
            //SetHelper.ListScanMessage.ShowInfoQueue(stationName + " 扫描到条码为" + str.ToString());

            if (SetHelper.MesSetting.ListGroup[hardIndex].RegexRule != "")
            {
                Regex regex = new Regex(SetHelper.MesSetting.ListGroup[hardIndex].RegexRule);

                bool isOk = regex.IsMatch(str);
                if (!isOk)
                {
                    System.Windows.MessageBox.Show("条码包含非法字符，请检查条码并重新扫描");
                    //break;
                    return;
                }
             
                SetHelper.NowMaterialCode[hardIndex] = str;
                //OnScanOpenBox?.Invoke(str);//将各个页面的码更新
                //}

                if (stationName.ToUpper().Contains("OP1030"))
                {
                    SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "定子码_" + (hardIndex + 1).ToString(), str.Trim());
                }
 

                //部分工位例如OP1010直接扫码进站，用码的长度来识别是否为进站的码，内部包含是否需要FeedingCheck
                if (SetHelper.MesSetting.ListGroup[hardIndex].SNCodeLen > 0)
                {
                    SetHelper.ListScanMessage.ShowInfoQueue(stationName + " 2=>");
                    if (stationName.ToUpper().Contains("OP1010") || stationName.ToUpper().Contains("OP3040")|| 
                        stationName.ToUpper().Contains("OP4020")|| stationName.ToUpper().Contains("OP2020") || stationName.ToUpper().Contains("OP2035")
                        || stationName.ToUpper().Contains("OP2030") || stationName.ToUpper().Contains("NG_IO"))
                    {
                        SetHelper.ListScanMessage.ShowInfoQueue(stationName + " 3=>");
                        SetHelper.ListScanMessage.ShowInfoQueue(stationName + $" 5=>{hardIndex} {str.Trim().Length} {SetHelper.MesSetting.ListGroup[hardIndex].SNCodeLen}");
                        if (str.Trim().Length == SetHelper.MesSetting.ListGroup[hardIndex].SNCodeLen||stationName.ToUpper().Contains("OP2030")|| stationName.ToUpper().Contains("OP2020") || stationName.ToUpper().Contains("OP2035"))
                        {    //弹窗未关闭
                            if (DataManager.IsOP1010ViewOpen)
                            {

                                SetHelper.ListScanMessage.ShowInfoQueue(stationName + $" 4=>{str}");
                                SNCode = str;

                                if ((stationName.ToUpper().Contains("OP2020M") || stationName.ToUpper().Contains("OP2030M")) && SetHelper.NowProduct.ProductID==25)
                                {


                                    (bool, string, string) response0 =
                                        await SetHelper.mesManager.FeedingCheck(SNCode.GetFeedingCheck(hardIndex), hardIndex);

                                    SetHelper.siemens.WriteItem(SetModel.PLCGroupName.WriteGroup, "产品SN" + "_" + (hardIndex + 1).ToString(), SNCode);

                                    SetHelper.resultModel[hardIndex].Result1 = response0.Item1 switch
                                    {
                                        true => "OK",
                                        false => "NG",
                                    };
                                    SetHelper.resultModel[hardIndex].CheckInSN = SNCode;

                                    if (!response0.Item1)
                                    {
                                        SetHelper.ListMesMessage.ShowInfoQueue(
                                            $"{stationName} FeedingCheck失败：{response0.Item2}");
                                        var result0 = SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "进站结果_" + (hardIndex + 1), 2);
                                        SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} {SNCode}--{stationName} 进站结果{hardIndex + 1}写{2}" +
                                            $"{(result0 ? "成功" : "失败")}");
                                    }
                                    return;
                                }
                                //object res = null;
                                //if (SetHelper.siemens.ReadItem(PLCGroupName.TriggerGroup, "产品进站启动_" + (hardIndex + 1), ref res))
                                //{
                                //    if (res.ObjToBool() == false)
                                //    {
                                //        SetHelper.ListPLCMessage.ShowInfoQueue($"产品进站启动__{hardIndex + 1}值为 {res}");
                                //        return;
                                //    }
                                //}
                                SetHelper.ListPLCMessage.ShowInfoQueue($"扫码触发 产品进站启动_{hardIndex + 1} True");
                                SetHelper.dataManager.ProductCheckIn((hardIndex + 1).ToString(), str.Trim());
   
                                if (!SetHelper.IsOpen[hardIndex])
                                {
                                    SetHelper.ListScanMessage.ShowInfoQueue($"{stationName} 工位未开启(IsOpen=false),不触发进站");
                                }

                            }
                            else
                            {
                                SetHelper.ListScanMessage.ShowInfoQueue($"{stationName} 扫码SN码窗口已经关闭,扫码不触发进站");
                            }
                        }
                        if (str.Trim().Length != SetHelper.MesSetting.ListGroup[hardIndex].SNCodeLen && DataManager.IsOP1010ViewOpen && !stationName.ToUpper().Contains("OP2030"))
                        {
                            SetHelper.ListScanMessage.ShowInfoQueue($"{stationName} 扫码长度:{str.Trim().Length} 与设定SN码长度:{SetHelper.MesSetting.ListGroup[hardIndex].SNCodeLen}不符,不触发进站");
                        }
                    }
                }
                SetHelper.ListScanMessage.ShowInfoQueue(stationName + $" 6=>{SetHelper.MesSetting.ListGroup[hardIndex].FeedingSNCodeLen} {SetHelper.MesSetting.ListGroup[hardIndex].SNCodeLen} {!SetHelper.IsOpen[hardIndex]}");
                //OP3030等直接扫码FeedingCheck，判断当不需要进站，但需要FeedingCheck时，直接触发FeedingCheck 如果设置不需要feedcheck直接给码则直接发码
                if (SetHelper.MesSetting.ListGroup[hardIndex].FeedingSNCodeLen > 0 && SetHelper.MesSetting.ListGroup[hardIndex].SNCodeLen <= 0
                        && !SetHelper.IsOpen[hardIndex])//如果linkcomp中，则不触发
                {
                    if (str.Trim().Length == SetHelper.MesSetting.ListGroup[hardIndex].FeedingSNCodeLen)
                    {
                        string rule = SetHelper.MesSetting.ListGroup[hardIndex].CodeRule;
                        if (!string.IsNullOrEmpty(rule))
                        {
                            bool isCodeOK = str.Length >= rule.Length && str.Substring(0, rule.Length) == rule;
                            if (!isCodeOK)
                            {
                                MessageBox.Show($"{str} 条码规则不符合 {rule}！");
                                return;
                            }
                        }

                        var x2 = Task.Run(async () =>
                         {
                             if (SetHelper.MesSetting.ListGroup[hardIndex].IsGivePlcCode == "1")
                             {
                                 //将扫码结果发送给PLC
                                 bool result1 = false;

                                 result1 = SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "扫描材料码_" + (hardIndex + 1).ToString(), str.Trim());

                                 SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 扫描材料码写{str.Trim()},{(result1 ? "成功" : "失败")}");

                                 //如果是OP3021工位，扫完油底壳码后,不校验油底壳码，需要将OP3030的胶水码进行FeedingCheck校验，给PLC完成信号
                                 if (stationName.Contains("OP3021"))
                                 {
                                     //string jsonGlue = File.ReadAllText(SetHelper.gluepath);
                                     //ObservableCollection<MaterailModel> glueMaterails = JSON.FromJson<ObservableCollection<MaterailModel>>(jsonGlue);
                                     ObservableCollection<MaterailOnOffModel> glueMaterails = SetHelper.ReadSys<ObservableCollection<MaterailOnOffModel>>(SetHelper.gluepath);
                                     foreach (var item in glueMaterails)
                                     {
                                         if (!string.IsNullOrEmpty(item.GlueCode) && item.LocationNo.ToUpper().Contains("OP3030") && stationName.Contains("OP3021"))
                                         {
                                            await  SetHelper.dataManager.FeedingCheckAsync((hardIndex + 1).ToString(), item.GlueCode.Trim());
                                         }
                                     }
                                     return;//直接返回
                                 }
                                 bool result = false;
                                 result = SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "检查材料合法性结果_" + (hardIndex + 1).ToString(), result1 == true ? 1 : 2);
                                 SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 材料合法性校验结果写{result1},{(result ? "成功" : "失败")}");
                             }
                             else
                             {
                                 //将扫码结果发送给PLC
                                 bool result1 = false;

                                 result1 = SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "扫描材料码_" + (hardIndex + 1).ToString(), str.Trim());
                                 SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 扫描材料码写{str.Trim()},{(result1 ? "成功" : "失败")}", true, EnumLogType.log.ToString(), result1);
                                 await SetHelper.dataManager.FeedingCheckAsync((hardIndex + 1).ToString(), str.Trim());
                             }
                         });
                    }
                    else
                    {
                        SetHelper.ListScanMessage.ShowInfoQueue($"{stationName} 扫码长度:{str.Trim().Length} 与材料码长度:{SetHelper.MesSetting.ListGroup[hardIndex].FeedingSNCodeLen}不符,不写给PLC或触发FeedingCheck");
                    }
                }
            }
        }


        private static IMiscService CreateMiscService()
        {
            try
            {
                if (App.Container != null)
                {
                    return App.Container.Resolve<IMiscService>();
                }
            }
            catch
            {
                throw;
            }

            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configs"));
            return new MiscService();
        }





    }
}
