using MES.Comm;
using MES.MesModel.Request;
using MES.MesModel.Response;
using MES.SetModel;
using MES.View;
using MES.ViewModel;
using S7.Net.Types;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using static MES.Extension;
using DateTime = System.DateTime;
namespace MES.Manager
{
    public partial class DataManager
    {
        public async Task DataCollectoinAsync(string stationNumber)
        {
            int iNumber = Convert.ToInt32(stationNumber) - 1;
            string stationName = SetHelper.StationNumber.numberGroups[iNumber].Name;

            #region 读SN

            object obj = new object();
            string SN = "";
            if (SetHelper.siemens.ReadItem(PLCGroupName.WriteGroup, "扫描材料码_" + stationNumber, ref obj))
            {
                SN = obj.Obj2String();
                SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读到扫描材料码_2为{SN}");
            }
            else
            {
                SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读扫描材料码_2失败");
            }

            #endregion 读SN

            object data = new object();
            List<DCData> dcInfoList = new List<DCData>();
            List<CompList> compList = new List<CompList>();

            var dic = SetHelper.siemens.DicDataItems[PLCGroupName.DataCollectionGroup.ToString()];//<TagName,DataItem>
                                                                                                  //读取对应工位的参数
            dic = dic.Where(it => it.Key.Contains("_" + stationNumber)).ToDictionary(it => it.Key, it => it.Value);
            if (dic.Count != 0)
            {
                List<string> TagNameList = dic.Keys.ToList();
                var dataItems = dic.Select(x => x.Value).ToArray();
                if (!SetHelper.siemens.ReadItems(dataItems, ref data))
                {
                    SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读取DataCollection数据失败！");
                }
                if (data is object[] datarray)
                {
                    for (int i = 0; i < datarray.Length; i++)
                    {
                        SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读到{TagNameList[i]}为{datarray[i]}");

                        DCData dC_Info = new DCData()
                        {
                            Item = TagNameList[i].Substring(0, TagNameList[i].LastIndexOf('_')),//去掉下划线
                            Value = datarray[i].ToString(),
                        };
                        dcInfoList.Add(dC_Info);
                    }
                }
            }

            #region 数据收集上传

            int checkInResult = 1;//mes校验结果
            (bool, string) response = await SetHelper.mesManager.DataCollection(SN.GetDataCollectionModel(dcInfoList.ToArray(), compList.ToArray(), iNumber), iNumber);
            checkInResult = response.Item1 ? 1 : 2;//1成功2失败
            SetHelper.ListMesMessage.ShowInfoQueue($"{stationName} {SN}--数据上传结果：{response.Item1},消息：{response.Item2},产品码{SN}");

            #endregion 数据收集上传

            #region 判断结果发送PLC

            bool result = false;//发plc结果
            result = SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "数据上传结果_" + stationNumber, checkInResult);
            SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} {SN}--数据上传结果{checkInResult}{(result ? "成功" : "失败")}");

            #endregion 判断结果发送PLC
        }

        public async Task SNCarrierBindAsync(EnumBindType enumBindType, string stationNumber)
        {
            int iNumber = Convert.ToInt32(stationNumber) - 1;
            string stationName = SetHelper.StationNumber.numberGroups[iNumber].Name;
            if (enumBindType == EnumBindType.UNBIND)
            {
                #region 读载具码

                object obj = new object();
                string carryID = "";
                if (SetHelper.siemens.ReadItem(PLCGroupName.ReadGroup, "解绑载具码_" + stationNumber, ref obj))
                {
                    carryID = obj.Obj2String();
                    SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读到解绑载具码为{carryID}");
                }
                else
                {
                    SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读解绑载具码失败");
                }

                #endregion 读载具码

                #region 解绑

                int checkInResult = 1;//mes校验结果
                (bool, string) response = await SetHelper.mesManager.CarrierBind(carryID.GetCarrierBindModel(iNumber, enumBindType), iNumber);
                checkInResult = response.Item1 ? (response.Item2 != "" ? 1 : 2) : 2;//1成功2失败
                SetHelper.ListMesMessage.ShowInfoQueue($"{stationName}  {carryID}--产品解绑结果：{response.Item1},消息：{response.Item2}");
                string SN = response.Item2;

                #endregion 解绑

                #region 判断结果发送PLC

                bool result = false;//发plc结果
                result = SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "解绑SN_" + stationNumber, SN);
                SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName}  {carryID}--解绑SN写{SN}{(result ? "成功" : "失败")}");
                result = SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "解绑结果_" + stationNumber, checkInResult);
                SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName}--解绑结果写{checkInResult}{(result ? "成功" : "失败")}");
                //弹窗提示有新增绿色文字
                if (!response.Item1 && stationName.ToUpper().Contains("NG_IO"))
                {
                    string msg0 = $"{stationName} 载具码{carryID} SN码:{SN}-产品解绑结果：{response.Item1},\r\n\r\nMES反馈的消息：{response.Item2}";
                    await Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (SetHelper.IsMsgWindowOpen)
                        {
                            popupWindow.Close();
                        }
                        popupWindow = new PopupWindow(msg0);
                        popupWindow.Show();
                    });
                }
                else
                {
                    string msg0 = $"{stationName} 载具码{carryID} SN码:{SN}-产品解绑结果：{response.Item1},\r\n\r\nMES反馈的消息：{response.Item2}";
                    await Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (SetHelper.IsMsgGreenWindowOpen)
                        {
                            popupWindowGreen.Close();
                        }
                        popupWindowGreen = new PopupWindowGreen(msg0);
                        popupWindowGreen.Show();
                    });
                }
                #endregion
            }
            else
            {
                #region 读载具码

                object obj = new object();
                string carryID = "";
                if (SetHelper.siemens.ReadItem(PLCGroupName.ReadGroup, "绑定载具码_" + stationNumber, ref obj))
                {
                    carryID = obj.Obj2String();
                    SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读到绑定载具码为{carryID}");
                }
                else
                {
                    SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读绑定载具码失败");
                }

                #endregion 读载具码

                #region 读SN

                string SN = "";
                if (SetHelper.siemens.ReadItem(PLCGroupName.ReadGroup, "绑定SN_" + stationNumber, ref obj))
                {
                    SN = obj.Obj2String();
                    SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读到绑定SN为{SN}");
                }
                else
                {
                    SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读绑定SN失败");
                }

                #endregion 读SN

                #region 绑定

                int checkInResult = 1;//mes校验结果
                (bool, string) response = await SetHelper.mesManager.CarrierBind(carryID.GetCarrierBindModel(iNumber, enumBindType, SN), iNumber);
                checkInResult = response.Item1 ? 1 : 2;//1成功2失败
                SetHelper.ListMesMessage.ShowInfoQueue($"{stationName} {carryID}--产品绑定结果：{response.Item1},消息：{response.Item2},\r\n\r\n产品码{SN}");
                if (!response.Item1 && stationName.ToUpper().Contains("NG_IO"))
                {
                    string msg0 = $"{stationName} 载具码{carryID} SN码:{SN}-产品绑定结果：{response.Item1},\r\n\r\nMES反馈的消息：{response.Item2}";
                    await Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (SetHelper.IsMsgWindowOpen)
                        {
                            popupWindow.Close();
                        }
                        popupWindow = new PopupWindow(msg0);
                        popupWindow.Show();
                    });
                }
                else
                {
                    string msg0 = $"{stationName} 载具码{carryID} SN码:{SN}-产品绑定结果：{response.Item1},\r\n\r\nMES反馈的消息：{response.Item2}";
                    await Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (SetHelper.IsMsgGreenWindowOpen)
                        {
                            popupWindowGreen.Close();
                        }
                        popupWindowGreen = new PopupWindowGreen(msg0);
                        popupWindowGreen.Show();
                    });
                }
                #endregion

                #region 判断结果发送PLC

                bool result = false;//发plc结果
                result = SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "绑定结果_" + stationNumber, checkInResult);
                SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} {carryID}--绑定结果写{checkInResult}{(result ? "成功" : "失败")}");

                #endregion 判断结果发送PLC
            }
        }

        public async void GetWeight(string stationNumber)
        {
            int iNumber = Convert.ToInt32(stationNumber) - 1;
            string stationName = SetHelper.StationNumber.numberGroups[iNumber].Name;

            #region 读SN

            object obj = new object();
            string SN = "";
            if (SetHelper.siemens.ReadItem(PLCGroupName.WriteGroup, "产品SN_" + stationNumber, ref obj))
            {
                SN = obj.Obj2String();
                SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读到产品SN_{stationNumber}为{SN}");
            }
            else
            {
                SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读产品SN__{stationNumber}失败");
            }

            #endregion 读SN

            #region 获取数据

            int checkInResult = 1;//mes校验结果
            (bool, string, string) response = await SetHelper.mesManager.CarrierCheck(SN.GetCarrierCheckModel(iNumber), iNumber);
            checkInResult = response.Item1 ? 1 : 2;//1成功2失败
            SetHelper.ListMesMessage.ShowInfoQueue($"{stationName} {SN}--获取重量结果：{response.Item1},消息：{response.Item2},重量：{response.Item3},产品码{SN}");

            #endregion 获取数据

            #region 判断结果发送PLC

            bool result = false;//发plc结果
            result = SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "重量_" + stationNumber, response.Item3.ObjToFloat());
            SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} {SN}--写重量{response.Item3.ObjToFloat()}{(result ? "成功" : "失败")}");

            #endregion 判断结果发送PLC
        }

        /// <summary>
        /// 切型  触摸屏点击切型→软件请求MES→MES返回产品型号→软件切换型号→发送产品型号到PLC→PLC切型
        /// </summary>
        /// <param name="Number"></param>
        /// <returns></returns>
        public async void ChangeProductType(int ProductType, int Number)
        {
            var Type = SetHelper.GetProductType(ProductType);
            if (Type == null)
            {
                SetHelper.ListMesMessage.ShowInfoQueue($"未找到产品型号为{ProductType}的型号，切换失败，请检查是否配置", true, EnumLogType.log.ToString(), true);
                return;
            }
            ObservableCollection<MaterailModel> materails = SetHelper.ReadSys<ObservableCollection<MaterailModel>>(SetHelper.materialpath);

            #region 型号切换时,检查批追物料

            if (!SetHelper.IsFirstStart && SetHelper.NowProduct.ProductID != ProductType && materails != null && !SetHelper.StationNumber.numberGroups.FirstOrDefault().Name.Contains("OP3040"))
            {
                foreach (var item in materails)
                {
                    if (item.GlueCode == "")
                    {
                        continue;
                    }

                    string stationName = item.LocationNo;
                    Thread.Sleep(200);
                    int stationNumber = SetHelper.StationNumber.numberGroups.FirstOrDefault(x => x.Name == stationName).Number - 1;

                    var result = await SetHelper.mesManager.CompSNChange(item.GlueCode.GetCompSNChangeModel(stationNumber), stationNumber);

                    if (result.Item1 == 1)
                    {
                        SetHelper.ListMesMessage.ShowInfoQueue($"批追物料校验一致");
                    }
                    else if (result.Item1 == 2)
                    {
                        SetHelper.ListMesMessage.ShowInfoQueue($"批追物料{item.GlueCode}校验不一致:{result.Item2}");

                        var offlineResult = await SetHelper.mesManager.CompSNOffline(item.GlueCode.GetCompSNOffline(stationNumber, item.LightNumber), stationNumber);

                        if (offlineResult.Item1)
                        {
                            SetHelper.ListMesMessage.ShowInfoQueue($"批追物料{item.GlueCode}下料成功");
                            MaterialOfflineAction(item.GlueCode);
                        }
                        else
                        {
                            SetHelper.ListMesMessage.ShowInfoQueue($"批追物料{item.GlueCode}下料失败,请手动下料");
                        }
                    }
                    else
                    {
                        SetHelper.ListMesMessage.ShowInfoQueue($"批追物料校验接口调用异常，请检查网络。");
                    }
                }
            }

            #endregion 型号切换时,检查批追物料

            SetHelper.InitializedSetting(ProductType);
            SetHelper.StartOk = true;
            GetStatusInfo();
            ChangeParamsAction(Type);
            FormulaSend();

            SetHelper.IsFirstStart = false;
        }

        public bool FormulaSend()
        {
            try
            {
                //发送给PLC，让PLC切型

                #region 发送配方数据到PLC

                if (!SetHelper.siemens.DicDataItems.ContainsKey(PLCGroupName.FormulaGroup.ToString())) return false;
                var dic = SetHelper.siemens.DicDataItems[PLCGroupName.FormulaGroup.ToString()].ToDictionary(it => it.Key, it => it.Value);//<TagName,DataItem>

                if (dic.Count > 0)
                {
                    List<string> TagNameList = dic.Keys.ToList();
                    var dataItems = dic.Select(x => x.Value).ToArray();
                    List<object> dataValues = new List<object>();
                    foreach (var item in TagNameList)
                    {
                        dataValues.Add(SetHelper.siemens.FormulaData[item]);
                    }

                    if (!SetHelper.siemens.WriteItems(dataItems, dataValues.ToArray()))
                    {
                        SetHelper.ListPLCMessage.ShowInfoQueue($"写入配方数据失败！");
                    }
                    else
                    {
                        for (int i = 0; i < dataItems.Length; i++)
                        {
                            SetHelper.ListPLCMessage.ShowInfoQueue($"{TagNameList[i]}写{dataValues[i]}完成");
                        }
                        SetHelper.ListPLCMessage.ShowInfoQueue($"写入配方数据成功！");
                        return true;
                    }
                }

                #endregion 发送配方数据到PLC
            }
            catch (Exception ex)
            {
                SetHelper.ListPLCMessage.ShowInfoQueue(ex.ToString());
            }
            return false;
        }

        /// <summary>
        /// 通过MES获取产品型号
        /// </summary>
        /// <param name="Number"></param>
        /// <returns>（接口调用结果，产品型号）</returns>
        public async Task<(bool, int)> GetProductFromMES(int Number)
        {
            Number = Number - 1;
            (bool, int) res = (false, 0);
            string message = "";
            try
            {
                var response = await SetHelper.mesManager.ChangeProductType(Number.GetChangeProductTypeModel(), Number);
                if (response.Item1)//MES返回成功
                {
                    string[] result = response.Item2.Split(',');
                    if (result.Length == 3)//返回的信息规则正常
                    {
                        var productType = SetHelper.products.FirstOrDefault(x => x.ProductName == result[1].ToString());
                        if (productType != null)//与本地配置的产品型号能匹配上
                        {
                            res.Item1 = response.Item1;
                            res.Item2 = productType.ProductID;
                            message = $"获取产品型号成功:{response.Item2}，产品型号：{result[1]}";

                            SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "产品型号_1", res.Item2);
                            SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "产品型号结果_1", res.Item1);
                        }
                        else
                        {
                            message = $"未匹配到产品型号:{result[1]}";
                        }
                    }
                    else
                    {
                        message = "获取产品型号时，MES返回信息规则不符";
                    }
                }
                else
                {
                    message = $"获取产品型号失败:{response.Item3 ?? ""}";
                }
                SetHelper.ListMesMessage.ShowInfoQueue(message);

                //校验失败了
                if (!response.Item1)
                {
                    await Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (SetHelper.IsMsgWindowOpen)
                        {
                            popupWindow.Close();
                        }
                        popupWindow = new PopupWindow(message);
                        popupWindow.Show();
                    });
                }
            }
            catch (Exception ex)
            {
                SetHelper.ListMesMessage.ShowInfoQueue(ex.ToString());
            }
            return res;
        }

        public async Task StatusUpLoad(string number, int statusValue)
        {
            int iNumber = Convert.ToInt32(number) - 1;

            //更新当前工位的状态
            status[iNumber] = statusValue;
            //SetHelper.ListPLCMessage.ShowInfoQueue("收到PLC设备状态信号:" + statusValue);
            //1. Run  运行
            //2.Stop  停机
            //3.Maintain  保养
            //4.Idle  
            //5.Standby  

            //设备状态变化发布
            StatusManager.PublishMessage(status);
        }
        /*
         多个工位只上传一个机器的错误原因如下；
        大多数情况下，Status=1，即正常运行
        机器正常运行时，第一次必然走else if (LastStatus != Status)分支，读取工位1的状态，更新LastStatus = 1
        但是当读取工位2的状态时，由于三个分支都不满足，直接跳过，开启下一轮循环，继续读取工位1的状态
        但是由于未满足15秒计时，for循环一直空转，直到第工位1满足计时，触发 else if (sw.ElapsedMilliseconds > 15 * 1000)分支
        然后会重置计时器，又开始以上for循环空转，永远无法上传工位2状态。
         */
        // 1. 在类级别（全局）声明一个 CancellationTokenSource 变量
        // 1. 在类级别（全局）声明状态监控专用的取消令牌源
        private CancellationTokenSource _statusTokenSource;

        /// <summary>
        /// 设备状态自动上传MES（深度重构版）
        /// 深度融合 ListenPLC 的防抖与跳变即时触发思想，工位线程并发隔离，彻底解决急停上报延迟 Bug
        /// </summary>
        public void GetStatusInfo()
        {
            // 核心安全逻辑：如果之前已经有任务在运行，先发送取消请求并释放，防止切型或重载时线程重叠泄漏
            if (_statusTokenSource != null)
            {
                _statusTokenSource.Cancel(); // 通知所有正在运行的工位监控子线程安全退出
                _statusTokenSource.Dispose();
            }

            // 实例化全新的令牌源
            _statusTokenSource = new CancellationTokenSource();
            CancellationToken token = _statusTokenSource.Token;

            Task.Run(async () =>
            {
                int flag = 0;
                while (flag <= 10 && !SetHelper.StartOk)
                {
                    if (token.IsCancellationRequested) return;
                    await Task.Delay(500);
                    SetHelper.ListOEEMessage.ShowInfoQueue("等待配置加载完成，才能获取设备状态信息...");
                    flag++;
                }

                // 配置加载完成后，获取实际的工位总数
                int count = SetHelper.StationNumber.numberGroups.Count;
                SetHelper.ListOEEMessage.ShowInfoQueue($"状态上传功能已启动，监控工位数量：{count}");

                // 为各个工位声明独立的已发送警报缓存
                string[] lastSentAlarms = new string[count];
                for (int i = 0; i < count; i++)
                {
                    lastSentAlarms[i] = "";
                }

                // 为每一个工位平行启动一个专属监控线程
                for (int i = 0; i < count; i++)
                {
                    int stationIndex = i;
                    _ = StartIndividualStationMonitorAsync(stationIndex, lastSentAlarms, token);
                }

            }, token);
        }

        /// <summary>
        /// 专属工位状态高效防抖与监控线程（按 ListenPLC 状态机标准重写）
        /// </summary>
        private Task StartIndividualStationMonitorAsync(int iNumber, string[] lastSentAlarms, CancellationToken token)
        {
            return Task.Run(async () =>
            {
                string stationName = SetHelper.StationNumber.numberGroups[iNumber].Name;
                string tagItem = "设备运行状态_" + (iNumber + 1);

                // 核心通信及控制参数（可根据项目实际需求微调）
                int heartbeatInterval = 15 * 1000; // 客户要求的 15 秒定时心跳周期
                int pollInterval = 200;           // 高频采样间隔 200ms
                int debounceDuration = 800;       // 防抖稳定所需时间 800ms
                int requiredCount = Math.Max(1, debounceDuration / pollInterval); // 稳定所需的连续相同采样次数

                int? lastConfirmedValue = null;   // 最终成功上报给 MES 端的、已确认的稳定状态旧值
                int? pendingValue = null;          // 当前处于防抖观测期的候选状态值
                int stableCount = 0;               // 连续相同值计数器

                Stopwatch heartbeatWatch = new Stopwatch();
                heartbeatWatch.Start();

                // 初始化时错开各工位的心跳上报时间（例如工位1加0s，工位2加2s），防止同一秒内由于并发请求导致上位机网卡瞬时堵塞
                int initialOffset = (SetHelper.StationNumber.numberGroups.Count - iNumber - 1) * 2000;

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        object temp = null;
                        // 从 PLC 中高频同步读取该工位状态
                        bool readSuccess = SetHelper.siemens.ReadItem(PLCGroupName.TriggerGroup, tagItem, ref temp);

                        if (readSuccess)
                        {
                            int intValue = temp.Obj2Int();

                            if (intValue != 0)
                            {
                                // 防抖
                                if (pendingValue.HasValue && intValue == pendingValue.Value)
                                {
                                    stableCount++;
                                    // 信号持续稳定时间达到了 requiredCount (800ms)
                                    if (stableCount >= requiredCount)
                                    {
                                        // 检查这个稳定的新状态是否与上一次成功上报的值不同
                                        if (intValue != lastConfirmedValue)
                                        {
                                            lastConfirmedValue = intValue;

                                            // 极速沿响应：检测到状态真正切换（如 1运行->2停止），立刻绕过心跳计时上报MES！
                                            SetHelper.ListOEEMessage.ShowInfoQueue($"工位 {stationName} 检测到状态安全跳变至: {intValue}");

                                            await UploadStatusAlarmInfoAsync(intValue, iNumber, lastSentAlarms);

                                            // 跳变上报成功后，重置心跳计时器，重新开始计算下一个15秒
                                            heartbeatWatch.Restart();
                                            initialOffset = 0; // 发生跳变后，清理首次启动产生的偏移
                                        }
                                    }
                                }
                                else
                                {
                                    // 状态发生改变，重新开始防抖技术确认
                                    pendingValue = intValue;
                                    stableCount = 1;
                                }
                            }
                        }
                        else
                        {
                            SetHelper.ListPLCMessage.ShowInfoQueue($"读取PLC节点 {tagItem} 失败", false, "PLC_Error");
                        }

                        //定时心跳上报逻辑
                        if (heartbeatWatch.ElapsedMilliseconds + initialOffset > heartbeatInterval)
                        {
                            // 优先选取已经防抖确认的稳定状态，如果没有，则拿 pendingValue 做保底，确保不会漏发
                            int statusToSend = lastConfirmedValue ?? (pendingValue ?? 0);

                            if (statusToSend != 0)
                            {
                                // SetHelper.ListOEEMessage.ShowInfoQueue($"[定时心跳] 工位 {stationName} 触发 15 秒定时状态上报 (当前状态: {statusToSend}).");
                                await UploadStatusAlarmInfoAsync(statusToSend, iNumber, lastSentAlarms);
                            }

                            heartbeatWatch.Restart();
                            initialOffset = 0; // 清理首次启动产生的偏移
                        }

                        // 进入高频采样的下一次标准延迟
                        await Task.Delay(pollInterval, token);
                    }
                    catch (OperationCanceledException)
                    {
                        break; // 收到取消请求，平稳退出 while 循环
                    }
                    catch (Exception ex)
                    {
                        SetHelper.ListMesMessage.ShowInfoQueue($"工位 {stationName} 状态监控灾难异常: " + ex.Message, true);
                        // 通信异常时延迟 2 秒重试，防止 PLC 报错或断网时出现死循环刷屏导致 CPU 占满
                        await Task.Delay(2000, token);
                    }
                }

                SetHelper.ListOEEMessage.ShowInfoQueue($"工位 {stationName} 的独立状态监控线程已安全退出。");
            }, token);
        }

        // 改变了函数签名，新增了一个string警报数组参数
        public async Task UploadStatusAlarmInfoAsync(int status, int iNumber, string[] lastAlarms, bool AlarmChange = false)
        {

            object o = new object();
            bool res = SetHelper.siemens.ReadItem(PLCGroupName.ReadGroup, "设备报警_" + (iNumber + 1), ref o);
            string currentAlarm = o.Obj2String();
            await SetHelper.mesManager.EQStatus((status == 2 ? (status.ToString() + "_" + currentAlarm) : status.ToString()).GetStatusModel(iNumber), iNumber);

            if (status != 2) return; 

            // 处理报警逻辑
            object obj = new object();
            bool result = SetHelper.siemens.ReadItem(PLCGroupName.ReadGroup, "设备报警_" + (iNumber + 1), ref obj);
            string currentAlarmId = obj.Obj2String();
            // 使用 lastAlarms[iNumber] 比较，确保每个工位数据独立
            if (result && !string.IsNullOrEmpty(currentAlarmId) && lastAlarms[iNumber] != currentAlarmId)
            {
                await SetHelper.mesManager.EQAlarm(currentAlarmId.GetAlarmModel(iNumber), iNumber);
                lastAlarms[iNumber] = currentAlarmId; // 记录该工位已经发送过的报警
            }
        }

        private Dictionary<int, int> NumberAndTime = new Dictionary<int, int>();

        /// <summary>
        /// 设备状态自动上传MES-客户建议15秒上传一次
        /// </summary>
        public void UploadStatusStart()
        {
            try
            {
                Stopwatch[] sws = new Stopwatch[SetHelper.StationNumber.numberGroups.Count];

                int[] Status_old = new int[SetHelper.StationNumber.numberGroups.Count];

                for (int i = 0; i < SetHelper.StationNumber.numberGroups.Count; i++)
                {
                    int time = SetHelper.MesSetting.ListGroup[i].StatusUploadTime;
                    if (time == 0)
                    {
                        continue;//0秒则不开启当前工位上传功能
                    }
                    NumberAndTime.Add(i, time);
                    sws[i] = new Stopwatch();
                    sws[i].Restart();
                }
                //开启上传的工位循环上传，默认每个工位间隔时间相同
                Task.Run(async () =>
                {
                    while (NumberAndTime.Count > 0)
                    {
                        Thread.Sleep(10);
                        if (SetHelper.StartOk)
                        {
                            Parallel.ForEach(NumberAndTime, async item =>
                            {
                                if (status[item.Key] == Status_old[item.Key] && status[item.Key] != 0)//如果和上一次循环的值一样
                                {
                                    if (sws[item.Key].ElapsedMilliseconds > item.Value * 1000) //时间大于设定值
                                    {
                                        //上传
                                        // (bool, string) reuslt = await SetHelper.mesManager.EQStatus(StatusModelHelper.GetStatusModel(status[item.Key].ToString(), item.Key), item.Key);
                                        // await UploadStatus(item.Key);
                                        sws[item.Key].Restart();//重启计时器
                                    }
                                }
                                else //设备状态变化了，直接上传
                                {
                                    //(bool, string) reuslt = await SetHelper.mesManager.EQStatus(StatusModelHelper.GetStatusModel(status[item.Key].ToString(), item.Key), item.Key);
                                    sws[item.Key].Restart();//重启计时器
                                }
                                Status_old[item.Key] = status[item.Key];
                            });
                            Thread.Sleep(50);

                            //foreach (var item in NumberAndTime)
                            //{
                            //    await UploadStatus(item.Key);
                            //    Thread.Sleep((item.Value * 1000) / NumberAndTime.Count);
                            //}
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                SetHelper.ListMesMessage.ShowInfoQueue("设备状态自动更新上传MES异常失败:" + ex, false);
            }
        }

        public async Task UploadStatus(int i)
        {
            int AlarmId = 0;
            //判断当前工位有无报警，有报警要把最先报警的序号拼接上去 例如设备状态为2，报警序号为3，结果为2_3
            foreach (var tagItem in dicAlarms)
            {
                Match match = Regex.Match(tagItem.Key, @"(\d+)$");
                //默认是1
                string Number = "1";
                if (match.Success)
                {
                    Number = match.Value;
                }
                int iNumber = Convert.ToInt32(Number) - 1;
                if (iNumber == i)
                {
                    // 查找Dt最小且不为DateTime.MinValue的成员
                    var alarmWithMinDt = tagItem.Value
                        .Where(a => a.DtStart != DateTime.MinValue)  // 排除DateTime.MinValue
                        .OrderBy(a => a.DtStart)  // 按Dt升序排序，最小的在前
                        .FirstOrDefault();  // 获取最小的一个，若不存在返回null
                    if (alarmWithMinDt != null)
                    {
                        AlarmId = alarmWithMinDt.Id;
                    }
                }
            }

            string statusNow = "";
            if (AlarmId == 0)
            {
                statusNow = status[i].ToString();
            }
            else
            {
                statusNow = status[i].ToString() + "_" + AlarmId.ToString();
            }
            //statusNow = "1";//测试用
            //(bool, string) reuslt = await SetHelper.mesManager.EQStatus(StatusModelHelper.GetStatusModel(statusNow, i), i);
            //if (!reuslt.Item1)
            //{
            //SetHelper.ListMesMessage.ShowInfoQueue(SetHelper.StationNumber.numberGroups[i].Name + "设备状态更新上传MES失败:" + reuslt.Item2);
            //}
        }

        private Dictionary<string, List<Alarm>> dicAlarms = new Dictionary<string, List<Alarm>>();
        private PopupWindow popupWindow;
        private PopupWindowGreen popupWindowGreen;
        public static OP1010View op1010View;
        public static bool IsOP1010ViewOpen;

        /// <summary>
        /// 校验设置的参数-由PLC触发
        /// </summary>
        public async void UploadGetPara(string number)
        {
            int iNumber = Convert.ToInt32(number) - 1;
            string stationName = SetHelper.StationNumber.numberGroups[iNumber].Name;

            object obj = new object();
            string version = "";

            if (SetHelper.siemens.ReadItem(PLCGroupName.ReadGroup, "设定参数版本_" + number, ref obj))
            {
                version = obj.Obj2String();
                SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读到参数版本为{version}");
            }
            else
            {
                SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读取参数版本失败");
            }

            object data = new object();

            int getParaResult = 1;
            //从设定参数组中获取参数
            var dic = SetHelper.siemens.DicDataItems[PLCGroupName.GetParaGroup.ToString()];
            dic = dic.Where(it => it.Key.Contains("_" + number)).ToDictionary(it => it.Key, it => it.Value);

            List<ParaList> paraLists = new List<ParaList>();

            //获取ParaList
            if (dic.Count != 0)
            {
                List<string> TagNameList = dic.Keys.ToList();
                List<DataItem> TagValueList = dic.Values.ToList();
                var dataItems = dic.Select(x => x.Value).ToArray();
                if (!SetHelper.siemens.ReadItems(dataItems, ref data))
                {
                    SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读取设定参数数据失败！");
                }
                if (data is object[] datarray)
                {
                    for (int i = 0; i < datarray.Length; i++)
                    {
                        SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读到{TagNameList[i]}为{datarray[i]}");
                        PLCTag tag = SetHelper.PLCSetting.ListGroup.FirstOrDefault(x => x.GroupType == PLCGroupName.GetParaGroup.ToString())?.ListTag.FirstOrDefault(x => TagNameList[i].Contains(x.TagName));

                        ParaList para_Info = new ParaList()
                        {
                            ParaItem = TagNameList[i].Substring(0, TagNameList[i].LastIndexOf('_')),
                            Value = datarray[i].ToString(),
                            Up = tag.UpLimit.ToString(),
                            Down = tag.LowLimit.ToString()
                        };
                        paraLists.Add(para_Info);
                    }
                }
            }
            else
            {
                SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 设定参数组数据获取失败！");
            }

            List<Para_Info> paraInfo = new List<Para_Info>
            {
                new Para_Info() { ParaREV = version, ParaList = paraLists.ToArray() }
            };

            //上传MES参数
            (bool, string, string) response = await SetHelper.mesManager.GetParaCheck(paraInfo.GetGetParaModel(iNumber), iNumber);
            string msg = $"{stationName} 设定参数上报结果：{response.Item1}\r\n\r\nMES返回信息：{response.Item2}\r\n\r\nnMES返回完整信息:\r\n{response.Item3}";
            SetHelper.ListMesMessage.ShowInfoQueue(msg);

            #region 给PLC发送完成信号

            getParaResult = response.Item1 ? 1 : 2;
            bool result = false;
            result = SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "设定参数上报结果_" + number, getParaResult);
            SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 设定参数结果写{getParaResult},{(result ? "成功" : "失败")}");

            #endregion 给PLC发送完成信号

            if (!response.Item1)
            {
                await Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    //MessageBox.Show("校验失败：" + msg);
                    if (SetHelper.IsMsgWindowOpen)
                    {
                        popupWindow.Close();
                    }
                    popupWindow = new PopupWindow(msg);
                    popupWindow.Show();
                });
            }
        }

        public class Alarm
        {
            public int Id { get; set; }
            public DateTime DtStart { get; set; }
            public DateTime DtEnd { get; set; }
        }

        public static bool[] ScanSuccess;

        /// <summary>
        /// 进站
        /// </summary>
        /// <param name="stationNumber"></param>




        public async void AlarmUpload(string number)
        {
            try
            {
                int iNumber = Convert.ToInt32(number) - 1;
                string stationName = SetHelper.StationNumber.numberGroups[iNumber].Name;
            }
            catch (Exception ex)
            {
                SetHelper.ListPLCMessage.ShowInfoQueue($"报警信息上传出错--{ex.ToString()}");
            }
        }

        public static MaterialAlarmView materialAlarmView = new MaterialAlarmView("");
        public static MaterialWarnView materialWarnView = new MaterialWarnView("");



        /// <summary>
        /// OEE数据收集
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public async Task OEEDataCollectionAsync(string number)
        {
            int iNumber = Convert.ToInt32(number) - 1;
            string stationName = SetHelper.StationNumber.numberGroups[iNumber].Name;
            try
            {
                //SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, PLCTagItem.产品SN, "A12123jd7");
                string SN = SetHelper.NowProductCode;
                object obj = new object();
                string carryID = "";

                //if (SetHelper.siemens.ReadItem(PLCGroupName.ReadGroup, "产品SN_" + number, ref obj))
                //{
                //    SN = obj.Obj2String();
                //    SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读到产品SN为{SN}");
                //}
                //else
                //{
                //    SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读产品SN失败");
                //    //return;
                //}
                if (SetHelper.siemens.ReadItem(PLCGroupName.ReadGroup, "出站载具码_" + number, ref obj))
                {
                    carryID = obj.Obj2String();
                    SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读到出站载具码为{carryID}");
                }
                else
                {
                    SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读出站载具码失败");
                    //return;
                }

                #region 读取产品需要上传MES的数据

                object data = new object();
                int oeeResult = 1;
                List<OeeInfo> oeeInfoList = new List<OeeInfo>();
                var dic = SetHelper.siemens.DicDataItems[PLCGroupName.OeeGroup.ToString()];

                //读取对应工位的oee参数
                //循环时间 552  设定CT 556 稼动率 560 抛料率 564 抛料数量 568
                //有些工位螺栓有2-3种批次号，有的工位是垫片+螺栓两种批次号
                //需要分工位讨论（5.16日加）
                dic = dic.Where(it => it.Key.Contains("_" + number)).ToDictionary(it => it.Key, it => it.Value);
                if (dic.Count != 0)
                {
                    List<string> TagNameList = dic.Keys.ToList();

                    //string json = File.ReadAllText(SetHelper.materialpath);
                    // List<MaterailModel> materails = SetHelper.ReadSys<List<MaterailModel>>(SetHelper.materialpath);

                    var dataItems = dic.Select(x => x.Value).ToArray();//PLC点位信息

                    if (!SetHelper.siemens.ReadItems(dataItems, ref data))
                    {
                        SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读取OEE数据失败！");
                    }
                    if (data is object[] datarray)
                    {
                        for (int i = 0; i < datarray.Length; i++)
                        {
                            SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读到OEE参数:{TagNameList[i]}为{datarray[i]}");
                            OeeInfo dC_Info = new OeeInfo()
                            {
                                Item = TagNameList[i].Substring(0, TagNameList[i].LastIndexOf('_')),//去掉下划线
                                Value = datarray[i].ToString()
                            };

                            oeeInfoList.Add(dC_Info);
                        }

                    }
                }

                #endregion 读取产品需要上传MES的数据

                #region 数据上传MES

                (bool, string, string) response = await SetHelper.mesManager.OEEDataCollect(oeeInfoList.GetOEEModel(carryID, iNumber), iNumber);
                string msg = $"{stationName} 载具码:{carryID} \r\n\r\n OEE数据收集结果：{response.Item1},\r\n\r\nMES返回消息：{response.Item2}\r\n\r\nMES返回完整信息:\r\n{response.Item3}";
                SetHelper.ListMesMessage.ShowInfoQueue(msg);
                oeeResult = response.Item1 ? 1 : 2;

                #endregion 数据上传MES

                if (!response.Item1)
                {
                    await Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        //MessageBox.Show("校验失败：" + msg);
                        if (SetHelper.IsMsgWindowOpen)
                        {
                            popupWindow.Close();
                        }
                        popupWindow = new PopupWindow(msg);
                        popupWindow.Show();
                    });
                }

                // GetOEEDataAsync(carryID);
            }
            catch (Exception ex)
            {
                SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 产品OEE数据收集出错--{ex.ToString()}");
            }
        }

        /// <summary>
        /// 检查材料合法性，从PLC获取产品码给MES，返回PASS or FAIL
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public async Task FeedingCheckAsync(string number, string sn)
        {
            int iNumber = Convert.ToInt32(number) - 1;
            string stationName = SetHelper.StationNumber.numberGroups[iNumber].Name;
            try
            {
                string SN = sn;

                int FeedingCheckResult = 1;

                #region 数据上传MES

                (bool, string, string) response = await SetHelper.mesManager.FeedingCheck(SN.GetFeedingCheck(iNumber), iNumber);
                string msg = $"{stationName} \r\n\r\n材料合法性校验结果\r\n{response.Item1}\r\n\r\n消息:\r\n{response.Item2}\r\n\r\nMES返回完整信息:{response.Item3}";
                SetHelper.ListMesMessage.ShowInfoQueue(msg);
                FeedingCheckResult = response.Item1 ? 1 : 2;

                #endregion 数据上传MES

                #region 写物料校验完成

                bool result = false;
                result = SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "检查材料合法性结果_" + number, FeedingCheckResult);
                SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 材料合法性校验结果写{FeedingCheckResult},{(result ? "成功" : "失败")}", true, EnumLogType.log.ToString(), result);
                SetHelper.NowProductCode = "";

                #endregion 写物料校验完成

                if (!response.Item1)
                {
                    await Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        //MessageBox.Show("校验失败：" + msg);
                        if (SetHelper.IsMsgWindowOpen)
                        {
                            popupWindow.Close();
                        }
                        popupWindow = new PopupWindow(msg);
                        popupWindow.Show();
                    });
                }
            }
            catch (Exception ex)
            {
                SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 材料合法性校验出错--{ex.ToString()}");
            }
        }

        /// <summary>
        /// 核对载具或者Tray盘 读取PLC载具码发给PLC校验
        /// </summary>
        /// <param name="number">根据配置的触发信号组标签名_1 _2 _3</param>
        /// <returns></returns>
        public async Task CarrierCheckAsync(string number)
        {
            int iNumber = Convert.ToInt32(number) - 1;
            string stationName = SetHelper.StationNumber.numberGroups[iNumber].Name;

            try
            {
                string SN = SetHelper.NowProductCode;

                #region 读载具码

                object obj = new object();
                //核对载具码用出站载具码
                if (SetHelper.siemens.ReadItem(PLCGroupName.ReadGroup, "出站载具码_" + number, ref obj))
                {
                    SN = obj.Obj2String();
                    SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 核对载具码 读到载具码为{SN}");
                }
                else
                {
                    SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 核对载具码 读载具码失败");
                }

                #endregion 读载具码

                int result = 1;

                //SN = "1030000HR10000008TC00010";//测试用

                (bool, string, string) response = await SetHelper.mesManager.CarrierCheck(SN.GetCarrierCheckModel(iNumber), iNumber);
                //(bool, string, string) response = (true, "", "");//测试用
                string msg = $"{stationName} 核对载具码结果：{response.Item1}\r\n\r\n MES返回信息：{response.Item2}\r\n\r\n MES返回完整信息:\r\n{response.Item3}";
                SetHelper.ListMesMessage.ShowInfoQueue(msg);
                result = response.Item1 ? 1 : 2;

                #region 写物料校验完成

                bool bResult = false;
                bResult = SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "核对载具码结果_" + number, result);
                SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 核对载具码结果写{result},{(bResult ? "成功" : "失败")}");
                SetHelper.NowProductCode = "";

                #endregion 写物料校验完成

                //优先展示材料Link
                if (SetHelper.MesSetting.ListGroup[iNumber].IsCarryCheckStation == "1" && SetHelper.MesSetting.ListGroup[iNumber].ScanMaterialCount < 1)
                {
                    SetHelper.resultModel[iNumber].Result2 = response.Item1 ? "OK" : "NG";
                    SetHelper.resultModel[iNumber].MaterialSN = SN;
                }

                if (!response.Item1)
                {
                    await Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        //MessageBox.Show("校验失败：" + msg);
                        if (SetHelper.IsMsgWindowOpen)
                        {
                            popupWindow.Close();
                        }
                        popupWindow = new PopupWindow(msg);
                        popupWindow.Show();
                    });
                }
                else
                {
                    //原本为核对载具码成功后此处触发扫码，已经换为PLC进站信号弹窗扫码
                    //OP1010工位弹窗提示员工扫码
                    //if (stationName.Contains("OP1010"))
                    //{
                    //    await Application.Current.Dispatcher.BeginInvoke(() =>
                    //    {
                    //        if (IsOP1010ViewOpen)
                    //        {
                    //            op1010View.Close();
                    //        }

                    //        op1010View = new OP1010View("等待扫描SN码进站");
                    //        op1010View.Show();
                    //        IsOP1010ViewOpen = true;
                    //    });
                    //}
                }
            }
            catch (Exception ex)
            {
                SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 核对载具码失败--{ex.ToString()}");
            }
        }

        public async void UploadPrint(string number)
        {
            int iNumber = Convert.ToInt32(number) - 1;
            string stationName = SetHelper.StationNumber.numberGroups[iNumber].Name;
            try
            {
                string SN = SetHelper.NowProductCode;

                #region 读产品SN

                object obj = new object();
                if (SetHelper.siemens.ReadItem(PLCGroupName.ReadGroup, "产品SN_" + number, ref obj))
                {
                    SN = obj.Obj2String();
                    SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 打印信号发送 读到产品SN为{SN}");
                }
                else
                {
                    SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 打印信号发送 读产品SN失败");
                }

                #endregion 读产品SN

                int result = 1;

                (bool, string, string) response = await SetHelper.mesManager.CodeSoftPrint(SN.GetCodeSoftPrintModel(iNumber), iNumber);
                string msg = $"{stationName} 打印信号发送结果：{response.Item1},\r\n\r\nMES返回消息：{response.Item2}\r\n\r\nMES返回消息:\r\n{response.Item3}";
                SetHelper.ListMesMessage.ShowInfoQueue(msg);
                result = response.Item1 ? 1 : 2;

                #region 给PLC写完成信号

                bool bResult = false;
                bResult = SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "打印信号发送结果_" + number, result);
                SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 打印信号发送结果写{result},{(bResult ? "成功" : "失败")}");
                SetHelper.NowProductCode = "";

                #endregion 给PLC写完成信号

                if (!response.Item1)
                {
                    await Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        //MessageBox.Show("校验失败：" + msg);
                        if (SetHelper.IsMsgWindowOpen)
                        {
                            popupWindow.Close();
                        }
                        popupWindow = new PopupWindow(msg);
                        popupWindow.Show();
                    });
                }
            }
            catch (Exception ex)
            {
                SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 打印信号发送失败--{ex.ToString()}");
            }
        }

        /// <summary>
        /// 图片上传
        /// </summary>
        public void PictureUploadAsync()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        //本地图片命名规则
                        //Original/Detection_SN_该SN的第几张图片_1/2
                        //前面随便加本地地址
                        DateTime dtNow = DateTime.Now;
                        List<string> strings = new List<string>() { "OK", "NG" };

                        //分别传递各个工位的图片 每个工位找对应的
                        for (int i = 0; i < SetHelper.StationNumber.numberGroups.Count; i++)
                        {
                            //映射盘地址
                            string netMainPath = SetHelper.MesSetting.ListGroup[i].MappingDiskPath;
                            //如果地址为空为未配置，不上传
                            if (string.IsNullOrEmpty(netMainPath) || netMainPath == "\\") continue;

                            string ccdNmuber = SetHelper.MesSetting.ListGroup[i].CCDNumber;
                            //分别传OK和NG的图片
                            foreach (var item in strings)
                            {
                                int count = 1;
                                string SN = "";

                                if (!SetHelper.MesSetting.ListGroup[i].MappingDiskPath.EndsWith("\\")) netMainPath = SetHelper.MesSetting.ListGroup[i].MappingDiskPath + "\\";
                                if (netMainPath == "") continue;

                                //映射盘地址
                                string netPath = $"{netMainPath}{SetHelper.MesSetting.ListGroup[i].Line.Substring(0, 2)}\\{dtNow.ToString("yyyyMM")}\\{dtNow.ToString("dd")}\\Line{SetHelper.MesSetting.ListGroup[i].Line.Substring(SetHelper.MesSetting.ListGroup[i].Line.Length - 2, 2)}\\{SetHelper.MesSetting.ListGroup[i].StationID}\\{SetHelper.MesSetting.ListGroup[i].MachineID}\\{item}\\";
                                //原图片地址
                                string path = $"{SetHelper.MesSetting.ListGroup[i].PictureFilePath}{item}\\";
                                //已经上传的图片地址
                                string uploadedpath = $"{SetHelper.MesSetting.ListGroup[i].PictureUploadedFilePath}{item}\\";
                                if (!Directory.Exists(path))
                                {
                                    Directory.CreateDirectory(path);
                                }
                                if (!Directory.Exists(uploadedpath))
                                {
                                    Directory.CreateDirectory(uploadedpath);
                                }
                                if (!Directory.Exists(netPath))
                                {
                                    Directory.CreateDirectory(netPath);
                                }
                                //从本地文件夹中获取图片，每次最多只获取1张
                                var DirectoryList = Directory.GetFiles(path).Where(x => (x.EndsWith(".jpeg") || (x.EndsWith(".jpg") || x.EndsWith(".bmp") || x.EndsWith(".png")))).Take(1).ToList();
                                List<string> fileList = new List<string>();

                                //完整路径
                                DirectoryList.ForEach((x) =>
                                {
                                    fileList.Add(new System.IO.FileInfo(x).Name);
                                });

                                //var t = new DirectoryInfo(path).GetFiles("*").Where(x => (x.Name.EndsWith(".jpg") || x.Name.EndsWith(".bmp") || x.Name.EndsWith(".png"))).Select(x => x.Name).ToList();
                                if (fileList.Count == 0) continue;

                                List<MesModel.Request.SNList> snlists = new List<MesModel.Request.SNList>();
                                Dictionary<string, List<FileInfos>> fileinfos = new Dictionary<string, List<FileInfos>>();
                                Dictionary<string, string> localNetName = new Dictionary<string, string>();

                                foreach (var picture in fileList)
                                {
                                    #region 文件名解析

                                    int ccdNo = 1;
                                    string type = PictureType.Original.ToString();
                                    string[] ListPictureData = picture.Split('_').ToArray();
                                    if (ListPictureData.Length == 3)
                                    {//Original_123_1.jpg
                                        type = ListPictureData[0].ToString();//Original Detection
                                        SN = ListPictureData[1];//进站后获取的
                                        count = ListPictureData[2].Split('.').FirstOrDefault().Obj2Int();//同一个sn下的第多少个 1，2，3
                                    }
                                    else if (ListPictureData.Length == 4)
                                    {//Original_123_1_1.jpg
                                        type = ListPictureData[0].ToString();
                                        SN = ListPictureData[1];
                                        ccdNo = ListPictureData[2].Obj2Int();//可能有多个相机，则要区分
                                        count = ListPictureData[3].Split('.').FirstOrDefault().Obj2Int();
                                        if (ccdNo != Convert.ToInt32(ccdNmuber) && Convert.ToInt32(ccdNmuber) != 0)//如果不是这个工站的则不上传。如果设置为0，则默认上传到这个工站
                                        {
                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        SetHelper.ListMesMessage.ShowInfoQueue($"图片命名不符合规约,文件夹：{item}图片名字：{picture}");
                                        continue;
                                    }
                                    if (SN == "")
                                    {
                                        SetHelper.ListMesMessage.ShowInfoQueue($"图片命名不符合规约,文件夹：{item}图片名字：{picture}");
                                        continue;
                                    }
                                    //count = fileinfos.Count(x => x.Key == SN) + 1;

                                    #endregion 文件名解析

                                    #region 拼接MES端文件名，放到集合中

                                    //拼接新的上传文件的文件名
                                    var jpg = new FileInfos()
                                    {
                                        //yyyyMMdd_StationID_Result_MMdd_HHmmss_MachineID_穴位_Original/Detection_SN_该SN的第几张图片
                                        FileName = $"{dtNow.ToString("yyyyMMdd")}_{SetHelper.MesSetting.ListGroup[i].StationID}_{item}_{SN}_{dtNow.ToString("MMdd")}_{dtNow.ToString("HHmmss")}_{SetHelper.MesSetting.ListGroup[i].MachineID}_01_{type}_{count}.jpg",
                                        //W:\Line前两位\yyyyMM\dd\Line后两位\StationID\MachineID\(OK或NG)
                                        FilePath = netPath,
                                    };

                                    //如果是同一个SN则放到一个fileinfos中上传
                                    if (fileinfos.ContainsKey(SN))
                                    {
                                        fileinfos[SN].Add(jpg);
                                    }
                                    else
                                    {
                                        List<FileInfos> files = new List<FileInfos>();
                                        files.Add(jpg);
                                        fileinfos.Add(SN, files);
                                    }

                                    #endregion 拼接MES端文件名，放到集合中

                                    #region 本地文件名称和mes组合文件名绑定

                                    if (picture != null && localNetName.ContainsKey(picture))
                                    {
                                        localNetName[picture] = jpg.FileName;
                                    }
                                    else
                                    {
                                        localNetName.Add(picture, jpg.FileName);
                                    }

                                    #endregion 本地文件名称和mes组合文件名绑定
                                }

                                #region 文件转移到本地已上传文件夹后上传到映射盘

                                if (fileinfos.Count == 0) continue;

                                //if (response.Item1)
                                //{
                                foreach (var name in fileList)
                                {
                                    Thread.Sleep(1000);
                                    var file = DirectoryList.FirstOrDefault(x => x.Contains(name));
                                    if (file != null)
                                    {
                                        string netName = "";
                                        if (localNetName.TryGetValue(name, out netName) && !string.IsNullOrEmpty(netName))
                                        {
                                            string uploadedfilepath = $"{SetHelper.MesSetting.ListGroup[i].PictureUploadedFilePath}{item}\\{(netName ?? name)}";
                                            if (!Directory.Exists(uploadedpath))
                                            {
                                                Directory.CreateDirectory(uploadedpath);
                                            }

                                            #region 上传完成的文件传输到本地已完成文件夹

                                            if (File.Exists(uploadedfilepath))
                                            {
                                                File.Delete(uploadedfilepath);
                                            }
                                            File.Move(file, uploadedfilepath);

                                            #endregion 上传完成的文件传输到本地已完成文件夹

                                            #region 传输到映射盘

                                            if (!netPath.EndsWith("\\")) netPath = netPath + "\\";
                                            string netFile = $"{netPath}{(netName ?? name)}";
                                            if (File.Exists(netFile))
                                            {
                                                File.Delete(netFile);
                                            }
                                            File.Copy(uploadedfilepath, netFile);

                                            #endregion 传输到映射盘
                                        }
                                    }
                                }
                                //}

                                #endregion 文件转移到本地已上传文件夹后上传到映射盘

                                #region 上传MES

                                snlists = fileinfos.Select(x => new MesModel.Request.SNList()
                                {
                                    SN = x.Key,
                                    FileInfo = x.Value.ToArray()
                                }).ToList();

                                FileUploadModel model = new FileUploadModel()
                                {
                                    Line = SetHelper.MesSetting.ListGroup[i].Line,
                                    MachineID = SetHelper.MesSetting.ListGroup[i].MachineID,
                                    OPID = SetHelper.MesSetting.ListGroup[i].OPID,
                                    StationID = SetHelper.MesSetting.ListGroup[i].StationID,
                                    SNList = snlists.ToArray()
                                };

                                (bool, string) response = await SetHelper.mesManager.FileUpLoad(model, i);

                                #endregion 上传MES

                                #region 清除超时图片

                                _ = DeleteLocalPicture();

                                #endregion 清除超时图片

                                SetHelper.ListMesMessage.ShowInfoQueue($"图片上传结果：{response.Item1},消息：{response.Item2}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        SetHelper.ListMesMessage.ShowInfoQueue($"图片上传失败：{ex.ToString()}", false, "ex");
                    }
                    Thread.Sleep(1000 * 3);
                }
            });
        }

        public void GetAlarmInfo()
        {
            bool[] oldalarm = null;
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        object obj = new object();
                        bool result = SetHelper.siemens.ReadItem(PLCGroupName.ReadGroup, PLCTagItem.设备报警, ref obj);
                        if (!result)
                        {
                            Thread.Sleep(5000);
                            continue;
                        }

                        var alarms = (BitArray)obj;
                        bool[] nowalarm = new bool[alarms.Length];
                        if (oldalarm == null)
                        {
                            oldalarm = new bool[alarms.Length];
                        }
                        alarms.CopyTo(nowalarm, 0);
                        for (int i = 0; i < nowalarm.Length; i++)
                        {
                            if (oldalarm[i] != nowalarm[i])
                            {
                                oldalarm[i] = nowalarm[i];
                                if (nowalarm[i])
                                {
                                    //alarms.CopyTo(oldalarm, 0);
                                    //(bool, string) reuslt = await SetHelper.mesManager.EQAlarm(i.GetAlarmModel());
                                    //SetHelper.ListMesMessage.ShowInfoQueue($"设备报警上传结果：{reuslt.Item1},消息：{reuslt.Item2}");
                                    break;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        SetHelper.ListMesMessage.ShowInfoQueue(ex.ToString());
                    }

                    Thread.Sleep(3 * 1000);
                }
            });
        }

        public void GetAlarmInfo1()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    try
                    {
                        object obj = new object();
                        bool result = SetHelper.siemens.ReadItem(PLCGroupName.ReadGroup, PLCTagItem.设备报警, ref obj);

                        if (obj is short[] intAlarm)
                        {
                            for (int i = 0; i < intAlarm.Length; i++)
                            {
                                if (intAlarm[i] == 1)
                                {
                                    // (bool, string) reuslt = await SetHelper.mesManager.EQAlarm(i.GetAlarmModel());
                                }
                            }
                        }
                        else if (obj is BitArray boolAlarm)
                        {
                            for (int i = 0; i < boolAlarm.Length; i++)
                            {
                                if (boolAlarm[i])
                                {
                                    // (bool, string) reuslt = await SetHelper.mesManager.EQAlarm(i.GetAlarmModel());
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        SetHelper.ListMesMessage.ShowInfoQueue(ex.ToString());
                    }
                    //  SetHelper.ListMesMessage.ShowInfoQueue($"设备报警结果：{reuslt.Item1},消息：{reuslt.Item2}");
                    Thread.Sleep(1000 * 15);
                }
            });
        }

        public async Task GetOEEDataAsync(string carryid)
        {
            try
            {
                double time = (SetHelper.DateEnd - SetHelper.DateStart).TotalSeconds;
                object data = new object();
                List<OeeInfo> dcInfoList = new List<OeeInfo>();
                dcInfoList.Add(new OeeInfo() { Item = "Cycle_Time", Value = time.Obj2String() });
                dcInfoList.Add(new OeeInfo() { Item = "Eq_Active_Rate", Value = "100%" });
                dcInfoList.Add(new OeeInfo() { Item = "Takt_Time", Value = time.Obj2String() });
                dcInfoList.Add(new OeeInfo() { Item = "TossingCOLLECTION", Value = "0" });
                //(bool, string) reuslt = await SetHelper.mesManager.EQOEE(dcInfoList.GetOEEModel(carryid));
                // SetHelper.ListMesMessage.ShowInfoQueue($"OEE上传结果：{reuslt.Item1},消息：{reuslt.Item2}");
            }
            catch (Exception ex)
            {
                SetHelper.ListMesMessage.ShowInfoQueue(ex.ToString());
            }
        }

        public Task DeleteLocalPicture()
        {
            try
            {
                DateTime dateTime = DateTime.Now;
                List<string> strings = new List<string>() { "OK", "NG" };
                for (int i = 0; i < SetHelper.StationNumber.numberGroups.Count; i++)
                {
                    List<string> Paths = new List<string>() { SetHelper.MesSetting.ListGroup[i].PictureFilePath, SetHelper.MesSetting.ListGroup[i].PictureUploadedFilePath };
                    foreach (var itempath in Paths)
                    {
                        foreach (var item in strings)
                        {
                            string path = itempath;

                            if (!path.EndsWith("\\"))
                            {
                                path = path + "\\";
                            }
                            path = path + item + "\\";

                            DirectoryInfo directoryInfo = new DirectoryInfo(path);
                            System.IO.FileInfo[] fileInfos = directoryInfo.GetFiles();
                            foreach (var file in fileInfos)
                            {
                                double days = (dateTime - file.CreationTime).TotalDays;
                                if (days >= SetHelper.MesSetting.ListGroup[i].PictureSaveTime)
                                {
                                    file.Delete();
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SetHelper.ListMesMessage.ShowInfoQueue($"删除超时图片异常：{ex.ToString()}", false);
            }
            return Task.CompletedTask;
        }

        public Task DeleteLocalLog()
        {
            try
            {
                DateTime dateTime = DateTime.Now;

                string path = SetHelper.logmainpath;
                if (!Directory.Exists(path)) return Task.CompletedTask;
                if (!path.EndsWith("\\"))
                {
                    path = path + "\\";
                }
                path = path + "\\";

                DirectoryInfo directoryInfo = new DirectoryInfo(path);
                System.IO.FileInfo[] fileInfos = directoryInfo.GetFiles();
                foreach (var file in fileInfos)
                {
                    double days = (dateTime - file.CreationTime).TotalDays;
                    if (days >= SetHelper.MesSetting.ListGroup[0].PictureSaveTime)
                    {
                        file.Delete();
                    }
                }
            }
            catch (Exception ex)
            {
                SetHelper.ListMesMessage.ShowInfoQueue($"删除超时日志异常：{ex.ToString()}", false);
            }

            return Task.CompletedTask;
        }

        public void GetGlueShortage()
        {
            Task.Run(async () =>
            {
                Stopwatch sw = new Stopwatch();
                sw.Restart();
                while (true)
                {
                    Thread.Sleep(10);
                    try
                    {
                        var mes = SetHelper.MesSetting.ListGroup.FirstOrDefault(x => x.IsGlueStation == "1");
                        if (mes == null)
                        {
                            continue;
                        }

                        var number = SetHelper.MesSetting.ListGroup.IndexOf(mes);

                        object obj = new object();
                        bool result = SetHelper.siemens.ReadItem(PLCGroupName.ReadGroup, "缺胶_" + (number + 1), ref obj);
                        bool shortage = obj.ObjToBool();
                        if (!shortage)
                        {
                            Thread.Sleep(5 * 1000);
                        }
                        else
                        {
                            var response = await SetHelper.mesManager.GlueOnOrOffLine(number.GetGlueShortageModel(), number);
                            Thread.Sleep(5 * 60 * 1000);
                        }
                    }
                    catch (Exception ex)
                    {
                        SetHelper.ListMesMessage.ShowInfoQueue(ex.ToString(), true);
                    }
                }
            });
        }


        public Task DeleteLocalMesLog()
        {
            try
            {
                string path = "D:\\MESLOG\\";

                if (!Directory.Exists(path)) return Task.CompletedTask;

                DirectoryInfo directoryInfo = new DirectoryInfo(path);
                DirectoryInfo[] yearInfos = directoryInfo.GetDirectories();

                DateTime dateTime = DateTime.Now;

                foreach (var year in yearInfos)
                {
                    string pathy = path + year + "\\";
                    DirectoryInfo directoryInfomonth = new DirectoryInfo(pathy);
                    DirectoryInfo[] monthInfos = directoryInfomonth.GetDirectories();
                    if (monthInfos.Count() == 0)
                    {
                        year.Delete(true);
                        continue;
                    }
                    foreach (var month in monthInfos)
                    {
                        string pathm = path + year + "\\" + month + "\\";
                        DirectoryInfo directoryInfday = new DirectoryInfo(pathm);
                        DirectoryInfo[] dayInfos = directoryInfday.GetDirectories();
                        if (dayInfos.Count() == 0)
                        {
                            month.Delete(true);
                            continue;
                        }
                        foreach (var day in dayInfos)
                        {
                            string dt = $"{year}-{month}-{day} 00:00:00";
                            double days = (dateTime - dt.Obj2DateTime()).TotalDays;
                            if (days >= SetHelper.MesSetting.ListGroup[0].PictureSaveTime)
                            {
                                day.Delete(true);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                SetHelper.ListMesMessage.ShowInfoQueue($"删除超时mes日志异常：{ex.ToString()}", false);
            }
            return Task.CompletedTask;
        }
    }
}
