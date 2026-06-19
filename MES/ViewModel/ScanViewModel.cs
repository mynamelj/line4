using MES.Comm;
using MES.Manager;
using MES.SetModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Xml.Linq;

namespace MES.ViewModel
{
    public class ScanViewModel : ProductChangeBase
    {
        public ScanViewModel()
        {
            ScanSetting = new ObservableCollection<ScanSettingModel>(SetHelper.ScanSetting);
        }



        public override void GetParams(ProductTypeModel product)
        {
            ScanSetting = SetHelper.LoadConfig<ObservableCollection<ScanSettingModel>>(SetHelper.scan, product);
        }

        public ObservableCollection<ScanSettingModel> ScanSetting { get; set; } = new ObservableCollection<ScanSettingModel>();

        public ICommand SaveCommand => new RelayCommand(() =>
        {
            SetHelper.SaveConfig(ScanSetting, SetHelper.scan, ProductType);
        });

        public ICommand AddCommand => new RelayCommand(() =>
        {
            if (SetHelper.IsAdmin)
            {
                ScanSetting.Add(new ScanSettingModel());
            }
            else
            {
                MessageBox.Show("权限不足");
            }
        });

        public ICommand DeleteCommand => new RelayCommand<ScanSettingModel>((scan) =>
        {
            if (SetHelper.IsAdmin)
            {
                ScanSetting.Remove(scan);
            }
            else
            {
                MessageBox.Show("权限不足");
            }
        });
    }
}
