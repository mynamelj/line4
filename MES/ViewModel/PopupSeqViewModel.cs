using MES.Manager;
using MES.MesModel.Response;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;
using System.Xml.Linq;

namespace MES.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class PopupSeqViewModel
    {

        public PopupSeqViewModel()
        {
            //MaterialCodeList = (new MaterialInfo() { MaterialName = "材料" + SetHelper.MaterialCount });

        }

        public MaterialInfo MaterialCodeList { get; set; } = new MaterialInfo();
        public bool isRun = true;
        public void GetMaterialCode()
        {
            Task.Run(() =>
            {
                while (isRun)
                {
                    Thread.Sleep(100);
                    if (Application.Current != null)
                    {
                        Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
                        {
                            if (SetHelper.IsRestart[number])//进站触发时关闭所有未关闭的弹窗
                            {
                                _window.Close();
                                SetHelper.ListScanMessage.ShowInfoQueue($"进站{number + 1} 触发，现存弹窗关闭", false);
                                isRun = false;
                            }

                            if (!string.IsNullOrEmpty(SetHelper.NowMaterialCode[number]))
                            {
                                SetHelper.ListScanMessage.ShowInfoQueue($"接收到条码{number + 1} ，弹窗关闭", false);
                                _window.Close();
                                isRun = false;
                            }
                        }));
                    }


                }
            });
        }

        private Window _window;
        string name;
        int number;
        string msg;
        private void WindowLoaded(object obj)
        {
            _window = obj as Window;
            MaterialCodeList = (new MaterialInfo() { MaterialName = name, MaterialMsg = msg });
        }

        public ICommand WindowLoadedCmd => new RelayCommand<object>((obj) =>
        {
            var popup = obj as MES.View.PopupSeqView;
            name = popup.Name;
            number = popup.Number;
            msg = popup.Msg;
            WindowLoaded(obj);
            GetMaterialCode();
        });

        public ICommand AddCommand => new RelayCommand(() =>
        {
            // SetHelper.NowMaterialCode = "111";
        });

    }
    [AddINotifyPropertyChangedInterface]
    public class MaterialInfo
    {
        public string MaterialName { get; set; } = "";
        public string MaterialCode { get; set; } = "";
        public string MaterialMsg { get; set; } = "";
    }
}
