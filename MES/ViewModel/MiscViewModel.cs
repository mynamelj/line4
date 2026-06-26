using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MES.Service;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace MES.ViewModel
{
    public partial class MiscViewModel : ObservableObject
    {

        [ObservableProperty]
        private IMiscService _miscService;

        [ObservableProperty]
        private Dictionary<string, string> _prefixDic;
        public MiscViewModel(IMiscService miscService)
        {
            _miscService = miscService;
            _prefixDic = _miscService.SNPrefixes.ToDictionary(p => p.Name, p => p.Value);
        }


        [RelayCommand]
        private void SaveSettings()
        {
            MiscService.SNPrefixes.ToList().ForEach(item => item.Value = PrefixDic[item.Name]);

            MiscService.SaveSettings();
            MessageBox.Show("配置已保存！");
        }


    }
}

