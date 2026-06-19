using MES.Comm;
using MES.Manager;
using MES.MesModel.Response;
using MES.SetModel;
using MES.View;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Threading;

namespace MES.ViewModel
{
    public class LightConfigViewModel : ProductChangeBase
    {
        public ObservableCollection<LightConfig> LightConfigList { get; set; } = new ObservableCollection<LightConfig>();

        public void LightConfig(List<LightConfig> configs)
        {
            //LightConfigList = new ObservableCollection<LightConfig>(configs);

            LightConfigList.Clear();
            foreach (var item in configs)
            {
                LightConfigList.Add(item);
            }
        }

        public LightConfigViewModel()
        {

        }


        public ICommand SaveLightConfigCmd => new RelayCommand(() =>
        {
            SetHelper.LightConfig.ListGroup = new ObservableCollection<LightConfig>(LightConfigList);
            //string json = JSON.ToJsonFormat(SetHelper.LightConfig.ListGroup);
            //File.WriteAllText(SetHelper.lightConfigPath, json);
            SetHelper.SaveConfig(SetHelper.LightConfig.ListGroup, SetHelper.lightConfig, ProductType);
            MessageBox.Show("保存成功", "提示", MessageBoxButton.OK);
        });
    }
}
