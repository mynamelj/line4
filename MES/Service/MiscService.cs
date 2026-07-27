using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.IO;
using MES.Manager;

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

        [ObservableProperty]
        private Dictionary<string, List<string>> _sNPrefixDic = new Dictionary<string, List<string>>();

        private Dictionary<string, int> MapIndexDic { get; set; } = new Dictionary<string, int>();

        private HashSet<string> AmbiguousPrefixes { get; set; } =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public MiscService()
        {
            ReloadSettings();
        }

        public void ReloadSettings()
        {
            string miscFilePath = Path.Combine(basePath, "configs", "misc.json");


            if (File.Exists(miscFilePath))
            {
                try
                {
                    string jsonContent = System.IO.File.ReadAllText(miscFilePath);
                    var sNPrefixes = Newtonsoft.Json.JsonConvert.DeserializeObject<ObservableCollection<SNPrefix>>(jsonContent);
                    SNPrefixes = sNPrefixes ?? SNPrefixes;
                }
                catch (Exception ex)
                {
                    ShowScanMessage($"读取SN前缀配置失败:{ex.Message}");
                    throw;
                }
            }
            else
            {
                // 如果文件不存在，可以选择创建一个默认的配置文件
                Directory.CreateDirectory(Path.GetDirectoryName(miscFilePath));
                string defaultJsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(SNPrefixes, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(miscFilePath, defaultJsonContent);
            }

            MapSNPrefixDic();
        }


        public void MapSNPrefixDic()
        {
            var prefixDic = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var mapIndexDic = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in SNPrefixes ?? new ObservableCollection<SNPrefix>())
            {
                if (item == null || string.IsNullOrWhiteSpace(item.Name))
                {
                    ShowScanMessage("存在名称为空的SN前缀配置,已忽略");
                    continue;
                }

                if (!TryGetStationIndex(item.Name, out int stationIndex))
                {
                    ShowScanMessage($"SN前缀配置名称{item.Name}无法识别,已忽略");
                    continue;
                }

                mapIndexDic[item.Name] = stationIndex;
                prefixDic[item.Name] = (item.Value ?? string.Empty)
                    .Split(',')
                    .Select(prefix => prefix.Trim())
                    .Where(prefix => !string.IsNullOrEmpty(prefix))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }

            var ambiguousPrefixes = prefixDic
                .SelectMany(item => item.Value.Select(prefix => new
                {
                    Prefix = prefix,
                    StationIndex = mapIndexDic[item.Key]
                }))
                .GroupBy(item => item.Prefix, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Select(item => item.StationIndex).Distinct().Count() > 1)
                .Select(group => group.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var prefix in ambiguousPrefixes)
            {
                ShowScanMessage($"SN前缀{prefix}配置到多个工位,已禁用该前缀");
            }

            SNPrefixDic = prefixDic;
            MapIndexDic = mapIndexDic;
            AmbiguousPrefixes = ambiguousPrefixes;
        }


         [RelayCommand]
        public void SaveSettings()
        {
            string miscFilePath = Path.Combine(basePath, "configs", "misc.json");
            string jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(SNPrefixes, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(miscFilePath, jsonContent);
            MapSNPrefixDic();
        }
        //2030站规则:index为0的扫码枪扫压装件，index为1的扫码枪扫啮合件,其中压装站的index为1,3,啮合站的index为0,2
        //20302把枪扫四个站,20201把枪扫两个站,所以2020扫码枪的index一定为0,无需额外判断直接返回映射
        //2020站只扫差速器和输出轴,2030只扫中间轴和输入轴,所以可以通过SN前缀来判断是哪个站,然后根据index来返回对应的站号
        public int ResolveStationIndex(int hardIndex, string str)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                ShowScanMessage("扫码内容为空,无法匹配SN前缀");
                return -1;
            }

            if (hardIndex != 0 && hardIndex != 1)
            {
                ShowScanMessage($"扫码枪序号{hardIndex}无效,无法匹配SN前缀");
                return -1;
            }

            foreach (var item in SNPrefixDic
                .SelectMany(kvp => kvp.Value.Select(prefix => new { Name = kvp.Key, Prefix = prefix }))
                .OrderByDescending(item => item.Prefix.Length))
            {
                if (!str.StartsWith(item.Prefix, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (AmbiguousPrefixes.Contains(item.Prefix))
                {
                    ShowScanMessage($"扫码{str}匹配到重复SN前缀{item.Prefix},不触发进站");
                    return -1;
                }

                if (!MapIndexDic.TryGetValue(item.Name, out int stationIndex))
                {
                    ShowScanMessage($"SN前缀{item.Prefix}未配置工位映射,不触发进站");
                    return -1;
                }

                int resolvedIndex = hardIndex == 0 ? stationIndex : stationIndex - 1;
                ShowScanMessage($"扫码{str}匹配SN前缀{item.Prefix},解析到工位序号{resolvedIndex + 1}");
                return resolvedIndex;
            }

            ShowScanMessage($"扫码{str}未匹配到SN前缀,不触发进站");
            return -1;
        }

        private static bool TryGetStationIndex(string name, out int stationIndex)
        {
            if (name.IndexOf("inputShaft", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                stationIndex = 1;
                return true;
            }

            if (name.IndexOf("intermediateShaft", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                stationIndex = 3;
                return true;
            }

            if (name.IndexOf("outputShaft", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                stationIndex = 0;
                return true;
            }

            if (name.IndexOf("differential", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                stationIndex = 1;
                return true;
            }

            stationIndex = -1;
            return false;
        }

        private static void ShowScanMessage(string message)
        {
            SetHelper.ListScanMessage?.ShowInfoQueue(message);
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

