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
            new SNPrefix { Name = "XP2030inputShaftSNPrefix", Value = string.Empty },
            new SNPrefix { Name = "XP2030intermediateShaftSNPrefix", Value = string.Empty },
            new SNPrefix { Name = "QR2030inputShaftSNPrefix", Value = string.Empty },
            new SNPrefix { Name = "QR2030intermediateShaftSNPrefix", Value = string.Empty }
        };

        [ObservableProperty]
        private Dictionary<string, List<string>> _sNPrefixDic = new Dictionary<string, List<string>>();

        private Dictionary<string, string> MapStationDic { get; set; } = new Dictionary<string, string>();

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
            var mapStationDic = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in SNPrefixes ?? new ObservableCollection<SNPrefix>())
            {
                if (item == null || string.IsNullOrWhiteSpace(item.Name))
                {
                    ShowScanMessage("存在名称为空的SN前缀配置,已忽略");
                    continue;
                }

                if (!TryGetStationName(item.Name, out string stationName))
                {
                    ShowScanMessage($"SN前缀配置名称{item.Name}无法识别,已忽略");
                    continue;
                }

                mapStationDic[item.Name] = stationName;
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
                    StationName = mapStationDic[item.Key]
                }))
                .GroupBy(item => item.Prefix, StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Select(item => item.StationName).Distinct().Count() > 1)
                .Select(group => group.Key)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var prefix in ambiguousPrefixes)
            {
                ShowScanMessage($"SN前缀{prefix}配置到多个工位,已禁用该前缀");
            }

            SNPrefixDic = prefixDic;
            MapStationDic = mapStationDic;
            AmbiguousPrefixes = ambiguousPrefixes;
        }


         [RelayCommand]
        public void SaveSettings()
        {
            string miscFilePath = Path.Combine(basePath, "configs", "misc.json");

            foreach (var item in SNPrefixes)
            {
                item.Value = (item.Value ?? string.Empty).Replace('，', ',');
            }

            string jsonContent = Newtonsoft.Json.JsonConvert.SerializeObject(SNPrefixes, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(miscFilePath, jsonContent);
            MapSNPrefixDic();
        }
        // 根据扫码枪序号与SN前缀映射到对应工位。
        // 啮合站已移出；两把枪均按SN前缀查找当前配置中的压装站，不再按枪号偏移工位索引。
        // misc.json中的XP2030/QR2030名称是既有配置键，保留以兼容现场已维护的前缀。
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

                if (!MapStationDic.TryGetValue(item.Name, out string stationName))
                {
                    ShowScanMessage($"SN前缀{item.Prefix}未配置工位映射,不触发进站");
                    return -1;
                }

                var stations = SetHelper.StationNumber.numberGroups;
                var matchingStations = stations.Where(station =>
                    string.Equals(station?.Name, stationName, StringComparison.OrdinalIgnoreCase)).ToList();
                if (matchingStations.Count != 1)
                {
                    ShowScanMessage($"SN前缀{item.Prefix}对应工站{stationName}在当前配置中不存在或不唯一,不触发进站");
                    return -1;
                }

                int resolvedIndex = stations.IndexOf(matchingStations[0]);
                ShowScanMessage($"扫码{str}匹配SN前缀{item.Prefix},解析到工位序号{resolvedIndex + 1}({stationName})");
                return resolvedIndex;
            }

            ShowScanMessage($"扫码{str}未匹配到SN前缀,不触发进站");
            return -1;
        }

        private static bool TryGetStationName(string name, out string stationName)
        {
            if (name.IndexOf("inputShaft", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                stationName = "OP2020PressFit1";
                return true;
            }

            if (name.IndexOf("intermediateShaft", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                stationName = "OP2020PressFit2";
                return true;
            }

            stationName = null;
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

