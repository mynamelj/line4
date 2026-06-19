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
    /// <summary>
    /// 弃用
    /// </summary>
    [AddINotifyPropertyChangedInterface]
    public class PopupViewModel
    {
        public PopupViewModel()
        {
            //for (int i = 0; i < SetHelper.MesSetting.ListGroup[0].MaterialCount; i++)
            //{
            //    MaterialCodeList.Add(new MaterialInfo() { MaterialName = "材料" + (i + 1) });
            //}
        }
        // public double Height { get; set; } = 50;
        public ObservableCollection<MaterialInfo> MaterialCodeList { get; set; } = new ObservableCollection<MaterialInfo>();
        public bool isRun = true;

        public void GetMaterialCode()
        {
            //int index = 0;
            //Task.Run(() =>
            //{
            //    while (isRun)
            //    {
            //        Thread.Sleep(100);
            //        if (!string.IsNullOrEmpty(SetHelper.NowMaterialCode))
            //        {
            //            if (Application.Current != null)
            //            {
            //                Application.Current.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
            //                {
            //                    MaterialCodeList[index].MaterialCode = SetHelper.NowMaterialCode;

            //                    index++;
            //                    SetHelper.NowMaterialCode = "";
            //                    if (index == SetHelper.MesSetting.ListGroup[0].MaterialCount)
            //                    {
            //                        //SetHelper.MaterialCodeList = MaterialCodeList.Select(x => x.MaterialCode).ToList();
            //                        isRun = false;
            //                        _window.Close();
            //                    }
            //                }));
            //            }
            //        }
            //    }
            //});
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
            //SetHelper.NowMaterialCode = "111";
        });

    }

}
