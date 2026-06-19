using MES.Comm;
using MES.Manager;
using MES.SetModel;
using PropertyChanged;
using S7.Net;
using System.Collections;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MES.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class PLCMonitorViewModel : ProductChangeBase
    {
        public PLCMonitorViewModel()
        {
            MonitorHelper.siemens.ChangeAll = true;
            MonitorHelper.siemens.OnDataChange += Siemens_OnDataChange;
            PLCSetting = SetHelper.PLCSetting;
            MonitorHelper.siemens.InitailzePLC();
        }

        /// <summary>
        /// 数据回调
        /// </summary>
        /// <param name="TagName"></param>
        /// <param name="Address"></param>
        /// <param name="Bit"></param>
        /// <param name="TagValue"></param>
        /// <exception cref="NotImplementedException"></exception>
        private void Siemens_OnDataChange(string TagName, int Address, int Bit, object TagValue)
        {
            foreach (var PLCSettingGroup in PLCSetting.ListGroup)
            {
                foreach (var PLCTag in PLCSettingGroup.ListTag)
                {
                    if (!PLCTag.TagName.Contains('_'))
                    {
                        PLCTag.TagName = PLCTag.TagName + "_1";
                    }
                    if (!PLCTag.TagName.Contains("_1") && !PLCTag.TagName.Contains("_2") && !PLCTag.TagName.Contains("_3") && !PLCTag.TagName.Contains("_4") && !PLCTag.TagName.Contains("_5") && !PLCTag.TagName.Contains("_6"))
                    {
                        PLCTag.TagName = PLCTag.TagName + "_1";
                    }
                    if (TagName.Equals(PLCTag.TagName))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            if (TagValue is BitArray)
                            {
                                List<string> ls = new List<string>();
                                foreach (bool bit in (BitArray)TagValue)
                                {
                                    ls.Add(bit ? "1" : "0");
                                }
                                PLCTag.CurrentValue = string.Join(",", ls);
                            }
                            else
                            {
                                PLCTag.CurrentValue = TagValue;
                                PLCTag.BackColor = TagValue.ToString().ToLower() == "true" ? "SkyBlue" : "White";
                            }
                        });
                        break;
                    }
                }
            }
        }

        public override void GetParams(ProductTypeModel product)
        {
            PLCSetting = SetHelper.LoadConfig<PLCSettingModel>(SetHelper.plc, product);
            MonitorHelper.siemens.InitailzePLC();
        }

        public PLCSettingModel PLCSetting { get; set; } = new PLCSettingModel();
        public PLCTag SelectPLCTag { get; set; } = new PLCTag();
        public PLCTag EditPLCTag { get; set; } = new PLCTag();
        public PLCGroup SelectPLCGroup { get; set; } = new PLCGroup();
        public PLCGroup EditPLCGroup { get; set; } = new PLCGroup();

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
                EditPLCTag.CurrentValue = SelectPLCTag.CurrentValue;
            }
            //SaveCommand.Execute(null);
        });

        public ObservableCollection<bool> isMonitor { get; set; } = new ObservableCollection<bool>();
    }
}