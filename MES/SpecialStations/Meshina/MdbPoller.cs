using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace MES.SpecialStations.Meshina
{
    public sealed class MdbFile
    {
        public string Path { get; set; }
        public DateTime CreatedUtc { get; set; }
        public DateTime WrittenUtc { get; set; }
        public long Length { get; set; }
        public string Stamp => Length + ":" + WrittenUtc.Ticks;
    }

    public sealed class MdbPoller
    {
        private readonly string directory;
        private string lastPath;
        private string lastStamp;
        private int stableCount;

        public MdbPoller(string directory) { this.directory = directory; }

        public List<MdbFile> Snapshot()
        {
            // 目录不可访问时必须报错，不能把失败当成空目录建立基线。
            return new DirectoryInfo(directory).GetFiles("*.mdb", SearchOption.TopDirectoryOnly)
                .Select(f => new MdbFile { Path = f.FullName, CreatedUtc = f.CreationTimeUtc,
                    WrittenUtc = f.LastWriteTimeUtc, Length = f.Length }).ToList();
        }

        public List<MdbFile> Candidates(MeshinaJob job, IEnumerable<string> processed)
        {
            var excluded = new HashSet<string>(job.BaselineFiles.Concat(processed), StringComparer.OrdinalIgnoreCase);
            return Snapshot().Where(f => !excluded.Contains(f.Path) && f.CreatedUtc > job.ScanTimeUtc
                && HasCurrentMeasurementTime(f.Path, job.ScanTimeUtc)).ToList();
        }

        public static bool HasCurrentMeasurementTime(string path, DateTime scanUtc)
        {
            var match = Regex.Match(System.IO.Path.GetFileName(path), @"_(\d{8}_\d{6})\.mdb$", RegexOptions.IgnoreCase);
            if (!match.Success || !DateTime.TryParseExact(match.Groups[1].Value, "yyyyMMdd_HHmmss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime measured)) return false;
            // 文件名只有秒精度，允许扫码同一秒；新建时间仍要求严格晚于扫码时间。
            var localScan = scanUtc.ToLocalTime();
            return measured >= localScan.AddTicks(-(localScan.Ticks % TimeSpan.TicksPerSecond));
        }

        public bool IsStable(MdbFile file, int requiredCount)
        {
            if (file.Path == lastPath && file.Stamp == lastStamp) stableCount++;
            else { lastPath = file.Path; lastStamp = file.Stamp; stableCount = 1; }
            return file.Length > 0 && stableCount >= requiredCount;
        }

        public void Reset() { lastPath = null; lastStamp = null; stableCount = 0; }
    }
}
