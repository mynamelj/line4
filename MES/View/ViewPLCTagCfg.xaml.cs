using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TsExtend;
using TsExtend.Comm;
using TsExtend.FileHelper;
using TSWeighingSystem.ViewModel.Set;

namespace TSWeighingSystem.Views.Set.PLC设置
{

    public partial class ViewPLCTagCfg : Window
    {
        ViewModelSetPLC vmSet = new ViewModelSetPLC();

        List<PlcGroup> listGroup = new List<PlcGroup>();

        public ViewPLCTagCfg()
        {
            InitializeComponent();
            this.DataContext = vmSet;
            this.Loaded += MainWindow_Loaded;
        }

        /// <summary>
        /// 界面加载
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            vmSet.ListGroup.Clear();
            vmSet.ListTag.Clear();
            listGroup = FileListHelper.GetListT<List<PlcGroup>>("PLCGroup");
            if (listGroup != null)
            {
                foreach (PlcGroup item in listGroup)
                {
                    vmSet.ListGroup.Add(new PlcGroupItem()
                    {
                        CheckIntervel = item.CheckIntervel,
                        Comment = item.Comment,
                        GroupName = item.GroupName,
                        GroupType = item.GroupType,
                    });
                }

                if (vmSet.ListGroup.Count > 0)
                {
                    vmSet.SelectGroup = vmSet.ListGroup[0];
                }
            }
        }

        /// <summary>
        /// 选中组配置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            vmSet.ListTag.Clear();
            if (vmSet.SelectGroup == null)
            {
                return;
            }
            var tags = listGroup.FirstOrDefault(x => x.GroupName == vmSet.SelectGroup.GroupName);
            if (tags != null)
            {
                foreach (var item in tags.ListTag)
                {
                    vmSet.ListTag.Add(new PlcTagItem()
                    {
                        Comment = item.Comment,
                        DefaultValue = item.DefaultValue,
                        PlcDevice = item.PlcDevice,
                        TagAddress = item.TagAddress,
                        TagName = item.TagName,
                        TagType = item.TagType,
                    });
                }
                if (vmSet.ListTag.Count > 0)
                {
                    vmSet.SelectTag = vmSet.ListTag[0];
                }
            }
        }

        /// <summary>
        /// 添加组配置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnClickAddGroup(object sender, RoutedEventArgs e)
        {
            if (vmSet.EditGroup == null)
            {
                MessageBox.Show("意外错误");
                return;
            }
            if (vmSet.EditGroup.CheckIntervel < 200
           || vmSet.EditGroup.GroupName == ""
           || vmSet.EditGroup.GroupType == "")
            {
                MessageBox.Show("请检查参数");
                return;
            }

            if (vmSet.ListGroup.Count(x => x.GroupName == vmSet.EditGroup.GroupName) > 0)
            {
                MessageBox.Show("重复配置");
                return;
            }
            PlcGroupItem group = new PlcGroupItem()
            {
                CheckIntervel = vmSet.EditGroup.CheckIntervel,
                Comment = vmSet.EditGroup.Comment,
                GroupName = vmSet.EditGroup.GroupName,
                GroupType = vmSet.EditGroup.GroupType
            };

            listGroup.Add(new PlcGroup()
            {
                CheckIntervel = vmSet.EditGroup.CheckIntervel,
                Comment = vmSet.EditGroup.Comment,
                GroupName = vmSet.EditGroup.GroupName,
                GroupType = vmSet.EditGroup.GroupType
            });


            vmSet.ListGroup.Add(group);
            vmSet.SelectGroup = group;
        }

        /// <summary>
        /// 修改组配置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnClickModify(object sender, RoutedEventArgs e)
        {
            if (vmSet.SelectGroup == null)
            {
                MessageBox.Show("请选择需要修改的");
                return;
            }
            if (vmSet.EditGroup.CheckIntervel < 200
           || vmSet.EditGroup.GroupName == ""
           || vmSet.EditGroup.GroupType == "")
            {
                MessageBox.Show("请检查参数");
                return;
            }

            var group = listGroup.FirstOrDefault(x => x.GroupName == vmSet.SelectGroup.GroupName);

            group.CheckIntervel = vmSet.EditGroup.CheckIntervel;
            group.Comment = vmSet.EditGroup.Comment;
            group.GroupName = vmSet.EditGroup.GroupName;
            group.GroupType = vmSet.EditGroup.GroupType;

            vmSet.SelectGroup.CheckIntervel = vmSet.EditGroup.CheckIntervel;
            vmSet.SelectGroup.Comment = vmSet.EditGroup.Comment;
            vmSet.SelectGroup.GroupName = vmSet.EditGroup.GroupName;
            vmSet.SelectGroup.GroupType = vmSet.EditGroup.GroupType;
        }

        /// <summary>
        /// 删除组配置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnClickDelete(object sender, RoutedEventArgs e)
        {
            if (vmSet.SelectGroup == null)
            {
                MessageBox.Show("请选择需要修改的");
                return;
            }
            string strName = vmSet.SelectGroup.GroupName;
            vmSet.ListGroup.Remove(vmSet.SelectGroup);
            listGroup.Remove(listGroup.First(x => x.GroupName == strName));
        }

        /// <summary>
        /// 添加标签配置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnClickAddTag(object sender, RoutedEventArgs e)
        {
            if (vmSet.EditTag == null)
            {
                MessageBox.Show("意外错误");
                return;
            }

            if (vmSet.EditTag.DefaultValue == ""
             || vmSet.EditTag.PlcDevice == ""
             || vmSet.EditTag.TagAddress == ""
             || vmSet.EditTag.TagName == ""
             || vmSet.EditTag.TagType == "")
            {
                MessageBox.Show("请检查参数");
                return;
            }

            if (vmSet.ListTag.Count(x => x.TagName == vmSet.EditTag.TagName) > 0)
            {
                MessageBox.Show("重复配置");
                return;
            }
            PlcTagItem group = new PlcTagItem()
            {
                Comment = vmSet.EditTag.Comment,
                DefaultValue = vmSet.EditTag.DefaultValue,
                PlcDevice = vmSet.EditTag.PlcDevice,
                TagAddress = vmSet.EditTag.TagAddress,
                TagName = vmSet.EditTag.TagName,
                TagType = vmSet.EditTag.TagType,
            };

            PlcTag tag = new PlcTag()
            {
                Comment = vmSet.EditTag.Comment,
                DefaultValue = vmSet.EditTag.DefaultValue,
                PlcDevice = vmSet.EditTag.PlcDevice,
                TagAddress = vmSet.EditTag.TagAddress,
                TagName = vmSet.EditTag.TagName,
                TagType = vmSet.EditTag.TagType,
            };
            listGroup.FirstOrDefault(x => x.GroupName == vmSet.SelectGroup.GroupName).ListTag.Add(tag);
            vmSet.ListTag.Add(group);
            vmSet.SelectTag = group;
        }

        /// <summary>
        /// 修改标签配置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnClickModifyTag(object sender, RoutedEventArgs e)
        {
            if (vmSet.SelectTag == null)
            {
                MessageBox.Show("请选择需要修改的");
                return;
            }
            if (vmSet.EditTag.DefaultValue == ""
             || vmSet.EditTag.PlcDevice == ""
             || vmSet.EditTag.TagAddress == ""
             || vmSet.EditTag.TagName == ""
             || vmSet.EditTag.TagType == "")
            {
                MessageBox.Show("请检查参数");
                return;
            }
            if (vmSet.SelectTag.TagName != vmSet.EditTag.TagName && vmSet.ListTag.Count(x => x.TagName == vmSet.EditTag.TagName) > 0)
            {
                MessageBox.Show("重复配置");
                return;
            }
            var tag = listGroup.FirstOrDefault(x => x.GroupName == vmSet.SelectGroup.GroupName).ListTag.First(x => x.TagName == vmSet.SelectTag.TagName);
            vmSet.SelectTag.Comment = vmSet.EditTag.Comment;
            vmSet.SelectTag.DefaultValue = vmSet.EditTag.DefaultValue;
            vmSet.SelectTag.PlcDevice = vmSet.EditTag.PlcDevice;
            vmSet.SelectTag.TagAddress = vmSet.EditTag.TagAddress;
            vmSet.SelectTag.TagName = vmSet.EditTag.TagName;
            vmSet.SelectTag.TagType = vmSet.EditTag.TagType;

            tag.Comment = vmSet.EditTag.Comment;
            tag.DefaultValue = vmSet.EditTag.DefaultValue;
            tag.PlcDevice = vmSet.EditTag.PlcDevice;
            tag.TagAddress = vmSet.EditTag.TagAddress;
            tag.TagName = vmSet.EditTag.TagName;
            tag.TagType = vmSet.EditTag.TagType;
        }

        /// <summary>
        /// 删除标签配置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnClickDeleteTag(object sender, RoutedEventArgs e)
        {
            if (vmSet.SelectTag == null)
            {
                MessageBox.Show("请选择需要修改的");
                return;
            }
            listGroup.FirstOrDefault(x => x.GroupName == vmSet.SelectGroup.GroupName).ListTag.Remove(listGroup.FirstOrDefault(x => x.GroupName == vmSet.SelectGroup.GroupName).ListTag.First(x => x.TagName == vmSet.SelectTag.TagName));
            vmSet.ListTag.Remove(vmSet.SelectTag);
        }

        private string Warp = "\r\n";
        private string Space = "  ";

        /// <summary>
        /// cs生成
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnClickSaveCs(object sender, RoutedEventArgs e)
        {
            try
            {
                string CS_Content = "using System;" + Warp;
                CS_Content += "using System.Collections.Generic;" + Warp;
                CS_Content += "using System.Linq;" + Warp;
                CS_Content += "using System.Text;" + Warp + Warp;
                CS_Content += "namespace MES.Comm" + Warp;
                CS_Content += "{" + Warp;//////括号1

                #region Plcz组枚举
                CS_Content += Space + "public enum PlcGroupInfo" + Warp;
                CS_Content += Space + "{" + Warp;///括号2
                                                 ///
                foreach (var item in vmSet.ListGroup)
                {
                    CS_Content += Space + Space + item.GroupName + "," + Warp;
                }
                CS_Content += Space + "}" + Warp;///括号2
                #endregion

                #region 各组标签枚举
                CS_Content += Space + "public enum PLCTagItem" + Warp;
                CS_Content += Space + "{" + Warp;///括号2
                foreach (var GroupItem in listGroup)
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

        /// <summary>
        /// xml保存
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnClickSave(object sender, RoutedEventArgs e)
        {
            FileListHelper.SaveListT(listGroup, "PLCGroup");
            listGroup = FileListHelper.GetListT<List<PlcGroup>>("PLCGroup");
            MessageBox.Show("保存成功!");
        }
    }
}

