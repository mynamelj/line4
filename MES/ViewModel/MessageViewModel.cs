using MES.Comm;
using MES.Manager;
using MES.MesModel.Request;
using MES.SetModel;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace MES.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class MessageViewModel
    {

        public MessageViewModel()
        {
            //SetHelper.ResultModel = new ResultModel()
            //{
            //    Result1 = "OK",
            //    Result2 = "OK",
            //    Result3 = "OK",
            //    MaterialSN = "IYASDBJDNJKASND&HJASDKJNAS_OAJSDNK2IASNDJ1829EHDJNHJASDKJNAS_OAJ",
            //    CheckInSN = "12376842JSDKFJSNDE8",
            //    CheckOutSN = "12376842JSDKFJSNDE8"
            //};
            //初始化赋值
            Opid = SetHelper.Opid;

            ShowMessage();
            if (Opid != null)
            {
                oldOpid = new OPID[Opid.Length];
                for (int i = 0; i < Opid.Length; i++)
                {                                                                                                                                                                                                                                                                                                                                                                                                       
                    oldOpid[i] = new OPID();
                    oldOpid[i].Id = Opid[i].Id;
                }
            }

        }

        public MesSettingModel MesSetting { get; set; } = SetHelper.MesSetting;
        public static ObservableCollection<MessageModel> ListPLCMessage { get; set; } = new ObservableCollection<MessageModel>();
        public static ObservableCollection<MessageModel> ListMesMessage { get; set; } = new ObservableCollection<MessageModel>();
        public static ObservableCollection<MessageModel> ListScanMessage { get; set; } = new ObservableCollection<MessageModel>();
        public static ObservableCollection<MessageModel> ListOEEMessage { get; set; } = new ObservableCollection<MessageModel>();
        public static ObservableCollection<MessageModel> ListOtherMessage { get; set; } = new ObservableCollection<MessageModel>();
        public Visibility IsSpoutOil { get; set; } = Visibility.Collapsed;
        public string Content { get; set; } = "喷油开启";
        //public Visibility LinkVis { get; set; } = SetHelper.MesSetting.ListGroup[0].MaterialCount == 0 ? Visibility.Collapsed : Visibility.Visible;

        public List<ResultModel> ResultModel { get; set; }
        public OPID[] Opid { get; set; }
        public string ButtonTime { get; set; } = "";
        public string PlcHeart { get; set; } = "";
        public string ProductType { get; set; } = "";
        public string RepairStatus { get; set; }
        public Brush RepairStatusColor { get; set; } = Brushes.Green;
        public Visibility RepairStationVisibility { get; set; } = Visibility.Collapsed;
        public static string PlcHeartStatic { get; set; } = "";
        private OPID[] oldOpid;

        public ICommand SpoutOilCommand => new RelayCommand<string>((s) =>
        {
            if (!SetHelper.IsAdmin)
            {
                MessageBox.Show("当前权限不足");
                return;
            }
            bool result = false;
            if (s == "喷油开启")
            {
                result= SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "触发喷油_1", true);
                Content = "喷油关闭";
                SetHelper.ListPLCMessage.ShowInfoQueue($"触发喷油写true,{(result ? "成功" : "失败")}");

            }
            else
            {
                result=SetHelper.siemens.WriteItem(PLCGroupName.WriteGroup, "触发喷油_1", false);
                Content = "喷油开启";
                SetHelper.ListPLCMessage.ShowInfoQueue($"触发喷油写false,{(result ? "成功" : "失败")}");

            }

        });

        private void ShowMessage()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    PlcHeart = PlcHeartStatic;
                    ProductType = SetHelper.NowProduct?.ProductName;

                    if (DataManager.RepairStation==2)
                    {
                        RepairStatus = DataManager.specialRepairFlag == 5 ? "返修状态:打散" : (DataManager.specialRepairFlag == 6 ? "返修状态:合装" : "返修关闭");
                    }
                    else if(DataManager.RepairStation==1) 
                    {
                        RepairStatus= DataManager.RepairFlag == true ? "返修开启" : "返修关闭";
                    }
                    
                    RepairStatusColor = DataManager.specialRepairFlag > 1 ? Brushes.Red : Brushes.Green;
                    RepairStationVisibility = DataManager.RepairStation > 0 ? Visibility.Visible : Visibility.Collapsed;
                    if (Application.Current != null)
                    {
                        Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            MessageModel message = new MessageModel();
                            if (SetHelper.ListPLCMessage.TryDequeue(out message))
                            {
                                ListPLCMessage.Insert(0, message);
                                if (ListPLCMessage.Count() > 1000)
                                {
                                    ListPLCMessage.RemoveAt(ListPLCMessage.Count() - 1);
                                }
                            }
                            message = new MessageModel();
                            if (SetHelper.ListMesMessage.TryDequeue(out message))
                            {
                                ListMesMessage.Insert(0, message);
                                if (ListMesMessage.Count() > 800)
                                {
                                    ListMesMessage.RemoveAt(ListMesMessage.Count() - 1);
                                }
                            }
                            message = new MessageModel();
                            if (SetHelper.ListScanMessage.TryDequeue(out message))
                            {
                                ListScanMessage.Insert(0, message);
                                if (ListScanMessage.Count() > 500)
                                {
                                    ListScanMessage.RemoveAt(ListScanMessage.Count() - 1);
                                }
                            }
                            message = new MessageModel();
                            if (SetHelper.ListOEEMessage.TryDequeue(out message))
                            {
                                ListOEEMessage.Insert(0, message);
                                if (ListOEEMessage.Count() > 200)
                                {
                                    ListOEEMessage.RemoveAt(ListOEEMessage.Count() - 1);
                                }
                            }
                            message = new MessageModel();
                            if (SetHelper.ListOtherMessage.TryDequeue(out message))
                            {
                                ListOtherMessage.Insert(0, message);
                                if (ListOtherMessage.Count() > 100)
                                {
                                    ListOtherMessage.RemoveAt(ListOtherMessage.Count() - 1);
                                }
                            }
                        }));
                        SetHelper.dataManager.DeleteLocalLog();

                        ResultModel = SetHelper.resultModel?.ToList();
                        Opid = SetHelper.Opid;
                        //将工号实时更新
                        //if (Opid != null)
                        //{
                        //    OPID[] opids = new OPID[8];//留8个
                        //    for (int i = 0; i < Opid.Length; i++)
                        //    {
                        //        opids[i] = new OPID() { Id = SetHelper.Opid[i].Id };
                        //    }
                        //    for (int i = 0; i < Opid.Length; i++)
                        //    {
                        //        if (oldOpid[i].Id != Opid[i].Id)
                        //        {
                        //            opids[i] = new OPID() { Id = SetHelper.Opid[i].Id };
                        //            string opidJson = JSON.ToJsonFormat(opids);
                        //            File.WriteAllText(SetHelper.opidPath, opidJson);
                        //        }
                        //        oldOpid[i].Id = Opid[i].Id;
                        //    }
                        //}

                        if (SetHelper.MesSetting.ListGroup.Count > 0 && SetHelper.MesSetting.ListGroup[0]?.IsMauaStation == "1")
                        {
                            object time = 0;
                            SetHelper.siemens.ReadItem(PLCGroupName.ReadGroup, "按钮时间_1", ref time);

                            if (time.ObjToFloat() != 0)
                            {
                                ButtonTime = $"放行按钮按下时间：{time}s";
                            }
                            else
                            {
                                ButtonTime = "";
                            }
                        }

                        if (SetHelper.IsAdmin)
                        {
                            IsSpoutOil = Visibility.Visible;
                        }
                        else
                        {
                            IsSpoutOil = Visibility.Collapsed;
                        }
                    }

                    Thread.Sleep(100);
                }
            });
        }

    }



}
