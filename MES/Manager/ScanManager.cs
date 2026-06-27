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

        private Dictionary<string, string> ruleDict = new Dictionary<string, string>();

        private readonly Dictionary<string, int> stationIndexMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> ambiguousPrefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public ScanManager()
        {
            miscService = CreateMiscService();
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string filePath = Path.Combine(baseDirectory, "misc.json");
            if(File.Exists(filePath))
            {
                try
                {
                    string jsonStr = File.ReadAllText(filePath, System.Text.Encoding.UTF8);
                    JObject jo = JObject.Parse(jsonStr);
                    ruleDict = jo["扫码匹配规则"]?.ToObject<Dictionary<string, string>>() ?? ruleDict;

                }
                catch (Exception ex)
                {
                    SetHelper.ListOEEMessage.ShowInfoQueue(ex.Message);
                }
            }
            SNprefixMapStations();

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
            string stationName = "";


            SetHelper.ListScanMessage.ShowInfoQueue(stationName + " 扫描到条码为" + str.ToString());


            int stationIndex = ResolveStationIndex(hardIndex,str);
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
                OnScanOpenBox?.Invoke(str);//将各个页面的码更新
                //}

                if (stationName.ToUpper().Contains("OP1030"))
                {
                    SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "定子码_" + (hardIndex + 1).ToString(), str.Trim());
                }
 

                //部分工位例如OP1010直接扫码进站，用码的长度来识别是否为进站的码，内部包含是否需要FeedingCheck
                if (SetHelper.MesSetting.ListGroup[hardIndex].SNCodeLen > 0)
                {
                    SetHelper.ListScanMessage.ShowInfoQueue(stationName + " 2=>");
                    if (stationName.ToUpper().Contains("OP1010") || stationName.ToUpper().Contains("OP3040")
                        || stationName.ToUpper().Contains("OP4030")|| stationName.ToUpper().Contains("OP2020")
                        || stationName.ToUpper().Contains("OP2030") || stationName.ToUpper().Contains("1NG_IO"))
                    {
                        SetHelper.ListScanMessage.ShowInfoQueue(stationName + " 3=>");
                        SetHelper.ListScanMessage.ShowInfoQueue(stationName + $" 5=>{hardIndex} {str.Trim().Length} {SetHelper.MesSetting.ListGroup[hardIndex].SNCodeLen}");
                        if (str.Trim().Length == SetHelper.MesSetting.ListGroup[hardIndex].SNCodeLen)
                        {    //弹窗未关闭
                            if (DataManager.IsOP1010ViewOpen)
                            {
                                if (str.Trim().Length == SetHelper.MesSetting.ListGroup[hardIndex].SNCodeLen)
                                {
                                    SetHelper.ListScanMessage.ShowInfoQueue(stationName + $" 4=>{str}");
                                    SNCode = str;

                                    if (!CanTriggerScanCheckIn(stationName, hardIndex))
                                    {
                                        return;
                                    }

                                    if (stationName.ToUpper().Contains("OP2020M") || stationName.ToUpper().Contains("OP2030M"))
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


                                    SetHelper.dataManager.Siemens_OnDataChange(
                                        "产品进站启动" + "_" + (hardIndex + 1).ToString(),
                                        0,
                                        0,
                                        true);

                                }
                                else
                                {
                                    if (!SetHelper.IsOpen[hardIndex])
                                    {
                                        SetHelper.ListScanMessage.ShowInfoQueue($"{stationName} 扫码长度:{str.Trim().Length} 与设定SN码长度:{SetHelper.MesSetting.ListGroup[hardIndex].SNCodeLen}不符,不触发进站");
                                    }
                                }
                            }
                            else
                            {
                                SetHelper.ListScanMessage.ShowInfoQueue($"{stationName} 扫码SN码窗口已经关闭,扫码不触发进站");
                            }
                        }
                        if (str.Trim().Length != SetHelper.MesSetting.ListGroup[hardIndex].SNCodeLen && DataManager.IsOP1010ViewOpen)
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
                    int feedingResult = 2;
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
            }

            Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "configs"));
            return new MiscService();
        }

        private bool CanTriggerScanCheckIn(string stationName, int hardIndex)
        {
            if (!ContainsStation(stationName, "OP2020") && !ContainsStation(stationName, "OP2030"))
            {
                return true;
            }

            object obj = null;
            string tagName = "PLC进站流程ID_" + (hardIndex + 1);
            if (!SetHelper.siemens.ReadItem(PLCGroupName.ReadGroup, tagName, ref obj))
            {
                SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读取{tagName}失败,不触发进站");
                return false;
            }

            if (!IsPlcCheckInActive(obj))
            {
                SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} {tagName}为{obj},不触发进站");
                return false;
            }

            return true;
        }

        private static bool IsPlcCheckInActive(object obj)
        {
            if (obj == null)
            {
                return false;
            }

            if (obj is bool boolValue)
            {
                return boolValue;
            }

            string value = obj.ToString()?.Trim() ?? string.Empty;
            if (int.TryParse(value, out int numberValue))
            {
                return numberValue != 0;
            }

            return bool.TryParse(value, out bool parsedValue) && parsedValue;
        }

        private bool IsSpecialStationPc(string stationName)
        {
            string firstStationName = SetHelper.StationNumber.numberGroups.FirstOrDefault()?.Name ?? string.Empty;
            return ContainsStation(firstStationName, stationName);
        }

        private int ResolveStationIndex(int hardIndex, string scanStr)
        {
            bool isOp2020 = IsSpecialStationPc("OP2020");
            bool isOp2030 = IsSpecialStationPc("OP2030");
            if (!isOp2020 && !isOp2030)
            {
                return hardIndex;
            }

            if (stationIndexMap.Count == 0)
            {
                SetHelper.ListScanMessage.ShowInfoQueue("SN前缀映射未配置,使用扫码枪序号匹配工站");
                return IsValidStationIndex(hardIndex) ? hardIndex : -1;
            }

            if (!TryResolvePrefixStation(scanStr, out int mappedIndex))
            {
                SetHelper.ListScanMessage.ShowInfoQueue($"扫码{scanStr}未匹配到SN前缀,不触发进站");
                return -1;
            }

            int stationIndex = mappedIndex;
            if (isOp2030 && hardIndex == 1)
            {
                stationIndex = mappedIndex - 1;
            }

            if (!IsValidStationIndex(stationIndex))
            {
                SetHelper.ListScanMessage.ShowInfoQueue($"扫码{scanStr}解析到工站序号{stationIndex + 1}无效,不触发进站");
                return -1;
            }

            return stationIndex;
        }

        private bool TryResolvePrefixStation(string scanStr, out int stationIndex)
        {
            foreach (var prefix in ambiguousPrefixes)
            {
                if (scanStr.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    SetHelper.ListScanMessage.ShowInfoQueue($"SN前缀{prefix}配置重复,无法判断工站");
                    stationIndex = -1;
                    return false;
                }
            }

            foreach (var kvp in stationIndexMap.OrderByDescending(x => x.Key.Length))
            {
                if (scanStr.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
                {
                    stationIndex = kvp.Value;
                    return true;
                }
            }

            stationIndex = -1;
            return false;
        }

        private static bool IsValidStationIndex(int stationIndex)
        {
            return stationIndex >= 0 && stationIndex < SetHelper.StationNumber.numberGroups.Count;
        }

        private static bool ContainsStation(string stationName, string key)
        {
            return stationName?.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void SNprefixMapStations()
        {
            foreach (var Sn in miscService.SNPrefixes ?? new ObservableCollection<SNPrefix>())
            {
                if (string.IsNullOrWhiteSpace(Sn.Name) || string.IsNullOrWhiteSpace(Sn.Value)) continue;

                // 包含 outputShaft 映射为 0
                if (Sn.Name.Contains("outputShaft"))
                {
                    AddStationPrefix(Sn.Value, 0);
                }
                // 包含 differential 或 inputShaft 映射为 1
                else if (Sn.Name.Contains("differential") || Sn.Name.Contains("inputShaft"))
                {
                    AddStationPrefix(Sn.Value, 1);
                }
                // 包含 intermediateShaft 映射为 3
                else if (Sn.Name.Contains("intermediateShaft"))
                {
                    AddStationPrefix(Sn.Value, 3);
                }
            }
        }

        private void AddStationPrefix(string prefix, int stationIndex)
        {
            prefix = prefix.Trim();
            if (stationIndexMap.TryGetValue(prefix, out int existingIndex))
            {
                if (existingIndex != stationIndex)
                {
                    stationIndexMap.Remove(prefix);
                    ambiguousPrefixes.Add(prefix);
                    SetHelper.ListScanMessage.ShowInfoQueue($"SN前缀{prefix}同时配置到工站{existingIndex + 1}和工站{stationIndex + 1},已禁用该前缀");
                }
                return;
            }

            if (!ambiguousPrefixes.Contains(prefix))
            {
                stationIndexMap[prefix] = stationIndex;
            }
        }
    }
}
