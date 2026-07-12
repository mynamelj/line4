using MES.Comm;
using MES.MesModel.Request;
using MES.MesModel.Response;
using MES.SetModel;
using MES.View;
using MES.ViewModel;
using System.Collections.ObjectModel;
using System.Windows;
using DateTime = System.DateTime;
namespace MES.Manager
{
    public partial class DataManager
    {
        /// <summary>
        /// 出站
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        /// 

        Dictionary<string, string> lightDic = new Dictionary<string, string>
        {
            { "灯号1", "输入轴" },
            { "灯号2", "中间轴" },
            { "灯号3", "差速器" }
        };



        public async Task ProductCheckOutAsync(string number)
        {
            int iNumber = Convert.ToInt32(number) - 1;
            string stationName = SetHelper.StationNumber.numberGroups[iNumber].Name;
            try
            {
                //手动工位增加清除登录状态的功能
                if (SetHelper.MesSetting.ListGroup[iNumber].IsMauaStation == "1")
                {
                    MainWindowViewModel.CheckInOrOut = true;
                    if (stationName.ToUpper().Contains("OP5130"))
                    {
                        SetHelper.siemens.WriteItem(SetModel.PLCGroupName.WriteGroup, "操作权限_2", false);
                    }

                    SetHelper.siemens.WriteItem(SetModel.PLCGroupName.WriteGroup, "操作权限_1", false);
                }
                //SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, PLCTagItem.产品SN, "A12123jd7");
                string SN = SetHelper.NowProductCode;

                #region 读流程ID

                object obj = new object();
                int SeqID = 0;
                int CheckInResult = 0;
                string carryID = "";
                if (SetHelper.siemens.ReadItem(PLCGroupName.ReadGroup, "PLC出站流程ID_" + number, ref obj))
                {
                    SeqID = obj.Obj2Int();
                    SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读到流程ID为{SeqID}");
                }
                else
                {
                    SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读流程ID失败");
                    //return;
                }
                if (SetHelper.siemens.ReadItem(PLCGroupName.ReadGroup, "产品SN_" + number, ref obj))
                {
                    SN = obj.Obj2String();
                    SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读到产品SN为{SN}");
                }
                else
                {
                    SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读产品SN失败");
                    //return;
                }
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
                if (SetHelper.siemens.ReadItem(PLCGroupName.WriteGroup, "进站结果_" + number, ref obj))
                {
                    CheckInResult = obj.Obj2Int();
                    SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读到进站结果为{CheckInResult}");
                }
                else
                {
                    SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读进站结果失败");
                    //return;
                }

                int outChannel = 0;
                if (stationName.ToUpper().Contains("OP1040"))
                {
                    //OP1040出站流道信息增加
                    if (SetHelper.siemens.ReadItem(PLCGroupName.ReadGroup, "出站流道_" + number, ref obj))
                    {
                        outChannel = obj.Obj2Int();
                        SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读到出站流道为{outChannel}");
                    }
                    else
                    {
                        SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读出站流道失败");
                        //return;
                    }
                }

                #endregion 读流程ID

                object data = new object();
                int checkOutResult = 1;
                List<DC_Info> dcInfoList = new List<DC_Info>();
                List<CompList> compList = new List<CompList>();

                //string json = File.ReadAllText(SetHelper.materialpath);
                ObservableCollection<MaterailModel> materails = SetHelper.ReadSys<ObservableCollection<MaterailModel>>(SetHelper.materialpath);

                //string jsonGlue = File.ReadAllText(SetHelper.gluepath);
                ObservableCollection<MaterailOnOffModel> glueMaterails = SetHelper.ReadSys<ObservableCollection<MaterailOnOffModel>>(SetHelper.gluepath);

                //只有返修状态（5或6）且工站为 OP5005 或 OP2010 时才不上传，其余全部上传。
                if (!((CheckInResult == 5 || CheckInResult == 6 ) ))
                {
                    #region 读取产品需要上传MES的数据
                    //结构：Dictionary<组名, Dictionary<标签名, 数据项对象>>
                    //从这个大字典中，取出键名为 "CheckOutGroup"（出站组）的那一部分子字典。
                    var dic = SetHelper.siemens.DicDataItems[PLCGroupName.CheckOutGroup.ToString()];//<TagName,DataItem>                                                                    //读取对应工位的参数
                    dic = dic.Where(it => it.Key.Contains("_" + number)).ToDictionary(it => it.Key, it => it.Value);

                    if ( dic.Count != 0)
                    {
                        List<string> TagNameList = dic.Keys.ToList();
                        var dataItems = dic.Select(x => x.Value).ToArray();
                        if (!SetHelper.siemens.ReadItems(dataItems, ref data))
                        {
                            SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读取产品出站数据失败！");
                        }
                        if (data is object[] datarray)
                        {
                            for (int i = 0; i < datarray.Length; i++)
                            {
                                SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读到{TagNameList[i]}为{datarray[i]}");
                                PLCTag tag = SetHelper.PLCSetting.ListGroup.FirstOrDefault(x => x.GroupType == PLCGroupName.CheckOutGroup.ToString())?.ListTag.FirstOrDefault(x => TagNameList[i].Contains(x.TagName));
                                if (tag?.DataType.ToLower() == "string")
                                {
                                    compList.Add(new CompList() { CompID = datarray[i].ToString(), Qty = 1 });
                                }
                                else
                                {
                                    DC_Info dC_Info = new DC_Info()
                                    {
                                        Item = TagNameList[i].Substring(0, TagNameList[i].LastIndexOf('_')),//去掉下划线
                                        Value = datarray[i].ToString(),
                                        Result = "Pass",
                                    };
                                    dcInfoList.Add(dC_Info);
                                }
                            }
                        }
                    }

                    #endregion 读取产品需要上传MES的

                    #region 获取compList物料信息 放批次号及使用的数量

                    // 处理批追物料（普通批次料，如螺栓、垫片等）
                    if (materails != null && materails.Count > 0)
                    {
                        // 根据当前工位是否为"选垫工位"走不同逻辑
                        // IsSelectStation=="1" 表示该工位存在多种规格垫片，需要根据PLC灯号动态选择
                        if (SetHelper.MesSetting.ListGroup[iNumber].IsSelectStation != "1")
                        {
                            // ---- 普通工位：直接遍历所有已上料的批追物料 ----
                            foreach (var item in materails)
                            {
                                // 过滤条件：
                                // 1. GlueCode不为空（即该物料已上料，有有效批次码）
                                // 2. LocationNo == stationName（只取本工位的物料，排除其他工位）
                                if (!string.IsNullOrEmpty(item.GlueCode) && item.LocationNo == stationName)
                                {
                                    // 将物料批次码和本次使用数量加入compList，用于出站时上报MES
                                    // CompID：物料批次条码
                                    // Qty：本次工序的使用数量（由上料时配置）
                                    compList.Add(new CompList() { CompID = item.GlueCode, Qty = item.UseCountOnce });
                                }
                            }
                        }
                        else
                        {
                            //  选垫工位 垫片规格由PLC灯号决定，需动态读取 
                            // 从PLC读取组（ReadGroup）的标签配置中，找出所有含"灯号"的标签
                            // 支持配置多个灯号标签（对应多种垫片物料的情况）
                            var tags = SetHelper.PLCSetting.ListGroup
                                .FirstOrDefault(x => x.GroupType == PLCGroupName.ReadGroup.ToString())?
                                .ListTag.Where(x => x.TagName.Contains("灯号")).ToList();

                            if (tags != null && tags.Count > 0)
                            {
                                foreach (var tag in tags)
                                {
                                    string tagName = tag.TagName;

                                    if (!lightDic.TryGetValue(tagName, out string type))
                                    {
                                        SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 未配置{tagName}对应的垫片类型");
                                        continue;
                                    }

                                    int lightID = 0;

                                    if (SetHelper.siemens.ReadItem(PLCGroupName.ReadGroup, tagName + "_" + number, ref obj))
                                    {
                                        lightID = obj.Obj2Int();
                                        SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读到出站{tagName}为{lightID}");
                                    }
                                    else
                                    {
                                        SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 读出站{tagName}失败");
                                        continue;
                                    }

                                    var filterMaterails = materails.Where(m =>
                                        !string.IsNullOrEmpty(m.GlueCode)
                                        && m.LocationNo == stationName
                                        && !string.IsNullOrEmpty(m.MaterialName)
                                        && m.MaterialName.Contains(type)
                                        && m.LightNumber == lightID);

                                    foreach (var item in filterMaterails)
                                    {
                                        compList.Add(new CompList()
                                        {
                                            CompID = item.GlueCode,
                                            Qty = item.UseCountOnce
                                        });
                                    }
                                }

                                // 非垫片类通用物料，只加一次，避免在多个灯号循环中重复扣料
                                foreach (var item in materails)
                                {
                                    if (!string.IsNullOrEmpty(item.GlueCode)
                                        && item.LocationNo == stationName
                                        && item.LightNumber == 0)
                                    {
                                        compList.Add(new CompList()
                                        {
                                            CompID = item.GlueCode,
                                            Qty = item.UseCountOnce
                                        });
                                    }
                                }
                            }
                        }
                    }

                    // 处理胶水物料（GlueMaterial，如密封胶、润滑脂等）
                    if (glueMaterails != null && glueMaterails.Count > 0)
                    {
                        // 同样区分普通工位和选垫工位
                        if (SetHelper.MesSetting.ListGroup[iNumber].IsSelectStation != "1")
                        {
                            // ---- 普通工位：仅当该工位配置为"胶水工位"时才上报胶水物料 ----
                            // IsGlueStation=="1" 表示本工位有涂胶工序（如OP3030涂胶工位）
                            if (SetHelper.MesSetting.ListGroup[iNumber].IsGlueStation == "1")
                            {
                                foreach (var item in glueMaterails)
                                {
                                    // 情况1：本工位自己的胶水，正常上报
                                    if (!string.IsNullOrEmpty(item.GlueCode) && item.LocationNo == stationName)
                                    {
                                        compList.Add(new CompList() { CompID = item.GlueCode, Qty = 1 });
                                    }

                                    // OP3021工位特殊处理
                                    // OP3021工位本身不涂胶，但其油底壳需要使用OP3030工位的胶水码
                                    // 因此出站时需要带上OP3030的胶水码一起上报给MES
                                    if (!string.IsNullOrEmpty(item.GlueCode)
                                        && item.LocationNo.ToUpper().Contains("OP3030")
                                        && stationName.Contains("OP3021"))
                                    {
                                        compList.Add(new CompList() { CompID = item.GlueCode, Qty = 1 });
                                    }
                                }
                            }
                            // 若 IsGlueStation != "1"，则不上报胶水物料（非胶水工位无需处理）
                        }
                        else
                        {
                            // 选垫工位胶水料不需要按灯号区分，直接按工位匹配上报 
                            // 注意：此处历史代码保留了按灯号筛选的注释逻辑，但实际已简化为直接按工位匹配
                            // 原因：选垫工位的胶水（如润滑脂）不受垫片选型影响，每次都要用
                            foreach (var item in glueMaterails)
                            {
                                if (!string.IsNullOrEmpty(item.GlueCode) && item.LocationNo == stationName)
                                {
                                    // 胶水每次使用数量固定为1（一桶/一支）
                                    compList.Add(new CompList() { CompID = item.GlueCode, Qty = 1 });
                                }
                            }
                        }
                    }

                    #endregion 获取compList物料信息 放批次号及使用的数量
                }


                #region 数据上传MES

                // PictureUploadAsync();

                //SN = "1033115GA1000000STC00190";//测试用
                //carryID = "CSF-G01-1-KTRZ-991";//测试用

                (bool, string, SN_InfoItem[], string) response = await SetHelper.mesManager.CheckOut(dcInfoList.GetSNCheckoutModel(compList, SN, carryID, iNumber, outChannel), iNumber);
                //(bool, string, SN_InfoItem[], string) response = (false, "该工位物料报警", null, "完整JSON\r\n"+"1"+"\r\n" + "1" + "\r\n" + "1" + "\r\n" + "1" + "\r\n" + "1" + "\r\n" + "1" + "\r\n" + "1" + "\r\n" + "1" + "\r\n" + "1" + "\r\n" + "1" + "\r\n" + "1" + "\r\n" + "1" + "\r\n" + "1" + "\r\n" + "1" + "\r\n");
                string msg = $"{stationName} 载具码:{carryID} \r\n\r\nSN码:{SN}  产品出站结果：{response.Item1},\r\n\r\nMES返回消息：{response.Item2}\r\n\r\nMES返回完整信息:\r\n{response.Item4}";
                SetHelper.ListMesMessage.ShowInfoQueue(msg);
                checkOutResult = response.Item1 ? 1 : 2;
                //将结果保存到结果Model中，供界面显示
                SetHelper.resultModel[iNumber].Result3 = response.Item1 ? "OK" : "NG";
                SetHelper.resultModel[iNumber].CheckOutSN = SN;

                #endregion 数据上传MES

                #region 写出站完成

                // MES返回消息同时包含"物料不足"和"预警"关键字
                // 说明物料快用完了，但还能继续生产，属于"黄色预警"
                // 例如：三相螺栓数量不足但尚未断料，提醒操作员及时补料
                if (response.Item2.Contains("物料不足") && response.Item2.Contains("预警"))
                {
                    // 出站结果置为3（约定：1=OK, 2=NG, 3=物料不足预警, 4=物料不足报警）
                    checkOutResult = 3;
                    // 必须切回UI线程操作界面（WPF规定）
                    await Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (!MaterialWarnView.IsOpened)
                        {
                            // 预警弹窗未打开，直接新建并显示（橙色文字）
                            materialWarnView = new MaterialWarnView("");
                            materialWarnView.Msg = msg; // msg 包含工站名、SN码、MES完整返回信息
                            materialWarnView.Show();
                        }
                        else
                        {
                            // 预警弹窗已打开（可能是上一条消息），先关闭旧的再打开新的
                            // 保证界面显示的是最新预警信息
                            materialWarnView.Close();
                            materialWarnView = new MaterialWarnView("");
                            materialWarnView.Msg = msg;
                            materialWarnView.Show();
                        }
                    });
                }
                // 业务含义：MES返回消息包含"物料不足"但不含"预警"
                // 说明物料已经断料或严重不足，属于"红色报警"，生产可能需要停线
                else if (response.Item2.Contains("物料不足"))
                {
                    // 出站结果置为4
                    checkOutResult = 4;

                    await Application.Current.Dispatcher.BeginInvoke(() =>
                    {
                        if (!MaterialAlarmView.IsOpened)
                        {
                            // 报警弹窗未打开，直接新建并显示（红色文字，标题"Alarm!报警!"）
                            materialAlarmView = new MaterialAlarmView("");
                            materialAlarmView.Msg = msg;
                            materialAlarmView.Show();
                        }
                        else
                        {
                            // 报警弹窗已打开，关闭旧的再显示新的
                            materialAlarmView.Close();
                            materialAlarmView = new MaterialAlarmView("");
                            materialAlarmView.Msg = msg;
                            materialAlarmView.Show();
                        }
                    });
                }

                // 向PLC写出站结果
                bool result = false;

                // 将出站结果（1/2/3/4）写入PLC对应工位的"出站结果"寄存器
                // PLC读取该值后据此控制流水线动作（如放行/停线/报警灯亮起）
                result = SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "出站结果_" + number, checkOutResult);
                SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 出站结果写{checkOutResult},{(result ? "成功" : "失败")}");

                // 将PC出站流程ID写回PLC，PLC用此ID与进站流程ID配对校验流程完整性
                result = SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "PC出站流程ID_" + number, SeqID);
                SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 出站流程ID写{SeqID},{(result ? "成功" : "失败")}");

                // 清空当前产品码缓存，避免影响下一个产品的出站流程
                SetHelper.NowProductCode = "";

                // 业务背景：出站时MES会对该产品消耗的批追物料（如三相螺栓批次码）进行扣料
                // 如果某批次物料数量不够扣，MES会在 response.Item3（SN_Info[]）中返回具体哪个物料不足
                // 此时需要将上料页面中对应物料的批次码清空，提示操作员重新扫码上料
                if (response.Item3 != null && materails != null)
                {
                    // 出站失败 且 MES返回了具体的物料不足信息列表
                    if (response.Item1 == false && response.Item3.Count() > 0)
                    {
                        foreach (var sninfo in response.Item3) // 遍历MES返回的每条物料不足信息
                        {
                            foreach (var item in materails) // 遍历当前工位已上料的物料列表
                            {
                                // 匹配：物料批次码一致 且 MES消息中含"不够扣料"关键字
                                // 说明该批次码对应的物料在本次出站中数量不足
                                if (item.GlueCode == sninfo.SN && sninfo.Msg_ID.Contains("不够扣料"))
                                {
                                    // 清空该物料的批次码
                                    // 操作员在"批次号上料"页面会看到该物料码变空，需重新扫码上料
                                    item.GlueCode = "";
                                }
                            }
                        }

                        // 通过消息总线（发布-订阅模式）将更新后的物料列表推送到"批次号上料"界面
                        // MaterialOnOffViewModel 订阅了此事件，会更新UI并触发相应的下料接口调用
                        MessageManager.PublishMessage(materails);
                    }
                }
                #endregion 写出站完成

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
                SetHelper.DateEnd = DateTime.Now;

                // GetOEEDataAsync(carryID);
            }
            catch (Exception ex)
            {
                SetHelper.ListPLCMessage.ShowInfoQueue($"{stationName} 产品出站出错--{ex.ToString()}");
            }
        }
    }
}
