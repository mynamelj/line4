using MES.Comm;
using MES.Manager;
using MES.SetModel;
using PropertyChanged;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace MES.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class PLCSetViewModel : ProductChangeBase
    {
        public PLCSetViewModel()
        {
            PLCSetting = SetHelper.PLCSetting;
        }



        public override void GetParams(ProductTypeModel product)
        {
            PLCSetting = SetHelper.LoadConfig<PLCSettingModel>(SetHelper.plc, product);
        }

        public PLCSettingModel PLCSetting { get; set; } = new PLCSettingModel();
        public PLCTag SelectPLCTag { get; set; } = new PLCTag();
        public PLCTag EditPLCTag { get; set; } = new PLCTag();
        public PLCGroup SelectPLCGroup { get; set; } = new PLCGroup();
        public PLCGroup EditPLCGroup { get; set; } = new PLCGroup();

        public ObservableCollection<int> DbIndex { get; set; } = new ObservableCollection<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        public ObservableCollection<int> NumberIndex { get; set; } = new ObservableCollection<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        public int SelectDbItem { get; set; } = 1;
        public int CopyFromNumber { get; set; } = 1;
        public int CopyToNumber { get; set; } = 1;

        public ICommand SelectionChange => new RelayCommand(() =>
        {
            if (EditPLCGroup != null && SelectPLCGroup != null)
            {
                EditPLCGroup.GroupName = SelectPLCGroup.GroupName;
                EditPLCGroup.GroupDesc = SelectPLCGroup.GroupDesc;
                EditPLCGroup.GroupType = SelectPLCGroup.GroupType;
                EditPLCGroup.RefreshTime = SelectPLCGroup.RefreshTime;
                //SaveCommand.Execute(null);
            }

        });

        public ICommand TagSelectionChange => new RelayCommand(() =>
        {
            if (SelectPLCTag != null && EditPLCTag != null)
            {
                EditPLCTag.TagAddress = SelectPLCTag.TagAddress;
                EditPLCTag.TagName = SelectPLCTag.TagName;
                EditPLCTag.TagDbArea = SelectPLCTag.TagDbArea;
                EditPLCTag.TagDesc = SelectPLCTag.TagDesc;
                EditPLCTag.DataType = SelectPLCTag.DataType;
                EditPLCTag.UpLimit = SelectPLCTag.UpLimit;
                EditPLCTag.LowLimit = SelectPLCTag.LowLimit;
                EditPLCTag.TagValue = SelectPLCTag.TagValue;
            }
            //SaveCommand.Execute(null);
        });

        public ICommand AddGroupCommand => new RelayCommand<PLCGroup>((g) =>
        {
            if (SetHelper.IsAdmin)
            {
                if (string.IsNullOrEmpty(g.GroupType) || string.IsNullOrEmpty(g.GroupName))
                {
                    MessageBox.Show("请将组信息填写完整！");
                    return;
                }
                if (!IsGroupRepeat(g.GroupType))
                {
                    MessageBox.Show("组类型重复，请检查！");
                    return;
                }
                PLCSetting.ListGroup.Add(new PLCGroup()
                {
                    GroupName = g.GroupName,
                    GroupDesc = g.GroupDesc,
                    GroupType = g.GroupType,
                    RefreshTime = g.RefreshTime
                });

                SaveCommand.Execute(null);
            }
            else
            {
                MessageBox.Show("权限不足");
            }



        });

        public ICommand EditGroupCommand => new RelayCommand<PLCGroup>((g) =>
        {
            if (SetHelper.IsAdmin)
            {
                if (string.IsNullOrEmpty(g.GroupType) || string.IsNullOrEmpty(g.GroupName))
                {
                    MessageBox.Show("请将组信息填写完整！");
                    return;
                }
                if (!IsGroupRepeat(g.GroupType))
                {
                    MessageBox.Show("组类型重复，请检查！");
                    return;
                }
                if (SelectPLCGroup != null)
                {
                    SelectPLCGroup.GroupName = g.GroupName;
                    SelectPLCGroup.GroupDesc = g.GroupDesc;
                    SelectPLCGroup.GroupType = g.GroupType;
                    SelectPLCGroup.RefreshTime = g.RefreshTime;
                    SaveCommand.Execute(null);
                }
                else
                {
                    MessageBox.Show("请选择需要修改的项");
                }
            }
            else
            {
                MessageBox.Show("权限不足");
            }

        });

        public ICommand DeleteGroupCommand => new RelayCommand<PLCGroup>((g) =>
        {
            if (SetHelper.IsAdmin)
            {
                if (SelectPLCGroup != null)
                {
                    PLCSetting.ListGroup.Remove(SelectPLCGroup);
                    SaveCommand.Execute(null);
                }
                else
                {
                    MessageBox.Show("请选择需要删除的项");
                }
            }
            else
            {
                MessageBox.Show("权限不足");
            }

        });

        public bool isAll { get; set; } = false;

        public ICommand AddTagCommand => new RelayCommand<PLCTag>((t) =>
        {
            if (SetHelper.IsAdmin)
            {
                if (string.IsNullOrEmpty(t.TagAddress))
                {
                    MessageBox.Show("请填写地址位！");
                    return;
                }
                //if (!t.TagName.Contains("载具码"))//20250404载具码点位可以重复，进出站载具码可能地址一样
                //{
                //    if (!IsTagRepeat(t.TagDbArea, t.TagAddress))
                //    {
                //        ;
                //        MessageBox.Show("重复点位，请检查！");
                //        return;
                //    }
                //}

                if (SelectPLCGroup != null && SelectPLCGroup.ListTag != null && t != null)
                {
                    PLCTag tag = new PLCTag()
                    {
                        TagAddress = t.TagAddress,
                        TagDbArea = t.TagDbArea,
                        DataType = t.DataType,
                        TagDesc = t.TagDesc,
                        TagName = t.TagName,
                        LowLimit = t.LowLimit,
                        UpLimit = t.UpLimit,
                        TagValue = t.TagValue,
                    };

                    SelectPLCGroup.ListTag.Add(tag);
                    if (isAll)
                    {
                        foreach (var item in SetHelper.products)
                        {
                            var plc = SetHelper.LoadConfig<PLCSettingModel>(SetHelper.plc, item);
                            plc.ListGroup.FirstOrDefault(x => x.GroupType == SelectPLCGroup.GroupType).ListTag.Add(tag);
                            SetHelper.SaveConfig(plc, SetHelper.plc, item);
                        }

                    }

                    SaveCommand.Execute(null);
                }
            }
            else
            {
                MessageBox.Show("权限不足");
            }



        });

        public ICommand EditTagCommand => new RelayCommand<PLCTag>((t) =>
        {
            if (SetHelper.IsAdmin)
            {

                if (string.IsNullOrEmpty(t.TagAddress))
                {
                    MessageBox.Show("请填写地址位！");
                    return;
                }
                //if (!t.TagName.Contains("载具码"))//20250404载具码点位可以重复，进出站载具码可能地址一样
                //{
                //    if (!IsTagRepeat(t.TagDbArea, t.TagAddress))
                //    {
                //        MessageBox.Show("重复点位，请检查！");
                //        return;
                //    }
                //}
                if (SelectPLCGroup != null && SelectPLCGroup.ListTag != null && SelectPLCTag != null && t != null)
                {
                    string TagName = SelectPLCTag.TagName;
                    SelectPLCTag.TagName = t.TagName;
                    SelectPLCTag.TagDbArea = t.TagDbArea;
                    SelectPLCTag.TagAddress = t.TagAddress;
                    SelectPLCTag.TagDesc = t.TagDesc;
                    SelectPLCTag.DataType = t.DataType;
                    SelectPLCTag.UpLimit = t.UpLimit;
                    SelectPLCTag.LowLimit = t.LowLimit;
                    SelectPLCTag.TagValue = t.TagValue;

                    SaveCommand.Execute(null);

                    if (isAll)
                    {
                        foreach (var item in SetHelper.products)
                        {
                            var plc = SetHelper.LoadConfig<PLCSettingModel>(SetHelper.plc, item);
                            var tag = plc.ListGroup.FirstOrDefault(x => x.GroupType == SelectPLCGroup.GroupType)?.ListTag?.FirstOrDefault(x => x.TagName == TagName);

                            if (tag != null)
                            {
                                tag.TagName = t.TagName;
                                tag.TagDbArea = t.TagDbArea;
                                tag.TagAddress = t.TagAddress;
                                tag.TagDesc = t.TagDesc;
                                tag.DataType = t.DataType;
                                tag.UpLimit = t.UpLimit;
                                tag.LowLimit = t.LowLimit;
                                tag.TagValue = t.TagValue;
                                SetHelper.SaveConfig(plc, SetHelper.plc, item);
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("请选择需要修改的项");
                }
            }
            else
            {
                MessageBox.Show("权限不足");
            }


        });

        public ICommand CopyDbCommand => new RelayCommand(() =>
        {
            if (SetHelper.IsAdmin)
            {
                if (CopyFromNumber == CopyToNumber)
                {
                    MessageBox.Show("请选择不同的序号");
                    return;
                }

                PLCSettingModel plcSetting = new PLCSettingModel();
                plcSetting = DataExtend.Clone(PLCSetting);

                //ObservableCollection<PLCGroup> newPlcGroup = new ObservableCollection<PLCGroup>();
                //newPlcGroup = DataExtend.Clone(PLCSetting.ListGroup);
                foreach (var group in PLCSetting.ListGroup)
                {
                    if (group.GroupName != "出站数据读取信号组" && group.GroupName != "设定参数上报组")
                    {
                        foreach (var tag in group.ListTag)
                        {
                            var tagname = tag.TagName;
                            if (!tagname.Contains("_"))
                            {
                                tagname = tagname + "_1";
                            }
                            //修改所选择的DB块
                            if (tagname.Contains("_" + CopyFromNumber))
                            {
                                var newtag = DataExtend.Clone(tag);
                                newtag.TagName = tagname.Replace("_" + CopyFromNumber, "_" + CopyToNumber);
                                if (!group.ListTag.Any(it => it.TagName == newtag.TagName))
                                {
                                    foreach (var items in plcSetting.ListGroup)
                                    {
                                        if (items.GroupName == group.GroupName)
                                        {
                                            items.ListTag.Add(newtag);
                                            //foreach (var item in items.ListTag)
                                            //{
                                            //    if(it)
                                            //}
                                            //item.ListTag

                                        }
                                    }
                                    //newTags.Add(tag);
                                }
                            }
                        }

                    }
                }

                //保存到配置文件

                //string plcjson = JSON.ToJsonFormat(plcSetting);

                //File.WriteAllText(SetHelper.plcpath, plcjson);
                SetHelper.SaveConfig(plcSetting, SetHelper.plc, ProductType);
                MessageBox.Show("复制成功，重启生效");
            }
            else
            {
                MessageBox.Show("权限不足");
            }


        });

        public ICommand DeleteTagCommand => new RelayCommand<PLCTag>((t) =>
        {
            if (SetHelper.IsAdmin)
            {
                if (SelectPLCGroup != null && SelectPLCGroup.ListTag != null && SelectPLCTag != null && t != null)
                {
                    string TagName = SelectPLCTag.TagName;
                    string GroupType = SelectPLCGroup.GroupType;
                    SelectPLCGroup.ListTag.Remove(SelectPLCTag);
                    SaveCommand.Execute(null);


                    if (isAll)
                    {
                        foreach (var item in SetHelper.products)
                        {
                            var plc = SetHelper.LoadConfig<PLCSettingModel>(SetHelper.plc, item);
                            var tag = plc.ListGroup.FirstOrDefault(x => x.GroupType == SelectPLCGroup.GroupType)?.ListTag?.FirstOrDefault(x => x.TagName == TagName);

                            if (tag != null)
                            {
                                plc.ListGroup.FirstOrDefault(x => x.GroupType == SelectPLCGroup.GroupType).ListTag.Remove(tag);
                                SetHelper.SaveConfig(plc, SetHelper.plc, item);
                            }
                        }
                    }
                }
                else
                {
                    MessageBox.Show("请选择需要删除的项");
                }
            }
            else
            {
                MessageBox.Show("权限不足");
            }



        });

        public ICommand EditDBCommand => new RelayCommand(() =>
        {
            if (SetHelper.IsAdmin)
            {
                var item = SelectDbItem;
                foreach (var group in PLCSetting.ListGroup)
                {
                    foreach (var tag in group.ListTag)
                    {
                        //修改所选择的DB块
                        if (tag.TagName.Contains("_" + item))
                        {
                            if (group.GroupType == PLCGroupName.WriteGroup.ToString())
                            {
                                tag.TagDbArea = PLCSetting.WriteDB;
                            }
                            else
                            {
                                tag.TagDbArea = PLCSetting.ReadDB;
                            }
                        }
                        if (SelectDbItem == 1)
                        {
                            if (!tag.TagName.Contains("_"))
                            {
                                if (group.GroupType == PLCGroupName.WriteGroup.ToString())
                                {
                                    tag.TagDbArea = PLCSetting.WriteDB;
                                }
                                else
                                {
                                    tag.TagDbArea = PLCSetting.ReadDB;
                                }
                            }
                        }
                    }
                }
                SaveCommand.Execute(null);
            }
            else
            {
                MessageBox.Show("权限不足");
            }



        });

        public ICommand SaveCommand => new RelayCommand(() =>
        {

            //string plcjson = JSON.ToJsonFormat(PLCSetting);

            //File.WriteAllText(SetHelper.plcpath, plcjson);

            SetHelper.SaveConfig(PLCSetting, SetHelper.plc, ProductType);

        });

        public ICommand ExportCommand => new RelayCommand(() =>
        {
            WriteCS();
        });
        private string Warp = "\r\n";
        private string Space = "  ";

        /// <summary>
        /// cs生成
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void WriteCS()
        {
            try
            {
                string CS_Content = "using System;" + Warp;
                CS_Content += "using System.Collections.Generic;" + Warp;
                CS_Content += "using System.Linq;" + Warp;
                CS_Content += "using System.Text;" + Warp + Warp;
                CS_Content += "namespace MES.Comm" + Warp;
                CS_Content += "{" + Warp;//////括号1

                #region 各组标签枚举
                CS_Content += Space + "public enum PLCTagItem" + Warp;
                CS_Content += Space + "{" + Warp;///括号2
                foreach (var GroupItem in PLCSetting.ListGroup)
                {
                    foreach (var tagItem in GroupItem.ListTag)
                    {
                        CS_Content += Space + Space + tagItem.TagName + "," + Warp;
                    }
                }
                CS_Content += Space + "}" + Warp;///括号2

                #endregion

                CS_Content += "}" + Warp;//////括号1
                if (File.Exists("PLCTagItem.cs"))
                {
                    File.Delete("PLCTagItem.cs");
                }
                StreamWriter filewrite1 = new StreamWriter("PLCTagItem.cs", true);

                filewrite1.Write(CS_Content);
                filewrite1.Close();
                MessageBox.Show("数据导出成功,保存在" + "PLCTagItem.cs", "提示");
            }
            catch (Exception ex)
            {
                MessageBox.Show("发生异常" + ex, "提示");
            }
        }


        private bool IsTagRepeat(int tagDB, string tagAddress)
        {
            try
            {
                foreach (var group in PLCSetting.ListGroup)
                {
                    var tag = group.ListTag.Where(x => x != SelectPLCTag && x.TagDbArea == tagDB && x.TagAddress == tagAddress).FirstOrDefault();
                    if (tag != null)
                    {
                        return false;
                    }
                }

            }
            catch (Exception ex)
            {
                SetHelper.ListPLCMessage.ShowInfoQueue(ex.ToString());
            }

            return true;
        }

        private bool IsGroupRepeat(string groupType)
        {
            try
            {
                var group = PLCSetting.ListGroup.Where(x => x != SelectPLCGroup && x.GroupType == groupType).FirstOrDefault();
                if (group != null)
                {
                    return false;
                }

            }
            catch (Exception ex)
            {
                SetHelper.ListPLCMessage.ShowInfoQueue(ex.ToString());
            }

            return true;
        }

        public ICommand ToJsonCommand => new RelayCommand(() =>
        {
            try
            {


                List<AlarmData> alarmDatas = new List<AlarmData>();
                System.Windows.Forms.OpenFileDialog openFile = new System.Windows.Forms.OpenFileDialog();
                if (openFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string path = openFile.FileName;
                    List<string> data = File.ReadLines(path).ToList();
                    foreach (var item in data)
                    {
                        string[] dt = item.Split('\t');
                        alarmDatas.Add(new AlarmData()
                        {
                            ID = dt[1].Obj2Int(),
                            AlarmCode = dt[0].ToString(),
                            AlarmInfo = dt[2].ToString()
                        });
                    }
                    if (alarmDatas.Count > 0)
                    {
                        File.WriteAllText(SetHelper.alarmpath, alarmDatas.ToJsonFormat());
                        MessageBox.Show("导出报警Json成功");
                    }

                }
            }
            catch (Exception ex)
            {
                SetHelper.ListPLCMessage.ShowInfoQueue(ex.ToString());
            }

        });

    }

    public class AlarmData
    {
        public int ID { get; set; }
        public string AlarmCode { get; set; }
        public string AlarmInfo { get; set; }
    }
}
