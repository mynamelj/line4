using System.IO;
using Newtonsoft.Json;

namespace MES.SpecialStations.Meshina
{
    public sealed class MeshinaSettings
    {
        public string DataDirectory { get; set; } = @"D:\盘齿轮双啮检测系统\结果\统计";
        public string Provider { get; set; } = "Microsoft.ACE.OLEDB.12.0";
        public int PollIntervalMilliseconds { get; set; } = 1000;
        public int StablePollCount { get; set; } = 3;
        public List<string> RequiredFields { get; set; } = new List<string> { "Fi", "fii", "Fr" };
        // MES项目名不一致时，在配置中填写 MDB字段名 -> MES项目名。
        public Dictionary<string, string> ItemNames { get; set; } = new Dictionary<string, string>();

        public static bool IsStationName(string name)
        {
            return IsUsbScannerStationName(name) || IsSerialScannerStationName(name);
        }

        public static bool IsUsbScannerStationName(string name)
        {
            string stationName = (name ?? "").Trim().ToUpperInvariant();
            return stationName.Contains("OP2020M") || stationName.Contains("2020MESHINA");
        }

        public static bool IsSerialScannerStationName(string name)
        {
            string stationName = (name ?? "").Trim().ToUpperInvariant();
            return stationName.Contains("2030MESHINA1") || stationName.Contains("2030MESHINA2");
        }

        public static MeshinaSettings Load(string path)
        {
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonConvert.SerializeObject(new MeshinaSettings(), Formatting.Indented));
            }
            return JsonConvert.DeserializeObject<MeshinaSettings>(File.ReadAllText(path));
        }
    }
}
