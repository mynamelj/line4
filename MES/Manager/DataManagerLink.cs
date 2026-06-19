
using MES.MesModel.Request;
using MES.SetModel;
using MES.View;
using System.Windows;


namespace MES.Manager
{
    public partial class DataManager
    {
        private bool[] LinkCompRunning;
        private object[] LinkCompLocks;
        public async Task LinkCompStart(string stationNumber)
        {
            //20250401让PLC触发扫码，避免扫码NG无法关弹窗放行
            int iNumber = Convert.ToInt32(stationNumber) - 1;
            string stationName = SetHelper.StationNumber.numberGroups[iNumber].Name;

            bool canRun = false;

            lock (LinkCompLocks[iNumber])
            {
                if (!LinkCompRunning[iNumber])
                {
                    LinkCompRunning[iNumber] = true;
                    canRun = true;
                }
            }

            if (!canRun)
            {
                SetHelper.ListPLCMessage.ShowInfoQueue(
                    $"{stationName} LinkComp流程正在执行，忽略重复扫描材料码启动_{stationNumber}");
                return;
            }
            try
            {
                if (SetHelper.MesSetting.ListGroup[iNumber].ScanMaterialCount > 0)
            {

                    //精追码扫码扫码数量大于0，全部扫码后给PLC发送扫码完成信号
                    ScanSuccess[iNumber] = false;
                    object obj_MESWrite = new object();
                    string SN_MESWrite = "";
                    string SN = "";

                    object obj = new object();

                    //20260119 调工厂MES linkCom 时要取DB6000‘进站产品SN’作为SN 和精追料绑定
                    if (stationName.Contains("OP3040"))
                    {
                        if (SetHelper.siemens.ReadItem(PLCGroupName.ReadGroup, "进站产品SN_" + stationNumber, ref obj))
                        {
                            SN = obj.Obj2String();
                            SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读到进站产品SN为{SN}");
                        }
                        else
                        {
                            SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读进站产品SN失败");
                            bool result0 = false;
                            result0 = SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "扫描材料码结果_" + stationNumber, 2);
                            SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读取PLC的'进站产品SN失败。扫描材料码结果_{stationNumber}写{2}{(result0 ? "成功" : "失败")}");
                        }
                    }
                    else if (stationName.Contains("OP2045"))//20260120  OP2045单独处理
                    {
                        bool result0 = false;
                        if (SetHelper.siemens.ReadItem(PLCGroupName.ReadGroup, "产品SN_" + stationNumber, ref obj))
                        {
                            SN = obj.Obj2String();
                            SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读取区DB 产品SN为{SN}");
                        }
                        else
                        {
                            SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读产品SN失败");
                            result0 = SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "扫描材料码结果_" + stationNumber, 2);
                            SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读取PLC 读取区DB的SN失败。扫描材料码结果_{stationNumber}写{2}{(result0 ? "成功" : "失败")}");
                        }
                        if (SetHelper.siemens.ReadItem(PLCGroupName.WriteGroup, "产品SN_" + stationNumber, ref obj_MESWrite))
                        {
                            SN_MESWrite = obj.Obj2String();
                            SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读取进站MES返回SN 产品SN为{SN}");
                        }
                        if (SN != SN_MESWrite)
                        {
                            result0 = SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "扫描材料码结果_" + stationNumber, 2);
                            SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读取PLC 读取区DB的SN{SN} 与进站返回SN{SN_MESWrite} 不同");
                        }
                    }
                    else
                    {
                        if (SetHelper.siemens.ReadItem(PLCGroupName.ReadGroup, "产品SN_" + stationNumber, ref obj))
                        {
                            SN = obj.Obj2String();
                            SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读到产品SN为{SN}");
                        }
                        else
                        {
                            SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读产品SN失败");
                            bool result0 = false;
                            result0 = SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "扫描材料码结果_" + stationNumber, 2);
                            SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读取PLC的SN失败。扫描材料码结果_{stationNumber}写{2}{(result0 ? "成功" : "失败")}");
                        }
                    }
                    //20260119  OP2045单独处理
                    if (stationName.Contains("OP2045"))
                    {
                        try
                        {
                            for (int i = 0; i < SetHelper.MesSetting.ListGroup[iNumber].ScanMaterialCount; i++)
                            {
                                SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 第{i + 1}循环开始！");
                                bool LinkResult = await ProductLinkCompAsync_OP2045((iNumber + 1).ToString(), SN, SetHelper.MesSetting.ListGroup[iNumber].ScanMaterialCount, i + 1);
                                if (!LinkResult)
                                {
                                    ScanSuccess[iNumber] = false;
                                }
                                else
                                {
                                    ScanSuccess[iNumber] = true;
                                }
                                SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 第{i + 1}循环结束！执行结果：{ScanSuccess[iNumber]}");
                            }
                            SetHelper.resultModel[iNumber].Result2 = ScanSuccess[iNumber] == true ? "OK" : "NG";
                            bool result_new = false;
                            int value_new = ScanSuccess[iNumber] == true ? 1 : 2;
                            result_new = SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "扫描材料码结果_" + stationNumber, value_new);
                            SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 全部码扫描完成。扫描材料码结果_{stationNumber}写{value_new}{(result_new ? "成功" : "失败")}");
                        }
                        catch (Exception ex)
                        {
                            SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 执行报错！！！" + ex.ToString(), false, "ex");
                        }
                    }
                    else
                    {
                        for (int i = 0; i < SetHelper.MesSetting.ListGroup[iNumber].ScanMaterialCount; i++)
                        {
                            bool LinkResult = await ProductLinkCompAsync((iNumber + 1).ToString(), SN, SetHelper.MesSetting.ListGroup[iNumber].ScanMaterialCount, i + 1);
                            //if (!LinkResult)
                            //{
                            //    continue;//继续执行扫码操作
                            //}
                            if (SetHelper.IsRestart[iNumber])//PLC给关闭弹窗信号，关闭所有弹窗
                            {
                                ScanSuccess[iNumber] = false;
                                break;
                            }
                            if (!LinkResult)
                            {
                                ScanSuccess[iNumber] = false;
                            }
                            else
                            {
                                ScanSuccess[iNumber] = true;
                            }

                            //关窗口
                            SetHelper.IsRestart[iNumber] = true;
                            Thread.Sleep(500);
                            SetHelper.IsRestart[iNumber] = false;
                        }
                        SetHelper.resultModel[iNumber].Result2 = ScanSuccess[iNumber] == true ? "OK" : "NG";
                        bool result = false;
                        int value = ScanSuccess[iNumber] == true ? 1 : 2;
                        result = SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "扫描材料码结果_" + stationNumber, value);
                        SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 全部码扫描完成。扫描材料码结果_{stationNumber}写{value}{(result ? "成功" : "失败")}");
                    }

                }
            }
            catch (Exception ex)
            {
                SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 执行报错！！！" + ex.ToString(), false, "ex");
            }
            finally
            {
              LinkCompRunning[iNumber] = false;
            }
        }

        public async Task<bool> ProductLinkCompAsync_OP2045(string number, string sn, int tatolNumber, int currentNumber)
        {
            int iNumber = Convert.ToInt32(number) - 1;
            string stationName = SetHelper.StationNumber.numberGroups[iNumber].Name;
            string SN = sn;//SetHelper.NowProductCode;
            int feedingResult = 2;
            int linkResult = 2;
            string materialCode = "";
            string showMsg = $"请扫精追码上传MES,共{tatolNumber}个，当前第{currentNumber}个";
            string mesReturn = $"请扫精追码上传MES,共{tatolNumber}个，当前第{currentNumber}个";
            SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读PLC物料码开始");
            object obj = new object();
            if (SetHelper.siemens.ReadItem(PLCGroupName.ReadGroup, "物料码_" + number, ref obj))
            {
                materialCode = obj.Obj2String();
                SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读到物料码为{materialCode}");
            }
            else
            {
                SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读PLC物料码失败");
                Thread.Sleep(3000);
            }
            SetHelper.NowMaterialCode[iNumber] = materialCode;
            //先feedingCheck扫描出的物料码,再绑定Link
            (bool, string, string) reuslt0 = await SetHelper.mesManager.FeedingCheck(SetHelper.NowMaterialCode[iNumber].GetFeedingCheck(iNumber), iNumber);
            feedingResult = reuslt0.Item1 ? 1 : 2;
            string msg0 = $"{stationName} SN:{SN} 精追码{SetHelper.NowMaterialCode[iNumber]} --FeedingCheck结果：{reuslt0.Item1},消息：{reuslt0.Item2}";
            SetHelper.ListMesMessage.ShowInfoQueue(msg0);
            if (!reuslt0.Item1)
            {
                //增加弹窗报警
                await Application.Current.Dispatcher.BeginInvoke(() =>
                {
                    //MessageBox.Show("校验失败：" + msg);
                    if (SetHelper.IsMsgWindowOpen)
                    {
                        popupWindow.Close();
                    }
                    popupWindow = new PopupWindow(msg0);
                    popupWindow.Show();
                });
                showMsg = "";
                mesReturn = $"条码{SetHelper.NowMaterialCode[iNumber]}FeedingCheck上传失败请重新扫码\r\n\r\nMES返回信息:{reuslt0.Item2}";
                return false;
            }

            #region 材料绑定上传

            (bool, string) reuslt = await SetHelper.mesManager.LinkComp(SetHelper.NowMaterialCode[iNumber].GetLinkCompModel(SN, iNumber), iNumber);
            linkResult = reuslt.Item1 ? 1 : 2;
            //SetHelper.resultModel[iNumber].Result2 = reuslt.Item1 ? "OK" : "NG";
            SetHelper.resultModel[iNumber].MaterialSN = SetHelper.NowMaterialCode[iNumber];
            string msg = $"{stationName} SN:{SN}--产品LinkComp结果：{reuslt.Item1},消息：{reuslt.Item2}";
            SetHelper.ListMesMessage.ShowInfoQueue(msg);
            if (!reuslt.Item1)
            {
                showMsg = "";
                mesReturn = $"条码{SetHelper.NowMaterialCode[iNumber]}LinkComp上传失败请重新扫码\r\n\r\nMES返回信息:{reuslt.Item2}";
                return false;
            }
            else
            {
                return true;
            }

            #endregion 材料绑定上传
        }

        /// <summary>
        /// 材料Link PLC给True触发后，弹窗等待扫码，如果扫码上传失败可以重复扫。PLC将信号置为False后关闭弹窗强制结束流程,扫描材料码后将材料吗与SN码发送给MES绑定
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        /// 

        public async Task<bool> ProductLinkCompAsync(string number, string sn, int tatolNumber, int currentNumber)
        {
            int iNumber = Convert.ToInt32(number) - 1;
            string stationName = SetHelper.StationNumber.numberGroups[iNumber].Name;

            //材料码数量需要设置
            //if (SetHelper.MesSetting.ListGroup[iNumber].MaterialCount <= 0) return;

            SetHelper.NowMaterialCode[iNumber] = "";
            SetHelper.IsRestart[iNumber] = false;

            string SN = sn;//SetHelper.NowProductCode;
            int feedingResult = 2;
            int linkResult = 2;

            //SN = "1030000HR10000008TC00010";//测试用

            #region 材料绑定

            string materialCode = "";

            //3000.50
            if (stationName.ToUpper().Contains("OP1030"))
            {
                object obj = new object();
                if (SetHelper.siemens.ReadItem(PLCGroupName.ReadGroup, "绑定定子码_" + number, ref obj))
                {
                    materialCode = obj.Obj2String();
                    SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读到绑定定子码为{materialCode}");
                }
                else
                {
                    SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读PLC绑定定子码失败");
                    materialCode = "1";
                }
            }



            //if (SN != "")
            //{
            //SetHelper.MaterialCount++;
            SetHelper.NowMaterialCode[iNumber] = "";
            SetHelper.ListMesMessage.ShowInfoQueue($"{stationName} 请扫描精追码...");
            string showMsg = $"请扫精追码上传MES,共{tatolNumber}个，当前第{currentNumber}个";
            string mesReturn = $"请扫精追码上传MES,共{tatolNumber}个，当前第{currentNumber}个";
            while (!SetHelper.IsRestart[iNumber])
            {
                Thread.Sleep(10);

                if (Application.Current != null)
                {
                    await Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (!SetHelper.IsOpen[iNumber])
                        {
                            //重新打开，清空条码
                            showMsg = mesReturn;
                            SetHelper.NowMaterialCode[iNumber] = "";
                            PopupSeqView window = new PopupSeqView(stationName, iNumber, showMsg);
                            //window.Height = 100;
                            window.ShowActivated = true;
                            if (stationName.ToUpper().Contains("OP3045"))
                            {
                                window.WindowStartupLocation = WindowStartupLocation.Manual;
                                window.Top = 0;
                                window.Left = 400;
                            }
                            else
                            {
                                window.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                            }
                            window.Show();
                        }
                    });
                }

                if (stationName.Contains("OP1030"))
                {
                    SetHelper.NowMaterialCode[iNumber] = materialCode;
                    materialCode = "";
                }


                //OP3050或者OP3045-2进行LinkComp时直接读取PLC发送的物料码信号 偏移量2630.48
                if (stationName.Contains("OP3050")
                || (stationName.Contains("OP3045") && stationName.Contains("2"))
                 || stationName.Contains("OP2040"))
                //|| stationName.Contains("OP2045"))//20260119
                {
                    object obj = new object();
                    if (SetHelper.siemens.ReadItem(PLCGroupName.ReadGroup, "物料码_" + number, ref obj))
                    {
                        materialCode = obj.Obj2String();
                        SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读到物料码为{materialCode}");
                    }
                    else
                    {
                        SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读PLC物料码失败");
                        Thread.Sleep(3000);
                    }

                    SetHelper.NowMaterialCode[iNumber] = materialCode;
                }

                if (SetHelper.NowMaterialCode[iNumber] == "")
                {
                    continue;
                }
                else
                {
                    //先feedingCheck扫描出的物料码,再绑定Link
                    (bool, string, string) reuslt0 = await SetHelper.mesManager.FeedingCheck(SetHelper.NowMaterialCode[iNumber].GetFeedingCheck(iNumber), iNumber);
                    feedingResult = reuslt0.Item1 ? 1 : 2;
                    string msg0 = $"{stationName} SN:{SN} 精追码{SetHelper.NowMaterialCode[iNumber]} --FeedingCheck结果：{reuslt0.Item1},消息：{reuslt0.Item2}";
                    SetHelper.ListMesMessage.ShowInfoQueue(msg0);
                    if (!reuslt0.Item1)
                    {
                        await Application.Current.Dispatcher.BeginInvoke(() =>
                        {
                            //增加弹窗报警
                            //MessageBox.Show("校验失败：" + msg);
                            if (SetHelper.IsMsgWindowOpen)
                            {
                                popupWindow.Close();
                            }
                            popupWindow = new PopupWindow(msg0);
                            popupWindow.Show();
                        });
                        showMsg = "";
                        mesReturn = $"条码{SetHelper.NowMaterialCode[iNumber]}FeedingCheck上传失败请重新扫码\r\n\r\nMES返回信息:{reuslt0.Item2}";
                        if (stationName.Contains("OP3050")
                            || (stationName.Contains("OP3045") && stationName.Contains("2"))
                            || stationName.Contains("OP2040") || stationName.Contains("OP1030"))
                        //  || stationName.Contains("OP2045"))//20260119
                        {
                            return false;
                        }
                        else
                        {
                            Thread.Sleep(100);
                            SetHelper.NowMaterialCode[iNumber] = "";
                            continue;
                        }
                    }

                    #region 材料绑定上传

                    (bool, string) reuslt = await SetHelper.mesManager.LinkComp(SetHelper.NowMaterialCode[iNumber].GetLinkCompModel(SN, iNumber), iNumber);

                    //(bool, string) reuslt = (true,"MES返回的信息");//测试用
                    linkResult = reuslt.Item1 ? 1 : 2;
                    //SetHelper.resultModel[iNumber].Result2 = reuslt.Item1 ? "OK" : "NG";
                    SetHelper.resultModel[iNumber].MaterialSN = SetHelper.NowMaterialCode[iNumber];
                    string msg = $"{stationName} SN:{SN}--产品LinkComp结果：{reuslt.Item1},消息：{reuslt.Item2}";
                    SetHelper.ListMesMessage.ShowInfoQueue(msg);
                    if (!reuslt.Item1)
                    {
                        showMsg = "";
                        mesReturn = $"条码{SetHelper.NowMaterialCode[iNumber]}LinkComp上传失败请重新扫码\r\n\r\nMES返回信息:{reuslt.Item2}";
                        if (stationName.Contains("OP3050")
                            || (stationName.Contains("OP3045") && stationName.Contains("2"))
                            || stationName.Contains("OP2040"))
                        //   || stationName.Contains("OP2045"))//20260119
                        {
                            return false;
                        }
                        else
                        {
                            Thread.Sleep(100);
                            SetHelper.NowMaterialCode[iNumber] = "";
                            continue;
                        }
                    }
                    else
                    {
                        return true;
                    }

                    #endregion 材料绑定上传
                }
            }
            return false;

            #endregion 材料绑定

            #region 判断结果发送PLC

            //bool result = false;//发plc结果
            //result = SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "扫描材料码结果_" + number, checkInResult);
            //SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 扫描材料码结果写{checkInResult}{(result ? "成功" : "失败")}");

            #endregion 判断结果发送PLC
        }
    }
}
