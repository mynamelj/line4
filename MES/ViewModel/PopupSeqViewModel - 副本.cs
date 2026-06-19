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
using System.Windows.Threading;

namespace MES.ViewModel
{
    [AddINotifyPropertyChangedInterface]
    public class PopupSeqViewModel
    {

        public PopupSeqViewModel()
        {
            MaterialCodeList.Add(new MaterialInfo() { MaterialName = "材料" + SetHelper.MaterialCount });
        }

        public ObservableCollection<MaterialInfo> MaterialCodeList { get; set; } = new ObservableCollection<MaterialInfo>();
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
                            if (SetHelper.IsRestart)//进站触发时关闭所有未关闭的弹窗
                            {
                                isRun = false;
                                _window.Close();
                                SetHelper.IsRestart = false;
                                SetHelper.ListScanMessage.ShowInfoQueue("弹窗关闭", false);
                            }
                            if (!string.IsNullOrEmpty(SetHelper.NowMaterialCode))
                            {

                                MaterialCodeList[0].MaterialCode = SetHelper.NowMaterialCode;
                                SetHelper.NowMaterialCode = "";
                                SetHelper.MaterialCodeList = MaterialCodeList.Select(x => x.MaterialCode).ToList();
                                isRun = false;
                                _window.Close();

                            }
                        }));
                    }
                    else
                    {
                        SetHelper.ListScanMessage.ShowInfoQueue("获取主线程失败", false);
                    }
                }
            });
        }

        private Window _window;
        private void WindowLoaded(object obj)
        {
            _window = obj as Window;
        }

        public ICommand WindowLoadedCmd => new RelayCommand<object>((obj) =>
        {
            WindowLoaded(obj);
            GetMaterialCode();
        });

        public ICommand AddCommand => new RelayCommand(() =>
        {
            SetHelper.NowMaterialCode = "111";
        });

    }
    [AddINotifyPropertyChangedInterface]
    public class MaterialInfo
    {
        public string MaterialName { get; set; } = "";
        public string MaterialCode { get; set; } = "";
    }
}
