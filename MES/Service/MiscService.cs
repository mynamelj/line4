using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;

namespace MES.Service
{
    public partial  class MiscService: ObservableObject,IMiscService
    {
        private string basePath = AppDomain.CurrentDomain.BaseDirectory;

        [ObservableProperty]
        private ObservableCollection<SNPrefix> _sNPrefixes = new ObservableCollection<SNPrefix>
        {
            new SNPrefix { Name = "XP2020outputShaftSNPrefix", Value = string.Empty },
            new SNPrefix { Name = "XP2020differentialSNPrefix", Value = string.Empty },
            new SNPrefix { Name = "QR2020outputShaftSNPrefix", Value = string.Empty },
            new SNPrefix { Name = "QR2020differentialSNPrefix", Value = string.Empty },
            new SNPrefix { Name = "XP2030inputShaftSNPrefix", Value = string.Empty },
            new SNPrefix { Name = "XP2030intermediateShaftSNPrefix", Value = string.Empty },
            new SNPrefix { Name = "QR2030inputShaftSNPrefix", Value = string.Empty },
            new SNPrefix { Name = "QR2030intermediateShaftSNPrefix", Value = string.Empty }
        };

        public MiscService()
        {
            string miscFilePath = Path.Combine(basePath, "configs", "misc.json");


            if (File.Exists(miscFilePath))
            {
                try
                {
                    string jsonContent = System.IO.File.ReadAllText(miscFilePath);
                    var sNPrefixes = Newtonsoft.Json.JsonConvert.DeserializeObject<ObservableCollection<SNPrefix>>(jsonContent);
                    SNPrefixes = sNPrefixes;
                }
                catch
                {
                    throw;
                }
            }
            else
            {
                // 如果文件不存在，可以选择创建一个默认的配置文件
                string defaultJsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(SNPrefixes, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(miscFilePath, defaultJsonContent);
            }
        }
        [RelayCommand]
        public void SaveSettings()
        {
            string miscFilePath = Path.Combine(basePath, "configs", "misc.json");
            string jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(SNPrefixes, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(miscFilePath, jsonContent);
        }
    }

    public partial class SNPrefix : ObservableObject
    {
        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string value;
    }
}

