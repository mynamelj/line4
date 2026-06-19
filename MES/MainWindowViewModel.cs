using MES.Comm;
using MES.Manager;
using MES.MesModel.Request;
using MES.SetModel;
using MES.View;
using MES.ViewModel;
using Newtonsoft.Json;
using Polly.Utilities;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Threading;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace MES
{
    [AddINotifyPropertyChangedInterface]
    public class MainWindowViewModel
    {
        public MainWindowViewModel()
        {
            Initial();
        }

        public ICommand Loaded => new RelayCommand(() =>
        {
        });

        public async void Initial()
        {
            SetHelper.InitializedSetting();

            ClearUploadState();
        }

        private void ClearUploadState()
        {
            var x = Task.Run(() =>
            {
                while (true)
                {
                    //if (CheckInOrOut)
                    //{
                    //    SetHelper.IsAdmin = false;
                    //    Msg = "用户未登录";
                    //    SetHelper.NowUser = "";
                    //    CheckInOrOut = false;
                    //}
                    if (SetHelper.IsAdmin)
                    {
                        object obj = new object();
                        bool result = SetHelper.siemens.ReadItem(SetModel.PLCGroupName.WriteGroup, "操作权限_1", ref obj);
                        if (obj.ObjToBool() == false && result)
                        {
                            SetHelper.IsAdmin = false;
                            Msg = "用户未登录";
                            SetHelper.NowUser = "";
                            CheckInOrOut = false;
                        }
                    }
                    Thread.Sleep(100);
                }
            });
        }

        public WindowState WindowState { get; set; } = WindowState.Normal;
        public string Msg { get; set; } = "用户未登录";

        /// <summary>
        /// 校验是否进出站
        /// </summary>
        public static bool CheckInOrOut = false;

        private DispatcherTimer timer;

        public ICommand LoginCommand => new RelayCommand(() =>
        {
            SetHelper.IsAdmin = true;
            SetHelper.NowUser = "MES";
            Msg = SetHelper.NowUser == "" ? "用户未登录" : $"当前用户：{SetHelper.NowUser}";

            // 同步将操作权限通知给 PLC
            if (SetHelper.siemens != null)
            {
                SetHelper.siemens.WriteItem(SetModel.PLCGroupName.WriteGroup, "操作权限_1", true);
            }
        });

        public ICommand QuitCommand => new RelayCommand(() =>
        {
            SetHelper.ListMesMessage.ShowInfoQueue($"用户{SetHelper.NowUser}已退出", false, "userLogin");

            SetHelper.IsAdmin = false;
            Msg = "用户未登录";
            SetHelper.NowUser = "";
            SetHelper.siemens.WriteItem(SetModel.PLCGroupName.WriteGroup, "操作权限_1", false);

            foreach (var item in SetHelper.StationNumber.numberGroups)
            {
                if (item.Name.Contains("OP5130"))//5130手动工位一带2
                {
                    SetHelper.siemens.WriteItem(SetModel.PLCGroupName.WriteGroup, "操作权限_2", false);
                }
            }
            ;
            System.Windows.Forms.MessageBox.Show("用户已退出");
        });
    }
}