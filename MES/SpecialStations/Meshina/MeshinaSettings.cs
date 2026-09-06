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
            name = (name ?? "").Trim();
            return string.Equals(name, "2020Meshina", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "OP2020Meshina", StringComparison.OrdinalIgnoreCase);
        }

        public static MeshinaSettings Load(string path)
        {
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, JsonConvert.SerializeObject(new MeshinaSettings(), Formatting.Indented));
            }
            var settings = JsonConvert.DeserializeObject<MeshinaSettings>(File.ReadAllText(path))
                ?? throw new InvalidDataException("啮合站配置为空");
            if (!Path.IsPathRooted(settings.DataDirectory) || string.IsNullOrWhiteSpace(settings.Provider)
                || settings.PollIntervalMilliseconds < 200 || settings.StablePollCount < 2
                || settings.ItemNames == null || settings.RequiredFields == null
                || settings.RequiredFields.Any(f => !MdbReader.NumericFields.Contains(f)))
                throw new InvalidDataException("啮合站目录必须是绝对路径，轮询间隔至少200ms，稳定次数至少2次");
            return settings;
        }
    }
}
